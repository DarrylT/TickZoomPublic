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
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

using Krs.Ats.IBNet;
using Krs.Ats.IBNet.Contracts;
using TickZoom.Api;

//using System.Data;
namespace TickZoom.InteractiveBrokers
{

	public class IBInterface : Provider, PhysicalOrderHandler
	{
		private static readonly Log log = Factory.Log.GetLogger(typeof(IBInterface));
		private static readonly bool debug = log.IsDebugEnabled;
        private readonly object readersLock = new object();
	    private readonly static object listLock = new object();
        private IBClient client;
        private int nextValidId = 0;
        private Dictionary<ulong,SymbolHandler> symbolHandlers = new Dictionary<ulong,SymbolHandler>();

        public IBInterface()
		{
        }
        
        private void Initialize() {
            client = new IBClient();
            client.ThrowExceptions = true;

            client.TickPrice += client_TickPrice;
            client.TickSize += client_TickSize;
            client.Error += client_Error;
            client.NextValidId += client_NextValidId;
            client.UpdateMarketDepth += client_UpdateMktDepth;
            client.RealTimeBar += client_RealTimeBar;
            client.OrderStatus += client_OrderStatus;
            client.OpenOrder += client_OpenOrder;
            client.OpenOrderEnd += client_OpenOrderEnd;
            client.ExecDetails += new EventHandler<ExecDetailsEventArgs>(client_ExecDetails);
            client.UpdatePortfolio += client_UpdatePortfolio;
            client.ReportException += client_ReportException;
            client.Connect("127.0.0.1", 7496, 0);
            client.RequestAccountUpdates(true,null);
            Thread.Sleep(1000);
        }
        
		Receiver receiver;
        public void Start(Receiver receiver)
        {
        	log.Info("IBInterface Startup");
        	this.receiver = (Receiver) receiver;
        	Initialize();
			string appDataFolder = Factory.Settings["AppDataFolder"];
			if( appDataFolder == null) {
				throw new ApplicationException("Sorry, AppDataFolder must be set in the app.config file.");
			}
			string configFile = appDataFolder+@"/Providers/IBProviderService.config";
			
			LoadProperties(configFile);
			
        }

        Dictionary<string, string> data;
        string configFile;
        private void LoadProperties(string configFile) {
        	this.configFile = configFile;
			data = new Dictionary<string, string>();
			if( !File.Exists(configFile) ) {
				Directory.CreateDirectory(Path.GetDirectoryName(configFile));
		        using (StreamWriter sw = new StreamWriter(configFile)) 
		        {
		            // Add some text to the file.
		            sw.WriteLine("ClientId=3712");
		            sw.WriteLine("EquityOrForex=equity");
		            sw.WriteLine("LiveOrDemo=demo");
		            sw.WriteLine("ForexDemoUserName=CHANGEME");
		            sw.WriteLine("ForexDemoPassword=CHANGEME");
		            sw.WriteLine("EquityDemoUserName=CHANGEME");
		            sw.WriteLine("EquityDemoPassword=CHANGEME");
		            sw.WriteLine("ForexLiveUserName=CHANGEME");
		            sw.WriteLine("ForexLivePassword=CHANGEME");
		            sw.WriteLine("EquityLiveUserName=CHANGEME");
		            sw.WriteLine("EquityLivePassword=CHANGEME");
		            // Arbitrary objects can also be written to the file.
		        }
			} 
			
			foreach (var row in File.ReadAllLines(configFile)) {
				string[] nameValue = row.Split('=');
				data.Add(nameValue[0].Trim(),nameValue[1].Trim());
			}
		}
        
        private string GetProperty( string name) {
        	string value;
			if( !data.TryGetValue(name,out value) ||
				value == null || value.Length == 0 || value.Contains("CHANGEME")) {
				throw new ApplicationException(name + " property must be set in " + configFile);
			}
        	return value;
        }
        
        
        public void Stop(Receiver receiver) {
        }
        
		private string UpperFirst(string input)
		{
			string temp = input.Substring(0, 1);
			return temp.ToUpper() + input.Remove(0, 1);
		}        
        
		public void StartSymbol(Receiver receiver, SymbolInfo symbol, TimeStamp lastTimeStamp)
		{
			if( debug) log.Debug("StartSymbol " + symbol + ", " + lastTimeStamp);
            Equity equity = new Equity(symbol.Symbol);
            SymbolHandler handler = GetSymbolHandler(symbol,receiver);
            client.RequestMarketData((int)symbol.BinaryIdentifier, equity, null, false, false);
			receiver.OnRealTime(symbol);
		}
		
