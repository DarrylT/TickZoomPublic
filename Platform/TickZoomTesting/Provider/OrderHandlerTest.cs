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
 * Date: 8/13/2009
 * Time: 3:59 PM
 * <http://www.tickzoom.org/wiki/Licenses>.
 */
#endregion


using System;
using System.Collections.Generic;
using NUnit.Framework;
using TickZoom.Api;

namespace Orders
{
	[TestFixture]
	public class OrderHandlerTest {
		SymbolInfo symbol = Factory.Symbol.LookupSymbol("CSCO");
		List<LogicalOrder> orders = new List<LogicalOrder>();
		TestOrderHandler handler;
		
		public OrderHandlerTest() {
			handler = new TestOrderHandler(symbol);
		}
		
		[SetUp]
		public void Setup() {
			orders.Clear();
		}
		
		public int CreateLogicalEntry(OrderType type, double price, int size) {
			LogicalOrder logical = Factory.Engine.LogicalOrder(symbol,null);
			logical.IsActive = true;
			logical.TradeDirection = TradeDirection.Entry;
			logical.Type = type;
			logical.Price = price;
			logical.Positions = size;
			orders.Add(logical);
			return logical.Id;
		}
		
		public int CreateLogicalExit(OrderType type, double price) {
			LogicalOrder logical = Factory.Engine.LogicalOrder(symbol,null);
			logical.IsActive = true;
			logical.TradeDirection = TradeDirection.Exit;
			logical.Type = type;
			logical.Price = price;
			orders.Add(logical);
			return logical.Id;
		}
		
		[Test]
		public void Test01FlatZeroOrders() {
			handler.ClearPhysicalOrders();
			
			int buyLimitId = CreateLogicalEntry(OrderType.BuyLimit,234.12,1000);
			int buyStopId = CreateLogicalEntry(OrderType.SellStop,154.12,1000);
			CreateLogicalExit(OrderType.SellLimit,334.12);
			CreateLogicalExit(OrderType.SellStop,134.12);
			CreateLogicalExit(OrderType.BuyLimit,124.12);
			CreateLogicalExit(OrderType.BuyStop,194.12);
			
			double position = 0;
			handler.SetActualPosition(position);
			handler.SetDesiredPosition(position);
			handler.SetLogicalOrders(orders);
			handler.PerformCompare();
			
			Assert.AreEqual(0,handler.CanceledOrders.Count);
			Assert.AreEqual(0,handler.ChangedOrders.Count);
			Assert.AreEqual(2,handler.CreatedOrders.Count);
			
			PhysicalOrder order = handler.CreatedOrders[0];
			Assert.AreEqual(OrderType.BuyLimit,order.Type);
			Assert.AreEqual(234.12,order.Price);
			Assert.AreEqual(1000,order.Size);
			Assert.AreEqual(buyLimitId,order.LogicalOrderId);
			Assert.IsNull(order.BrokerOrder);

			order = handler.CreatedOrders[1];
			Assert.AreEqual(OrderType.SellStop,order.Type);
			Assert.AreEqual(154.12,order.Price);
			Assert.AreEqual(1000,order.Size);
			Assert.AreEqual(buyStopId,order.LogicalOrderId);
			Assert.IsNull(order.BrokerOrder);
		}
		
		[Test]
		public void Test02FlatTwoOrders() {
			handler.ClearPhysicalOrders();
			
			int buyLimitId = CreateLogicalEntry(OrderType.BuyLimit,234.12,1000);
			int sellStopId = CreateLogicalEntry(OrderType.SellStop,154.12,1000);
			CreateLogicalExit(OrderType.SellLimit,334.12);
			CreateLogicalExit(OrderType.SellStop,134.12);
			CreateLogicalExit(OrderType.BuyLimit,124.12);
			CreateLogicalExit(OrderType.BuyStop,194.12);
			
			object buyOrder = new object();
			handler.AddPhysicalOrder(OrderType.BuyLimit,234.12,1000,buyLimitId,buyOrder);
			object sellOrder = new object();
			handler.AddPhysicalOrder(OrderType.SellStop,154.12,1000,sellStopId,sellOrder);
			
			double position = 0;
			handler.SetActualPosition(position);
			handler.SetDesiredPosition(position);
			handler.SetLogicalOrders(orders);
			handler.PerformCompare();
			
			Assert.AreEqual(0,handler.CanceledOrders.Count);
			Assert.AreEqual(0,handler.ChangedOrders.Count);
			Assert.AreEqual(0,handler.CreatedOrders.Count);
		}
		
