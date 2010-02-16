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
	public class SymbolHandler {
		private TickIO tickIO = Factory.TickUtil.TickIO();
		private Receiver receiver;
		private SymbolInfo symbol;
		private bool isInitialized = false;
		public int Position;
		public int BidSize;
		public double Bid;
		public int AskSize;
		public double Ask;
		public int LastSize;
		public double Last;
		public SymbolHandler(SymbolInfo symbol, Receiver receiver) {
			this.symbol = symbol;
			this.receiver = receiver;
		}
		public void SendQuote() {
			if( isInitialized) {
				if( symbol.QuoteType == QuoteType.Level1) {
					tickIO.Initialize();
					tickIO.SetSymbol(symbol.BinaryIdentifier);
					tickIO.SetTime(TimeStamp.UtcNow);
					tickIO.SetQuote(Bid,Ask,(ushort)BidSize,(ushort)AskSize);
					TickBinary binary = tickIO.Extract();
					receiver.OnSend(ref binary);
				}
			} else {
				VerifyInitialized();
			}
		}
		private void VerifyInitialized() {
			if(BidSize > 0 && Bid > 0 && AskSize > 0 && Ask > 0 && LastSize > 0 & Last > 0) {
				isInitialized = true;
			}
		}
		public void SendTimeAndSales() {
			if( isInitialized) {
				if( symbol.TimeAndSales == TimeAndSales.ActualTrades) {
					tickIO.Initialize();
					tickIO.SetSymbol(symbol.BinaryIdentifier);
					tickIO.SetTime(TimeStamp.UtcNow);
					tickIO.SetTrade(Last,LastSize);
					if( symbol.QuoteType == QuoteType.Level1) {
						tickIO.SetQuote(Bid,Ask,(ushort)BidSize,(ushort)AskSize);
					}
					TickBinary binary = tickIO.Extract();
					receiver.OnSend(ref binary);
				}
			} else {
				VerifyInitialized();
			}
		}
	}
}
