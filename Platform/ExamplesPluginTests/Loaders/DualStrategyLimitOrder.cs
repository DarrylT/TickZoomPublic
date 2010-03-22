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
using NUnit.Framework;
using System.Threading;
using TickZoom;
using TickZoom.Api;
using TickZoom.Common;

namespace Loaders
{
	[TestFixture]
	public class DualStrategyLimitOrder : StrategyTest
	{
		Log log = Factory.Log.GetLogger(typeof(DualStrategyLimitOrder));
		Portfolio portfolio;
		Strategy strategy1;
		Strategy strategy2;
		public DualStrategyLimitOrder() {
			Symbols = "USD/JPY,EUR/USD";
			ShowCharts = false;
			StoreKnownGood = false;
			BreakPoint.SetBarBreakPoint(15);
			BreakPoint.SetSymbolConstraint("EUR/USD");
		}
			
		[TestFixtureSetUp]
		public override void RunStrategy() {
			base.RunStrategy();
			try {
				Starter starter = CreateStarter();
				
				// Set run properties as in the GUI.
				starter.ProjectProperties.Starter.StartTime = new TimeStamp(1800,1,1);
	    		starter.ProjectProperties.Starter.EndTime = new TimeStamp(2009,06,10);
	    		starter.DataFolder = "TestData";
	    		starter.ProjectProperties.Starter.Symbols = Symbols;
				starter.ProjectProperties.Starter.IntervalDefault = Intervals.Minute1;
	    		starter.CreateChartCallback = new CreateChartCallback(HistoricalCreateChart);
	    		starter.ShowChartCallback = new ShowChartCallback(HistoricalShowChart);
				// Run the loader.
				TestDualStrategyLoader loader = new TestDualStrategyLoader();
	    		starter.Run(loader);
	
	    		// Get the stategy
	    		portfolio = loader.TopModel as Portfolio;
	    		strategy1 = portfolio.Strategies[0];
	    		strategy2 = portfolio.Strategies[1];
	    		LoadTrades();
	    		LoadBarData();
			} catch( Exception ex) {
				log.Error("Setup error.", ex);
				throw;
			}
		}
		
		[Test]
		public void VerifyCurrentEquity() {
			Assert.AreEqual( 6340.30D,Math.Round(portfolio.Performance.Equity.CurrentEquity,2),"current equity");
		}
		[Test]
		public void VerifyOpenEquity() {
			Assert.AreEqual( -496.40D,portfolio.Performance.Equity.OpenEquity,"open equity");
		}
		[Test]
		public void VerifyClosedEquity() {
			Assert.AreEqual( 6836.70,Math.Round(portfolio.Performance.Equity.ClosedEquity,2),"closed equity");
		}
		[Test]
		public void VerifyStartingEquity() {
			Assert.AreEqual( 10000,portfolio.Performance.Equity.StartingEquity,"starting equity");
		}
		
		[Test]
		public void VerifyStrategy1Trades() {
			VerifyTrades(strategy1);
		}
	
		[Test]
		public void VerifyStrategy2Trades() {
			VerifyTrades(strategy2);
		}
		
		[Test]
		public void VerifyStrategy1TradeCount() {
			VerifyTradeCount(strategy1);
		}
		
		[Test]
		public void VerifyStrategy2TradeCount() {
			VerifyTradeCount(strategy2);
		}
		
		[Test]
		public void VerifyStrategy1BarData() {
			VerifyBarData(strategy1);
		}
		
		[Test]
		public void VerifyStrategy2BarData() {
			VerifyBarData(strategy2);
		}
		
		[Test]
		public void VerifyStrategy1BarDataCount() {
			VerifyBarDataCount(strategy1);
		}
		
		[Test]
		public void VerifyStrategy2BarDataCount() {
			VerifyBarDataCount(strategy2);
		}
		
		[Test]
		public void CompareBars() {
			CompareChart(portfolio,GetChart(portfolio.SymbolDefault));
		}
	}
	
	public class TestDualStrategyLoader : ModelLoaderCommon
	{
		public TestDualStrategyLoader() {
			/// <summary>
			/// IMPORTANT: You can personalize the name of each model loader.
			/// </summary>
			category = "Example";
			name = "Dual Symbol";
			this.IsVisibleInGUI = false;
		}
		
		public override void OnInitialize(ProjectProperties properties) {
		}
		
		public override void OnLoad(ProjectProperties properties) {
			properties.Engine.RealtimeOutput = false;
			foreach( var symbol in properties. Starter.SymbolProperties) {
				string name = "ExampleOrderStrategy+" + symbol.Symbol;
				ExampleOrderStrategy strategy = (ExampleOrderStrategy) CreateStrategy("ExampleOrderStrategy",name);
				strategy.Multiplier = 10D;
				strategy.SymbolDefault = symbol.Symbol;
				AddDependency("Portfolio",strategy);
			}
			TopModel = GetPortfolio("Portfolio");
		}
	}
}
