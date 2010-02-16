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
 * Date: 2/22/2009
 * Time: 1:05 PM
 * <http://www.tickzoom.org/wiki/Licenses>.
 */
#endregion


using System;
using NUnit.Framework;
using System.Collections.Generic;
using TickZoom.Api;
using TickZoom.TickUtil;

namespace TickZoom.TickData
{
	[TestFixture]
	public class BitBufferTest
	{
		Log log = Factory.Log.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		[TestFixtureTearDown]
		public void TearDown() {
		}
		
		public struct Code {
			public int Value;
			public int Bits;
			public Code(string binary) {
				Bits = binary.Length;
				Value = 0;
				Value = BinaryToInt(binary);
			}
			private int BinaryToInt(string binaryNumber)
			{
				int converted = 0;
				for( int i = binaryNumber.Length - 1; i >= 0; i--) {
					if( binaryNumber[i] == '1' ) converted = converted | (((int)1) << (binaryNumber.Length-1-i));
				}
				return converted;
			}
		}
		[Test]
		public void BufferTest()
		{
			BitBuffer buffer = new BitBuffer(1024);
			
			List<Code> codes = new List<Code>();
			
			codes.Add( new Code( "00"));
			codes.Add( new Code( "01"));
			codes.Add( new Code( "10"));
			codes.Add( new Code( "1100"));
			codes.Add( new Code( "1101"));
			codes.Add( new Code( "1110"));
			codes.Add( new Code( "111100"));
			codes.Add( new Code( "111101"));
			codes.Add( new Code( "111110"));
			codes.Add( new Code( "111101"));
			codes.Add( new Code( "111110"));
			codes.Add( new Code( "11111100"));
			codes.Add( new Code( "11111101"));
			codes.Add( new Code( "11111110"));
			
			// Add 3 true bits
			for( int i=0;i<3;i++) {
				buffer.Add(true);
			}
			// Add 2 false bits
			for( int i=0;i<2;i++) {
				buffer.Add(false);
			}
			// Add 7 true bits
			for( int i=0;i<7;i++) {
				buffer.Add(true);
			}
			
			// Add 1 false bit
			buffer.Add(false);
			
			// Add a 10000011 byte
			buffer.Add( (byte) ( (1<<7) | 3) );
			
			// Add 3 bits 101
			buffer.Add( (byte) 5, 3 );
			
			// Add 11 bits 101 01010101
			byte[] bytes = new Byte[2];
			bytes[0]=1 + 4 + 16 + 64;
			bytes[1]=5;
			buffer.Add( bytes, 11);
			
			Assert.AreEqual("00000101-01010101-10110000-01101111-11100111",buffer.DebugBytes);
			
			
		}
	}
}
