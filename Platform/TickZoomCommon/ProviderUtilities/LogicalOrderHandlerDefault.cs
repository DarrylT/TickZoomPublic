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

namespace TickZoom.Common
{
	public class LogicalOrderHandlerDefault : LogicalOrderHandler {
		SymbolInfo symbol;
		PhysicalOrderHandler brokerOrders;
		List<PhysicalOrderDefault> physicalOrders;
		IList<LogicalOrder> originalLogicals;
		List<LogicalOrder> logicalOrders;
		List<LogicalOrder> extraLogicals = new List<LogicalOrder>();
		double actualPosition;
		double desiredPosition;
		
		public LogicalOrderHandlerDefault(SymbolInfo symbol, PhysicalOrderHandler brokerOrders) {
			this.symbol = symbol;
			this.brokerOrders = brokerOrders;
			this.physicalOrders = new List<PhysicalOrderDefault>();
			this.logicalOrders = new List<LogicalOrder>();
		}
		
		public void ClearPhysicalOrders() {
			physicalOrders.Clear();
		}
		
		public void SetActualPosition(double position) {
			this.actualPosition = position;
		}
		public void AddPhysicalOrder( OrderType type, double price, int size, int logicalOrderId, object brokerOrder) {
			physicalOrders.Add( new PhysicalOrderDefault(symbol,type,price,size,logicalOrderId,brokerOrder));
		}

		private bool TryMatchId( LogicalOrder logical, out PhysicalOrderDefault physicalOrder) {
			foreach( var physical in physicalOrders) {
				if( logical.Id == physical.LogicalOrderId) {
					physicalOrder = physical;
					return true;
				}
			}
			physicalOrder = default(PhysicalOrderDefault);
			return false;
		}
		
		private bool TryMatchTypeOnly( LogicalOrder logical, out PhysicalOrderDefault physicalOrder) {
			double difference = logical.Positions - Math.Abs(actualPosition);
			foreach( var physical in physicalOrders) {
				if( logical.Type == physical.Type) {
					if( logical.TradeDirection == TradeDirection.Entry) {
						if( difference != 0) {
							physicalOrder = physical;
							return true;
						}
					}
					if( logical.TradeDirection == TradeDirection.Exit) {
						if( actualPosition != 0) {
							physicalOrder = physical;
							return true;
						}
					}
				}
			}
			physicalOrder = default(PhysicalOrderDefault);
			return false;
		}
		
		private void ProcessMatchPhysicalEntry( LogicalOrder logical, PhysicalOrderDefault physical) {
			double difference = logical.Positions - Math.Abs(actualPosition);
			if( difference == 0) {
				brokerOrders.OnCancelBrokerOrder(physical);
			} else if( difference != physical.Size) {
				if( actualPosition == 0) {
					physicalOrders.Remove(physical);
					physical = new PhysicalOrderDefault(symbol,logical,difference,physical.BrokerOrder);
					brokerOrders.OnChangeBrokerOrder(physical);
				} else {
					if( actualPosition > 0) {
						if( logical.Type == OrderType.BuyStop || logical.Type == OrderType.BuyLimit) {
							physicalOrders.Remove(physical);
							physical = new PhysicalOrderDefault(symbol,logical,difference,physical.BrokerOrder);
							brokerOrders.OnChangeBrokerOrder(physical);
						} else {
							brokerOrders.OnCancelBrokerOrder(physical);
						}
					}
					if( actualPosition < 0) {
						if( logical.Type == OrderType.SellStop || logical.Type == OrderType.SellLimit) {
							physicalOrders.Remove(physical);
							physical = new PhysicalOrderDefault(symbol,logical,difference,physical.BrokerOrder);
							brokerOrders.OnChangeBrokerOrder(physical);
						} else {
							brokerOrders.OnCancelBrokerOrder(physical);
						}
					}
				}
			}
		}
		
