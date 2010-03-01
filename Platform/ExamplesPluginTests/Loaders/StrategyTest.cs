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
 * Date: 5/25/2009
 * Time: 3:36 PM
 * <http://www.tickzoom.org/wiki/Licenses>.
 */
#endregion


using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;
using TickZoom;
using TickZoom.Api;
using TickZoom.Common;
using TickZoom.TickUtil;
using System.IO;
using ZedGraph;

namespace Loaders
{
	public class StrategyTest
	{
		static readonly Log log = Factory.Log.GetLogger(typeof(StrategyTest));
		static readonly bool debug = log.IsDebugEnabled;
		string dataFolder = "TestData";
		List<ChartThread> chartThreads = new List<ChartThread>();
		List<TickAggregator> aggregators = new List<TickAggregator>();
		Dictionary<string,List<TradeInfo>> goodTradeMap = new Dictionary<string,List<TradeInfo>>();
		Dictionary<string,List<TradeInfo>> testTradeMap = new Dictionary<string,List<TradeInfo>>();
		
		[TestFixtureSetUp]
		public virtual void RunStrategy() {
			string appDataFolder = Factory.Settings["AppDataFolder"];
			string filePath = appDataFolder + @"\Logs\Trades.log";
			File.Delete(filePath);
		}
		
		[TestFixtureTearDown]
		public void CloseCharts() {
   			HistoricalCloseCharts();
		}
		
		public virtual Provider LoadHistoricalDataAsFakeRealTimeData(string[] symbols, TimeStamp startTime, TimeStamp endTime) {
			log.Debug("Creating Aggregator");
			TickAggregator aggregator = new TickAggregator();
			for(int i=0; i<symbols.Length; i++) {
	    		TickReader tickReader = new TickReader();
	    		tickReader.StartTime = startTime;
//	    		tickReader.EndTime = endTime;
	    		tickReader.Initialize(dataFolder,symbols[i]);
				aggregator.Add(Factory.Symbol.LookupSymbol(symbols[i]),tickReader,startTime);
			}
			aggregators.Add(aggregator);
			return aggregator;
		}
		
		public class TradeInfo {
			public double ClosedEquity;
			public double ProfitLoss;
			public TransactionPairBinary Trade;
		}
		
		public void LoadTrades() {
			string fileDir = @"..\..\Platform\ExamplesPluginTests\Loaders\Trades\";
			string filePath = fileDir + GetType().Name + "Trades.log";
			string appDataFolder = Factory.Settings["AppDataFolder"];
			goodTradeMap.Clear();
			LoadTrades(filePath,goodTradeMap);
			filePath = appDataFolder + @"\Logs\Trades.log";
			LoadTrades(filePath,testTradeMap);
		}
		
		public void LoadTrades(string filePath, Dictionary<string,List<TradeInfo>> tempTrades) {
			using( FileStream fileStream = new FileStream(filePath,FileMode.Open,FileAccess.Read,FileShare.ReadWrite)) {
				StreamReader file = new StreamReader(fileStream);
				string line;
				while( (line = file.ReadLine()) != null) {
					string[] fields = line.Split(',');
					int fieldIndex = 0;
					string strategyName = fields[fieldIndex++];
					TradeInfo testInfo = new TradeInfo();
					
					testInfo.ClosedEquity = double.Parse(fields[fieldIndex++]);
					testInfo.ProfitLoss = double.Parse(fields[fieldIndex++]);
					
					line = string.Join(",",fields,fieldIndex,fields.Length-fieldIndex);
					testInfo.Trade = TransactionPairBinary.Parse(line);
					List<TradeInfo> tradeList;
					if( tempTrades.TryGetValue(strategyName,out tradeList)) {
						tradeList.Add(testInfo);
					} else {
						tradeList = new List<TradeInfo>();
						tradeList.Add(testInfo);
						tempTrades.Add(strategyName,tradeList);
					}
				}
			}
		}
		
		public void VerifyTradeCount(Strategy strategy) {
			List<TradeInfo> goodTrades = goodTradeMap[strategy.Name];
			List<TradeInfo> testTrades = testTradeMap[strategy.Name];
			Assert.AreEqual(goodTrades.Count,testTrades.Count);
		}
		