		[Test]
		public void Test03LongEntryFilled() {
			handler.ClearPhysicalOrders();
			
			
			CreateLogicalEntry(OrderType.BuyLimit,234.12,1000);
			int sellStopId = CreateLogicalEntry(OrderType.SellStop,154.12,1000);
			int sellLimitId = CreateLogicalExit(OrderType.SellLimit,334.12);
			int sellStop2Id = CreateLogicalExit(OrderType.SellStop,134.12);
			CreateLogicalExit(OrderType.BuyLimit,124.12);
			CreateLogicalExit(OrderType.BuyStop,194.12);
			
			object sellOrder = new object();
			handler.AddPhysicalOrder(OrderType.SellStop,154.12,1000,sellStopId,sellOrder);
			
			double position = 1000; // Pretend we're flat.
			handler.SetActualPosition(position);
			handler.SetDesiredPosition(position);
			handler.SetLogicalOrders(orders);
			handler.PerformCompare();
			
			Assert.AreEqual(1,handler.CanceledOrders.Count);
			Assert.AreEqual(0,handler.ChangedOrders.Count);
			Assert.AreEqual(2,handler.CreatedOrders.Count);
			
			PhysicalOrder order = handler.CanceledOrders[0];
			Assert.AreEqual(OrderType.SellStop,order.Type);
			Assert.AreEqual(154.12,order.Price);
			Assert.AreEqual(1000,order.Size);
			Assert.AreEqual(sellStopId,order.LogicalOrderId);
			Assert.AreEqual(sellOrder,order.BrokerOrder);
			
			order = handler.CreatedOrders[0];
			Assert.AreEqual(OrderType.SellLimit,order.Type);
			Assert.AreEqual(334.12,order.Price);
			Assert.AreEqual(1000,order.Size);
			Assert.AreEqual(sellLimitId,order.LogicalOrderId);
			Assert.IsNull(order.BrokerOrder);
			
			order = handler.CreatedOrders[1];
			Assert.AreEqual(OrderType.SellStop,order.Type);
			Assert.AreEqual(134.12,order.Price);
			Assert.AreEqual(1000,order.Size);
			Assert.AreEqual(sellStop2Id,order.LogicalOrderId);
			Assert.IsNull(order.BrokerOrder);
		}
		
		[Test]
		public void Test04LongTwoOrders() {
			handler.ClearPhysicalOrders();
			
			
			CreateLogicalEntry(OrderType.BuyLimit,234.12,1000);
			CreateLogicalEntry(OrderType.SellStop,154.12,1000);
			int sellLimitId = CreateLogicalExit(OrderType.SellLimit,334.12);
			int sellStopId = CreateLogicalExit(OrderType.SellStop,134.12);
			CreateLogicalExit(OrderType.BuyLimit,124.12);
			CreateLogicalExit(OrderType.BuyStop,194.12);
			
			object sellOrder1 = new object();
			handler.AddPhysicalOrder(OrderType.SellStop,134.12,1000,sellStopId,sellOrder1);
			object sellOrder2 = new object();
			handler.AddPhysicalOrder(OrderType.SellLimit,334.12,1000,sellLimitId,sellOrder2);
			
			double position = 1000; // Pretend we're flat.
			handler.SetActualPosition(position);
			handler.SetDesiredPosition(position);
			handler.SetLogicalOrders(orders);
			handler.PerformCompare();
			
			Assert.AreEqual(0,handler.CanceledOrders.Count);
			Assert.AreEqual(0,handler.ChangedOrders.Count);
			Assert.AreEqual(0,handler.CreatedOrders.Count);
		}
		
		[Test]
		public void Test05LongPartialEntry() {
			handler.ClearPhysicalOrders();

			// Position now long but an entry order is still working at 
			// only part of the size. 
			// So size is 500 but order is still 500 due to original order 1000;
			
			int buyLimitId = CreateLogicalEntry(OrderType.BuyLimit,234.12,1000);
			int sellStopId = CreateLogicalEntry(OrderType.SellStop,154.12,1000);
			int sellLimitId = CreateLogicalExit(OrderType.SellLimit,334.12);
			int sellStop2Id = CreateLogicalExit(OrderType.SellStop,134.12);
			CreateLogicalExit(OrderType.BuyLimit,124.12);
			CreateLogicalExit(OrderType.BuyStop,194.12);
			
			object buyOrder = new object();
			handler.AddPhysicalOrder(OrderType.BuyLimit,234.12,500,buyLimitId,buyOrder);
			object sellOrder = new object();
			handler.AddPhysicalOrder(OrderType.SellStop,154.12,1000,sellStopId,sellOrder);
			
			double position = 500; // Pretend we're flat.
			handler.SetActualPosition(position);

			handler.SetDesiredPosition(position);
			handler.SetLogicalOrders(orders);
			handler.PerformCompare();
			
			Assert.AreEqual(1,handler.CanceledOrders.Count);
			Assert.AreEqual(0,handler.ChangedOrders.Count);
			Assert.AreEqual(2,handler.CreatedOrders.Count);
			
			PhysicalOrder order = handler.CanceledOrders[0];
			Assert.AreEqual(OrderType.SellStop,order.Type);
			Assert.AreEqual(154.12,order.Price);
			Assert.AreEqual(1000,order.Size);
			Assert.AreEqual(sellStopId,order.LogicalOrderId);
			Assert.AreEqual(sellOrder,order.BrokerOrder);
			
			order = handler.CreatedOrders[0];
			Assert.AreEqual(OrderType.SellLimit,order.Type);
			Assert.AreEqual(334.12,order.Price);
			Assert.AreEqual(500,order.Size);
			Assert.AreEqual(sellLimitId,order.LogicalOrderId);
			Assert.IsNull(order.BrokerOrder);
			
			order = handler.CreatedOrders[1];
			Assert.AreEqual(OrderType.SellStop,order.Type);
			Assert.AreEqual(134.12,order.Price);
			Assert.AreEqual(500,order.Size);
			Assert.AreEqual(sellStop2Id,order.LogicalOrderId);
			Assert.IsNull(order.BrokerOrder);
		}
		
