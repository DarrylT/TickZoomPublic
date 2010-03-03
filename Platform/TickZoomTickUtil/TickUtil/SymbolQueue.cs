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
using System.Collections.Generic;
using System.Text;
using System.Threading;
using TickZoom;
using TickZoom.Api;
using TickZoom.TickUtil;

namespace TickZoom.TickUtil
{
	public class SymbolQueue : Receiver {
		static readonly Log log = Factory.Log.GetLogger(typeof(SymbolQueue));
		static readonly bool debug = log.IsDebugEnabled;
		private SymbolInfo symbol;
		private Provider provider;
		private TickQueue tickQueue;
		private TimeStamp startTime;
		public TickBinary NextTick;
		private ReceiverState receiverState = ReceiverState.Ready;
		
		public ReceiverState OnGetReceiverState(SymbolInfo symbol) {
			return receiverState;
		}
		
		public SymbolQueue(SymbolInfo symbol, Provider provider, TimeStamp startTime) {
			this.symbol = symbol;
			this.provider = provider;
			this.tickQueue = Factory.TickUtil.TickQueue(typeof(SymbolQueue));
			this.tickQueue.StartEnqueue = Start;
			this.startTime = startTime;
			NextTick = new TickBinary();
			NextTick.Symbol = symbol.BinaryIdentifier;
			provider.Start(this);
		}
		
		private void Start() {
			provider.StartSymbol(this,symbol,startTime);
		}
		
		public Provider Provider {
			get { return provider; }
		}
		
		public SymbolInfo Symbol {
			get { return symbol; }
		}
		
		public void OnStart()
		{
		}
		
		public bool CanReceive {
			get { return tickQueue.CanEnqueue; }
		}
		
		public void OnSend(ref TickBinary o)
		{
			tickQueue.EnQueue(ref o);
		}

        public void OnPositionChange(SymbolInfo symbol, LogicalFillBinary fill)
	    {
	        throw new NotImplementedException();
	    }

	    public void Receive(ref TickBinary tick) {
			tickQueue.Dequeue(ref tick);
		}
		
		public void OnRealTime(SymbolInfo symbol1) {
			tickQueue.EnQueue(EntryType.StartRealTime, symbol);
		}
		
		public void OnHistorical(SymbolInfo symbol1) {
			tickQueue.EnQueue(EntryType.StartHistorical, symbol1);
		}
		
		public void OnStop()
		{
			tickQueue.EnQueue(EntryType.Terminate, symbol);
		}
		
		public void OnEndHistorical(SymbolInfo symbol1)
		{
			tickQueue.EnQueue(EntryType.EndHistorical, symbol);
		}
		
		public void OnEndRealTime(SymbolInfo symbol1)
		{
			tickQueue.EnQueue(EntryType.EndRealTime, symbol);
		}
		
		public void OnError(string error)
		{
			tickQueue.EnQueue(EntryType.Error, error);
		}
	}
}
