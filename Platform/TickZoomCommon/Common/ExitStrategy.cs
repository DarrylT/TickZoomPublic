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
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using TickZoom.TickUtil;

using TickZoom.Api;

namespace TickZoom.Common
{
	public class ExitStrategy : StrategySupport
	{
		private bool controlStrategy = false;
		private double strategySignal = 0;
		private LogicalOrder stopLossOrder;
		private LogicalOrder marketOrder;
		private double stopLoss = 0;
		private double targetProfit = 0;
		private double breakEven = 0;
		private double entryPrice = 0;
		private double trailStop = 0;
		private double dailyMaxProfit = 0;
		private double dailyMaxLoss = 0;
		private double weeklyMaxProfit = 0;
		private double weeklyMaxLoss = 0;
		private double monthlyMaxProfit = 0;
		private double monthlyMaxLoss = 0;
		private LogicalOrder breakEvenStopOrder;
		private double breakEvenStop = 0;
		private double pnl = 0;
		private double maxPnl = 0;
		bool stopTradingToday = false;
		bool stopTradingThisWeek = false;
		bool stopTradingThisMonth = false;
		PositionCommon position;
		
		public ExitStrategy(Strategy strategy) : base( strategy) {
//			RequestUpdate(Intervals.Day1);
//			RequestUpdate(Intervals.Week1);
//			RequestUpdate(Intervals.Month1);
			position = new PositionCommon(strategy);
		}
		
		EventContext context;
		public override void Intercept(EventContext context, EventType eventType, object eventDetail)
		{
			if( eventType == EventType.Initialize) {
				Strategy.AddInterceptor( EventType.Tick, this);
				OnInitialize();
			}
			context.Invoke();
			this.context = context;
			if( eventType == EventType.Tick) {
				OnProcessPosition((Tick) eventDetail);
			}
		}
				
		public void OnInitialize()
		{
			marketOrder = Factory.Engine.LogicalOrder(Strategy.Data.SymbolInfo,Strategy);
			marketOrder.TradeDirection = TradeDirection.Exit;
			Strategy.OrderManager.Add(marketOrder);
			breakEvenStopOrder = Factory.Engine.LogicalOrder(Strategy.Data.SymbolInfo,Strategy);
			breakEvenStopOrder.TradeDirection = TradeDirection.Exit;
			stopLossOrder = Factory.Engine.LogicalOrder(Strategy.Data.SymbolInfo,Strategy);
			Strategy.OrderManager.Add(stopLossOrder);
			stopLossOrder.TradeDirection = TradeDirection.Exit;
			stopLossOrder.Tag = "ExitStrategy" ;
			Strategy.OrderManager.Add(breakEvenStopOrder);
//			log.WriteFile( LogName + " chain = " + Chain.ToChainString());
			if( IsTrace) Log.Trace(Strategy.FullName+".Initialize()");
			Strategy.Drawing.Color = Color.Black;
		}
		
		public void OnProcessPosition(Tick tick) {
			// Handle ActiveNow orders.
			if( Strategy.ActiveOrders.Count > 0) {
				Strategy.OrderManager.ProcessOrders(tick);
			}
			
			if( stopTradingToday || stopTradingThisWeek || stopTradingThisMonth ) {
				return;
			}
			
			if( (strategySignal>0) != context.Position.IsLong || (strategySignal<0) != context.Position.IsShort ) {
				strategySignal = context.Position.Current;
				entryPrice = context.Position.Price;
				maxPnl = 0;
				position.Copy(context.Position);
				trailStop = 0;
				breakEvenStop = 0;
				CancelOrders();
			} 
			
			if( position.HasPosition ) {
				// copy signal in case of increased position size
				double exitPrice;
				if( strategySignal > 0) {
					exitPrice = tick.IsQuote ? tick.Bid : tick.Price;
					pnl = (exitPrice - entryPrice).Round();
				} else {
					exitPrice = tick.IsQuote ? tick.Ask : tick.Price;
					pnl = (entryPrice - exitPrice).Round();
				}
				maxPnl = pnl > maxPnl ? pnl : maxPnl;
				if( stopLoss > 0) processStopLoss(tick);
				if( trailStop > 0) processTrailStop(tick);
				if( breakEven > 0) processBreakEven(tick);
				if( targetProfit > 0) processTargetProfit(tick);
				if( dailyMaxProfit > 0) processDailyMaxProfit(tick);
				if( dailyMaxLoss > 0) processDailyMaxLoss(tick);
				if( weeklyMaxProfit > 0) processWeeklyMaxProfit(tick);
				if( weeklyMaxLoss > 0) processWeeklyMaxLoss(tick);
				if( monthlyMaxProfit > 0) processMonthlyMaxProfit(tick);
				if( monthlyMaxLoss > 0) processMonthlyMaxLoss(tick);
			}
			
			context.Position.Copy(position);
		}
	