		[Test]
		public void Test06LongPartialExit() {
			handler.ClearPhysicalOrders();
			
			int sellLimitId = CreateLogicalExit(OrderType.SellLimit,334.12);
			int sellStopId = CreateLogicalExit(OrderType.SellStop,134.12);
			CreateLogicalExit(OrderType.BuyLimit,124.12);
			CreateLogicalExit(OrderType.BuyStop,194.12);
			
			object sellOrder1 = new object();
			handler.AddPhysicalOrder(OrderType.SellStop,134.12,1000,sellStopId,sellOrder1);
			object sellOrder2 = new object();
			handler.AddPhysicalOrder(OrderType.SellLimit,334.12,500,sellLimitId,sellOrder2);
			
			double position = 500;
			handler.SetActualPosition(position);

			handler.SetDesiredPosition(position);
			handler.SetLogicalOrders(orders);
			handler.PerformCompare();
			
			Assert.AreEqual(0,handler.CanceledOrders.Count);
			Assert.AreEqual(1,handler.ChangedOrders.Count);
			Assert.AreEqual(0,handler.CreatedOrders.Count);
			
			PhysicalOrder order = handler.ChangedOrders[0];
			Assert.AreEqual(OrderType.SellStop,order.Type);
			Assert.AreEqual(134.12,order.Price);
			Assert.AreEqual(500,order.Size);
			Assert.AreEqual(sellStopId,order.LogicalOrderId);
			Assert.AreEqual(sellOrder1,order.BrokerOrder);
		}
		
		[Test]
		public void Test07ShortEntryFilled() {
			handler.ClearPhysicalOrders();
			
			int buyLimitId = CreateLogicalEntry(OrderType.BuyLimit,234.12,1000);
			CreateLogicalEntry(OrderType.SellStop,154.12,1000);
			CreateLogicalExit(OrderType.SellLimit,334.12);
			CreateLogicalExit(OrderType.SellStop,134.12);
			int buyLimit2Id = CreateLogicalExit(OrderType.BuyLimit,124.12);
			int buyStopId = CreateLogicalExit(OrderType.BuyStop,194.12);
			
			object buyOrder = new object();
			handler.AddPhysicalOrder(OrderType.BuyLimit,234.12,1000,buyLimitId,buyOrder);
			
			double position = -1000; 
			handler.SetActualPosition(position);

			handler.SetDesiredPosition(position);
			handler.SetLogicalOrders(orders);
			handler.PerformCompare();
			
			Assert.AreEqual(1,handler.CanceledOrders.Count);
			Assert.AreEqual(0,handler.ChangedOrders.Count);
			Assert.AreEqual(2,handler.CreatedOrders.Count);
			
			PhysicalOrder order = handler.CanceledOrders[0];
			Assert.AreEqual(OrderType.BuyLimit,order.Type);
			Assert.AreEqual(234.12,order.Price);
			Assert.AreEqual(1000,order.Size);
			Assert.AreEqual(buyLimitId,order.LogicalOrderId);
			Assert.AreEqual(buyOrder,order.BrokerOrder);
			
			order = handler.CreatedOrders[0];
			Assert.AreEqual(OrderType.BuyLimit,order.Type);
			Assert.AreEqual(124.12,order.Price);
			Assert.AreEqual(1000,order.Size);
			Assert.AreEqual(buyLimit2Id,order.LogicalOrderId);
			Assert.IsNull(order.BrokerOrder);
			
			order = handler.CreatedOrders[1];
			Assert.AreEqual(OrderType.BuyStop,order.Type);
			Assert.AreEqual(194.12,order.Price);
			Assert.AreEqual(1000,order.Size);
			Assert.AreEqual(buyStopId,order.LogicalOrderId);
			Assert.IsNull(order.BrokerOrder);
		}
		
