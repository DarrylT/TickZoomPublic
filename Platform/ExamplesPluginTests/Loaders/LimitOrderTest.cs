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
	public class LimitOrderTest : StrategyTest
	{
		Log log = Factory.Log.GetLogger(typeof(ExampleSimulatedTest));
		ExampleOrderStrategy strategy;
		public LimitOrderTest() {
			Symbols = "USD/JPY";
			StoreKnownGood = false;
			ShowCharts = false;
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
				TestLimitOrderLoader loader = new TestLimitOrderLoader();
	    		starter.Run(loader);
	
	    		// Get the stategy
	    		strategy = loader.TopModel as ExampleOrderStrategy;
	    		LoadTrades();
			} catch( Exception ex) {
				log.Error("Setup error.", ex);
				throw;
			}
		}
		
		[Test]
		public void VerifyCurrentEquity() {
			Assert.AreEqual( 6380,strategy.Performance.Equity.CurrentEquity,"current equity");
		}
		[Test]
		public void VerifyOpenEquity() {
			Assert.AreEqual( -490,strategy.Performance.Equity.OpenEquity,"open equity");
		}
		[Test]
		public void VerifyClosedEquity() {
			Assert.AreEqual( 6870,strategy.Performance.Equity.ClosedEquity,"closed equity");
		}
		[Test]
		public void VerifyStartingEquity() {
			Assert.AreEqual( 10000,strategy.Performance.Equity.StartingEquity,"starting equity");
		}
		
		[Test]
		public void VerifyTrades() {
			VerifyTrades(strategy);
		}
	
		[Test]
		public void VerifyTradeCount() {
			VerifyTradeCount(strategy);
		}
		
		[Test]
		public void CompareBars() {
			CompareChart(strategy,GetChart(strategy.SymbolDefault));
		}
	}
	
	public class TestLimitOrderLoader : ModelLoaderCommon
	{
		public TestLimitOrderLoader() {
			/// <summary>
			/// IMPORTANT: You can personalize the name of each model loader.
			/// </summary>
			category = "Example";
			name = "Limit Order";
			this.IsVisibleInGUI = false;
		}
		
		public override void OnInitialize(ProjectProperties properties) {
		}
		
		public override void OnLoad(ProjectProperties properties) {
			ExampleOrderStrategy strategy = (ExampleOrderStrategy) CreateStrategy("ExampleOrderStrategy",name);
			strategy.Multiplier = 10D;
			TopModel = strategy;
		}
	}
}