		public void VerifyTrades(Strategy strategy) {
			List<TradeInfo> goodTrades = goodTradeMap[strategy.Name];
			List<TradeInfo> testTrades = testTradeMap[strategy.Name];
			for( int i=0; i<testTrades.Count && i<goodTrades.Count; i++) {
				TradeInfo testInfo = testTrades[i];
				TradeInfo goodInfo = goodTrades[i];
				TransactionPairBinary goodTrade = goodInfo.Trade;
				TransactionPairBinary testTrade = testInfo.Trade;
				Assert.AreEqual(goodTrade,testTrade,"Trade at " + i);
//				Assert.AreEqual(goodTrade.Direction,testTrade.Direction,"position at " + i);
//				Assert.AreEqual(goodTrade.EntryBar,testTrade.EntryBar,"entry bar at " + i);
//				Assert.AreEqual(goodTrade.EntryPrice,testTrade.EntryPrice,"EntryPrice at " + i);
//				Assert.AreEqual(goodTrade.EntryTime,testTrade.EntryTime,"EntryTime at " + i);
//				Assert.AreEqual(goodTrade.ExitBar,testTrade.ExitBar,"ExitBar at " + i);
//				Assert.AreEqual(goodTrade.ExitPrice,testTrade.ExitPrice,"ExitPrice at " + i);
//				Assert.AreEqual(goodTrade.ExitTime,testTrade.ExitTime,"ExitTime at " + i);
//				Assert.AreEqual(goodTrade.MaxPrice,testTrade.MaxPrice,"MaxPrice at " + i);
//				Assert.AreEqual(goodTrade.MinPrice,testTrade.MinPrice,"MinPrice at " + i);
				Assert.AreEqual(goodInfo.ProfitLoss,testInfo.ProfitLoss,"ProfitLoss at " + i);
				Assert.AreEqual(goodInfo.ClosedEquity,testInfo.ClosedEquity,"ClosedEquity at " + i);
			}
		}
		
		public void VerifyPair(Strategy strategy, int pairNum,
		                       string expectedEntryTime,
		                     double expectedEntryPrice,
		                      string expectedExitTime,
		                     double expectedExitPrice)
		{
			Assert.Greater(strategy.Performance.ComboTrades.Count, pairNum);
    		TransactionPairs pairs = strategy.Performance.ComboTrades;
    		TransactionPair pair = pairs[pairNum];
    		TimeStamp expEntryTime = new TimeStamp(expectedEntryTime);
    		Assert.AreEqual( expEntryTime, pair.EntryTime, "Pair " + pairNum + " Entry");
    		Assert.AreEqual( expectedEntryPrice, pair.EntryPrice, "Pair " + pairNum + " Entry");
    		
    		Assert.AreEqual( new TimeStamp(expectedExitTime), pair.ExitTime, "Pair " + pairNum + " Exit");
    		Assert.AreEqual( expectedExitPrice, pair.ExitPrice, "Pair " + pairNum + " Exit");
    		
    		double direction = pair.Direction;
		}
   		
		public void HistoricalShowChart()
        {
       		log.Debug("HistoricalShowChart() start.");
	       	try {
				for( int i=chartThreads.Count-1; i>=0; i--) {
					chartThreads[i].PortfolioDoc.ShowInvoke();
					chartThreads[i].PortfolioDoc.HideInvoke();
				}
        	} catch( Exception ex) {
        		log.Debug(ex.ToString());
        	}
        }
		
		public void HistoricalCloseCharts()
        {
	       	try {
				for( int i=chartThreads.Count-1; i>=0; i--) {
					chartThreads[i].Stop();
					chartThreads.RemoveAt(i);
				}
        	} catch( Exception ex) {
        		log.Debug(ex.ToString());
        	}
       		log.Debug("HistoricalShowChart() finished.");
        }
		
   		public TickZoom.Api.Chart HistoricalCreateChart()
        {
 			int oldCount = chartThreads.Count;
        	try {
 				ChartThread chartThread = new ChartThread();
	        	chartThreads.Add( chartThread);
        	} catch( Exception ex) {
        		log.Notice(ex.ToString());
        	}
 			return chartThreads[oldCount].PortfolioDoc.ChartControl;
        }
   		
   		public int ChartCount {
   			get { return chartThreads.Count; }
		}
   		