		public void StopSymbol(Receiver receiver, SymbolInfo symbol)
		{
			if( debug) log.Debug("StartSymbol");
            client.CancelMarketData((int)symbol.BinaryIdentifier);
			receiver.OnEndRealTime(symbol);
		}
		
		public void PositionChange(Receiver receiver, SymbolInfo symbol, double signal, IList<LogicalOrder> orders)
		{
			if( debug) log.Debug("PositionChange");
			
			LogicalOrderHandler handler = symbolHandlers[symbol.BinaryIdentifier].LogicalOrderHandler;
			handler.SetDesiredPosition(signal);
			handler.SetLogicalOrders(orders);
			
			client.RequestOpenOrders();
		}
		
		public void Stop()
		{	
        	client.Disconnect();
		}
		
        private void client_ExecDetails(object sender, ExecDetailsEventArgs e)
        {
            log.InfoFormat("Execution: {0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11}",
                e.Contract.Symbol, e.Execution.AccountNumber, e.Execution.ClientId, e.Execution.Exchange, e.Execution.ExecutionId,
                e.Execution.Liquidation, e.Execution.OrderId, e.Execution.PermId, e.Execution.Price, e.Execution.Shares, e.Execution.Side, e.Execution.Time);
        }

        private void client_RealTimeBar(object sender, RealTimeBarEventArgs e)
        {
        	if( debug) log.Debug("Received Real Time Bar: " + e.Close);
        }

        private void client_OrderStatus(object sender, OrderStatusEventArgs e)
        {
        	if(debug) log.Debug("Order Status for Id " + e.OrderId + " is " + e.Status);
        	bool deleteFlag = false;
        	switch( e.Status) {
        		case OrderStatus.Canceled:
        		case OrderStatus.Filled:
        		case OrderStatus.Inactive:
        		case OrderStatus.PendingCancel:
        			deleteFlag = true;
        			break;
        		case OrderStatus.PreSubmitted:
        		case OrderStatus.PartiallyFilled:
        		case OrderStatus.Submitted:
        			// LogicalOrderHandler will decide what to do with these.
        			break;
        		default:
        			log.Error("Unexpected order status: " + e.Status);
        			break;
        	}
        	if( deleteFlag) {
    			if( openOrders.ContainsKey(e.OrderId)) {
    				openOrders.Remove(e.OrderId);
    			}
	        	if(debug) log.Debug("Removing open order id " + e.OrderId);
        	}
        }

        private void client_OpenOrder(object sender, OpenOrderEventArgs e)
        {
        	if( debug) log.Debug("Open Order Id " + e.Order.OrderId + " " + e.Contract.Symbol + " " + OrderToString(e.Order));
        	openOrders[e.Order.OrderId] = e;
        }
        
        public void HandleOpenOrder(OpenOrderEventArgs e) {
        	if( debug) log.Debug("HandleOpenOrder id " + e.Order.OrderId + " " + e.Contract.Symbol + " " + OrderToString(e.Order));
        	TickZoom.Api.OrderType type = TickZoom.Api.OrderType.BuyMarket;
        	double price = 0;
        	switch( e.Order.OrderType) {
        		case Krs.Ats.IBNet.OrderType.Market:
        			if( e.Order.Action == ActionSide.Buy) {
	        			type = TickZoom.Api.OrderType.BuyMarket;
        			} else {
	        			type = TickZoom.Api.OrderType.SellMarket;
        			}
        			price = 0;
        			break;
        		case Krs.Ats.IBNet.OrderType.Limit:
        			if( e.Order.Action == ActionSide.Buy) {
	        			type = TickZoom.Api.OrderType.BuyLimit;
        			} else {
	        			type = TickZoom.Api.OrderType.SellLimit;
        			}
        			price = (double) e.Order.LimitPrice;
        			break;
        		case Krs.Ats.IBNet.OrderType.Stop:
        			if( e.Order.Action == ActionSide.Buy) {
	        			type = TickZoom.Api.OrderType.BuyStop;
        			} else {
	        			type = TickZoom.Api.OrderType.SellStop;
        			}
        			price = (double) e.Order.AuxPrice;
        			break;
        		default:
        			log.Error( "Unknown OrderType: " + e.Order.OrderType);
        			break;
        	}
        	SymbolInfo symbol = Factory.Symbol.LookupSymbol(e.Contract.Symbol);
        	SymbolHandler handler;
        	if( symbolHandlers.TryGetValue(symbol.BinaryIdentifier,out handler)) {
        		handler.LogicalOrderHandler.AddPhysicalOrder(type,price,e.Order.TotalQuantity,e.Order);
        	}
        }
        
        Dictionary<int,OpenOrderEventArgs> openOrders = new Dictionary<int, OpenOrderEventArgs>();
        
