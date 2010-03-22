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
using NUnit.Framework;
using TickZoom.Api;
using TickZoom.Common;
using TickZoom.TickUtil;

namespace TickZoom.Indicators
{
	[TestFixture]
	public class EMATest2
	{
		private static readonly Log log = Factory.Log.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		EMA ema;
		TestBars bars;
		
		[TearDown]
		public void TearDown() {
			
		}
		
		[SetUp]
		public void Setup() {
			bars = Factory.Engine.TestBars(Intervals.Day1);
			ema = new EMA(bars.Close,14);
			Assert.IsNotNull(ema, "constructor");
			ema.IntervalDefault = Intervals.Day1;
			ema.Bars = bars;
			ema.OnInitialize();
			for(int j=0; j<ema.Chain.Dependencies.Count; j++) {
				ModelInterface formula = ema.Chain.Dependencies[j].Model;
				formula.Bars = bars;
				formula.OnInitialize();
			}
		}
		
		[Test]
		public void Values()
		{
			SymbolInfo symbol = Factory.Symbol.LookupSymbol("USD_JPY");
			for( int i = 0; i < data.Length; i++) {
				// open, high, low, close all the same.
				bars.AddBar( symbol, data[i], data[i], data[i], data[i], 0);
				for(int j=0; j<ema.Chain.Dependencies.Count; j++) {
					ModelInterface formula = ema.Chain.Dependencies[j].Model;
					formula.OnBeforeIntervalOpen();
					formula.OnIntervalClose();
				}
				ema.OnBeforeIntervalOpen();
				ema.OnIntervalClose();
				Assert.AreEqual(result[i],Math.Round(ema[0]),"current result at " + i);
				if( i > 1) Assert.AreEqual(result[i-1],Math.Round(ema[1]),"result 1 back at " + i);
				if( i > 2) Assert.AreEqual(result[i-2],Math.Round(ema[2]),"result 2 back at " + i);
			}
		}
		
		private int[] data = new int[] {
			10000,
			10100,
			10040,
			10200,
			10760,
			11190,
			11300,
			12030,
			12360,
			12150,
			12440,
			12910,
			13270,
			12550,
			11890,
			12350,
			11930,
			11900,
			11370,
			10820,
			10720,
			11570,
			12520,
			13290,
			13590,
			13850,
			13500,
			13810,
			14430,
			13800,
			14140,
			13850,
			13210,
			13480,
			14140,
			14250,
			13600,
			13160,
			12940,
			13670,
			13770,
			13150,
			12990,
			12360,
			12580,
			13220,
			12220,
			11800,
			12230,
			11580,
			10680,
			9940,
			10300,
			11030,
			11790,
			11890
		};
	
		private int[] result = new int[] {
			10000,
			10013,
			10016,
			10041,
			10137,
			10277,
			10413,
			10629,
			10860,
			11032,
			11219,
			11445,
			11688,
			11803,
			11814,
			11886,
			11892,
			11893,
			11823,
			11689,
			11560,
			11561,
			11689,
			11902,
			12127,
			12357,
			12509,
			12683,
			12916,
			13033,
			13181,
			13270,
			13262,
			13291,
			13404,
			13517,
			13528,
			13479,
			13407,
			13442,
			13486,
			13441,
			13381,
			13244,
			13156,
			13164,
			13038,
			12873,
			12787,
			12626,
			12367,
			12043,
			11811,
			11706,
			11718,
			11740
		};
		
	}
}
