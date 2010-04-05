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
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Threading;

using TickZoom.Api;

namespace TickZoom.Common
{
	public class Performance : StrategyInterceptor
	{
		private static readonly Log barDataLog = Factory.Log.GetLogger("BarDataLog");
		private static readonly bool barDataInfo = barDataLog.IsInfoEnabled;
		private static readonly Log tradeLog = Factory.Log.GetLogger("TradeLog");
		private static readonly bool tradeInfo = tradeLog.IsInfoEnabled;
		private static readonly Log statsLog = Factory.Log.GetLogger("StatsLog");
		private static readonly bool statsInfo = statsLog.IsInfoEnabled;
		TransactionPairs comboTrades;
		TransactionPairsBinary comboTradesBinary;
		bool graphTrades = true;
		Equity equity;
		TradeProfitLoss profitLoss;
		List<double> positionChanges = new List<double>();
		PositionCommon position;
		Model model;
		
		public Performance(Model model)
		{
			this.model = model;
			profitLoss = new TradeProfitLoss(model);
			equity = new Equity(model,this);
			comboTradesBinary  = new TransactionPairsBinary();
			comboTradesBinary.Name = "ComboTrades";
			position = new PositionCommon(model);
		}
		
		public double GetCurrentPrice( double direction) {
			System.Diagnostics.Debug.Assert(direction!=0);
			Tick tick = model.Ticks[0];
			if( direction > 0) {
				return tick.IsQuote ? tick.Bid : tick.Price;
			} else {
				return tick.IsQuote ? tick.Ask : tick.Price;
			}
		}
		
		EventContext context;
		
		public void Intercept(EventContext context, EventType eventType, object eventDetail)
		{
			this.context = context;
			if( EventType.Initialize == eventType) {
				model.AddInterceptor( EventType.Close, this);
				model.AddInterceptor( EventType.Tick, this);
				OnInitialize();
			}
			if( EventType.Close == eventType && eventDetail == null) {
				OnIntervalClose();
			}
			context.Invoke();
			if( EventType.Tick == eventType) {
				OnProcessTick((Tick)eventDetail);
			}
		}
		
		public void OnInitialize()
		{ 
			comboTrades  = new TransactionPairs(GetCurrentPrice,profitLoss,comboTradesBinary);
			profitLoss.FullPointValue = model.Data.SymbolInfo.FullPointValue;

		}
		
		public bool OnProcessTick(Tick tick)
		{
			if( context.Position.Current != position.Current) {
				positionChanges.Add(context.Position.Current);
				if( position.IsFlat) {
					EnterComboTradeInternal(context.Position);
				} else if( context.Position.IsFlat) {
					ExitComboTradeInternal(context.Position);
				} else if( (context.Position.IsLong && position.IsShort) || (context.Position.IsShort && position.IsLong)) {
					// The signal must be opposite. Either -1 / 1 or 1 / -1
					ExitComboTradeInternal(context.Position);
					EnterComboTradeInternal(context.Position);
				} else {
					// Instead it has increased or decreased position size.
					ChangeComboSizeInternal();
				}
			} 
			position.Copy(context.Position);
			if( model is Strategy) {
				Strategy strategy = (Strategy) model;
				strategy.Result.Position.Copy(context.Position);
			}

			if( model is Portfolio) {
				Portfolio portfolio = (Portfolio) model;
				double tempNetClosedEquity = 0;
				foreach( Strategy tempStrategy in portfolio.Strategies) {
					tempNetClosedEquity += tempStrategy.Performance.Equity.ClosedEquity;
					tempNetClosedEquity -= tempStrategy.Performance.Equity.StartingEquity;
				}
				double tempNetPortfolioEquity = 0;
				tempNetPortfolioEquity += portfolio.Performance.Equity.ClosedEquity;
				tempNetPortfolioEquity -= portfolio.Performance.Equity.StartingEquity;
			}
			return true;
		}
		
		private void EnterComboTradeInternal(PositionInterface position) {
			EnterComboTrade(position);
		}

