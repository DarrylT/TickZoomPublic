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
using System.Runtime.InteropServices;

namespace TickZoom.Api
{
    [StructLayout(LayoutKind.Explicit)]
	public struct QueueItem {
	    [FieldOffset(0)] public int EventType;
	    [FieldOffset(4)] public TickBinary Tick;
	    [FieldOffset(4)] public PositionChange PositionChange;
	    [FieldOffset(4)] public OnPositionChange OnPositionChange;
	    [FieldOffset(4)] public CustomEvent CustomEvent;
	    [FieldOffset(4)] public PositionChange EventChange;
	    [FieldOffset(4)] public ErrorEvent ErrorEvent;
    }

    [StructLayout(LayoutKind.Explicit)]
	public struct ProviderEvent {
	    [FieldOffset(0)] public int Type;
	    [FieldOffset(4)] public StartStop StartStop;
	    [FieldOffset(4)] public StartSymbol StartSymbol;
	    [FieldOffset(4)] public StartSymbol StopSymbol;
	    [FieldOffset(4)] public PositionChange PositionChange;
	    [FieldOffset(4)] public CustomEvent CustomEvent;
    }
    
    public struct PositionChange {
	    public int Receiver;
        public ulong Symbol;
	    public double Position;
	    public int OrdersCount;
	    public int OrdersMemoryId;
    }
    
    public struct CustomEvent {
	    public int Receiver;
        public ulong Symbol;
	    public int DetailMemoryId;
    }
    
    public struct OnPositionChange {
	    public int Receiver;
        public ulong Symbol;
        public LogicalFillBinary Fill;
    }
    
    public struct StartStop {
	    public int Receiver;
    }
    
    public struct StartSymbol
    {
	    public int Receiver;
        public ulong Symbol;
        public TimeStamp StartTime;
    }
    
    public struct StopSymbol
    {
	    public int Receiver;
        public ulong Symbol;
    }
    
    public struct ErrorEvent
    {
//        public string Message;
    }
    
    public struct EventChange
    {
        public ulong Symbol;
    }	    
}


