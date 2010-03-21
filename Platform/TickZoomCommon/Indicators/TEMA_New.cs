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
	/// Triple Exponential Moving Average (TEMA). Presented by Patrick Mulloy in 
	/// the January 1994 issue of Technical Analysis of Stocks & Commodities magazine. 
	/// TEMA has less lag than a Exponential Moving Average (EMA). It is not a simple
	/// triple smoothing, but rather a composite of several EMAs.
	/// Pseudocode:
	/// TEMA = 3*EMA() -  3*EMA(EMA) + EMA(EMA(EMA))
	/// </summary>
	public class TEMA_New : IndicatorCommon
	{
		EMA E1;
		EMA E2;
		EMA E3;
		int period = 14;
		
		public TEMA_New( object anyPrice, int period) {
			AnyInput = anyPrice;
			StartValue = 0;
			this.period = period;
		}

		public override void OnInitialize() {
			Name = "TEMA_New";
			Drawing.Color = Color.LightBlue;
			Drawing.PaneType = PaneType.Primary;
			Drawing.IsVisible = true;
			E1 = Formula.EMA(Input, Period);
			E2 = Formula.EMA(E1, Period);
			E3 = Formula.EMA(E2, Period);
		}
		
		public override void Update() {
			this[0] = (3 * E1[0] - 3 * E2[0] + E3[0]);
			double result = this[0];
		}

		public int Period {
			get { return period; }
			set { period = value; }
		}
	}
}
