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

using Krs.Ats.IBNet.Contracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Krs.Ats.IBNet;
using TickZoom.Api;

//using System.Data;
namespace TickZoom.InteractiveBrokers
{

	public class IBInterface : Provider
	{
		private static readonly Log log = Factory.Log.GetLogger(typeof(IBInterface));
		private static readonly bool debug = log.IsDebugEnabled;
        private readonly object readersLock = new object();
	    private readonly static object listLock = new object();
        private IBClient client;
        private int nextValidId = 0;
        
        private List<SymbolHandler> symbolHandlers = new List<SymbolHandler>();

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
            client.ExecDetails += new EventHandler<ExecDetailsEventArgs>(client_ExecDetails);
            client.UpdatePortfolio += client_UpdatePortfolio;
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
        	client.Disconnect();
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
		
		public void PositionChange(Receiver receiver, SymbolInfo symbol, double signal)
		{
			try {
				SymbolHandler handler = symbolHandlers[(int)symbol.BinaryIdentifier];
				int delta = (int)(signal - handler.Position);
				if( delta != 0) {
					Contract contract = new Contract(symbol.Symbol,"SMART",SecurityType.Stock,"USD");
					Order order = new Order();
					order.OrderType = Krs.Ats.IBNet.OrderType.Market;
					if( delta > 0) {
						order.Action = ActionSide.Buy;
					} else {
						order.Action = ActionSide.Sell;
					}
					order.TotalQuantity = Math.Abs(delta);
					while(nextValidId==0) {
						Thread.Sleep(10);
					}
					nextValidId++;
					client.PlaceOrder(nextValidId,contract,order);
					if(debug) log.Debug("PlaceOrder: " + nextValidId + " for " + contract.Symbol + " = " + order.TotalQuantity);
				}
			} catch( Exception ex) {
				log.Error(ex.Message,ex);
				throw;
			}
		}
		
		public void Stop()
		{	
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
        	if(debug) log.Debug("Order Status " + e.Status);
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
        	SymbolHandler buffer = symbolHandlers[e.TickerId];
        	if( e.TickType == TickType.AskSize) {
        		buffer.AskSize = e.Size;
        	} else if( e.TickType == TickType.BidSize) {
        		buffer.BidSize = e.Size;
        	} else if( e.TickType == TickType.LastSize) {
        		buffer.LastSize = e.Size;
        	}
        }
        
        private SymbolHandler GetSymbolHandler(SymbolInfo symbol, Receiver receiver) {
        	if( symbolHandlers.Count <= (int) symbol.BinaryIdentifier) {
	            while( symbolHandlers.Count <= (int) symbol.BinaryIdentifier) {
        			if( symbolHandlers.Count == 0) {
        				symbolHandlers.Add(null);
        			} else {
	        			SymbolInfo tempSymbol = Factory.Symbol.LookupSymbol((ulong)symbolHandlers.Count);
    	    			symbolHandlers.Add(new SymbolHandler(tempSymbol,receiver));
        			}
	            }
        	}
        	return symbolHandlers[(int)symbol.BinaryIdentifier];
        }

        private void client_Error(object sender, Krs.Ats.IBNet.ErrorEventArgs e)
        {
            log.Error("Error: "+ e.ErrorMsg);
        }

        private void client_TickPrice(object sender, TickPriceEventArgs e)
        {
        	SymbolHandler buffer = symbolHandlers[e.TickerId];
        	if( e.TickType == TickType.AskPrice) {
        		buffer.Ask = (double) e.Price;
        		buffer.SendQuote();
        	} else if( e.TickType == TickType.BidPrice) {
        		buffer.Bid = (double) e.Price;
        		buffer.SendQuote();
        	} else if( e.TickType == TickType.LastPrice) {
        		buffer.Last = (double) e.Price;
        		buffer.SendTimeAndSales();
        	}
		}
        
        private void cilent_UpdateAccountSize(object sender, UpdateAccountValueEventArgs e) {
        }
        
        private void client_UpdatePortfolio(object sender, UpdatePortfolioEventArgs e) {
  			try {
        		SymbolInfo symbol = Factory.Symbol.LookupSymbol(e.Contract.Symbol);
        		SymbolHandler handler = GetSymbolHandler(symbol,receiver);
	        	handler.Position = e.Position;
	        	if(debug) log.Debug( "UpdatePortfolio: " + e.Contract.Symbol + " is " + e.Position);
  			} catch( ApplicationException ex) {
  				log.Warn("UpdatePortfolio: " + ex.Message);
  			}
        }
	}
}
