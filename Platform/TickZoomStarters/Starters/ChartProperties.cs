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
 * Date: 3/18/2009
 * Time: 6:52 PM
 * <http://www.tickzoom.org/wiki/Licenses>.
 */
#endregion

using System;
using System.ComponentModel;
using System.Drawing.Design;

using TickZoom.Api;

namespace TickZoom.Common
{

	
	/// <summary>
	/// Description of ChartProperties.
	/// </summary>
	public class ChartProperties : PropertiesBase, TickZoom.Api.ChartProperties
	{
		public readonly static Interval IntervalDay1 = Factory.Engine.DefineInterval(BarUnit.Day,1);
		
		TickZoom.Api.Interval intervalChartBar = IntervalDay1;
		
		public ChartProperties()
		{
			// Avoid exceptions during design mode.
			try {
				intervalChartBar = TickZoom.Api.Factory.Engine.DefineInterval(TickZoom.Api.BarUnit.Default,0);
				intervalChartBar = TickZoom.Api.Factory.Engine.DefineInterval(TickZoom.Api.BarUnit.Default,0);
				intervalChartBar = TickZoom.Api.Factory.Engine.DefineInterval(TickZoom.Api.BarUnit.Default,0);
			} catch {
				
			}
		}

		bool showPriceGraph = true;
		
		public bool ShowPriceGraph {
			get { return showPriceGraph; }
			set { showPriceGraph = value; }
		}
		
		/// <summary>
		/// Obsolete. Please use only IntervalChartBar instead.
		/// </summary>
		[Obsolete("Please use only IntervalChartBar instead.",true)]
		public Interval IntervalChartDisplay {
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}
		
		/// <summary>
		/// Obsolete. Please use only IntervalChartBar instead.
		/// </summary>
		[Obsolete("Please use only IntervalChartBar instead.",true)]
		public Interval IntervalChartUpdate {
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}
		
		public TickZoom.Api.Interval IntervalChartBar {
			get { return intervalChartBar; }
			set { intervalChartBar = value; }
		}

		TickZoom.Api.ChartType chartType = TickZoom.Api.ChartType.Bar;
		public TickZoom.Api.ChartType ChartType {
			get { return chartType; }
			set { chartType = value; }
		}
		
		public override string ToString()
		{
			return "";
		}
	}
}
