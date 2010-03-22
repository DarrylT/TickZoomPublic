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
 * Date: 8/7/2009
 * Time: 7:32 PM
 * <http://www.tickzoom.org/wiki/Licenses>.
 */
#endregion


using System;
using System.Collections.Generic;
using System.Threading;

using TickZoom.Api;
using TickZoom.TickUtil;

namespace TickZoom.Common
{
	public class VerifyFeedDefault : Receiver, VerifyFeed
	{
		private static readonly Log log = Factory.Log.GetLogger(typeof(VerifyFeed));
		private static readonly bool debug = log.IsDebugEnabled;
		private TickQueue tickQueue = Factory.TickUtil.TickQueue(typeof(VerifyFeed));
		private volatile bool isRealTime = false;
		private TaskLock syncTicks;
		private volatile ReceiverState receiverState = ReceiverState.Ready;

		public TickQueue TickQueue {
			get { return tickQueue; }
		}

		public ReceiverState OnGetReceiverState(SymbolInfo symbol)
		{
			return receiverState;
		}

		public VerifyFeedDefault()
		{
			tickQueue.StartEnqueue = Start;
		}

		public void Start()
		{
		}
		
		public long VerifyEvent(Action<SymbolInfo,int,object> assertTick, SymbolInfo symbol, int timeout)
		{
			return VerifyEvent(1, assertTick, symbol, timeout);
		}
		
		public long Verify(Action<TickIO, TickIO, ulong> assertTick, SymbolInfo symbol, int timeout)
		{
			return Verify(2, assertTick, symbol, timeout);
		}
		TickImpl lastTick = new TickImpl();
		
		int countLog = 0;
		TickBinary tickBinary = new TickBinary();
		TickImpl tick = new TickImpl();
		public long Verify(int expectedCount, Action<TickIO, TickIO, ulong> assertTick, SymbolInfo symbol, int timeout)
		{
			if (debug) log.Debug("VerifyFeed");
			syncTicks = SyncTicks.GetTickSync(symbol.BinaryIdentifier);
			int startTime = Environment.TickCount;
			count = 0;
			while (Environment.TickCount - startTime < timeout * 1000) {
				if (!tickQueue.CanDequeue)
					Thread.Sleep(100);
				if (tickQueue.CanDequeue) {
					if (HandleTick(expectedCount, assertTick, symbol)) {
						break;
					}
				}
			}
			return count;
		}
		
		public ReceiverState VerifyState(ReceiverState expectedState, SymbolInfo symbol, int timeout) {
			if (debug) log.Debug("VerifyFeed");
			syncTicks = SyncTicks.GetTickSync(symbol.BinaryIdentifier);
			int startTime = Environment.TickCount;
			count = 0;
			TickBinary binary = new TickBinary();
			while (Environment.TickCount - startTime < timeout * 1000) {
				if (tickQueue.CanDequeue) {
					try { 
						tickQueue.Dequeue(ref binary);
					} catch (QueueException ex) {
						if( HandleQueueException(ex)) {
							break;
						}
					}
				} else {
					Thread.Sleep(10);
				}
				if( receiverState == expectedState) {
					break;
				}
			}
			return receiverState;
		}
		
		public long VerifyEvent(int expectedCount, Action<SymbolInfo,int,object> assertEvent, SymbolInfo symbol, int timeout)
		{
			if (debug) log.Debug("VerifyEvent");
			int startTime = Environment.TickCount;
			count = 0;
			while (Environment.TickCount - startTime < timeout * 1000) {
				if (!tickQueue.CanDequeue)
					Thread.Sleep(100);
				if (tickQueue.CanDequeue) {
					if (HandleEvent(expectedCount, assertEvent, symbol)) {
						break;
					}
				}
			}
			return count;
		}
		
		public double VerifyPosition(double expectedPosition, SymbolInfo symbol, int timeout)
		{
			if (debug)
				log.Debug("VerifyFeed");
			int startTime = Environment.TickCount;
			count = 0;
			double position;
			TickBinary binary = new TickBinary();
			while (Environment.TickCount - startTime < timeout * 1000) {
				if (tickQueue.CanDequeue) {
					try { 
						tickQueue.Dequeue(ref binary);
					} catch (QueueException ex) {
						if( HandleQueueException(ex)) {
							break;
						}
					}
				} else {
					Thread.Sleep(10);
				}
				if( actualPositions.TryGetValue(symbol.BinaryIdentifier,out position)) {
					if( position == expectedPosition) {
						return expectedPosition;
					}
				}
			}
			if( actualPositions.TryGetValue(symbol.BinaryIdentifier,out position)) {
				return position;
			} else {
				throw new ApplicationException("Position was never set via call back.");
			}
		}

		private bool HandleTick(int expectedCount, Action<TickIO, TickIO, ulong> assertTick, SymbolInfo symbol)
		{
			try {
				tickQueue.Dequeue(ref tickBinary);
				tick.Inject(tickBinary);
				if (debug && countLog < 5) {
					log.Debug("Received a tick " + tick);
					countLog++;
				}
				startTime = Environment.TickCount;
				count++;
				if (count > 0) {
					assertTick(tick, lastTick, symbol.BinaryIdentifier);
				}
				lastTick.Copy(tick);
				syncTicks.Unlock();
				if (count >= expectedCount)
					return true;
			} catch (QueueException ex) {
				return( HandleQueueException(ex));
			}
			return false;
		}