		[Test]
		public void Test08ShortTwoOrders() {
			handler.ClearPhysicalOrders();
			
			CreateLogicalEntry(OrderType.BuyLimit,234.12,1000);
			CreateLogicalEntry(OrderType.SellStop,154.12,1000);
			CreateLogicalExit(OrderType.SellLimit,334.12);
			CreateLogicalExit(OrderType.SellStop,134.12);
			int buyLimitId = CreateLogicalExit(OrderType.BuyLimit,124.12);
			int buyStopId = CreateLogicalExit(OrderType.BuyStop,194.12);
			
			object buyOrder1 = new object();
			handler.AddPhysicalOrder(OrderType.BuyLimit,124.12,1000,buyLimitId,buyOrder1);
			object buyOrder2 = new object();
			handler.AddPhysicalOrder(OrderType.BuyStop,194.12,1000,buyStopId,buyOrder2);
			
			double position = -1000; 
			handler.SetActualPosition(position);

			handler.SetDesiredPosition(position);
			handler.SetLogicalOrders(orders);
			handler.PerformCompare();
			
			Assert.AreEqual(0,handler.CanceledOrders.Count);
			Assert.AreEqual(0,handler.ChangedOrders.Count);
			Assert.AreEqual(0,handler.CreatedOrders.Count);
		}
		
		[Test]
		public void Test09ShortPartialEntry() {
			handler.ClearPhysicalOrders();

			// Position now long but an entry order is still working at 
			// only part of the size. 
			// So size is 500 but order is still 500 due to original order 1000;
			
			int buyLimitId = CreateLogicalEntry(OrderType.BuyLimit,234.12,1000);
			int sellStopId = CreateLogicalEntry(OrderType.SellStop,154.12,1000);
			CreateLogicalExit(OrderType.SellLimit,334.12);
			CreateLogicalExit(OrderType.SellStop,134.12);
			int buyLimit2Id = CreateLogicalExit(OrderType.BuyLimit,124.12);
			int buyStopId = CreateLogicalExit(OrderType.BuyStop,194.12);
			
			object buyOrder = new object();
			handler.AddPhysicalOrder(OrderType.BuyLimit,234.12,1000,buyLimitId,buyOrder);
			object sellOrder = new object();
			handler.AddPhysicalOrder(OrderType.SellStop,154.12,500,sellStopId,sellOrder);
			
			double position = -500; // Pretend we're flat.
			handler.SetActualPosition(position);

			handler.SetDesiredPosition(position);
			handler.SetLogicalOrders(orders);
			handler.PerformCompare();
			
			Assert.AreEqual(1,handler.CanceledOrders.Count);
			Assert.AreEqual(0,handler.ChangedOrders.Count);
			Assert.AreEqual(2,handler.CreatedOrders.Count);
			
			PhysicalOrder order = handler.CanceledOrders[0];
			Assert.AreEqual(OrderType.BuyLimit,order.Type);
			Assert.AreEqual(234.12,order.Price);
			Assert.AreEqual(1000,order.Size);
			Assert.AreEqual(buyLimitId,order.LogicalOrderId);
			Assert.AreEqual(buyOrder,order.BrokerOrder);
			
			order = handler.CreatedOrders[0];
			Assert.AreEqual(OrderType.BuyLimit,order.Type);
			Assert.AreEqual(124.12,order.Price);
			Assert.AreEqual(500,order.Size);
			Assert.AreEqual(buyLimit2Id,order.LogicalOrderId);
			Assert.IsNull(order.BrokerOrder);
			
			order = handler.CreatedOrders[1];
			Assert.AreEqual(OrderType.BuyStop,order.Type);
			Assert.AreEqual(194.12,order.Price);
			Assert.AreEqual(500,order.Size);
			Assert.AreEqual(buyStopId,order.LogicalOrderId);
			Assert.IsNull(order.BrokerOrder);
		}
		
