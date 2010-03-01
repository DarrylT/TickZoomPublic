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
 * Date: 5/18/2009
 * Time: 12:54 PM
 * <http://www.tickzoom.org/wiki/Licenses>.
 */
#endregion

using System;
using System.Collections.Generic;
using TickZoom.Api;

namespace TickZoom.Api
{
	public interface FillSimulator
	{
		void ProcessOrders(Tick tick, IList<LogicalOrder> orders, double position);
		Func<LogicalOrder, double, double, int> DrawTrade { get; set; }
		Action<SymbolInfo, double, double, TimeStamp> ChangePosition { get; set; }
		bool IsSyntheticMarkets { get; set; }
		bool IsSyntheticLimits { get; set; }
		bool IsSyntheticStops { get; set; }
		SymbolInfo Symbol { get; set; }
	}
}
