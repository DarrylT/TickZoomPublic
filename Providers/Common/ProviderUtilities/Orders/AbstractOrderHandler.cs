#region Copyright
/*
 * Copyright 2008 M. Wayne Walter
 * Software: TickZoom Trading Platform
 * User: Wayne Walter
 * 
 * You can use and modify this software under the terms of the
 * TickZOOM General Public License Version 1.0 or (at your option)
 * any later version.
 * 
 * Businesses are restricted to 30 days of use.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * TickZOOM General Public License for more details.
 *
 * You should have received a copy of the TickZOOM General Public
 * License along with this program.  If not, see
 * 
 * 
 *
 * User: Wayne Walter
 * Date: 8/7/2009
 * Time: 7:29 PM
 * <http://www.tickzoom.org/wiki/Licenses>.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Text;
using System.Threading;

using TickZoom.Api;

namespace TickZoom.ProviderUtilities
{
	public abstract class AbstractOrderHandler {
		List<PhysicalOrder> physicalOrders;
		List<LogicalOrder> extraLogicals = new List<LogicalOrder>();
		double position;
		public AbstractOrderHandler() {
			physicalOrders = new List<PhysicalOrder>();
		}
		
		public virtual void Clear() {
			physicalOrders.Clear();
		}
		
		public void AddPhysicalOrder( OrderType type, double price, int size, object brokerOrder) {
			physicalOrders.Add( new PhysicalOrder(type,price,size,brokerOrder));
		}

		private bool TryMatchTypePrice( LogicalOrder logical, out PhysicalOrder physicalOrder) {
			foreach( var physical in physicalOrders) {
				if( logical.Type == physical.Type &&
				   logical.Price == physical.Price) {
					physicalOrder = physical;
					return true;
				}
			}
			physicalOrder = default(PhysicalOrder);
			return false;
		}
		
		private bool TryMatchTypeOnly( LogicalOrder logical, out PhysicalOrder physicalOrder) {
			double difference = logical.Positions - Math.Abs(position);
			foreach( var physical in physicalOrders) {
				if( logical.Type == physical.Type) {
					if( logical.TradeDirection == TradeDirection.Entry) {
						if( difference != 0) {
							physicalOrder = physical;
							return true;
						}
					}
					if( logical.TradeDirection == TradeDirection.Exit) {
						if( position != 0) {
							physicalOrder = physical;
							return true;
						}
					}
				}
			}
			physicalOrder = default(PhysicalOrder);
			return false;
		}
		
		private void ProcessMatchPhysicalEntry( LogicalOrder logical, PhysicalOrder physical) {
			double difference = logical.Positions - Math.Abs(position);
			if( difference == 0) {
				CancelBrokerOrder(physical);
			} else if( difference != physical.Size) {
				if( position == 0) {
					physicalOrders.Remove(physical);
					physical = new PhysicalOrder(logical,difference,physical.BrokerOrder);
					ChangeBrokerOrder(physical);
				} else {
					if( position > 0) {
						if( logical.Type == OrderType.BuyStop || logical.Type == OrderType.BuyLimit) {
							physicalOrders.Remove(physical);
							physical = new PhysicalOrder(logical,difference,physical.BrokerOrder);
							ChangeBrokerOrder(physical);
						} else {
							CancelBrokerOrder(physical);
						}
					}
					if( position < 0) {
						if( logical.Type == OrderType.SellStop || logical.Type == OrderType.SellLimit) {
							physicalOrders.Remove(physical);
							physical = new PhysicalOrder(logical,difference,physical.BrokerOrder);
							ChangeBrokerOrder(physical);
						} else {
							CancelBrokerOrder(physical);
						}
					}
				}
			}
		}
		
		private void ProcessMatchPhysicalExit( LogicalOrder logical, PhysicalOrder physical) {
			if( position == 0) {
				CancelBrokerOrder(physical);
			} else if( Math.Abs(position) != physical.Size || logical.Price != physical.Price) {
				physicalOrders.Remove(physical);
				physical = new PhysicalOrder(logical,Math.Abs(position),physical.BrokerOrder);
				ChangeBrokerOrder(physical);
			}
		}
		
		private void ProcessMatch(LogicalOrder logical, PhysicalOrder physical) {
			if( logical.TradeDirection == TradeDirection.Entry) {
				ProcessMatchPhysicalEntry( logical, physical);
			}
			if( logical.TradeDirection == TradeDirection.Exit) {
				ProcessMatchPhysicalExit( logical, physical);
			}
		}
		
		private void ProcessMissingPhysical(LogicalOrder logical) {
			// When flat, allow entry orders.
			if( position == 0) {
				if( logical.TradeDirection == TradeDirection.Entry) {
					PhysicalOrder physical = new PhysicalOrder(logical,logical.Positions,null);
					CreateBrokerOrder(physical);
				}
			} else {
				if( logical.TradeDirection == TradeDirection.Exit) {
					ProcessMissingPhysicalExit( logical);
				}
			}
		}
		
		private void ProcessMissingPhysicalExit(LogicalOrder logical) {
			if( position > 0 ) {
				if( logical.Type == OrderType.SellLimit ||
				  logical.Type == OrderType.SellStop) {
					PhysicalOrder physical = new PhysicalOrder(logical,position,null);
					CreateBrokerOrder(physical);
				}
			}
			if( position < 0 ) {
				if( logical.Type == OrderType.BuyLimit ||
				  logical.Type == OrderType.BuyStop) {
					PhysicalOrder physical = new PhysicalOrder(logical,Math.Abs(position),null);
					CreateBrokerOrder(physical);
				}
			}
		}
		
		private void ProcessMissingLogical(PhysicalOrder physical) {
			CancelBrokerOrder( physical);
		}
		
		public void PerformCompare(double position, IList<LogicalOrder> logicalOrders) {
			this.position = position;
			PhysicalOrder physical;
			extraLogicals.Clear();
			while( logicalOrders.Count > 0) {
				var logical = logicalOrders[0];
				if( TryMatchTypePrice(logical, out physical)) {
					ProcessMatch(logical,physical);
					physicalOrders.Remove(physical);
				} else {
					extraLogicals.Add(logical);
				}
				logicalOrders.Remove(logical);
			}
			while( extraLogicals.Count > 0) {
				var logical = extraLogicals[0];
				if( TryMatchTypeOnly(logical, out physical)) {
					ProcessMatch(logical,physical);
					physicalOrders.Remove(physical);
				} else {
					ProcessMissingPhysical(logical);
				}
				extraLogicals.Remove(logical);
			}
			while( physicalOrders.Count > 0) {
				physical = physicalOrders[0];
				ProcessMissingLogical(physical);
				physicalOrders.Remove(physical);
			}
		}
		
		public abstract void ChangeBrokerOrder(PhysicalOrder order);
		
		public abstract void CreateBrokerOrder(PhysicalOrder order);
		
		public abstract void CancelBrokerOrder(PhysicalOrder order);
	}
}
