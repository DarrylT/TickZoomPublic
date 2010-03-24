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
using System.Threading;

using NUnit.Framework;
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
		
		public ProviderTests() {
			string providerAssembly = Factory.Settings["ProviderAssembly"];
			if( !string.IsNullOrEmpty(providerAssembly)) {
				SetProviderAssembly( providerAssembly);
			}
			SyncTicks.Enabled = true;
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
		public virtual void TearDown() {
			if(debug) log.Debug("TearDown");
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
			Assert.AreEqual(0,Factory.Parallel.Tasks.Length,"running tasks");
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
	  		
	  		// Wait for it to switch out of real time or historical mode.
	  		var expectedState = ReceiverState.Ready;
	  		var actualState = verify.VerifyState(expectedState,symbol,5);
	  		Assert.AreEqual(expectedState,actualState,"after receiving a StopSymbol event, if your provider plugin was sending ticks then it must return either respond with an EndHistorical or EndRealTime event. If it has already sent one of those prior to the StopSymbol, then no reponse is required.");
	  		
	  		count = verify.Verify(0,assertTick,symbol,5);
	  		Assert.AreEqual(0,count,"your provider plugin must not send any more ticks after receiving a StopSymbol event.");
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
  			Assert.AreEqual(1,processes.Length,"Number of provider service processes.");
		}

		private void CreateEntry( OrderType orderType, double desiredPositions, double actualPosition) {
			CreateOrder(TradeDirection.Entry,orderType,desiredPositions,actualPosition);
		}
		private void CreateExit( OrderType orderType, double desiredPositions, double actualPosition) {
			CreateOrder(TradeDirection.Exit,orderType,desiredPositions,actualPosition);
		}
		private void CreateOrder( TradeDirection tradeDirection, OrderType orderType, double desiredPositions, double actualPosition) {
  			List<LogicalOrder> list = new List<LogicalOrder>();
  			LogicalOrder order = Factory.Engine.LogicalOrder(symbol,null);
  			order.TradeDirection = tradeDirection;
  			order.Type = orderType;
  			order.Positions = desiredPositions;
  			order.IsActive = true;
  			list.Add(order);
  			provider.SendEvent(verify,symbol,(int)EventType.PositionChange,new PositionChangeDetail(symbol,actualPosition,list));
		}
		
		[Test]
		public void TestMarketOrder() {
			int secondsDelay = 25;
			if(debug) log.Debug("===DemoConnectionTest===");
			provider.SendEvent(verify,symbol,(int)EventType.StartSymbol,new StartSymbolDetail(TimeStamp.MinValue));
  			long count = verify.Verify(1,assertTick,symbol,25);
  			double desiredPosition = 2;
  			log.Warn("Sending 1");
  			CreateEntry(OrderType.BuyMarket,desiredPosition,0);
  			double actualPosition = verify.VerifyPosition(desiredPosition,symbol,secondsDelay);
  			Assert.AreEqual(desiredPosition,actualPosition,"position");

  			desiredPosition = 0;
  			log.Warn("Sending 2");
  			CreateExit(OrderType.SellMarket,desiredPosition,actualPosition);
  			actualPosition = verify.VerifyPosition(desiredPosition,symbol,secondsDelay);
  			Assert.AreEqual(desiredPosition,actualPosition,"position");

  			desiredPosition = 2;
  			log.Warn("Sending 3");
  			CreateEntry(OrderType.BuyMarket,desiredPosition,actualPosition);
  			actualPosition = verify.VerifyPosition(desiredPosition,symbol,secondsDelay);
  			Assert.AreEqual(desiredPosition,actualPosition,"position");

  			desiredPosition = 2;
  			log.Warn("Sending 4");
  			CreateEntry(OrderType.BuyMarket,desiredPosition,actualPosition);
  			actualPosition = verify.VerifyPosition(desiredPosition,symbol,secondsDelay);
  			Assert.AreEqual(desiredPosition,actualPosition,"position");
  			
  			desiredPosition = 0;
  			log.Warn("Sending 5");
  			CreateExit(OrderType.SellMarket,desiredPosition,actualPosition);
  			actualPosition = verify.VerifyPosition(desiredPosition,symbol,secondsDelay);
  			Assert.AreEqual(desiredPosition,actualPosition,"position");
		}
		
		[Test]
		public void TestSignalChanges() {
			int secondsDelay = 25;
			if(debug) log.Debug("===DemoConnectionTest===");
			provider.SendEvent(verify,symbol,(int)EventType.StartSymbol,new StartSymbolDetail(TimeStamp.MinValue));
  			double expectedPosition = 5;
  			provider.SendEvent(verify,symbol,(int)EventType.PositionChange,new PositionChangeDetail(symbol,expectedPosition,null));
  			double position = verify.VerifyPosition(expectedPosition,symbol,secondsDelay);
  			Assert.AreEqual(expectedPosition,position,"position");
  			expectedPosition = 0;
  			provider.SendEvent(verify,symbol,(int)EventType.PositionChange,new PositionChangeDetail(symbol,expectedPosition,null));
  			position = verify.VerifyPosition(expectedPosition,symbol,secondsDelay);
  			Assert.AreEqual(expectedPosition,position,"position");

  			expectedPosition = 5;
  			provider.SendEvent(verify,symbol,(int)EventType.PositionChange,new PositionChangeDetail(symbol,expectedPosition,null));
  			position = verify.VerifyPosition(expectedPosition,symbol,secondsDelay);
  			Assert.AreEqual(expectedPosition,position,"position");

  			expectedPosition = 5;
  			provider.SendEvent(verify,symbol,(int)EventType.PositionChange,new PositionChangeDetail(symbol,expectedPosition,null));
  			position = verify.VerifyPosition(expectedPosition,symbol,secondsDelay);
  			Assert.AreEqual(expectedPosition,position,"position");
		}
		
		[Test]
		public void TestLogicalOrders() {
			provider.SendEvent(verify,symbol,(int)EventType.StartSymbol,new StartSymbolDetail(TimeStamp.MinValue));
  			provider.SendEvent(verify,symbol,(int)EventType.PositionChange,new PositionChangeDetail(symbol,0,orders));
  			long count = verify.Verify(1,assertTick,symbol,25);
			CreateLogicalEntry(OrderType.BuyLimit,15.12,3);
			CreateLogicalEntry(OrderType.SellLimit,34.12,3);
			CreateLogicalExit(OrderType.SellLimit,40.12);
			CreateLogicalExit(OrderType.SellStop,5.12);
			CreateLogicalExit(OrderType.BuyLimit,10.12);
			CreateLogicalExit(OrderType.BuyStop,45.12);
  			provider.SendEvent(verify,symbol,(int)EventType.PositionChange,new PositionChangeDetail(symbol,0,orders));
  			count = verify.Verify(2,assertTick,symbol,25);
  			Assert.GreaterOrEqual(count,2,"tick count");
  			Thread.Sleep(2000);
		}
		
		[Test]
		public void TestSpecificLogicalOrder() {
			provider.SendEvent(verify,symbol,(int)EventType.StartSymbol,new StartSymbolDetail(TimeStamp.MinValue));
  			provider.SendEvent(verify,symbol,(int)EventType.PositionChange,new PositionChangeDetail(symbol,0,orders));
			CreateLogicalEntry(OrderType.BuyLimit,503.72,4);
  			provider.SendEvent(verify,symbol,(int)EventType.PositionChange,new PositionChangeDetail(symbol,0,orders));
  			long count = verify.Verify(2,assertTick,symbol,25);
  			Assert.GreaterOrEqual(count,2,"tick count");
  			 Thread.Sleep(2000);
		}
		
		public void AssertLevel1( TickIO tick, TickIO lastTick, ulong symbol) {
        	Assert.IsTrue(tick.IsQuote || tick.IsTrade);
        	if( tick.IsQuote) {
	        	Assert.Greater(tick.Bid,0);
//	        	Assert.Greater(tick.BidLevel(0),0);
	        	Assert.Greater(tick.Ask,0);
//	        	Assert.Greater(tick.AskLevel(0),0);
        	}
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
		
		public static Log Log {
			get { return log; }
		}
	}
}
