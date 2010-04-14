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
 * Date: 4/4/2009
 * Time: 8:38 AM
 * <http://www.tickzoom.org/wiki/Licenses>.
 */
#endregion

using System;

namespace TickZoom.Api
{
	/// <summary>
	/// Description of ChartProperties.
	/// </summary>
	public interface ChartProperties
	{
		bool ShowPriceGraph {
			get;
			set;
		}
		
		/// <summary>
		/// Obsolete. Please use only IntervalChartBar instead.
		/// </summary>
		[Obsolete("Please use only IntervalChartBar instead.",true)]
		Interval IntervalChartDisplay {
			get;
			set;
		}
		
		/// <summary>
		/// Obsolete. Please use only IntervalChartBar instead.
		/// </summary>
		[Obsolete("Please use only IntervalChartBar instead.",true)]
		Interval IntervalChartUpdate {
			get;
			set;
		}
		
		Interval IntervalChartBar {
			get;
			set;
		}
		
		ChartType ChartType {
			get;
			set;
		}
		
		void CopyProperties(object obj);
	}
}
