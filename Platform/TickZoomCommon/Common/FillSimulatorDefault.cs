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
 * Date: 5/18/2009
 * Time: 12:54 PM
 * <http://www.tickzoom.org/wiki/Licenses>.
 */
#endregion

using System;
using System.Collections.Generic;
using TickZoom.Api;

namespace TickZoom.Common
{
	public class FillSimulatorDefault : FillSimulator
	{
		private static readonly Log Log = Factory.Log.GetLogger(typeof(FillSimulator));
		private static readonly bool IsTrace = Log.IsTraceEnabled;
		private static readonly bool IsDebug = Log.IsDebugEnabled;
		private static readonly bool IsNotice = Log.IsNoticeEnabled;
		IList<LogicalOrder> activeOrders;
		List<LogicalOrder> tempOrders = new List<LogicalOrder>();
		double position;
		private Action<SymbolInfo, double, double, TimeStamp> changePosition;
		private Func<LogicalOrder, double, double, int> drawTrade;
		private bool isSyntheticMarkets = true;
		private bool isSyntheticStops = true;
		private bool isSyntheticLimits = true;
		private SymbolInfo symbol;
		
		public FillSimulatorDefault()
		{
		}

		public void ProcessOrders(Tick tick, IList<LogicalOrder> orders, double position)
		{
			if( changePosition == null) {
				throw new ApplicationException("Please set the ChangePosition property with your callback method for simulated fills.");
			}
			if( symbol == null) {
				throw new ApplicationException("Please set the Symbol property for the " + GetType().Name + ".");
			}
			this.position = position;
			this.activeOrders = orders;
			tempOrders.Clear();
			tempOrders.AddRange(activeOrders);
			foreach (var order in tempOrders) {
				if (order.IsActive && order.TradeDirection == TradeDirection.Entry) {
					OnProcessEnterOrder(order, tick);
				}
				if (order.IsActive && order.TradeDirection == TradeDirection.Exit) {
					OnProcessExitOrder(order, tick);
				}
			}
		}
		
#region ExitOrder

		private void OnProcessExitOrder(LogicalOrder order, Tick tick)
		{
			if (IsTrace)
				Log.Trace("OnProcessEnterOrder()");
			if (IsLong) {
				if (order.Type == OrderType.BuyStop || order.Type == OrderType.BuyLimit) {
					order.IsActive = false;
				}
			}
			if (IsShort) {
				if (order.Type == OrderType.SellStop || order.Type == OrderType.SellLimit) {
					order.IsActive = false;
				}
			}

			if (IsLong) {
				switch (order.Type) {
					case OrderType.SellMarket:
						if (isSyntheticMarkets) {
							ProcessSellMarket(order, tick);
						}
						break;
					case OrderType.SellStop:
						if (isSyntheticStops) {
							ProcessSellStop(order, tick);
						}
						break;
					case OrderType.SellLimit:
						if (isSyntheticLimits) {
							ProcessSellLimit(order, tick);
						}
						break;
				}
			}

			if (IsShort) {
				switch (order.Type) {
					case OrderType.BuyMarket:
						if (isSyntheticMarkets) {
							ProcessBuyMarket(order, tick);
						}
						break;
					case OrderType.BuyStop:
						if (isSyntheticStops) {
							ProcessBuyStop(order, tick);
						}
						break;
					case OrderType.BuyLimit:
						if (isSyntheticLimits) {
							ProcessBuyLimit(order, tick);
						}
						break;
				}
			}
		}

		private void FlattenSignal(double price, Tick tick)
		{
			changePosition(symbol, 0, price, tick.Time);
			CancelExitOrders();
		}

		public void CancelExitOrders()
		{
			for (int i = activeOrders.Count - 1; i >= 0; i--) {
				LogicalOrder order = activeOrders[i];
				if (order.TradeDirection == TradeDirection.Exit) {
					order.IsActive = false;
				}
			}
		}