        private string OrderToString( Order order) {
        	StringBuilder sb = null;
    		sb = new StringBuilder();
        	sb.Append( order.Action);
        	sb.Append( " " );
        	sb.Append( order.OrderType);
        	sb.Append( " " );
        	switch( order.OrderType) {
        		case Krs.Ats.IBNet.OrderType.Market:
        			break;
        		case Krs.Ats.IBNet.OrderType.Limit:
        			sb.Append( " limit ");
        			sb.Append( order.LimitPrice);
        			break;
        		case Krs.Ats.IBNet.OrderType.Stop:
        			sb.Append( " stop ");
        			sb.Append( order.AuxPrice);
        			break;
        		case Krs.Ats.IBNet.OrderType.StopLimit:
        			sb.Append( " stop ");
        			sb.Append( order.AuxPrice);
        			sb.Append( " limit ");
        			sb.Append( order.LimitPrice);
        			break;
        		default:
        			log.Error( "Unknown OrderType: " + order.OrderType);
        			break;
        	}
        	sb.Append( " size ");
        	sb.Append( order.TotalQuantity);
        	return sb.ToString();
		}
        
        private void client_OpenOrderEnd(object sender, EventArgs e)
        {
        	if(debug) log.Debug("Open Order End ");
        	foreach( var kvp in symbolHandlers) {
        		LogicalOrderHandler handler = kvp.Value.LogicalOrderHandler;
        		handler.ClearPhysicalOrders();
        	}
        	foreach( var kvp in openOrders) {
        		HandleOpenOrder(kvp.Value);
        	}
        	foreach( var kvp in symbolHandlers) {
        		ulong symbolBinaryId = kvp.Key;
        		SymbolHandler symbolHandler = kvp.Value;
        		LogicalOrderHandler orderHandler = symbolHandler.LogicalOrderHandler;
	    		orderHandler.SetActualPosition(symbolHandler.Position);
    			orderHandler.PerformCompare();
        	}
        }
        
        private void client_UpdateMktDepth(object sender, UpdateMarketDepthEventArgs e)
        {
        	if(debug) log.Debug("Tick ID: " + e.TickerId + " Tick Side: " + EnumDescConverter.GetEnumDescription(e.Side) +
                              " Tick Size: " + e.Size + " Tick Price: " + e.Price + " Tick Position: " + e.Position +
                              " Operation: " + EnumDescConverter.GetEnumDescription(e.Operation));
        }

        private void client_NextValidId(object sender, NextValidIdEventArgs e)
        {
        	if( debug) log.Debug("Next Valid ID: " + e.OrderId);
        	nextValidId = e.OrderId;
        }

        private void client_TickSize(object sender, TickSizeEventArgs e)
        {
        	SymbolHandler buffer = symbolHandlers[(ulong)e.TickerId];
        	if( e.TickType == TickType.AskSize) {
        		buffer.AskSize = e.Size;
        	} else if( e.TickType == TickType.BidSize) {
        		buffer.BidSize = e.Size;
        	} else if( e.TickType == TickType.LastSize) {
        		buffer.LastSize = e.Size;
        	}
        }
        
        private SymbolHandler GetSymbolHandler(SymbolInfo symbol, Receiver receiver) {
        	SymbolHandler symbolHandler;
        	if( symbolHandlers.TryGetValue(symbol.BinaryIdentifier,out symbolHandler)) {
        		return symbolHandler;
        	} else {
    	    	symbolHandler = Factory.Utility.SymbolHandler(symbol,receiver);
    	    	symbolHandler.LogicalOrderHandler = Factory.Utility.LogicalOrderHandler(symbol,this);
    	    	symbolHandlers.Add(symbol.BinaryIdentifier,symbolHandler);
    	    	return symbolHandler;
        	}
        }

        private void RemoveSymbolHandler(SymbolInfo symbol) {
        	if( symbolHandlers.ContainsKey(symbol.BinaryIdentifier) ) {
        		symbolHandlers.Remove(symbol.BinaryIdentifier);
        	}
        }
        
        private void client_Error(object sender, Krs.Ats.IBNet.ErrorEventArgs e)
        {
            log.Error("Error: "+ e.ErrorMsg);
        }

        private void client_TickPrice(object sender, TickPriceEventArgs e)
        {
        	SymbolHandler buffer = symbolHandlers[(ulong)e.TickerId];
        	if( e.TickType == TickType.AskPrice) {
        		buffer.Ask = (double) e.Price;
        		buffer.SendQuote();
        	} else if( e.TickType == TickType.BidPrice) {
        		buffer.Bid = (double) e.Price;
        		buffer.SendQuote();
        	} else if( e.TickType == TickType.LastPrice) {
        		buffer.Last = (double) e.Price;
        		if( buffer.LastSize > 0) {
	        		buffer.SendTimeAndSales();
        		}
        	}
		}
        