		private void processDailyMaxProfit(Tick tick) {
			if( Strategy.Performance.Equity.ProfitToday >= dailyMaxProfit) {
				stopTradingToday = true;
				LogExit("DailyMaxProfit Exit at " + dailyMaxProfit);
				flattenSignal(tick,"Daily Profit Target");
			}
		}
		
		private void processDailyMaxLoss(Tick tick) {
			if( Strategy.Performance.Equity.ProfitToday >= dailyMaxLoss) {
				stopTradingToday = true;
				LogExit("DailyMaxLoss Exit at " + dailyMaxLoss);
				flattenSignal(tick,"Daily Stop Loss");
			}
		}
		
		private void processWeeklyMaxProfit(Tick tick) {
			if( Strategy.Performance.Equity.ProfitForWeek >= weeklyMaxProfit) {
				stopTradingThisWeek = true;
				LogExit("WeeklyMaxProfit Exit at " + weeklyMaxProfit);
				flattenSignal(tick,"Weekly Profit Target");
			}
		}
		
		private void processWeeklyMaxLoss(Tick tick) {
			if( - Strategy.Performance.Equity.ProfitForWeek >= weeklyMaxLoss) {
				stopTradingThisWeek = true;
				LogExit("WeeklyMaxLoss Exit at " + weeklyMaxLoss);
				flattenSignal(tick,"Weekly Stop Loss");
			}
		}
		
		private void processMonthlyMaxProfit(Tick tick) {
			if( Strategy.Performance.Equity.ProfitForMonth >= monthlyMaxProfit) {
				stopTradingThisMonth = true;
				LogExit("MonthlyMaxProfit Exit at " + monthlyMaxProfit);
				flattenSignal(tick,"Monthly Profit Target");
			}
		}
		
		private void processMonthlyMaxLoss(Tick tick) {
			if( - Strategy.Performance.Equity.ProfitForMonth >= monthlyMaxLoss) {
				stopTradingThisMonth = true;
				LogExit("MonthlyMaxLoss Exit at " + monthlyMaxLoss);
				flattenSignal(tick,"Monthly Stop Loss");
			}
		}
		
		private void CancelOrders() {
			marketOrder.IsActive = false;
			breakEvenStopOrder.IsActive = false;
			stopLossOrder.IsActive = false;
		}
		
		private void flattenSignal(Tick tick, string tag) {
			marketOrder.Tag = tag;
			flattenSignal(marketOrder,tick);
		}
		
		private void flattenSignal(LogicalOrder order, Tick tick) {
            if (Strategy.Performance.GraphTrades)
            {
				double fillPrice = 0;
				if( position.IsLong) {
					order.Positions = context.Position.Size;
					fillPrice = tick.Bid;
				}
				if( position.IsShort) {
					order.Positions = context.Position.Size;
					fillPrice = tick.Ask;
				}
                Strategy.Chart.DrawTrade(order, fillPrice, 0);
            }
            position.Change(0);
			CancelOrders();
			if( controlStrategy) {
				Strategy.Orders.Exit.ActiveNow.GoFlat();
				strategySignal = 0;
			}
		}
	
		private void processTargetProfit(Tick tick) {
			if( pnl >= targetProfit) {
				LogExit("TargetProfit Exit at " + targetProfit);
				flattenSignal(tick,"Target Profit");
			}
		}
		
		private void processStopLoss(Tick tick) {
			if( !stopLossOrder.IsActive) {
				stopLossOrder.IsActive = true;
				if( position.IsLong) {
					stopLossOrder.Type = OrderType.SellStop;
					stopLossOrder.Price = entryPrice - stopLoss;
				}
				if( position.IsShort) {
					stopLossOrder.Type = OrderType.BuyStop;
					stopLossOrder.Price = entryPrice + stopLoss;
				}
			}
			if( pnl <= -stopLoss) {
				LogExit("StopLoss " + stopLoss + " Exit at " + tick);
				flattenSignal(stopLossOrder,tick);
			}
		}
		
