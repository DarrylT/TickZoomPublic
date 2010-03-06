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
 * <http://www.tickzoom.org/wiki/Licenses>.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using NUnit.Framework;
using System.Threading;
using TickZoom.Api;

namespace TickZoom.Test
{
	public abstract class ProviderTests
	{
		private static readonly Log log = Factory.Log.GetLogger(typeof(TimeAndSales));
		private static readonly bool debug = log.IsDebugEnabled;		
		private List<LogicalOrder> orders = new List<LogicalOrder>();
		private Provider provider;
		protected SymbolInfo symbol;
		protected VerifyFeed verify;
		public enum TickTest {
			TimeAndSales,
			Level1
		}
			
		[TestFixtureSetUp]
		public void Init()
		{
			string appData = Factory.Settings["AppDataFolder"];
			File.Delete( appData + @"\Logs\"+providerAssembly+"Tests.log");
			File.Delete( appData + @"\Logs\"+providerAssembly+".log");
			
		}
		
		public void SetSymbol( string symbolString) {
			symbol = Factory.Symbol.LookupSymbol(symbolString);
		}
		
		private string providerAssembly = "TickZoomProviderMock";
		
		public void SetProviderAssembly( string providerAssembly) {
			this.providerAssembly = providerAssembly;	
		}
		
		public abstract Provider ProviderFactory();
		
		public void CreateProvider(bool inProcessFlag) {
			if( inProcessFlag) {
				provider = ProviderFactory();
			} else {
				provider = Factory.Provider.ProviderProcess("127.0.0.1",6492,providerAssembly+".exe");
			}
			verify = Factory.Utility.VerifyFeed();
			provider.SendEvent(verify,null,(int)EventType.Connect,null);
		}
		
		[SetUp]
		public void Setup() {
			CreateProvider(true);
		}
		
		[TearDown]
		public void TearDown() {
	  		provider.SendEvent(verify,null,(int)EventType.Disconnect,null);	
	  		provider.SendEvent(verify,null,(int)EventType.Terminate,null);		
			int start = Environment.TickCount;
			int elapsed = 0;
			if( Factory.Parallel.Tasks.Length > 0) {
				log.Warn("Found " + Factory.Parallel.Tasks.Length + " Parallel tasks still running...");
			}
			while( elapsed < 10000 && Factory.Parallel.Tasks.Length > 0) {
				Thread.Sleep(1000);
				elapsed = Environment.TickCount - start;
			}
			if( Factory.Parallel.Tasks.Length > 0) {
				log.Error("These tasks still running after " + elapsed + "ms.");
				log.Error(Factory.Parallel.GetStats());
			}
		}
		
		[Test]
		public void DemoConnectionTest() {
			if(debug) log.Debug("===DemoConnectionTest===");
			if(debug) log.Debug("===StartSymbol===");
			provider.SendEvent(verify,symbol,(int)EventType.StartSymbol,new StartSymbolDetail(TimeStamp.MinValue));
			if(debug) log.Debug("===VerifyFeed===");
	  		long count = verify.Verify(2,assertTick,symbol,25);
	  		Assert.GreaterOrEqual(count,2,"tick count");
		}
		
		Action<TickIO, TickIO, ulong> assertTick;
			
		[Test]		
		public void DemoStopSymbolTest() {
			if(debug) log.Debug("===DemoConnectionTest===");
			if(debug) log.Debug("===StartSymbol===");
			provider.SendEvent(verify,symbol,(int)EventType.StartSymbol,new StartSymbolDetail(TimeStamp.MinValue));
			if(debug) log.Debug("===VerifyFeed===");
	  		long count = verify.Verify(2,assertTick,symbol,25);
	  		Assert.GreaterOrEqual(count,2,"tick count");
			if(debug) log.Debug("===StopSymbol===");
	  		provider.SendEvent(verify,symbol,(int)EventType.StopSymbol,null);
	  		verify.TickQueue.Clear();
	  		count = verify.Verify(0,assertTick,symbol,10);
	  		Assert.AreEqual(0,count,"tick count");
		}
	
		[Test]
		public void DemoReConnectionTest() {
			provider.SendEvent(verify,symbol,(int)EventType.StartSymbol,new StartSymbolDetail(TimeStamp.MinValue));
	  		long count = verify.Verify(2,assertTick,symbol,25);
  			Assert.GreaterOrEqual(count,2,"tick count");
  			provider.SendEvent(verify,null,(int)EventType.Terminate,null);
  			CreateProvider(true);
  			provider.SendEvent(verify,symbol,(int)EventType.StartSymbol,new StartSymbolDetail(TimeStamp.MinValue));
  			count = verify.Verify(2,assertTick,symbol,25);
  			Assert.GreaterOrEqual(count,2,"tick count");
		}
	
		[Test]
		public void TestSeperateProcess() {
			provider.SendEvent(verify,null,(int)EventType.Terminate,null);
			CreateProvider(false);
			provider.SendEvent(verify,symbol,(int)EventType.StartSymbol,new StartSymbolDetail(TimeStamp.MinValue));
			if(debug) log.Debug("===VerifyFeed===");
  			long count = verify.Verify(2,assertTick,symbol,25);
  			Assert.GreaterOrEqual(count,2,"tick count");
  			Process[] processes = Process.GetProcessesByName(providerAssembly);
  			Assert.AreEqual(1,processes.Length,"Number of MBTradingService processes.");
		}
		
		[Test]
		public void TestMarketOrder() {
			int secondsDelay = 25;
			if(debug) log.Debug("===DemoConnectionTest===");
			provider.SendEvent(verify,symbol,(int)EventType.StartSymbol,new StartSymbolDetail(TimeStamp.MinValue));
  			double expectedPosition = 150;
  			provider.SendEvent(verify,symbol,(int)EventType.PositionChange,new PositionChangeDetail(expectedPosition,null));
  			double position = verify.VerifyPosition(expectedPosition,symbol,secondsDelay);
  			Assert.AreEqual(expectedPosition,position,"position");
  			expectedPosition = 0;
  			provider.SendEvent(verify,symbol,(int)EventType.PositionChange,new PositionChangeDetail(expectedPosition,null));
  			position = verify.VerifyPosition(expectedPosition,symbol,secondsDelay);
  			Assert.AreEqual(expectedPosition,position,"position");

  			expectedPosition = 150;
  			provider.SendEvent(verify,symbol,(int)EventType.PositionChange,new PositionChangeDetail(expectedPosition,null));
  			position = verify.VerifyPosition(expectedPosition,symbol,secondsDelay);
  			Assert.AreEqual(expectedPosition,position,"position");

  			expectedPosition = 150;
  			provider.SendEvent(verify,symbol,(int)EventType.PositionChange,new PositionChangeDetail(expectedPosition,null));
  			position = verify.VerifyPosition(expectedPosition,symbol,secondsDelay);
  			Assert.AreEqual(expectedPosition,position,"position");
		}
		
		[Test]
		public void TestLogicalOrders() {
			provider.SendEvent(verify,symbol,(int)EventType.StartSymbol,new StartSymbolDetail(TimeStamp.MinValue));
  			provider.SendEvent(verify,symbol,(int)EventType.PositionChange,new PositionChangeDetail(0,orders));
  			long count = verify.Verify(1,assertTick,symbol,25);
			CreateLogicalEntry(OrderType.BuyLimit,15.12,1000);
			CreateLogicalEntry(OrderType.SellLimit,34.12,1000);
			CreateLogicalExit(OrderType.SellLimit,40.12);
			CreateLogicalExit(OrderType.SellStop,5.12);
			CreateLogicalExit(OrderType.BuyLimit,10.12);
			CreateLogicalExit(OrderType.BuyStop,45.12);
  			provider.SendEvent(verify,symbol,(int)EventType.PositionChange,new PositionChangeDetail(0,orders));
  			count = verify.Verify(2,assertTick,symbol,25);
  			Assert.GreaterOrEqual(count,2,"tick count");
  			Thread.Sleep(2000);
		}
		
		public void AssertLevel1( TickIO tick, TickIO lastTick, ulong symbol) {
        	Assert.IsTrue(tick.IsQuote);
        	if( tick.IsQuote) {
	        	Assert.Greater(tick.Bid,0);
//	        	Assert.Greater(tick.BidLevel(0),0);
	        	Assert.Greater(tick.Ask,0);
//	        	Assert.Greater(tick.AskLevel(0),0);
        	}
        	Assert.IsFalse(tick.IsTrade);
        	if( tick.IsTrade) {
	        	Assert.Greater(tick.Price,0);
    	    	Assert.Greater(tick.Size,0);
        	}
    		Assert.IsTrue(tick.Time>=lastTick.Time,"tick.Time > lastTick.Time");
    		Assert.AreEqual(symbol,tick.lSymbol);
		}
		
		public void AssertTimeAndSales( TickIO tick, TickIO lastTick, ulong symbol) {
        	Assert.IsFalse(tick.IsQuote);
        	if( tick.IsQuote) {
	        	Assert.Greater(tick.Bid,0);
	        	Assert.Greater(tick.BidLevel(0),0);
	        	Assert.Greater(tick.Ask,0);
	        	Assert.Greater(tick.AskLevel(0),0);
        	}
        	Assert.IsTrue(tick.IsTrade);
        	if( tick.IsTrade) {
	        	Assert.Greater(tick.Price,0);
    	    	Assert.Greater(tick.Size,0);
        	}
    		Assert.IsTrue(tick.Time>=lastTick.Time,"tick.Time > lastTick.Time");
    		Assert.AreEqual(symbol,tick.lSymbol);
		}
		
		public void SetTickTest( TickTest test) {
			switch( test) {
				case TickTest.Level1:
					assertTick = AssertLevel1;
					break;
				case TickTest.TimeAndSales:
					assertTick = AssertTimeAndSales;
					break;
			}
		}
		
		public LogicalOrder CreateLogicalEntry(OrderType type, double price, int size) {
			LogicalOrder logical = Factory.Engine.LogicalOrder(symbol,null);
			logical.IsActive = true;
			logical.TradeDirection = TradeDirection.Entry;
			logical.Type = type;
			logical.Price = price;
			logical.Positions = size;
			orders.Add(logical);
			return logical;
		}
		
		public LogicalOrder CreateLogicalExit(OrderType type, double price) {
			LogicalOrder logical = Factory.Engine.LogicalOrder(symbol,null);
			logical.IsActive = true;
			logical.TradeDirection = TradeDirection.Exit;
			logical.Type = type;
			logical.Price = price;
			orders.Add(logical);
			return logical;
		}
	}
}
