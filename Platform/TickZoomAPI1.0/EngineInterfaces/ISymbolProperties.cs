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
 * Date: 4/2/2009
 * Time: 8:16 AM
 * <http://www.tickzoom.org/wiki/Licenses>.
 */
#endregion

using System;
using System.Collections.Generic;

namespace TickZoom.Api
{
	public interface ISymbolProperties : SymbolInfo
	{
		/// <summary>
		/// The time of day that the primary session for this symbol starts.
		/// </summary>
		new Elapsed SessionStart {
			get;
			set;
		}
		
		/// <summary>
		/// The time of day that the primary session for this symbol ends.
		/// </summary>
		new Elapsed SessionEnd {
			get;
			set;
		}
	 
 		/// <summary>
 		/// With which other symbols with this one get drawn on a chart? Returns
 		/// a group number where 0 means never draw this symbol on any chart.
 		/// All symbols with that same ChartGroup number will appear on the same
 		/// chart. You can only set this property inside your Loader before
 		/// the engine initializes the portfolios and strategies.
 		/// </summary>
 		new int ChartGroup {
 			get;
 			set;
 		}
	}
}
