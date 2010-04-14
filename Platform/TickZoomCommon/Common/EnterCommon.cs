#region Copyright
/*
 * Software: TickZoom Trading Platform
 * Copyright 2009 M. Wayne Walter
 * 
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, see <http://www.tickzoom.org/wiki/Licenses>
 * or write to Free Software Foundation, Inc., 51 Franklin Street,
 * Fifth Floor, Boston, MA  02110-1301, USA.
 * 
 */
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;

using TickZoom.Api;

namespace TickZoom.Common
{

	public class EnterCommon : StrategySupport
	{
		public class InternalOrders {
			public LogicalOrder buyMarket;
			public LogicalOrder sellMarket;
			public LogicalOrder buyStop;
			public LogicalOrder sellStop;
			public LogicalOrder buyLimit;
			public LogicalOrder sellLimit;
		}
		private InternalOrders orders = new InternalOrders();
		
		private bool enableWrongSideOrders = false;
		private bool allowReversal = true;
		private bool isNextBar = false;
		private int lotSize;
		
		public EnterCommon(Strategy strategy) : base(strategy) {
		}
		
		public void OnInitialize()
		{
			if( IsDebug) Log.Debug("OnInitialize()");
			lotSize = Strategy.Data.SymbolInfo.Level2LotSize;
			Strategy.Drawing.Color = Color.Black;
			orders.buyMarket = Factory.Engine.LogicalOrder(Strategy.Data.SymbolInfo,Strategy);
			orders.buyMarket.Type = OrderType.BuyMarket;
			orders.sellMarket = Factory.Engine.LogicalOrder(Strategy.Data.SymbolInfo,Strategy);
			orders.sellMarket.Type = OrderType.SellMarket;
			orders.buyStop = Factory.Engine.LogicalOrder(Strategy.Data.SymbolInfo,Strategy);
			orders.buyStop.Type = OrderType.BuyStop;
			orders.sellStop = Factory.Engine.LogicalOrder(Strategy.Data.SymbolInfo,Strategy);
			orders.sellStop.Type = OrderType.SellStop;
			orders.buyLimit = Factory.Engine.LogicalOrder(Strategy.Data.SymbolInfo,Strategy);
			orders.buyLimit.Type = OrderType.BuyLimit;
			orders.sellLimit = Factory.Engine.LogicalOrder(Strategy.Data.SymbolInfo,Strategy);
			orders.sellLimit.Type = OrderType.SellLimit;
			Strategy.AddOrder( orders.buyMarket);
			Strategy.AddOrder( orders.sellMarket);
			Strategy.AddOrder( orders.buyStop);
			Strategy.AddOrder( orders.sellStop);
			Strategy.AddOrder( orders.buyLimit);
			Strategy.AddOrder( orders.sellLimit);
		}
		
        public void CancelOrders()
        {
        	orders.buyMarket.IsActive = false;
            orders.sellMarket.IsActive = false;
        	orders.buyStop.IsActive = false;
            orders.sellStop.IsActive = false;
            orders.buyLimit.IsActive = false;
            orders.sellLimit.IsActive = false;
            
        	orders.buyMarket.IsNextBar = false;
            orders.sellMarket.IsNextBar = false;
        	orders.buyStop.IsNextBar = false;
            orders.sellStop.IsNextBar = false;
            orders.buyLimit.IsNextBar = false;
            orders.sellLimit.IsNextBar = false;
        }
		
		private void LogEntry(string description) {
			if( Strategy.Chart.IsDynamicUpdate) {
        		if( IsNotice) Log.Notice("Bar="+Strategy.Chart.DisplayBars.CurrentBar+", " + description);
			} else {
        		if( IsDebug) Log.Debug("Bar="+Strategy.Chart.DisplayBars.CurrentBar+", " + description);
			}
		}
		
        #region Properties		
        public void SellMarket() {
        	SellMarket(1);
        }
        
        public void SellMarket( double lots) {
        	if( Strategy.Position.IsShort) {
        		string reversal = allowReversal ? "or long " : "";
        		string reversalEnd = allowReversal ? " since AllowReversal is true" : "";
        		throw new TickZoomException("Strategy must be flat "+reversal+"before a sell market entry"+reversalEnd+".");
        	}
        	if( !allowReversal && Strategy.Position.IsLong) {
        		throw new TickZoomException("Strategy cannot enter sell market when position is short. Set AllowReversal to true to allow this.");
        	}
        	if( !allowReversal && Strategy.Position.HasPosition) {
        		throw new TickZoomException("Strategy must be flat before a short market entry.");
        	}
        	/// <summary>
        	/// comment.
        	/// </summary>
        	/// <param name="allowReversal"></param>
        	orders.sellMarket.Price = 0;
        	orders.sellMarket.Positions = lots * lotSize;
        	if( isNextBar) {
	        	orders.sellMarket.IsNextBar = true;
        	} else {
        		orders.sellMarket.IsActive = true;
        	}
        }
        
        [Obsolete("AllowReversals = true is now default until reverse order types.",true)]
        public void SellMarket(bool allowReversal) {
        }
        
        [Obsolete("AllowReversals = true is now default until reverse order types.",true)]
        public void SellMarket( double positions, bool allowReversal) {
		}
        
        public void BuyMarket() {
        	BuyMarket( 1);
        }
        