		[Test]
		public void Test10ShortPartialExit() {
			handler.ClearPhysicalOrders();
			
			CreateLogicalExit(OrderType.SellLimit,334.12);
			CreateLogicalExit(OrderType.SellStop,134.12);
			int buyLimitId = CreateLogicalExit(OrderType.BuyLimit,124.12);
			int buyStopId = CreateLogicalExit(OrderType.BuyStop,194.12);
			
			object buyOrder1 = new object();
			handler.AddPhysicalOrder(OrderType.BuyLimit,124.12,1000,buyLimitId,buyOrder1);
			object buyOrder2 = new object();
			handler.AddPhysicalOrder(OrderType.BuyStop,194.12,1000,buyStopId,buyOrder2);
			
			double position = -500; 
			handler.SetActualPosition(position);

			handler.SetDesiredPosition(position);
			handler.SetLogicalOrders(orders);
			handler.PerformCompare();
			
			Assert.AreEqual(0,handler.CanceledOrders.Count);
			Assert.AreEqual(2,handler.ChangedOrders.Count);
			Assert.AreEqual(0,handler.CreatedOrders.Count);
			
			PhysicalOrder order = handler.ChangedOrders[0];
			Assert.AreEqual(OrderType.BuyLimit,order.Type);
			Assert.AreEqual(124.12,order.Price);
			Assert.AreEqual(500,order.Size);
			Assert.AreEqual(buyLimitId,order.LogicalOrderId);
			Assert.AreEqual(buyOrder1,order.BrokerOrder);
			
			order = handler.ChangedOrders[1];
			Assert.AreEqual(OrderType.BuyStop,order.Type);
			Assert.AreEqual(194.12,order.Price);
			Assert.AreEqual(500,order.Size);
			Assert.AreEqual(buyStopId,order.LogicalOrderId);
			Assert.AreEqual(buyOrder2,order.BrokerOrder);
		}
		
		[Test]
		public void Test11FlatChangeSizes() {
			handler.ClearPhysicalOrders();
			
			int buyLimitId = CreateLogicalEntry(OrderType.BuyLimit,234.12,700);
			int sellStopId = CreateLogicalEntry(OrderType.SellStop,154.12,800);
			CreateLogicalExit(OrderType.SellLimit,334.12);
			CreateLogicalExit(OrderType.SellStop,134.12);
			CreateLogicalExit(OrderType.BuyLimit,124.12);
			CreateLogicalExit(OrderType.BuyStop,194.12);
			
			object buyOrder = new object();
			handler.AddPhysicalOrder(OrderType.BuyLimit,234.12,1000,buyLimitId,buyOrder);
			object sellOrder = new object();
			handler.AddPhysicalOrder(OrderType.SellStop,154.12,1000,sellStopId,sellOrder);
			
			double position = 0; // Pretend we're flat.
			handler.SetActualPosition(position);

			handler.SetDesiredPosition(position);
			handler.SetLogicalOrders(orders);
			handler.PerformCompare();
			
			Assert.AreEqual(0,handler.CanceledOrders.Count);
			Assert.AreEqual(2,handler.ChangedOrders.Count);
			Assert.AreEqual(0,handler.CreatedOrders.Count);
			
			PhysicalOrder order = handler.ChangedOrders[0];
			Assert.AreEqual(OrderType.BuyLimit,order.Type);
			Assert.AreEqual(234.12,order.Price);
			Assert.AreEqual(700,order.Size);
			Assert.AreEqual(buyLimitId,order.LogicalOrderId);
			Assert.AreEqual(buyOrder,order.BrokerOrder);
			
			order = handler.ChangedOrders[1];
			Assert.AreEqual(OrderType.SellStop,order.Type);
			Assert.AreEqual(154.12,order.Price);
			Assert.AreEqual(800,order.Size);
			Assert.AreEqual(sellStopId,order.LogicalOrderId);
			Assert.AreEqual(sellOrder,order.BrokerOrder);
			
		}
		
		[Test]
		public void Test12FlatChangePrices() {
			handler.ClearPhysicalOrders();
			
			int buyLimitId = CreateLogicalEntry(OrderType.BuyLimit,244.12,700);
			int sellStopId = CreateLogicalEntry(OrderType.SellStop,164.12,800);
			CreateLogicalExit(OrderType.SellLimit,374.12);
			CreateLogicalExit(OrderType.SellStop,184.12);
			CreateLogicalExit(OrderType.BuyLimit,194.12);
			CreateLogicalExit(OrderType.BuyStop,104.12);
			
			object buyOrder = new object();
			handler.AddPhysicalOrder(OrderType.BuyLimit,234.12,1000,buyLimitId,buyOrder);
			object sellOrder = new object();
			handler.AddPhysicalOrder(OrderType.SellStop,154.12,1000,sellStopId,sellOrder);
			
			double position = 0;
			handler.SetActualPosition(position);

			handler.SetDesiredPosition(position);
			handler.SetLogicalOrders(orders);
			handler.PerformCompare();
			
			Assert.AreEqual(0,handler.CanceledOrders.Count);
			Assert.AreEqual(2,handler.ChangedOrders.Count);
			Assert.AreEqual(0,handler.CreatedOrders.Count);
			
			PhysicalOrder order = handler.ChangedOrders[0];
			Assert.AreEqual(OrderType.BuyLimit,order.Type);
			Assert.AreEqual(244.12,order.Price);
			Assert.AreEqual(700,order.Size);
			Assert.AreEqual(buyLimitId,order.LogicalOrderId);
			Assert.AreEqual(buyOrder,order.BrokerOrder);
			
			order = handler.ChangedOrders[1];
			Assert.AreEqual(OrderType.SellStop,order.Type);
			Assert.AreEqual(164.12,order.Price);
			Assert.AreEqual(800,order.Size);
			Assert.AreEqual(sellStopId,order.LogicalOrderId);
			Assert.AreEqual(sellOrder,order.BrokerOrder);
			
		}
		
