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
		private IList<LogicalOrder> activeOrders;
		private double position;
		private Action<SymbolInfo, LogicalFill> changePosition;
		private Func<LogicalOrder, double, double, int> drawTrade;
		private bool useSyntheticMarkets = true;
		private bool useSyntheticStops = true;
		private bool useSyntheticLimits = true;
		private bool doEntryOrders = true;
		private bool doExitOrders = true;
		private bool doExitStrategyOrders = false;
		private SymbolInfo symbol;
		private bool allowReversal = true;
		private bool graphTrades = false;
		
		public FillSimulatorDefault()
		{
		}

		public FillSimulatorDefault(StrategyInterface strategyInterface)
		{
			Strategy strategy = (Strategy) strategyInterface;
			graphTrades = strategy.Performance.GraphTrades;
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
			for(int i=0; i<activeOrders.Count; i++) {
				LogicalOrder order = activeOrders[i];
				if (order.IsActive) {
					if (doEntryOrders && order.TradeDirection == TradeDirection.Entry) {
						OnProcessEnterOrder(order, tick);
					}
					if (doExitOrders && order.TradeDirection == TradeDirection.Exit) {
						OnProcessExitOrder(order, tick);
					}
					if (doExitStrategyOrders && order.TradeDirection == TradeDirection.ExitStrategy) {
						OnProcessExitOrder(order, tick);
					}
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
						if (useSyntheticMarkets) {
							ProcessSellMarket(order, tick);
						}
						break;
					case OrderType.SellStop:
						if (useSyntheticStops) {
							ProcessSellStop(order, tick);
						}
						break;
					case OrderType.SellLimit:
						if (useSyntheticLimits) {
							ProcessSellLimit(order, tick);
						}
						break;
				}
			}

			if (IsShort) {
				switch (order.Type) {
					case OrderType.BuyMarket:
						if (useSyntheticMarkets) {
							ProcessBuyMarket(order, tick);
						}
						break;
					case OrderType.BuyStop:
						if (useSyntheticStops) {
							ProcessBuyStop(order, tick);
						}
						break;
					case OrderType.BuyLimit:
						if (useSyntheticLimits) {
							ProcessBuyLimit(order, tick);
						}
						break;
				}
			}
		}

		private void FlattenSignal(double price, Tick tick, LogicalOrder order)
		{
			LogicalFillBinary fill = new LogicalFillBinary(0,price,tick.Time,order.Id);
			changePosition(symbol, fill);
			CancelExitOrders(order.TradeDirection);
		}

		public void CancelExitOrders(TradeDirection tradeDirection)
		{
			for (int i = activeOrders.Count - 1; i >= 0; i--) {
				LogicalOrder order = activeOrders[i];
				if (order.TradeDirection == tradeDirection) {
					order.IsActive = false;
				}
			}
		}

		private void ProcessBuyMarket(LogicalOrder order, Tick tick)
		{
			LogMsg("Buy Market Exit at " + tick);
			FlattenSignal(tick.Ask, tick, order);
			TryDrawTrade(order, tick.Ask, position);
		}
		
		private void TryDrawTrade(LogicalOrder order, double price, double position) {
			if (drawTrade != null && graphTrades == true) {
				drawTrade(order, price, position);
			}
		}

		private void ProcessSellMarket(LogicalOrder order, Tick tick)
		{
			LogMsg("Sell Market Exit at " + tick);
			FlattenSignal(tick.Ask, tick, order);
			TryDrawTrade(order, tick.Bid, position);
		}

		private void ProcessBuyStop(LogicalOrder order, Tick tick)
		{
			if (tick.Ask >= order.Price) {
				LogMsg("Buy Stop Exit at " + tick);
				FlattenSignal(tick.Ask, tick, order);
				TryDrawTrade(order, tick.Ask, position);
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
				FlattenSignal(price, tick, order);
				TryDrawTrade(order, price, position);
			}
		}

		private void ProcessSellStop(LogicalOrder order, Tick tick)
		{
			if (tick.Bid <= order.Price) {
				LogMsg("Sell Stop Exit at " + tick);
				FlattenSignal(tick.Bid, tick, order);
				TryDrawTrade(order, tick.Bid, position);
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
				FlattenSignal(price, tick, order);
				TryDrawTrade(order, price, position);
			}
		}

#endregion
		