		private void ProcessMatchPhysicalExit( LogicalOrder logical, PhysicalOrderDefault physical) {
			if( actualPosition == 0) {
				brokerOrders.OnCancelBrokerOrder(physical);
			} else if( Math.Abs(actualPosition) != physical.Size || logical.Price != physical.Price) {
				physicalOrders.Remove(physical);
				physical = new PhysicalOrderDefault(symbol,logical,Math.Abs(actualPosition),physical.BrokerOrder);
				brokerOrders.OnChangeBrokerOrder(physical);
			}
		}
		
		private void ProcessMatch(LogicalOrder logical, PhysicalOrderDefault physical) {
			if( logical.TradeDirection == TradeDirection.Entry) {
				ProcessMatchPhysicalEntry( logical, physical);
			}
			if( logical.TradeDirection == TradeDirection.Exit) {
				ProcessMatchPhysicalExit( logical, physical);
			}
		}
		
		private void ProcessMissingPhysical(LogicalOrder logical) {
			// When flat, allow entry orders.
			if( actualPosition == 0) {
				if( logical.TradeDirection == TradeDirection.Entry) {
					PhysicalOrder physical = new PhysicalOrderDefault(symbol,logical,logical.Positions,null);
					brokerOrders.OnCreateBrokerOrder(physical);
				}
			} else {
				if( logical.TradeDirection == TradeDirection.Exit) {
					ProcessMissingPhysicalExit( logical);
				}
			}
		}
		
		private void ProcessMissingPhysicalExit(LogicalOrder logical) {
			if( actualPosition > 0 ) {
				if( logical.Type == OrderType.SellLimit ||
				  logical.Type == OrderType.SellStop) {
					PhysicalOrder physical = new PhysicalOrderDefault(symbol,logical,actualPosition,null);
					brokerOrders.OnCreateBrokerOrder(physical);
				}
			}
			if( actualPosition < 0 ) {
				if( logical.Type == OrderType.BuyLimit ||
				  logical.Type == OrderType.BuyStop) {
					PhysicalOrder physical = new PhysicalOrderDefault(symbol,logical,Math.Abs(actualPosition),null);
					brokerOrders.OnCreateBrokerOrder(physical);
				}
			}
		}
		
		private void ProcessMissingLogical(PhysicalOrder physical) {
			brokerOrders.OnCancelBrokerOrder( physical);
		}
		
		private void ComparePosition() {
			double delta = desiredPosition - actualPosition;
			PhysicalOrder physical;
			if( delta > 0) {
				physical = new PhysicalOrderDefault(symbol,OrderType.BuyMarket,0,delta,0,null);
				brokerOrders.OnCreateBrokerOrder(physical);
			}
			if( delta < 0) {
				physical = new PhysicalOrderDefault(symbol,OrderType.SellMarket,0,Math.Abs(delta),0,null);
				brokerOrders.OnCreateBrokerOrder(physical);
			}
			actualPosition = desiredPosition;
		}
		
		public void SetLogicalOrders( IList<LogicalOrder> originalLogicals) {
			this.originalLogicals = originalLogicals;
		}
		
		public void SetDesiredPosition(	double position) {
			this.desiredPosition = position;
		}
		
		public void PerformCompare() {
			ComparePosition(); // First synchronize the position.
			
			// Now synchronize the orders.
			logicalOrders.Clear();
			if(originalLogicals != null) {
				logicalOrders.AddRange(originalLogicals);
			}
			PhysicalOrderDefault physical;
			extraLogicals.Clear();
			while( logicalOrders.Count > 0) {
				var logical = logicalOrders[0];
				if( TryMatchId(logical, out physical)) {
					ProcessMatch(logical,physical);
					physicalOrders.Remove(physical);
				} else {
					extraLogicals.Add(logical);
				}
				logicalOrders.Remove(logical);
			}
			while( extraLogicals.Count > 0) {
				var logical = extraLogicals[0];
				ProcessMissingPhysical(logical);
				extraLogicals.Remove(logical);
			}
			while( physicalOrders.Count > 0) {
				physical = physicalOrders[0];
				ProcessMissingLogical(physical);
				physicalOrders.Remove(physical);
			}
		}
	}
}