		[Test]
		public void Test13LongChangePrices() {
			handler.ClearPhysicalOrders();
			
			CreateLogicalEntry(OrderType.BuyLimit,244.12,1000);
			CreateLogicalEntry(OrderType.SellStop,164.12,1000);
			int sellLimitId = CreateLogicalExit(OrderType.SellLimit,374.12);
			int sellStopId = CreateLogicalExit(OrderType.SellStop,184.12);
			CreateLogicalExit(OrderType.BuyLimit,194.12);
			CreateLogicalExit(OrderType.BuyStop,104.12);
			
			object sellOrder1 = new object();
			handler.AddPhysicalOrder(OrderType.SellStop,134.12,1000,sellStopId,sellOrder1);
			object sellOrder2 = new object();
			handler.AddPhysicalOrder(OrderType.SellLimit,334.12,1000,sellLimitId,sellOrder2);
			
			double position = 1000;
			handler.SetActualPosition(position);

			handler.SetDesiredPosition(position);
			handler.SetLogicalOrders(orders);
			handler.PerformCompare();
			
			Assert.AreEqual(0,handler.CanceledOrders.Count);
			Assert.AreEqual(2,handler.ChangedOrders.Count);
			Assert.AreEqual(0,handler.CreatedOrders.Count);
			
			PhysicalOrder order = handler.ChangedOrders[0];
			Assert.AreEqual(OrderType.SellLimit,order.Type);
			Assert.AreEqual(374.12,order.Price);
			Assert.AreEqual(1000,order.Size);
			Assert.AreEqual(sellLimitId,order.LogicalOrderId);
			Assert.AreEqual(sellOrder2,order.BrokerOrder);
			
			order = handler.ChangedOrders[1];
			Assert.AreEqual(OrderType.SellStop,order.Type);
			Assert.AreEqual(184.12,order.Price);
			Assert.AreEqual(1000,order.Size);
			Assert.AreEqual(sellStopId,order.LogicalOrderId);
			Assert.AreEqual(sellOrder1,order.BrokerOrder);
			
		}
		
		[Test]
		public void Test14ShortToFlat() {
			handler.ClearPhysicalOrders();
			
			double position = -1000;
			handler.SetActualPosition(0); // Actual and desired differ!!!

			handler.SetDesiredPosition(position);
			handler.SetLogicalOrders(null);
			handler.PerformCompare();
			
			Assert.AreEqual(0,handler.CanceledOrders.Count);
			Assert.AreEqual(0,handler.ChangedOrders.Count);
			Assert.AreEqual(1,handler.CreatedOrders.Count);
			
			PhysicalOrder order = handler.CreatedOrders[0];
			Assert.AreEqual(OrderType.SellMarket,order.Type);
			Assert.AreEqual(0,order.Price);
			Assert.AreEqual(1000,order.Size);
			Assert.AreEqual(0,order.LogicalOrderId);
			Assert.IsNull(order.BrokerOrder);
			
		}
		
		[Test]
		public void Test15LongToFlat() {
			handler.ClearPhysicalOrders();
			
			double position = 1000;
			handler.SetActualPosition(0); // Actual and desired differ!!!

			handler.SetDesiredPosition(position);
			handler.SetLogicalOrders(null);
			handler.PerformCompare();
			
			Assert.AreEqual(0,handler.CanceledOrders.Count);
			Assert.AreEqual(0,handler.ChangedOrders.Count);
			Assert.AreEqual(1,handler.CreatedOrders.Count);
			
			PhysicalOrder order = handler.CreatedOrders[0];
			Assert.AreEqual(OrderType.BuyMarket,order.Type);
			Assert.AreEqual(0,order.Price);
			Assert.AreEqual(1000,order.Size);
			Assert.AreEqual(0,order.LogicalOrderId);
			Assert.IsNull(order.BrokerOrder);
			
		}
		
		[Test]
		public void Test16PendingSellMarket() {
			handler.ClearPhysicalOrders();
			
			double position = -10;
			handler.SetActualPosition(0); // Actual and desired differ!!!
			
			object sellOrder1 = new object();
			handler.AddPhysicalOrder(OrderType.SellMarket,134.12,10,0,sellOrder1);

			handler.SetDesiredPosition(position);
			handler.SetLogicalOrders(null);
			handler.PerformCompare();
			
			Assert.AreEqual(0,handler.CanceledOrders.Count);
			Assert.AreEqual(0,handler.ChangedOrders.Count);
			Assert.AreEqual(0,handler.CreatedOrders.Count);
		}
		