        private void client_UpdateAccountSize(object sender, UpdateAccountValueEventArgs e) {
        }
        
        private void client_ReportException(object sender, ReportExceptionEventArgs e) {
        	log.Error(e.Error.Message,e.Error);
        }
        
        private void client_UpdatePortfolio(object sender, UpdatePortfolioEventArgs e) {
  			try {
        		SymbolInfo symbol = Factory.Symbol.LookupSymbol(e.Contract.Symbol);
        		SymbolHandler handler = GetSymbolHandler(symbol,receiver);
        		handler.SetPosition(e.Position);
	        	if(debug) log.Debug( "UpdatePortfolio: " + e.Contract.Symbol + " is " + e.Position);
  			} catch( ApplicationException ex) {
  				log.Warn("UpdatePortfolio: " + ex.Message);
  			}
        }
		
		private Order ToBrokerOrder( PhysicalOrder physicalOrder) {
			Order brokerOrder = new Order();
			brokerOrder.OrderType = Krs.Ats.IBNet.OrderType.Market;
			switch( physicalOrder.Type ) {
				case TickZoom.Api.OrderType.BuyLimit:
					brokerOrder.Action = ActionSide.Buy;
					brokerOrder.OrderType = Krs.Ats.IBNet.OrderType.Limit;
					brokerOrder.LimitPrice = (decimal) physicalOrder.Price;
					break;
				case TickZoom.Api.OrderType.BuyMarket:
					brokerOrder.Action = ActionSide.Buy;
					brokerOrder.OrderType = Krs.Ats.IBNet.OrderType.Market;
					break;
				case TickZoom.Api.OrderType.BuyStop:
					brokerOrder.Action = ActionSide.Buy;
					brokerOrder.OrderType = Krs.Ats.IBNet.OrderType.Stop;
					brokerOrder.AuxPrice = (decimal) physicalOrder.Price;
					break;
				case TickZoom.Api.OrderType.SellLimit:
					brokerOrder.Action = ActionSide.Sell;
					brokerOrder.OrderType = Krs.Ats.IBNet.OrderType.Limit;
					brokerOrder.LimitPrice = (decimal) physicalOrder.Price;
					break;
				case TickZoom.Api.OrderType.SellMarket:
					brokerOrder.Action = ActionSide.Sell;
					brokerOrder.OrderType = Krs.Ats.IBNet.OrderType.Market;
					break;
				case TickZoom.Api.OrderType.SellStop:
					brokerOrder.Action = ActionSide.Sell;
					brokerOrder.OrderType = Krs.Ats.IBNet.OrderType.Stop;
					brokerOrder.AuxPrice = (decimal) physicalOrder.Price;
					break;
			}
			brokerOrder.TotalQuantity = (int) physicalOrder.Size;
			return brokerOrder;
		}
		
		public void OnCreateBrokerOrder(PhysicalOrder physicalOrder)
		{
			if( debug) log.Debug( "OnCreateBrokerOrder " + physicalOrder);
			SymbolInfo symbol = physicalOrder.Symbol;
			Contract contract = new Contract(symbol.Symbol,"SMART",SecurityType.Stock,"USD");
			Order brokerOrder = ToBrokerOrder(physicalOrder);
			while(nextValidId==0) {
				Thread.Sleep(10);
			}
			nextValidId++;
			client.PlaceOrder(nextValidId,contract,brokerOrder);
			if(debug) log.Debug("PlaceOrder: " + contract.Symbol + " " + OrderToString(brokerOrder));
		}
		
		public void OnCancelBrokerOrder(PhysicalOrder physicalOrder)
		{
			if( debug) log.Debug( "OnCancelBrokerOrder " + physicalOrder);
			Order order = physicalOrder.BrokerOrder as Order;
			if( order != null) {
				client.CancelOrder( order.OrderId);
			} else {
				throw new ApplicationException("BrokerOrder property want's an Order object.");
			}
		}
		
		public void OnChangeBrokerOrder(PhysicalOrder physicalOrder)
		{
			if( debug) log.Debug( "OnChangeBrokerOrder " + physicalOrder);
			Order order = physicalOrder.BrokerOrder as Order;
			if( order != null) {
				client.CancelOrder(order.OrderId);
				if(debug) log.Debug("Cancel Order (for change): " + physicalOrder.Symbol.Symbol + " " + OrderToString(order));
				OnCreateBrokerOrder(physicalOrder);
			} else {
				throw new ApplicationException("BrokerOrder property want's an Order object.");
			}
		}
	}
}