		private void ProcessBuyMarket(LogicalOrder order, Tick tick)
		{
			LogMsg("Buy Market Exit at " + tick);
			FlattenSignal(tick.Ask, tick);
			if (drawTrade != null) {
				drawTrade(order, tick.Ask, position);
			}
			CancelExitOrders();
		}

		private void ProcessSellMarket(LogicalOrder order, Tick tick)
		{
			LogMsg("Sell Market Exit at " + tick);
			FlattenSignal(tick.Ask, tick);
			if (drawTrade != null) {
				drawTrade(order, tick.Bid, position);
			}
			CancelExitOrders();
		}

		private void ProcessBuyStop(LogicalOrder order, Tick tick)
		{
			if (tick.Ask >= order.Price) {
				LogMsg("Buy Stop Exit at " + tick);
				FlattenSignal(tick.Ask, tick);
				if (drawTrade != null) {
					drawTrade(order, tick.Ask, position);
				}
				CancelExitOrders();
			}
		}

		private void ProcessBuyLimit(LogicalOrder order, Tick tick)
		{
			double price = 0;
			bool isFilled = false;
			if (tick.Ask <= order.Price) {
				price = tick.Ask;
				isFilled = true;
			} else if (tick.IsTrade && tick.Price < order.Price) {
				price = order.Price;
				isFilled = true;
			}
			if (isFilled) {
				LogMsg("Buy Limit Exit at " + tick);
				FlattenSignal(price, tick);
				if (drawTrade != null) {
					drawTrade(order, price, position);
				}
				CancelExitOrders();
			}
		}

		private void ProcessSellStop(LogicalOrder order, Tick tick)
		{
			if (tick.Bid <= order.Price) {
				LogMsg("Sell Stop Exit at " + tick);
				FlattenSignal(tick.Bid, tick);
				if (drawTrade != null) {
					drawTrade(order, tick.Bid, position);
				}
				CancelExitOrders();
			}
		}

		private void ProcessSellLimit(LogicalOrder order, Tick tick)
		{
			double price = 0;
			bool isFilled = false;
			if (tick.Bid >= order.Price) {
				price = tick.Bid;
				isFilled = true;
			} else if (tick.IsTrade && tick.Price > order.Price) {
				price = order.Price;
				isFilled = true;
			}
			if (isFilled) {
				LogMsg("Sell Stop Limit at " + tick);
				FlattenSignal(price, tick);
				if (drawTrade != null) {
					drawTrade(order, price, position);
				}
				CancelExitOrders();
			}
		}

#endregion
		
		private bool IsFlat {
			get { return position == 0; }
		}

		private bool IsShort {
			get { return position < 0; }
		}

		private bool IsLong {
			get { return position > 0; }
		}

		bool allowReversal = true;

#region EntryOrders

		private void OnProcessEnterOrder(LogicalOrder order, Tick tick)
		{
			if (IsTrace)
				Log.Trace("OnProcessEnterOrder()");
			if (IsFlat || (allowReversal && IsShort)) {
				if (order.Type == OrderType.BuyMarket && isSyntheticMarkets) {
					ProcessEnterBuyMarket(order, tick);
				}
				if (order.Type == OrderType.BuyStop && isSyntheticStops) {
					ProcessEnterBuyStop(order, tick);
				}
				if (order.Type == OrderType.BuyLimit && isSyntheticLimits) {
					ProcessEnterBuyLimit(order, tick);
				}
			}

			if (IsFlat || (allowReversal && IsLong)) {
				if (order.Type == OrderType.SellMarket && isSyntheticMarkets) {
					ProcessEnterSellMarket(order, tick);
				}
				if (order.Type == OrderType.SellStop && isSyntheticStops) {
					ProcessEnterSellStop(order, tick);
				}
				if (order.Type == OrderType.SellLimit && isSyntheticLimits) {
					ProcessEnterSellLimit(order, tick);
				}
			}
		}