		[Test]
		public void Test17PendingBuyMarket() {
			handler.ClearPhysicalOrders();
			
			double position = 10;
			handler.SetActualPosition(-5); // Actual and desired differ!!!
			
			object sellOrder1 = new object();
			handler.AddPhysicalOrder(OrderType.BuyMarket,134.12,15,0,sellOrder1);

			handler.SetDesiredPosition(position);
			handler.SetLogicalOrders(null);
			handler.PerformCompare();
			
			Assert.AreEqual(0,handler.CanceledOrders.Count);
			Assert.AreEqual(0,handler.ChangedOrders.Count);
			Assert.AreEqual(0,handler.CreatedOrders.Count);
		}
		
		[Test]
		public void Test18PendingExtraBuyMarket() {
			handler.ClearPhysicalOrders();
			
			double position = 10;
			handler.SetActualPosition(-5); // Actual and desired differ!!!
			
			object sellOrder1 = new object();
			handler.AddPhysicalOrder(OrderType.BuyMarket,134.12,15,0,sellOrder1);
			handler.AddPhysicalOrder(OrderType.BuyMarket,134.12,15,0,sellOrder1);

			handler.SetDesiredPosition(position);
			handler.SetLogicalOrders(null);
			handler.PerformCompare();
			
			Assert.AreEqual(0,handler.ChangedOrders.Count);
			Assert.AreEqual(0,handler.CreatedOrders.Count);
			Assert.AreEqual(1,handler.CanceledOrders.Count);
		}
		
		[Test]
		public void Test19PendingExtraSellMarket() {
			handler.ClearPhysicalOrders();
			
			double position = -10;
			handler.SetActualPosition(5); // Actual and desired differ!!!
			
			object sellOrder1 = new object();
			handler.AddPhysicalOrder(OrderType.SellMarket,134.12,15,0,sellOrder1);
			handler.AddPhysicalOrder(OrderType.SellMarket,134.12,15,0,sellOrder1);

			handler.SetDesiredPosition(position);
			handler.SetLogicalOrders(null);
			handler.PerformCompare();
			
			Assert.AreEqual(0,handler.ChangedOrders.Count);
			Assert.AreEqual(0,handler.CreatedOrders.Count);
			Assert.AreEqual(1,handler.CanceledOrders.Count);
		}
		
		[Test]
		public void Test20PendingUnneededSellMarket() {
			handler.ClearPhysicalOrders();
			
			double position = 0;
			handler.SetActualPosition(0); // Actual and desired differ!!!
			
			object sellOrder1 = new object();
			handler.AddPhysicalOrder(OrderType.SellMarket,134.12,15,0,sellOrder1);

			handler.SetDesiredPosition(position);
			handler.SetLogicalOrders(null);
			handler.PerformCompare();
			
			Assert.AreEqual(0,handler.ChangedOrders.Count);
			Assert.AreEqual(0,handler.CreatedOrders.Count);
			Assert.AreEqual(1,handler.CanceledOrders.Count);
		}
		
		[Test]
		public void Test21PendingUnneededBuyMarket() {
			handler.ClearPhysicalOrders();
			
			double position = 0;
			handler.SetActualPosition(0); // Actual and desired differ!!!
			
			object sellOrder1 = new object();
			handler.AddPhysicalOrder(OrderType.BuyMarket,134.12,15,0,sellOrder1);

			handler.SetDesiredPosition(position);
			handler.SetLogicalOrders(null);
			handler.PerformCompare();
			
			Assert.AreEqual(0,handler.ChangedOrders.Count);
			Assert.AreEqual(0,handler.CreatedOrders.Count);
			Assert.AreEqual(1,handler.CanceledOrders.Count);
		}
		
		[Test]
		public void Test22PendingWrongSideSellMarket() {
			handler.ClearPhysicalOrders();
			
			double position = 10;
			handler.SetActualPosition(-5); // Actual and desired differ!!!
			
			object sellOrder1 = new object();
			handler.AddPhysicalOrder(OrderType.SellMarket,134.12,15,0,sellOrder1);

			handler.SetDesiredPosition(position);
			handler.SetLogicalOrders(null);
			handler.PerformCompare();
			
			Assert.AreEqual(0,handler.ChangedOrders.Count);
			Assert.AreEqual(1,handler.CreatedOrders.Count);
			Assert.AreEqual(1,handler.CanceledOrders.Count);
		}
		
