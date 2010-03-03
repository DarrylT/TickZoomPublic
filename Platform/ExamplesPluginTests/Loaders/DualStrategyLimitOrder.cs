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
using TickZoom;
using TickZoom.Api;
using TickZoom.Common;

namespace Loaders
{
	[TestFixture]
	public class DualStrategyLimitOrder : StrategyTest
	{
		Log log = Factory.Log.GetLogger(typeof(ExampleSimulatedTest));
		Portfolio portfolio;
		Strategy strategy1;
		Strategy strategy2;
		public DualStrategyLimitOrder() {
			Symbols = "USD/JPY,EUR/USD";
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
			} catch( Exception ex) {
				log.Error("Setup error.", ex);
				throw;
			}
		}
		
		[Test]
		public void VerifyCurrentEquity() {
			Assert.AreEqual( 5887.80D,portfolio.Performance.Equity.CurrentEquity,"current equity");
		}
		[Test]
		public void VerifyOpenEquity() {
			Assert.AreEqual( -493.20D,portfolio.Performance.Equity.OpenEquity,"open equity");
		}
		[Test]
		public void VerifyClosedEquity() {
			Assert.AreEqual( 6381,portfolio.Performance.Equity.ClosedEquity,"closed equity");
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
			foreach( var symbol in properties.Starter.SymbolProperties) {
				string name = "ExampleOrderStrategy+" + symbol.Symbol;
				Strategy strategy = CreateStrategy("ExampleOrderStrategy",name);
				strategy.SymbolDefault = symbol.Symbol;
				AddDependency("Portfolio",strategy);
			}
			TopModel = GetPortfolio("Portfolio");
		}
	}
}