		private void ProcessEnterBuyStop(LogicalOrder order, Tick tick)
		{
			if (tick.Ask >= order.Price) {
				LogMsg("Long Stop Entry at " + tick);
				changePosition(symbol, order.Positions, tick.Ask, tick.Time);
				if (drawTrade != null) {
					drawTrade(order, tick.IsQuote ? tick.Ask : tick.Price, order.Positions);
				}
				CancelEnterOrders();
			}
		}

		private void ProcessEnterSellStop(LogicalOrder order, Tick tick)
		{
			if (tick.Ask <= order.Price) {
				LogMsg("Short Stop Entry at " + tick);
				changePosition(symbol, order.Positions, tick.Ask, tick.Time);
				if (drawTrade != null) {
					drawTrade(order, tick.IsQuote ? tick.Bid : tick.Price, order.Positions);
				}
				CancelEnterOrders();
			}
		}

		private void LogMsg(string description)
		{
		}

		private void ProcessEnterBuyMarket(LogicalOrder order, Tick tick)
		{
			LogMsg("Long Market Entry at " + tick);
			double price = tick.IsQuote ? tick.Ask : tick.Price;
			changePosition(symbol, order.Positions, price, tick.Time);
			if (drawTrade != null) {
				drawTrade(order, price, order.Positions);
			}
			CancelEnterOrders();
		}

		public void CancelEnterOrders()
		{
			for (int i = activeOrders.Count - 1; i >= 0; i--) {
				LogicalOrder order = activeOrders[i];
				if (order.TradeDirection == TradeDirection.Entry) {
					order.IsActive = false;
				}
			}
		}

		private void ProcessEnterBuyLimit(LogicalOrder order, Tick tick)
		{
			double price = 0;
			bool isFilled = false;
			if (tick.Ask <= order.Price) {
				price = tick.Ask;
				isFilled = true;
			} else if (tick.IsTrade && tick.Price < order.Price) {
				price = order.Price;
				isFilled = true;
			}
			if (isFilled) {
				LogMsg("Long Limit Entry at " + tick);
				changePosition(symbol, order.Positions, price, tick.Time);
				if (drawTrade != null) {
					drawTrade(order, price, order.Positions);
				}
				CancelEnterOrders();
			}
		}

		private void ProcessEnterSellMarket(LogicalOrder order, Tick tick)
		{
			LogMsg("Short Market Entry at " + tick);
			double price = tick.IsQuote ? tick.Bid : tick.Price;
			changePosition(symbol, -order.Positions, price, tick.Time);
			if (drawTrade != null) {
				drawTrade(order, price, -order.Positions);
			}
			CancelEnterOrders();
		}

		private void ProcessEnterSellLimit(LogicalOrder order, Tick tick)
		{
			double price = 0;
			bool isFilled = false;
			if (tick.Bid >= order.Price) {
				price = tick.Bid;
				isFilled = true;
			} else if (tick.IsTrade && tick.Price > order.Price) {
				price = order.Price;
				isFilled = true;
			}
			if (isFilled) {
				LogMsg("Short Limit Entry at " + tick);
				changePosition(symbol, -order.Positions, price, tick.Time);
				if (drawTrade != null) {
					drawTrade(order, price, -order.Positions);
				}
				CancelEnterOrders();
			}
		}

		#endregion

		public Func<LogicalOrder, double, double, int> DrawTrade {
			get { return drawTrade; }
			set { drawTrade = value; }
		}
		
		public Action<SymbolInfo, double, double, TimeStamp> ChangePosition {
			get { return changePosition; }
			set { changePosition = value; }
		}
		
		public bool IsSyntheticLimits {
			get { return isSyntheticLimits; }
			set { isSyntheticLimits = value; }
		}
		
		public bool IsSyntheticStops {
			get { return isSyntheticStops; }
			set { isSyntheticStops = value; }
		}
		
		public bool IsSyntheticMarkets {
			get { return isSyntheticMarkets; }
			set { isSyntheticMarkets = value; }
		}
		
		public SymbolInfo Symbol {
			get { return symbol; }
			set { symbol = value; }
		}
	}
}
