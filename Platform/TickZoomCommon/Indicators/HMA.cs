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
	/// The Hull Moving Average (HMA), developed by Alan Hull, is an extremely 
	/// fast and smooth moving average. In fact, the HMA almost eliminates lag 
	/// altogether and manages to improve smoothing at the same time.
	/// FB 20091230: Created
	/// </summary>
	public class HMA : IndicatorCommon
	{
		WMA W1;
		WMA W2;
		WMA W3;
		int period = 14;
		double halfPeriod;
		double sqrtPeriod;
		IndicatorCommon temp;
		
		public HMA(object anyPrice, int period) {
			AnyInput = anyPrice;
			StartValue = 0;
			this.period = period;
		}

		public override void OnInitialize() {
			Name = "HMA";
			Drawing.Color = Color.Green;
			Drawing.PaneType = PaneType.Primary;
			Drawing.IsVisible = true;
			
			temp = Formula.Indicator();
			halfPeriod = (Math.Ceiling(Convert.ToDouble(period/2)) - (Convert.ToDouble(period/2)) <= 0.5) ? Math.Ceiling(Convert.ToDouble(period/2)) : Math.Floor(Convert.ToDouble(period/2));
			sqrtPeriod = (Math.Ceiling(Math.Sqrt(Convert.ToDouble(period))) - Math.Sqrt(Convert.ToDouble(period)) <= 0.5) ? Math.Ceiling(Math.Sqrt(Convert.ToDouble(period))) : Math.Floor(Math.Sqrt(Convert.ToDouble(period)));
			W1 = Formula.WMA(Input, Convert.ToInt32(halfPeriod));
			W2 = Formula.WMA(Input, period);
		}
				
		public override void Update() {
			if (Count < period + 1 ) {
				this[0] = Input[0];
			} else {
				temp[0] = 2 * W1[0] - W2[0];
				W3 = Formula.WMA(temp, Convert.ToInt32(sqrtPeriod));
				this[0] = W3[0];
			}
		}

		public int Period {
			get { return period; }
			set { period = value; }
		}
	}
}