#region EntryOrders

		private void OnProcessEnterOrder(LogicalOrder order, Tick tick)
		{
			if (IsTrace)
				Log.Trace("OnProcessEnterOrder()");
			if (IsFlat || (allowReversal && IsShort)) {
				if (order.Type == OrderType.BuyMarket && useSyntheticMarkets) {
					ProcessEnterBuyMarket(order, tick);
				}
				if (order.Type == OrderType.BuyStop && useSyntheticStops) {
					ProcessEnterBuyStop(order, tick);
				}
				if (order.Type == OrderType.BuyLimit && useSyntheticLimits) {
					ProcessEnterBuyLimit(order, tick);
				}
			}

			if (IsFlat || (allowReversal && IsLong)) {
				if (order.Type == OrderType.SellMarket && useSyntheticMarkets) {
					ProcessEnterSellMarket(order, tick);
				}
				if (order.Type == OrderType.SellStop && useSyntheticStops) {
					ProcessEnterSellStop(order, tick);
				}
				if (order.Type == OrderType.SellLimit && useSyntheticLimits) {
					ProcessEnterSellLimit(order, tick);
				}
			}
		}

		private void ProcessEnterBuyStop(LogicalOrder order, Tick tick)
		{
			if (tick.Ask >= order.Price) {
				LogMsg("Long Stop Entry at " + tick);
				
				CreateLogicalFill(symbol, order.Positions, tick.Ask, tick.Time, order.Id);
				TryDrawTrade(order, tick.IsQuote ? tick.Ask : tick.Price, order.Positions);
				CancelEnterOrders();
			}
		}

		private void ProcessEnterSellStop(LogicalOrder order, Tick tick)
		{
			if (tick.Ask <= order.Price) {
				LogMsg("Short Stop Entry at " + tick);
				CreateLogicalFill(symbol, order.Positions, tick.Ask, tick.Time, order.Id);
				TryDrawTrade(order, tick.IsQuote ? tick.Bid : tick.Price, order.Positions);
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
			CreateLogicalFill(symbol, order.Positions, price, tick.Time, order.Id);
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
		
		private void CreateLogicalFill(SymbolInfo symbol, double position, double price, TimeStamp time, int logicalOrderId) {
			LogicalFillBinary fill = new LogicalFillBinary(position,price,time,logicalOrderId);
			changePosition(symbol,fill);
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
				CreateLogicalFill(symbol, order.Positions, price, tick.Time, order.Id);
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
			CreateLogicalFill(symbol, -order.Positions, price, tick.Time, order.Id);
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
				CreateLogicalFill(symbol, -order.Positions, price, tick.Time, order.Id);
				if (drawTrade != null) {
					drawTrade(order, price, -order.Positions);
				}
				CancelEnterOrders();
			}
		}
		
		public void ProcessFill(StrategyInterface strategyInterface, LogicalFill fill) {
			if( IsDebug) Log.Debug( "Considering fill: " + fill + " for strategy " + strategyInterface);
			Strategy strategy = (Strategy) strategyInterface;
			bool cancelAllEntries = false;
			bool cancelAllExits = false;
			bool cancelAllExitStrategies = false;
			int orderId = fill.OrderId;
			LogicalOrder filledOrder = null;
			foreach( var order in strategy.ActiveOrders) {
				if( order.Id == orderId) {
					if( IsDebug) Log.Debug( "Matched fill with orderId: " + orderId);
					if( order.TradeDirection == TradeDirection.Entry && !doEntryOrders) {
						if( IsDebug) Log.Debug( "Skipping fill, entry orders fills disabled.");
						return;
					}
					if( order.TradeDirection == TradeDirection.Exit && !doExitOrders) {
						if( IsDebug) Log.Debug( "Skipping fill, exit orders fills disabled.");
						return;
					}
					if( order.TradeDirection == TradeDirection.ExitStrategy && !doExitStrategyOrders) {
						if( IsDebug) Log.Debug( "Skipping fill, exit strategy orders fills disabled.");
						return;
					}
					filledOrder = order;
					if (drawTrade != null) {
						drawTrade(filledOrder,fill.Price,fill.Position);
					}
					if( IsDebug) Log.Debug( "Changing position because of fill");
					changePosition(strategy.Data.SymbolInfo,fill);
				}
			}
			if( filledOrder != null) {
				bool clean = false;
				if( filledOrder.TradeDirection == TradeDirection.Entry &&
				   doEntryOrders ) {
					cancelAllEntries = true;
					clean = true;
				}
				if( filledOrder.TradeDirection == TradeDirection.Exit &&
				   doExitOrders ) {
					cancelAllExits = true;
					clean = true;
				}
				if( filledOrder.TradeDirection == TradeDirection.ExitStrategy &&
				   doExitStrategyOrders ) {
					cancelAllExitStrategies = true;
					clean = true;
				}
				if( clean) {
					strategy.RefreshActiveOrders();
					foreach( var order in strategy.ActiveOrders) {
						if( order.TradeDirection == TradeDirection.Entry && cancelAllEntries) {
							order.IsActive = false;
						}
						if( order.TradeDirection == TradeDirection.Exit && cancelAllExits) {
							order.IsActive = false;
						}
						if( order.TradeDirection == TradeDirection.ExitStrategy && cancelAllExitStrategies) {
							order.IsActive = false;
						}
					}
				}
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
		
		public Func<LogicalOrder, double, double, int> DrawTrade {
			get { return drawTrade; }
			set { drawTrade = value; }
		}
		
		public Action<SymbolInfo, LogicalFill> ChangePosition {
			get { return changePosition; }
			set { changePosition = value; }
		}
		
		public bool UseSyntheticLimits {
			get { return useSyntheticLimits; }
			set { useSyntheticLimits = value; }
		}
		
		public bool UseSyntheticStops {
			get { return useSyntheticStops; }
			set { useSyntheticStops = value; }
		}
		
		public bool UseSyntheticMarkets {
			get { return useSyntheticMarkets; }
			set { useSyntheticMarkets = value; }
		}
		
		public SymbolInfo Symbol {
			get { return symbol; }
			set { symbol = value; }
		}
		
		public bool DoEntryOrders {
			get { return doEntryOrders; }
			set { doEntryOrders = value; }
		}
		
		public bool DoExitOrders {
			get { return doExitOrders; }
			set { doExitOrders = value; }
		}
		
		public bool DoExitStrategyOrders {
			get { return doExitStrategyOrders; }
			set { doExitStrategyOrders = value; }
		}
		
		public bool GraphTrades {
			get { return graphTrades; }
			set { graphTrades = value; }
		}
	}
}
