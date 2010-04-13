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
using System.IO;

namespace TickZoom.Api
{
	public interface AsyncReceiver : Receiver, IDisposable {
	}
	public interface Receiver {
		void OnEvent(SymbolInfo symbol, int eventType, object eventDetail);
	    ReceiverState OnGetReceiverState(SymbolInfo symbol);
	    bool CanReceive(SymbolInfo symbol);
	}
	
	public interface Serializable {
		void FromReader(MemoryStream reader);
		void ToWriter(MemoryStream memory);
	}
	
	public interface Serializer  {
		object FromReader(MemoryStream reader);
		void ToWriter(object eventDetail, MemoryStream memory);
		int EventType {
			get;
		}
	}
	
	public interface ReadWritable<T> : Serializable {
		void Inject(T tick);
		void SetSymbol(ulong lSymbol);
		/// <summary>
		/// Sets the time of the tick. 
		/// </summary>
		/// <param name="utcTime">Must be the UTC time.</param>
		void SetTime(TimeStamp utcTime);
		T Extract();
		int FromReader(byte version, BinaryReader reader);
		object ToPosition();
		byte DataVersion {
			get;
		}
	}
	
	public enum ReceiverState {
		Start,
		Ready,
		Historical,
		RealTime,
		Stop
	}
}
