#region Copyright
/*
 * Software: TickZoom Trading Platform
 * Copyright 2009 M. Wayne Walter
 * 
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, see <http://www.tickzoom.org/wiki/Licenses>
 * or write to Free Software Foundation, Inc., 51 Franklin Street,
 * Fifth Floor, Boston, MA  02110-1301, USA.
 * 
 */
#endregion

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

using TickZoom.Api;

namespace TickZoom.Common
{
	public class Strategy : Model, StrategyInterface 
	{
		PositionInterface position;
		private static readonly Log log = Factory.Log.GetLogger(typeof(Strategy));
		private static readonly bool debug = log.IsDebugEnabled;
		private static readonly bool trace = log.IsTraceEnabled;
		private readonly Log instanceLog;
		private readonly bool instanceDebug;
		private readonly bool instanceTrace;
		private Result result;
		private List<LogicalOrder> allOrders = new List<LogicalOrder>();
		private List<LogicalOrder> tempActiveOrders = new List<LogicalOrder>();
		private List<LogicalOrder> activeOrders = new List<LogicalOrder>();
		private List<LogicalOrder> nextBarOrders = new List<LogicalOrder>();
		private bool isActiveOrdersChanged = false;
		
		OrderHandlers orders;
		ExitCommon exitActiveNow;
		EnterCommon enterActiveNow;
		ExitCommon exitNextBar;
		EnterCommon enterNextBar;
		Performance performance;
		PositionSize positionSize;
		ExitStrategy exitStrategy;
		OrderManager preOrderManager;
		OrderManager postOrderManager;
		
		public Strategy()
		{
			instanceLog = Factory.Log.GetLogger(this.GetType()+"."+Name);
			instanceDebug = instanceLog.IsDebugEnabled;
			instanceTrace = instanceLog.IsTraceEnabled;
			position = new PositionCommon(this);
			if( trace) log.Trace("Constructor");
			Chain.Dependencies.Clear();
			isStrategy = true;
			result = new Result(this);
			/// <summary>
			/// 
			/// </summary>
			
			exitActiveNow = new ExitCommon(this);
			enterActiveNow = new EnterCommon(this);
			exitNextBar = new ExitCommon(this);
			exitNextBar.Orders = exitActiveNow.Orders;
			exitNextBar.IsNextBar = true;
			enterNextBar = new EnterCommon(this);
			enterNextBar.Orders = enterActiveNow.Orders;
			enterNextBar.IsNextBar = true;
			orders = new OrderHandlers(enterActiveNow,enterNextBar,exitActiveNow,exitNextBar);
			
			// Interceptors.
			performance = new Performance(this);
		    positionSize = new PositionSize(this);
		    exitStrategy = new ExitStrategy(this);
		    preOrderManager = Factory.Engine.OrderManager(this);
			postOrderManager = Factory.Engine.OrderManager(this);
			postOrderManager.PostProcess = true;
			postOrderManager.ChangePosition = exitStrategy.Position.Change;
			postOrderManager.DoEntryOrders = false;
			postOrderManager.DoExitOrders = false;
			postOrderManager.DoExitStrategyOrders = true;
		}
		
		public override void OnConfigure()
		{
			exitActiveNow.OnInitialize();
			enterActiveNow.OnInitialize();
			exitNextBar.OnInitialize();
			enterNextBar.OnInitialize();
			exitNextBar.OnInitialize();
			base.OnConfigure();

			BreakPoint.TrySetStrategy(this);
			AddInterceptor(preOrderManager);
			AddInterceptor(performance.Equity);
			AddInterceptor(performance);
			AddInterceptor(positionSize);
			AddInterceptor(exitStrategy);
			AddInterceptor(postOrderManager);
		}
		
		public override void OnEvent(EventContext context, EventType eventType, object eventDetail)
		{
			base.OnEvent(context, eventType, eventDetail);
			if( context.Position == null) {
				context.Position = new PositionCommon(this);
			}
			context.Position.Copy(Position);
		}
		
		[Obsolete("Please, use OnGetOptimizeResult() instead.",true)]
		public virtual string ToStatistics() {
			throw new NotImplementedException();
		}
		
		public virtual string OnGetOptimizeHeader(Dictionary<string,object> optimizeValues)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("Fitness");
			foreach( KeyValuePair<string,object> kvp in optimizeValues) {
				sb.Append(",");
				sb.Append(kvp.Key);
			}
			return sb.ToString();
		}
		