        public void BuyMarket(double lots) {
        	if( Strategy.Position.IsLong) {
        		string reversal = allowReversal ? "or short " : "";
        		string reversalEnd = allowReversal ? " since AllowReversal is true" : "";
        		throw new TickZoomException("Strategy must be flat "+reversal+"before a long market entry"+reversalEnd+".");
        	}
        	if( !allowReversal && Strategy.Position.IsShort) {
        		throw new TickZoomException("Strategy cannot enter long market when position is short. Set AllowReversal to true to allow this.");
        	}
        	orders.buyMarket.Price = 0;
        	orders.buyMarket.Positions = lots * lotSize;
        	if( isNextBar) {
	        	orders.buyMarket.IsNextBar = true;
        	} else {
        		orders.buyMarket.IsActive = true;
        	}
        }
        
        [Obsolete("AllowReversals = true is now default until reverse order types.",true)]
        public void BuyMarket(bool allowReversal) {
        }
        
        [Obsolete("AllowReversals = true is now default until reverse order types.",true)]
        public void BuyMarket( double positions, bool allowReversal) {
		}
        
        public void BuyLimit( double price) {
        	BuyLimit( price, 1);
        }
        	
        /// <summary>
        /// Create a active buy limit order.
        /// </summary>
        /// <param name="price">Order price.</param>
        /// <param name="positions">Number of positions as in 1, 2, 3, etc. To set the size of a single position, 
        ///  use PositionSize.Size.</param>

        public void BuyLimit( double price, double lots) {
        	if( Strategy.Position.HasPosition) {
        		throw new TickZoomException("Strategy must be flat before setting a long limit entry.");
        	}
        	orders.buyLimit.Price = price;
        	orders.buyLimit.Positions = lots * lotSize;
        	if( isNextBar) {
	        	orders.buyLimit.IsNextBar = true;
        	} else {
        		orders.buyLimit.IsActive = true;
        	}
		}
        
        public void SellLimit( double price) {
        	SellLimit( price, 1);
        }
        	
        /// <summary>
        /// Create a active sell limit order.
        /// </summary>
        /// <param name="price">Order price.</param>
        /// <param name="positions">Number of positions as in 1, 2, 3, etc. To set the size of a single position, 
        ///  use PositionSize.Size.</param>

        public void SellLimit( double price, double lots) {
        	if( Strategy.Position.HasPosition) {
        		throw new TickZoomException("Strategy must be flat before setting a short limit entry.");
        	}
        	orders.sellLimit.Price = price;
        	orders.sellLimit.Positions = lots * lotSize;
        	if( isNextBar) {
	        	orders.sellLimit.IsNextBar = true;
        	} else {
        		orders.sellLimit.IsActive = true;
        	}
		}
        
        public void BuyStop( double price) {
        	BuyStop( price, 1);
        }
        
        /// <summary>
        /// Create a active buy stop order.
        /// </summary>
        /// <param name="price">Order price.</param>
        /// <param name="positions">Number of positions as in 1, 2, 3, etc. To set the size of a single position, 
        ///  use PositionSize.Size.</param>

        public void BuyStop( double price, double lots) {
        	if( Strategy.Position.HasPosition) {
        		throw new TickZoomException("Strategy must be flat before setting a long stop entry.");
        	}
        	orders.buyStop.Price = price;
        	orders.buyStop.Positions = lots * lotSize;
        	if( isNextBar) {
	        	orders.buyStop.IsNextBar = true;
        	} else {
        		orders.buyStop.IsActive = true;
        	}
		}
	
        public void SellStop( double price) {
        	SellStop( price, 1);
        }
        
        /// <summary>
        /// Create a active sell stop order.
        /// </summary>
        /// <param name="price">Order price.</param>
        /// <param name="positions">Number of positions as in 1, 2, 3, etc. To set the size of a single position, 
        ///  use PositionSize.Size.</param>
        
        public void SellStop( double price, double lots) {
        	if( Strategy.Position.HasPosition) {
        		throw new TickZoomException("Strategy must be flat before setting a short stop entry.");
        	}
        	orders.sellStop.Price = price;
        	orders.sellStop.Positions = lots * lotSize;
        	if( isNextBar) {
	        	orders.sellStop.IsNextBar = true;
        	} else {
        		orders.sellStop.IsActive = true;
        	}
        }
        
		#endregion
	
		public override string ToString()
		{
			return Strategy.FullName;
		}
		
		public bool EnableWrongSideOrders {
			get { return enableWrongSideOrders; }
			set { enableWrongSideOrders = value; }
		}

		public bool HasBuyOrder {
			get {
				return orders.buyStop.IsActive || orders.buyStop.IsNextBar || 
					orders.buyLimit.IsActive || orders.buyLimit.IsNextBar ||
					orders.buyMarket.IsActive || orders.buyMarket.IsNextBar;
			}
		}
		
		public bool HasSellOrder {
			get {
				return orders.sellStop.IsActive || orders.sellStop.IsNextBar || 
					orders.sellLimit.IsActive || orders.sellLimit.IsNextBar || 
					orders.sellMarket.IsActive || orders.sellMarket.IsNextBar;
			}
		}
		
		internal InternalOrders Orders {
			get { return orders; }
			set { orders = value; }
		}
		
		internal bool IsNextBar {
			get { return isNextBar; }
			set { isNextBar = value; }
		}
	}
}
