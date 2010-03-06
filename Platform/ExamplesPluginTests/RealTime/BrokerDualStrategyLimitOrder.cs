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
using System.Configuration;
using Loaders;
using NUnit.Framework;
using System.IO;
using TickZoom;
using TickZoom.Api;
using TickZoom.Common;
using ZedGraph;

namespace MockProvider
{
	[TestFixture]
	public class ExperimentWithEnums {
		[Test]
		public void TestCastingEnum() {
			EventType eventType = (EventType) 405;
			
			switch( eventType) {
				case EventType.EndHistorical:
					break;
				default:
					break;
			}
			
			eventType = (EventType) 3;
			
			switch( eventType) {
				case EventType.EndHistorical:
					break;
				default:
					break;
			}
		}
	}
	[TestFixture]
	public class BrokerDualStrategyLimitOrder : DualStrategyLimitOrder {
		
		public BrokerDualStrategyLimitOrder() {
			ConfigurationManager.AppSettings.Set("ProviderAddress","InProcess");
			SyncTicks.Enabled = true;
			DeleteFiles();
			Symbols = "USD/JPY,EUR/USD";
//			BreakPoint.SetEngineConstraint();
//			BreakPoint.SetBarBreakPoint("2009-06-09 10:30:55.267");
			BreakPoint.SetBarBreakPoint(15);
			BreakPoint.SetSymbolConstraint("EUR/USD");
			ShowCharts = true;
		}
		
		public override Starter CreateStarter()
		{
			return new RealTimeStarter();
		}
		
		private void DeleteFiles() {
			while( true) {
				try {
					string appData = Factory.Settings["AppDataFolder"];
		 			File.Delete( appData + @"\TestServerCache\EURUSD_Tick.tck");
		 			File.Delete( appData + @"\TestServerCache\USDJPY_Tick.tck");
					break;
				} catch( Exception) {
				}
			}
		}
		
		[Test]
		public void CheckMockTradeCount() {
			Assert.AreEqual(34,SyncTicks.MockTradeCount);
		}
	}
}
