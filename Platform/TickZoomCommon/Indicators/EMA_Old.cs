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
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with this program; if not, see <http://www.tickzoom.org/wiki/Licenses>
* or write to Free Software Foundation, Inc., 51 Franklin Street,
* Fifth Floor, Boston, MA 02110-1301, USA.
*
*/
#endregion
 
using System;
using TickZoom.Api;
 
namespace TickZoom.Common
{
	/// <summary>
	/// Description of EMA.
	/// </summary>
	public class EMA_Old : IndicatorCommon
	{
		int period;
		 
		public EMA_Old(int period)
		{
			this.period = period;
		}
		 
		public void Reset(double value) {
			if( Count > 1) {
			this[1] = this[0] = value;
			}
		}
		 
		public void Set(double value) {
			if( Count == 1) {
				this[0] = value;
			} else {
				double last = this[1];
				this[0] = value * (2 / (1 + period)) + (1 - (2 / (1 + period))) * last;
			}
		}
	}
}

