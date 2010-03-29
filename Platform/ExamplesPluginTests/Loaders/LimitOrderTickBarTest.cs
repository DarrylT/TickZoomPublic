﻿#region Copyright
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
	public class LimitOrderTickBarTest : StrategyTest
	{
		Log log = Factory.Log.GetLogger(typeof(LimitOrderTickBarTest));
		ExampleOrderStrategy strategy;
		public LimitOrderTickBarTest() {
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
	    		starter.ProjectProperties.Starter.IntervalDefault = Factory.Engine.DefineInterval(BarUnit.Tick,25);
	    		starter.CreateChartCallback = new CreateChartCallback(HistoricalCreateChart);
	    		starter.ShowChartCallback = new ShowChartCallback(HistoricalShowChart);
				// Run the loader.
				TestLimitOrderLoader loader = new TestLimitOrderLoader();
	    		starter.Run(loader);
	
	    		// Get the stategy
	    		strategy = loader.TopModel as ExampleOrderStrategy;
	    		LoadTrades();
	    		LoadBarData();
			} catch( Exception ex) {
				log.Error("Setup error.", ex);
				throw;
			}
		}
		
		[Test]
		public void VerifyCurrentEquity() {
			Assert.AreEqual( 7210,strategy.Performance.Equity.CurrentEquity,"current equity");
		}
		[Test]
		public void VerifyOpenEquity() {
			Assert.AreEqual( 140,strategy.Performance.Equity.OpenEquity,"open equity");
		}
		[Test]
		public void VerifyClosedEquity() {
			Assert.AreEqual( 7070,strategy.Performance.Equity.ClosedEquity,"closed equity");
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
		public void VerifyBarData() {
			VerifyBarData(strategy);
		}
		
		[Test]
		public void CompareBars() {
			CompareChart(strategy,GetChart(strategy.SymbolDefault));
		}
	}
}
