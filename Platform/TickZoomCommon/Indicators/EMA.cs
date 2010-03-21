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
	/// Exponential Moving Average (EMA), a basic recursive MA. 
	/// FB 20091230: cleaned up
	/// </summary>
	public class EMA : IndicatorCommon
	{
		int period = 13;

		public EMA(object anyPrice, int period)	{
			AnyInput = anyPrice;
			StartValue = 0;
			this.period = period;
		}
		
		public override void OnInitialize()	{
			Name = "EMA";
			Drawing.Color = Color.Green;
			Drawing.PaneType = PaneType.Primary;
			Drawing.IsVisible = true;
			Drawing.GroupName = "EMA";
			Drawing.GraphType = GraphType.Line;
		}

		public override void Update() {
			if(Count < period + 1) this[0] = Input[0];
			else this[0] = Input[0] * (2 / (period + 1)) + this[1] * (1 - (2 / (period + 1)));
		}
		
		public int Period {
			get { return period; }
			set { period = value; }
		}

	}
}