		[Test]
		public void Test23PendingWrongSideBuyMarket() {
			handler.ClearPhysicalOrders();
			
			double position = -10;
			handler.SetActualPosition(5); // Actual and desired differ!!!
			
			object sellOrder1 = new object();
			handler.AddPhysicalOrder(OrderType.BuyMarket,134.12,15,0,sellOrder1);

			handler.SetDesiredPosition(position);
			handler.SetLogicalOrders(null);
			handler.PerformCompare();
			
			Assert.AreEqual(0,handler.ChangedOrders.Count);
			Assert.AreEqual(1,handler.CreatedOrders.Count);
			Assert.AreEqual(1,handler.CanceledOrders.Count);
		}
		
		[Test]
		public void Test24PendingBuyLimit() {
			handler.ClearPhysicalOrders();
			
			double position = 10;
			handler.SetActualPosition(-5); // Actual and desired differ!!!
			
			object sellOrder1 = new object();
			handler.AddPhysicalOrder(OrderType.BuyLimit,134.12,15,0,sellOrder1);

			handler.SetDesiredPosition(position);
			handler.SetLogicalOrders(null);
			handler.PerformCompare();
			
			Assert.AreEqual(0,handler.ChangedOrders.Count);
			Assert.AreEqual(1,handler.CreatedOrders.Count);
			Assert.AreEqual(1,handler.CanceledOrders.Count);
		}
		
		[Test]
		public void Test25PendingSellLimit() {
			handler.ClearPhysicalOrders();
			
			double position = -10;
			handler.SetActualPosition(5); // Actual and desired differ!!!
			
			object sellOrder1 = new object();
			handler.AddPhysicalOrder(OrderType.SellLimit,134.12,15,0,sellOrder1);

			handler.SetDesiredPosition(position);
			handler.SetLogicalOrders(null);
			handler.PerformCompare();
			
			Assert.AreEqual(0,handler.ChangedOrders.Count);
			Assert.AreEqual(1,handler.CreatedOrders.Count);
			Assert.AreEqual(1,handler.CanceledOrders.Count);
		}
		
		[Test]
		public void Test26PendingBuyAndSellLimit() {
			handler.ClearPhysicalOrders();
			
			CreateLogicalEntry(OrderType.BuyMarket,0,2);
			
			double position = 0;
			handler.SetActualPosition(0); // Actual and desired differ!!!
			
			object sellOrder1 = new object();
			object buyOrder1 = new object();
			handler.AddPhysicalOrder(OrderType.BuyLimit,15.12,3,0,buyOrder1);
			handler.AddPhysicalOrder(OrderType.SellLimit,34.12,3,0,sellOrder1);

			handler.SetDesiredPosition(position);
			handler.SetLogicalOrders(orders);
			handler.PerformCompare();
			
			Assert.AreEqual(0,handler.ChangedOrders.Count);
			Assert.AreEqual(1,handler.CreatedOrders.Count);
			Assert.AreEqual(2,handler.CanceledOrders.Count);
		}
		
		public class TestOrderHandler : PhysicalOrderHandler, LogicalOrderHandler {
			LogicalOrderHandler logicalHandler;
			public List<PhysicalOrder> CanceledOrders = new List<PhysicalOrder>();
			public List<PhysicalOrder> ChangedOrders = new List<PhysicalOrder>();
			public List<PhysicalOrder> CreatedOrders = new List<PhysicalOrder>();
			public TestOrderHandler(SymbolInfo symbol) {
				logicalHandler = Factory.Utility.LogicalOrderHandler(symbol,this);
			}
			public void ClearPhysicalOrders()
			{
				logicalHandler.ClearPhysicalOrders();
				CanceledOrders.Clear();
				ChangedOrders.Clear();
				CreatedOrders.Clear();
			}
			public void SetActualPosition( double position) {
				logicalHandler.SetActualPosition(position);
			}
			public void SetDesiredPosition( double position) {
				logicalHandler.SetDesiredPosition(position);
			}
			public void OnCancelBrokerOrder(PhysicalOrder order)
			{
				CanceledOrders.Add(order);
			}
			public void OnChangeBrokerOrder(PhysicalOrder order)
			{
				ChangedOrders.Add(order);
			}
			public void OnCreateBrokerOrder(PhysicalOrder order)
			{
				CreatedOrders.Add(order);
			}
			public void SetLogicalOrders(IList<LogicalOrder> logicalOrders) {
				logicalHandler.SetLogicalOrders(logicalOrders);
			}
			public void PerformCompare()
			{
				logicalHandler.PerformCompare();
			}
			
			public void AddPhysicalOrder(OrderType type, double price, int size, int logicalOrderId, object brokerOrder)
			{
				logicalHandler.AddPhysicalOrder(type,price,size,logicalOrderId,brokerOrder);
			}
		}
		
	}
}
