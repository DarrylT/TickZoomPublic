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
using System.Diagnostics;
using System.Drawing;
using System.Reflection;

using TickZoom.Api;

namespace TickZoom.Common
{

	public partial class Model : ModelEvents, ModelInterface
	{
		string name;
		Chain chain;
		protected bool isStrategy = false;
		protected bool isIndicator = false;
		Interval intervalDefault = Intervals.Default;
		string symbolDefault = "Default";
		DrawingInterface drawing;
		bool isActive = true;
	
		List<Interval> updateIntervals = new List<Interval>();
		Data data;
		Chart chart;
		Context context;
		Formula formula;
		private static readonly Log log = Factory.Log.GetLogger(typeof(Model));
		private static readonly bool debug = log.IsDebugEnabled;
		private static readonly bool trace = log.IsTraceEnabled;
		bool isOptimizeMode = false;
		List<StrategyInterceptor> strategyInterceptors = new List<StrategyInterceptor>();
		Dictionary<EventType,List<EventInterceptor>> eventInterceptors = new Dictionary<EventType,List<EventInterceptor>>();
		List<EventType> events = new List<EventType>();
		
		public Model()
		{
			name = GetType().Name;
			fullName = name;
			
			drawing = new DrawingCommon(this);
			formula = new Formula(this);
			
			if( trace) log.Trace(GetType().Name+".new");
			chain = Factory.Engine.Chain(this);

			RequestEvent( EventType.Open);
			RequestEvent( EventType.Close);
			RequestEvent( EventType.Tick);
			RequestEvent( EventType.LogicalFill);
			RequestEvent( EventType.EndHistorical);
		}

		public void AddInterceptor(StrategyInterceptor interceptor) {
			strategyInterceptors.Add(interceptor);
		}
		
		public void InsertInterceptor(StrategyInterceptor interceptor) {
			strategyInterceptors.Insert(0,interceptor);
		}
		
		public void AddInterceptor(EventType eventType, EventInterceptor interceptor) {
			List<EventInterceptor> list = GetInterceptorList(eventType);
			list.Add(interceptor);
		}
		
		public void InsertInterceptor(EventType eventType, EventInterceptor interceptor) {
			List<EventInterceptor> list = GetInterceptorList(eventType);
			list.Insert(0,interceptor);
		}
		
		private List<EventInterceptor> GetInterceptorList(EventType eventType) {
			List<EventInterceptor> list;
			if( eventInterceptors.TryGetValue( eventType, out list)) {
				return list;
			} else {
				list = new List<EventInterceptor>();
				eventInterceptors.Add(eventType,list);
				return list;
			}
		}

		public void RequestEvent( EventType eventType) {
			events.Add(eventType);
		}
		
		public void RequestUpdate( Interval interval) {
			updateIntervals.Add(interval);
		}
		
		[Browsable(false)]
		public IList<Interval> UpdateIntervals {
			get { return updateIntervals; }
		}
		
		public virtual bool OnWriteReport(string folder) {
			return false;
		}
	
		public virtual double OnCalculateProfitLoss(double position, double entry, double exit) {
			throw new NotImplementedException("The performance object ignores this method unless you override and provide your own implementation.");
		}

		[Browsable(false)]
		public virtual Interval IntervalDefault {
			get { return intervalDefault; }
			set { intervalDefault = value; }
		}
		
		public void AddIndicator( IndicatorCommon indicator) {
			if( chain.Dependencies.Contains(indicator.Chain)) {
				throw new ApplicationException( "Indicator " + indicator.Name + " already added.");
			}
			if( this is Strategy) {
				Strategy strategy = this as Strategy;
				indicator.Performance = strategy.Performance;
			} else if( this is IndicatorCommon) {
				IndicatorCommon thisIndicator = this as IndicatorCommon;
				indicator.Performance = thisIndicator.Performance;
			} else if( this is Portfolio) {
				Portfolio thisPortfolio = this as Portfolio;
				indicator.Performance = thisPortfolio.Performance;
			} else {
				throw new ApplicationException("Sorry, indicators can only be added to objects derived from " +
				                               typeof(Strategy).Name + ", " +
				                               typeof(Portfolio).Name + ", or " +
				                               typeof(IndicatorCommon).Name + ".");
			}
			// Apply Properties from project.xml, if any.
			if( properties != null) {
				string[] keys = properties.GetModelKeys();
				for( int i=0; i<keys.Length; i++) {
					ModelProperties indicatorProperties = properties.GetModel(keys[i]);
					if( indicator.name.Equals(indicatorProperties.Name) &&
					    indicatorProperties.ModelType == ModelType.Indicator) {
						indicator.OnProperties(indicatorProperties);
						break;
					}
				}
			}
			chain.Dependencies.Add(indicator.Chain);
		}
	
		[Browsable(false)]
		public Chart Chart {
			get { return chart; }
			set { chart = value; }
		}
		
		public override string ToString()
		{
			return name;
		}
		
		#region Convenience methods to access bar data
		[Browsable(false)]
		public Data Data {
			get { return data; }
			set { data = value; }
		}
		
		Bars years = null;
		[Browsable(false)]
		public Bars Years {
			get {
				if( years == null) years = data.Get(Intervals.Year1);
				return years;
			}
		}
		
		Bars months = null;
		[Browsable(false)]
		public Bars Months {
			get {
				if( months == null) months = data.Get(Intervals.Month1);
				return months;
			}
		}
		