		private void processTrailStop(Tick tick) {
			if( maxPnl - pnl >= trailStop) {
				LogExit("TailStop Exit at " + trailStop);
				flattenSignal(tick,"Trail Stop");
			}
		}
		
		public bool OnIntervalOpen(Interval interval) {
			if( interval.Equals(Intervals.Day1)) {
				stopTradingToday = false;
			}
			if( interval.Equals(Intervals.Week1)) {
				stopTradingThisWeek = false;
			}
			if( interval.Equals(Intervals.Month1)) {
				stopTradingThisMonth = false;
			}
			return true;
		}
	
		private void processBreakEven(Tick tick) {
			if( pnl >= breakEven) {
				breakEvenStopOrder.Tag = "Break Even";
				if( !breakEvenStopOrder.IsActive) {
					breakEvenStopOrder.IsActive = true;
					if( position.IsLong ) {
						breakEvenStopOrder.Type = OrderType.SellStop;
						breakEvenStopOrder.Price = entryPrice + breakEvenStop;
					}
					
					if( position.IsShort ) {
						breakEvenStopOrder.Type = OrderType.BuyStop;
						breakEvenStopOrder.Price = entryPrice - breakEvenStop;
					}
				}
			}
			if( breakEvenStopOrder.IsActive && pnl <= breakEvenStop) {
				LogExit("Break Even Exit at " + breakEvenStop);
				flattenSignal(breakEvenStopOrder,tick);
			}
		}
		
		private void LogExit(string description) {
			if( Strategy.Chart.IsDynamicUpdate) {
				Log.Notice(Strategy.Ticks[0].Time + ", Bar="+Strategy.Chart.DisplayBars.CurrentBar+", " + description);
			} else if( !Strategy.IsOptimizeMode) {
				if( IsDebug) Log.Debug(Strategy.Ticks[0].Time + ", Bar="+Strategy.Chart.DisplayBars.CurrentBar+", " + description);
			}
		}

        #region Properties
        
        [DefaultValue(0d)]
        public double StopLoss
        {
            get { return stopLoss; }
            set { // log.WriteFile(GetType().Name+".StopLoss("+value+")");
            	  stopLoss = Math.Max(0, value); }
        }		

        [DefaultValue(0d)]
		public double TrailStop
        {
            get { return trailStop; }
            set { trailStop = Math.Max(0, value); }
        }		
		
        [DefaultValue(0d)]
		public double TargetProfit
        {
            get { return targetProfit; }
            set { if( IsTrace) Log.Trace(GetType().Name+".TargetProfit("+value+")");
            	  targetProfit = Math.Max(0, value); }
        }		
		
        [DefaultValue(0d)]
		public double BreakEven
        {
            get { return breakEven; }
            set { breakEven = Math.Max(0, value); }
        }	
		
        [DefaultValue(false)]
		public bool ControlStrategy {
			get { return controlStrategy; }
			set { controlStrategy = value; }
		}
		
        [DefaultValue(0d)]
		public double WeeklyMaxProfit {
			get { return weeklyMaxProfit; }
			set { weeklyMaxProfit = value; }
		}
		
        [DefaultValue(0d)]
		public double WeeklyMaxLoss {
			get { return weeklyMaxLoss; }
			set { weeklyMaxLoss = value; }
		}
		
        [DefaultValue(0d)]
		public double DailyMaxProfit {
			get { return dailyMaxProfit; }
			set { dailyMaxProfit = value; }
		}
		
        [DefaultValue(0d)]
		public double DailyMaxLoss {
			get { return dailyMaxLoss; }
			set { dailyMaxLoss = value; }
		}
		
        [DefaultValue(0d)]
		public double MonthlyMaxLoss {
			get { return monthlyMaxLoss; }
			set { monthlyMaxLoss = value; }
		}
		
        [DefaultValue(0d)]
		public double MonthlyMaxProfit {
			get { return monthlyMaxProfit; }
			set { monthlyMaxProfit = value; }
		}
		#endregion
	
//		public override string ToString()
//		{
//			return Strategy.FullName;
//		}
		
		public PositionCommon Position {
			get { return position; }
			set { position = value; }
		}
	}

	[Obsolete("Please use ExitStrategy instead.",true)]
	public class ExitStrategyCommon : ExitStrategy
	{
		public ExitStrategyCommon(Strategy strategy) : base( strategy) {
			
		}
	}
		
}