		public void EnterComboTrade(PositionInterface position) {
			TransactionPairBinary pair = TransactionPairBinary.Create();
			pair.Direction = position.Current;
			pair.EntryPrice = position.Price;
			pair.EntryTime = position.Time;
			pair.EntryBar = model.Chart.ChartBars.BarCount;
			comboTradesBinary.Add(pair);
			if( model is Strategy) {
				Strategy strategy = (Strategy) model;
				strategy.OnEnterTrade();
			}
		}
		
		private void ChangeComboSizeInternal() {
			TransactionPairBinary combo = comboTradesBinary.Current;
			combo.ChangeSize(context.Position.Current,context.Position.Price);
			comboTradesBinary.Current = combo;
		}
		
		private void ExitComboTradeInternal(PositionInterface position) {
			ExitComboTrade(position);
		}
					
		public void ExitComboTrade(PositionInterface position) {
			TransactionPairBinary comboTrade = comboTradesBinary.Current;
			comboTrade.ExitPrice = position.Price;
			comboTrade.ExitTime = position.Time;
			comboTrade.ExitBar = model.Chart.ChartBars.BarCount;
			comboTrade.Completed = true;
			comboTradesBinary.Current = comboTrade;
			double pnl = profitLoss.CalculateProfit(comboTrade.Direction,comboTrade.EntryPrice,comboTrade.ExitPrice);
			Equity.OnChangeClosedEquity( pnl);
			if( tradeInfo) tradeLog.Info( model.Name + "," + Equity.ClosedEquity + "," + pnl + "," + comboTrade);
			if( model is Strategy) {
				Strategy strategy = (Strategy) model;
				strategy.OnExitTrade();
			}
		}
		
		protected virtual void EnterTrade() {

		}
		
		protected virtual void ExitTrade() {
			
		}
		
		public bool OnIntervalClose()
		{
			if( barDataInfo) {
				Bars bars = model.Bars;
				TimeStamp time = bars.Time[0];
				StringBuilder sb = new StringBuilder();
				sb.Append(model.Name);
				sb.Append(",");
				sb.Append(time);
				sb.Append(",");
				sb.Append(bars.Open[0]);
				sb.Append(",");
				sb.Append(bars.High[0]);
				sb.Append(",");
				sb.Append(bars.Low[0]);
				sb.Append(",");
				sb.Append(bars.Close[0]);
				barDataLog.Info( sb.ToString());
			}
			if( statsInfo) {
				Bars bars = model.Bars;
				TimeStamp time = bars.Time[0];
				StringBuilder sb = new StringBuilder();
				sb.Append(model.Name);
				sb.Append(",");
				sb.Append(time);
				sb.Append(",");
				sb.Append(equity.ClosedEquity);
				sb.Append(",");
				sb.Append(equity.OpenEquity);
				sb.Append(",");
				sb.Append(equity.CurrentEquity);
				statsLog.Info( sb.ToString());
			}
			return true;
		}
		
		public Equity Equity {
			get { return equity; }
			set { equity = value; }
		}
		
		public bool WriteReport(string name, string folder) {
			name = name.StripInvalidPathChars();
			TradeStatsReport tradeStats = new TradeStatsReport(this);
			tradeStats.WriteReport(name, folder);
			StrategyStats stats = new StrategyStats(ComboTrades);
			Equity.WriteReport(name,folder,stats);
			IndexForReport index = new IndexForReport(this);
			index.WriteReport(name, folder);
			return true;
		}

		public double Slippage {
			get { return profitLoss.Slippage; }
			set { profitLoss.Slippage = value; }
		}
		
		
		public List<double> PositionChanges {
			get { return positionChanges; }
		}

		public double Commission {
			get { return profitLoss.Commission; }
			set { profitLoss.Commission = value; }
		}
		
		public PositionCommon Position {
			get { return position; }
			set { position = value; }
		}
		
#region Obsolete Methods		
		
		[Obsolete("Use WriteReport(name,folder) instead.",true)]
		public void WriteReport(string name,StreamWriter writer) {
			throw new NotImplementedException();
		}