		Bars weeks = null;
		[Browsable(false)]
		public Bars Weeks {
			get {
				if( weeks == null) weeks = data.Get(Intervals.Week1);
				return weeks;
			}
		}
		
		Bars days = null;
		[Browsable(false)]
		public Bars Days {
			get {
				if( days == null) days = data.Get(Intervals.Day1);
				return days;
			}
		}
		
		Bars hours = null;
		[Browsable(false)]
		public Bars Hours {
			get {
				if( hours == null) hours = data.Get(Intervals.Hour1);
				return hours;
			}
		}
		
		Bars minutes = null;
		[Browsable(false)]
		public Bars Minutes {
			get {
				if( minutes == null) minutes = data.Get(Intervals.Minute1);
				return minutes;
			}
		}
		
		Bars sessions = null;
		[Browsable(false)]
		public Bars Sessions {
			get {
				if( sessions == null) sessions = data.Get(Intervals.Session1);
				return sessions;
			}
		}
		
		Bars range5 = null;
		[Browsable(false)]
		public Bars Range5 {
			get {
				if( range5 == null) range5 = data.Get(Intervals.Session1);
				return range5;
			}
		}
		
		Ticks ticks = null;
		[Browsable(false)]
		public Ticks Ticks {
			get { 
				if( ticks == null) ticks = data.Ticks;
				return ticks; }
		}
	
		Bars bars = null; 
		[Browsable(false)]
		public Bars Bars {
			get { return bars; }
			set { bars = value; }
		}
		#endregion
	
		[Browsable(false)]
		public Context Context {
			get { return context; }
			set { context = value; }
		}
		
		public virtual void Save( string fileName) {
			
		}
		
		public virtual void OnEvent(EventContext context, EventType eventType, object eventDetail) {
			switch( eventType) {
				case EventType.Initialize:
					OnInitialize();
					break;
				case EventType.Open:
					if( eventDetail == null) {
						OnBeforeIntervalOpen();
						OnIntervalOpen();
					} else {
						OnBeforeIntervalOpen((Interval)eventDetail);
						OnIntervalOpen((Interval)eventDetail);
					}
					break;
				case EventType.Close:
					if( eventDetail == null) {
						OnBeforeIntervalClose();
						OnIntervalClose();
					} else {
						OnBeforeIntervalClose((Interval)eventDetail);
						OnIntervalClose((Interval)eventDetail);
					}
					break;
				case EventType.Tick:
					OnProcessTick((Tick)eventDetail);
					break;
				case EventType.EndHistorical:
					OnEndHistorical();
					break;
			}
		}
		
		public void AddDependency( ModelInterface formula) {
			chain.Dependencies.Add(formula.Chain);
		}
	
		[Browsable(false)]
		public bool IsStrategy {
			get { return isStrategy; }
		}
		
		[Browsable(false)]
		public bool IsIndicator{
			get { return isIndicator; }
		}
		
		[Browsable(false)]
		public virtual string Name {
			get { return name; }
			set { name = value; }
		}
	
		[Browsable(false)]
		public Chain Chain {
			get { return chain; }
			set { chain = value; }
		}
		
		[Obsolete("Please, use FullName property instead.",true)]
		public string LogName {
			get { return name.Equals(GetType().Name) ? name : name+"."+GetType().Name; }
		}
	
		string fullName;
		public virtual string FullName {
			get { return fullName; }
			set { fullName = value; }
		}
		
		public Integers Integers() {
			return Factory.Engine.Integers();
		}
		
		public Integers Integers(int capacity) {
			return Factory.Engine.Integers(capacity);
		}
		
		public Doubles Doubles() {
			return Factory.Engine.Doubles();
		}
		
		public Doubles Doubles(int capacity) {
			return Factory.Engine.Doubles(capacity);
		}
		
		public Doubles Doubles(object obj) {
			return Factory.Engine.Doubles(obj);
		}
		
		public Series<T> Series<T>() {
			return Factory.Engine.Series<T>();
		}
		
		[Browsable(false)]
		public virtual DrawingInterface Drawing {
			get { return drawing; }
			set { drawing = value; }
		}
		
		[Obsolete("This method is never used. Override OnGetFitness() in a strategy instead.",true)]
		public virtual double Fitness {
			get { return 0; }
		}
		
		[Obsolete("Override OnGetFitness() or OnStatistics() in a strategy instead.",true)]
		public virtual string OnOptimizeResults() {
			throw new NotImplementedException();
		}
		
		[Browsable(false)]
		public Formula Formula {
			get { return formula; }
		}
		
		[Browsable(false)]
		public Provider Provider {
			get {
				throw new NotImplementedException();
			}
			set {
				throw new NotImplementedException();
			}
		}
		
		public bool IsOptimizeMode {
			get { return isOptimizeMode; }
			set { isOptimizeMode = value; }
		}
	
		public virtual string SymbolDefault {
			get { return symbolDefault; }
			set { symbolDefault = value; }
		}
		
		public Dictionary<EventType,List<EventInterceptor>> EventInterceptors {
			get { return eventInterceptors; }
		}
		
		public List<EventType> Events {
			get { return events; }
		}
		
		public List<StrategyInterceptor> StrategyInterceptors {
			get { return strategyInterceptors; }
		}

		/// <summary>
		/// Whether receiving events from the data engine or not.
		/// </summary>
		public bool IsActive {
			get { return isActive; }
			set { isActive = value; }
		}
	}


	/// <summary>
	/// Description of Formula.
	/// </summary>
	[Obsolete("Please use Model instead.",true)]
	public class ModelCommon : Model {
		
	}
	
}