   		public ChartControl GetChart( string symbol) {
   			ChartControl chart;
   			for( int i=0; i<chartThreads.Count; i++) {
				chart = GetChart(i);
				if( chart.Symbol.Symbol == symbol) {
					return chart;
				}
   			}
   			return null;
   		}

   		public ChartControl GetChart( int num) {
   			return chartThreads[num].PortfolioDoc.ChartControl;
   		}
		
		public string DataFolder {
			get { return dataFolder; }
			set { dataFolder = value; }
		}
		
		public void VerifyChartBarCount(string symbol, int expectedCount) {
			ChartControl chart = GetChart(symbol);
     		GraphPane pane = chart.DataGraph.MasterPane.PaneList[0];
    		Assert.IsNotNull(pane.CurveList);
    		Assert.Greater(pane.CurveList.Count,0);
    		Assert.AreEqual(symbol,chart.Symbol);
    		Assert.AreEqual(expectedCount,chart.StockPointList.Count,"Stock point list");
    		Assert.AreEqual(expectedCount,pane.CurveList[0].Points.Count,"Chart Curve");
		}
   		
		public static void CompareChart(Strategy strategy, ChartControl chart) {
     		GraphPane pane = chart.DataGraph.MasterPane.PaneList[0];
    		Assert.IsNotNull(pane.CurveList);
    		Assert.Greater(pane.CurveList.Count,0);
    		OHLCBarItem chartBars = (OHLCBarItem) pane.CurveList[0];
			Bars strategyBars = strategy.Bars;
			int firstMisMatch = int.MaxValue;
			int i, j;
    		for( i=0; i<strategyBars.Count; i++) {
				j=chartBars.NPts-i-1;
				if( j < 0 || j >= chartBars.NPts) {
					log.Debug("bar " + i + " is missing");
				} else {
	    			StockPt bar = (StockPt) chartBars[j];
	    			string match = "NOT match";
	    			if( strategyBars.Open[i] == bar.Open &&
	    			   strategyBars.High[i] == bar.High &&
	    			   strategyBars.Low[i] == bar.Low &&
	    			   strategyBars.Close[i] == bar.Close) {
	    			    match = "matches";
	    			} else {
	    				if( firstMisMatch == int.MaxValue) {
	    					firstMisMatch = i;
	    				}
	    			}
	    			log.Debug( "bar: " + i + ", point: " + j + " " + match + " days:"+strategyBars.Open[i]+","+strategyBars.High[i]+","+strategyBars.Low[i]+","+strategyBars.Close[i]+" => "+
	    				              bar.Open+","+bar.High+","+bar.Low+","+bar.Close);
	    			log.Debug( "bar: " + i + ", point: " + j + " " + match + " days:"+strategyBars.Time[i]+" "+
	    			              DateTime.FromOADate(bar.X));
				}
    		}
			if( firstMisMatch != int.MaxValue) {
				i = firstMisMatch;
				j=chartBars.NPts-i-1;
    			StockPt bar = (StockPt) chartBars[j];
    			Assert.AreEqual(strategyBars.Open[i],bar.Open,"Open for bar " + i + ", point " + j);
    			Assert.AreEqual(strategyBars.High[i],bar.High,"High for bar " + i + ", point " + j);
    			Assert.AreEqual(strategyBars.Low[i],bar.Low,"Low for bar " + i + ", point " + j);
    			Assert.AreEqual(strategyBars.Close[i],bar.Close,"Close for bar " + i + ", point " + j);
			}
   		}
			
		public void CompareChartCount(Strategy strategy) {
			ChartControl chart = GetChart(strategy.SymbolDefault);
     		GraphPane pane = chart.DataGraph.MasterPane.PaneList[0];
    		Assert.IsNotNull(pane.CurveList);
    		Assert.Greater(pane.CurveList.Count,0);
    		OHLCBarItem bars = (OHLCBarItem) pane.CurveList[0];
			Bars days = strategy.Days;
			Assert.AreEqual(strategy.SymbolDefault,chart.Symbol);
    		Assert.AreEqual(days.BarCount,chart.StockPointList.Count,"Stock point list");
			Assert.AreEqual(days.BarCount,bars.NPts,"Chart Points");
		}
   		
	}

}