		public virtual string OnGetOptimizeResult(Dictionary<string,object> optimizeValues)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(OnGetFitness());
			foreach( KeyValuePair<string,object> kvp in optimizeValues) {
				sb.Append(",");
				sb.Append(kvp.Value);
			}
			return sb.ToString();
		}
		
		public override bool OnWriteReport(string folder)
		{
			return performance.WriteReport(Name,folder);
		}
		
		public void OrderModified( LogicalOrder order) {
			if( order.IsActive ) {
				// Any change to an active order even if only 
				// a price change means the list change.
				IsActiveOrdersChanged = true;
				if( !activeOrders.Contains(order)) {
					bool found = false;
					for( int i=0; i<activeOrders.Count; i++) {
						LogicalOrder other = activeOrders[i];
						if( order.CompareTo(other) < 0) {
							activeOrders.Insert(i,order);
							found = true;
							break;
						}
					}
					if( !found) {
						activeOrders.Add(order);
					}
				}
			} else {
				if( activeOrders.Contains(order)) {
					activeOrders.Remove(order);
					// Since this order became inactive, it
					// means the active list changed.
					IsActiveOrdersChanged = true;
				}
			}
			if( order.IsNextBar) {
				if( !nextBarOrders.Contains(order)) {
					nextBarOrders.Add(order);
				}
			} else {
				if( nextBarOrders.Contains(order)) {
					nextBarOrders.Remove(order);
				}
			}
		}
		
		[Browsable(true)]
		[Category("Strategy Settings")]		
		public override Interval IntervalDefault {
			get { return base.IntervalDefault; }
			set { base.IntervalDefault = value; }
		}
		
		[Browsable(false)]
		public Strategy Next {
			get { return Chain.Next.Model as Strategy; }
		}
		
		[Browsable(false)]
		public Strategy Previous {
			get { return Chain.Previous.Model as Strategy; }
		}
		
		[Browsable(false)]
		public override string Name {
			get { return base.Name; }
			set { base.Name = value; }
		}

		public OrderHandlers Orders {
			get { return orders; }
		}
		
		[Obsolete("Please user Orders.Exit instead.",true)]
		public ExitCommon ExitActiveNow {
			get { return exitActiveNow; }
		}
		
		[Obsolete("Please user Orders.Enter instead.",true)]
		public EnterCommon EnterActiveNow {
			get { return enterActiveNow; }
		}
		
		[Category("Strategy Settings")]
		public ExitStrategy ExitStrategy {
			get { return exitStrategy; }
			set { exitStrategy = value; }
		}
		
		[Category("Strategy Settings")]
		public PositionSize PositionSize {
			get { return positionSize; }
			set { positionSize = value; }
		}

		[Category("Strategy Settings")]
		public Performance Performance {
			get { return performance; }
			set { performance = value;}
		}

		public virtual double OnGetFitness() {
			EquityStats stats = Performance.Equity.CalculateStatistics();
			return stats.Daily.SortinoRatio;
		}
		
		public Log Log {
			get { return instanceLog; }
		}
		
		public bool IsDebug {
			get { return instanceDebug; }
		}
		
		public bool IsTrace {
			get { return instanceTrace; }
		}
		
		public virtual void OnEnterTrade() {
			
		}
		
		public virtual void OnExitTrade() {
			
		}
		
		public PositionInterface Position {
			get { return position; }
			set { position = value; }
		}
		
		public ResultInterface Result {
			get { return result; }
		}


		public void AddOrder(LogicalOrder order)
		{
			allOrders.Add(order);
		}
		
		public void RefreshActiveOrders() {
			tempActiveOrders.Clear();
			tempActiveOrders.AddRange(activeOrders);
		}
		
		public IList<LogicalOrder> AllOrders {
			get {
				return allOrders;
			}
		}
		
		public LogicalOrder GetOrder(int orderId) {
			foreach( var order in allOrders) {
				if( order.Id == orderId){
					return order;
				}
			}
			throw new ApplicationException("Logical Order Id " + orderId + " was not found.");
		}
		
		public IList<LogicalOrder> ActiveOrders {
			get {
				return tempActiveOrders;
			}
		}
		
		public bool IsActiveOrdersChanged {
			get { return isActiveOrdersChanged; }
			set { isActiveOrdersChanged = value; }
		}
		
		public bool IsExitStrategyFlat {
			get { return exitStrategy.Position.IsFlat && position.HasPosition; }
		}
	}
	
	[Obsolete("Please user Strategy instead.",true)]
	public class StrategyCommon : Strategy {
		
	}
}