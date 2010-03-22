﻿#region Copyright
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
	public abstract class IndicatorTest
	{
		private static readonly Log log = Factory.Log.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		IndicatorCommon indicator;
		TestBars bars;
		
		protected abstract IndicatorCommon CreateIndicator();
		
		protected abstract double [] GetExpectedResults();
		
		[SetUp]
		public void Setup() {
			bars = Factory.Engine.TestBars(Intervals.Day1);
			indicator = CreateIndicator();
			Assert.IsNotNull(indicator, "constructor");
			indicator.IntervalDefault = Intervals.Day1;
			indicator.Bars = bars;
			indicator.OnConfigure();
			indicator.OnInitialize();
			for(int j=0; j<indicator.Chain.Dependencies.Count; j++) {
				Model formula = (Model) indicator.Chain.Dependencies[j].Model;
				formula.Bars = bars;
				formula.OnConfigure();
				formula.OnInitialize();
			}
		}
		
		[Test]
		public void Values()
		{
			SymbolInfo symbol = Factory.Symbol.LookupSymbol("USD_JPY");
			double[] results = GetExpectedResults();
			for( int i = 0; i < data.Length; i++) {
				// open, high, low, close all the same.
				bars.AddBar( symbol, data[i], data[i], data[i], data[i], 0);
				for(int j=0; j<indicator.Chain.Dependencies.Count; j++) {
					Model childIndicator = (Model) indicator.Chain.Dependencies[j].Model;
					childIndicator.OnBeforeIntervalOpen();
					childIndicator.OnIntervalOpen();
					childIndicator.OnIntervalClose();
				}
				indicator.OnBeforeIntervalOpen();
				indicator.OnIntervalClose();
				Assert.AreEqual(results[i],Math.Round(indicator[0]),"current result at " + i);
				if( i > 1) Assert.AreEqual(results[i-1],Math.Round(indicator[1]),"result 1 back at " + i);
				if( i > 2) Assert.AreEqual(results[i-2],Math.Round(indicator[2]),"result 2 back at " + i);
			}
		}
		
		protected double[] data = new double[] {
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
		
		public TestBars Bars {
			get { return bars; }
		}
	}
}
