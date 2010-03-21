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
using System.Drawing;
using TickZoom.Api;

namespace TickZoom.Common
{
	/// <summary>
	/// Description of SMA.
	/// </summary>
	public class PercentR : IndicatorCommon
	{
		int period;
		
		public PercentR() : this(5)
		{
		}
		
		public PercentR(int period)
		{
			this.period = period;
		}
		
		public override void OnInitialize() {
			Drawing.Color = Color.Blue;
			Drawing.PaneType = PaneType.Secondary;
			Drawing.IsVisible = true;
		}
		
		public override bool OnIntervalClose() {
			double high = Formula.Highest(Bars.High,period);
			double low = Formula.Lowest(Bars.Low,period);
			double last = Bars.Close[0];
			double result = (last - low) * 100 / (high - low + 1);
			this[0] = result;
			
			return true;
		}
		public int Period {
			get { return period; }
			set { period = value; }
		}
	}
}