		private bool HandleEvent(int expectedCount, Action<SymbolInfo,int,object> assertEvent, SymbolInfo symbol)
		{
			try {
				// Remove ticks just so as to get to the event we want to see.
				tickQueue.Dequeue(ref tickBinary);
				if (customEventType> 0) {
					assertEvent(customEventSymbol,customEventType,customEventDetail);
					count++;
				} else {
					Thread.Sleep(10);
				}
				if (count >= expectedCount)
					return true;
			} catch (QueueException ex) {
				return HandleQueueException(ex);
			}
			return false;
		}
		
		private bool HandleQueueException( QueueException ex) {
			switch (ex.EntryType) {
				case EventType.StartRealTime:
					receiverState = ReceiverState.RealTime;
					isRealTime = true;
					break;
				case EventType.EndHistorical:
					receiverState = ReceiverState.Ready;
					break;
				case EventType.EndRealTime:
					receiverState = ReceiverState.Ready;
					isRealTime = false;
					break;
				case EventType.Terminate:
					return true;
				default:
					throw new ApplicationException("Unexpected QueueException: " + ex.EntryType);
			}
			return false;
		}
		
		long count = 0;
		Task task;
		int startTime;
		public void StartTimeTheFeed()
		{
			startTime = Environment.TickCount;
			count = 0;
			countLog = 0;
			task = Factory.Parallel.Loop(this, TimeTheFeedTask);
		}

		public int EndTimeTheFeed()
		{
			task.Join();
			int endTime = Environment.TickCount;
			int elapsed = endTime - startTime;
			log.Notice("Processed " + count + " ticks in " + elapsed + "ms or " + (count * 1000 / elapsed) + "ticks/sec");
			Factory.TickUtil.TickQueue("Stats").LogStats();
			return elapsed / 1000;
		}

		public bool TimeTheFeedTask()
		{
			try {
				if (!tickQueue.CanDequeue)
					return false;
				tickQueue.Dequeue(ref tickBinary);
				tick.Inject(tickBinary);
				if (debug && count < 5) {
					log.Debug("Received a tick " + tick);
					countLog++;
				}
				count++;
				if (count % 1000000 == 0) {
					log.Notice("Read " + count + " ticks");
				}
				return true;
			} catch (QueueException ex) {
				if( HandleQueueException(ex)) {
					return true;
				} else {
					Factory.Parallel.CurrentTask.Stop();
				}
			}
			return false;
		}

		public void OnRealTime(SymbolInfo symbol)
		{
			isRealTime = true;
		}

		public void OnHistorical(SymbolInfo symbol)
		{
			receiverState = ReceiverState.Historical;
		}

		public bool CanReceive( SymbolInfo symbol) {
			return tickQueue != null && tickQueue.CanEnqueue;
		}

		public void OnSend(ref TickBinary o)
		{
			try {
				tickQueue.EnQueue(ref o);
			} catch (QueueException) {
				// Queue already terminated.
			}
		}

		Dictionary<ulong, double> actualPositions = new Dictionary<ulong, double>();

		public double GetPosition(SymbolInfo symbol)
		{
			return actualPositions[symbol.BinaryIdentifier];
		}

		public void OnPositionChange(SymbolInfo symbol, LogicalFillBinary fill)
		{
			log.Info("Got Logical Fill of " + symbol + " at " + fill.Price + " for " + fill.Position);
			actualPositions[symbol.BinaryIdentifier] = fill.Position;
		}

		public void OnStop()
		{
			Close();
		}

		public void OnError(string error)
		{
			log.Error(error);
			tickQueue.Terminate();
		}
		public void Close()
		{
			tickQueue.Terminate();
		}

		public void OnEndHistorical(SymbolInfo symbol)
		{
			tickQueue.EnQueue(EventType.EndHistorical, symbol);
		}

		public void OnEndRealTime(SymbolInfo symbol)
		{
			try {
				tickQueue.EnQueue(EventType.EndRealTime, symbol);
			} catch (QueueException) {
				// Queue was already ended.
			}
		}
		
		public bool IsRealTime {
			get { return isRealTime; }
		}
		
		volatile SymbolInfo customEventSymbol;
		volatile int customEventType;
		volatile object customEventDetail;
		public void OnCustomEvent(SymbolInfo symbol, int eventType, object eventDetail) {
			customEventSymbol = symbol;
			customEventType = eventType;
			customEventDetail = eventDetail;
		}
		
		public void OnEvent(SymbolInfo symbol, int eventType, object eventDetail) {
			try {
				switch( (EventType) eventType) {
					case EventType.Tick:
						TickBinary binary = (TickBinary) eventDetail;
						OnSend(ref binary);
						break;
					case EventType.EndHistorical:
						OnEndHistorical(symbol);
						break;
					case EventType.StartRealTime:
						OnRealTime(symbol);
						break;
					case EventType.StartHistorical:
						OnHistorical(symbol);
						break;
					case EventType.EndRealTime:
						OnEndRealTime(symbol);
						break;
					case EventType.Error:
						OnError((string)eventDetail);
						break;
					case EventType.LogicalFill:
						OnPositionChange(symbol,(LogicalFillBinary)eventDetail);
						break;
					case EventType.Terminate:
						OnStop();
			    		break;
					case EventType.Initialize:
					case EventType.Open:
					case EventType.Close:
					case EventType.PositionChange:
			    		throw new ApplicationException("Unexpected EventType: " + eventType);
					default:
			    		OnCustomEvent(symbol,eventType,eventDetail);
			    		break;
				}
			} catch( QueueException) {
				log.Warn("Already terminated.");
			}
		}
		
		public TickIO LastTick {
			get { return lastTick; }
		}
	}
}