		[Obsolete("Please use ComboTrades instead.",true)]
    	public TransactionPairs TransactionPairs {
			get { return null; }
		}
		
		[Obsolete("Please use Performance.Equity.Daily instead.",true)]
		public TransactionPairs CompletedDaily {
			get { return Equity.Daily; }
		}

		[Obsolete("Please use Performance.Equity.Weekly instead.",true)]
		public TransactionPairs CompletedWeekly {
			get { return Equity.Weekly; }
		}

		public TransactionPairs ComboTrades {
			get { 
				if( comboTradesBinary.Count > 0) {
					comboTradesBinary.Current.TryUpdate(model.Ticks[0]);
				}
				return comboTrades;
			}
		}
		
		[Obsolete("Please use Performance.Equity.Monthly instead.",true)]
		public TransactionPairs CompletedMonthly {
			get { return Equity.Monthly; }
		}
	
		[Obsolete("Please use Performance.Equity.Yearly instead.",true)]
		public TransactionPairs CompletedYearly {
			get { return Equity.Yearly; }
		}
		
		[Obsolete("Please use Performance.ComboTrades instead.",true)]
		public TransactionPairs CompletedComboTrades {
			get { return ComboTrades; }
		}
		
		[Obsolete("Please use TransactionPairs instead.",true)]
		public TransactionPairs Trades {
			get { return TransactionPairs; }
		}
		
		[Obsolete("Please use the same property at Performance.Equity.* instead.",true)]
		public TransactionPairs Daily {
			get { return Equity.Daily; }
		}
		
		[Obsolete("Please use the same property at Performance.Equity.* instead.",true)]
		public TransactionPairs Weekly {
			get { return Equity.Weekly; }
		}
		
		[Obsolete("Please use the same property at Performance.Equity.* instead.",true)]
		public TransactionPairs Monthly {
			get { return Equity.Monthly; }
		}
		
		[Obsolete("Please use the same property at Performance.Equity.* instead.",true)]
		public TransactionPairs Yearly {
			get { return Equity.Yearly; }
		}
		
		public bool GraphTrades {
			get { return graphTrades; }
			set { graphTrades = value; }
		}
		
		[Obsolete("Please use the same property at Performance.Equity.* instead.",true)]
		public double ProfitToday {
			get { return Equity.CurrentEquity; }
		}
		
		[Obsolete("Please use the same property at Performance.Equity.* instead.",true)]
		public double ProfitForWeek {
			get { return Equity.ProfitForWeek; }
		}

		[Obsolete("Please use the same property at Performance.Equity.* instead.",true)]
		public double ProfitForMonth {
			get { return Equity.ProfitForMonth; }
		}
		
		[Obsolete("Please use the same property at Performance.Equity.* instead.",true)]
		public double StartingEquity {
			get { return Equity.StartingEquity; }
			set { Equity.StartingEquity = value; }
		}
		
		[Obsolete("Please use the same property at Performance.Equity.* instead.",true)]
		public double CurrentEquity {
			get { 
				return Equity.CurrentEquity;
			}
		}
		
		[Obsolete("Please use the same property at Performance.Equity.* instead.",true)]
		public double ClosedEquity {
			get {
				return Equity.ClosedEquity;
			}
		}
		
		[Obsolete("Please use the same property at Performance.Equity.* instead.",true)]
		public double OpenEquity {
			get {
				return Equity.OpenEquity;
			}
		}
		
		public StrategyStats CalculateStatistics() {
			return new StrategyStats(ComboTrades);
		}
		
		[Obsolete("Please use the same property at Performance.Equity.* instead.",true)]
		public bool GraphEquity {
			get { return Equity.GraphEquity; }
			set { Equity.GraphEquity = value; }
		}
		
		[Obsolete("Please use Slippage and Commission properties on Performance.",true)]
		public ProfitLoss ProfitLoss {
			get { return null; }
			set { }
		}


#endregion		

	}

	[Obsolete("Please user Performance instead.",true)]
	public class PerformanceCommon : Performance {
		public PerformanceCommon(Strategy strategy) : base(strategy)
		{
			
		}
	}
	
}