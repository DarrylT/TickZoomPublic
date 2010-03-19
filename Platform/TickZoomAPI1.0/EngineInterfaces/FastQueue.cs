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

namespace TickZoom.Api
{
	public interface FastFillQueue : FastQueue<LogicalFillBinary> {
		
	}

    public interface FastQueue<T>
	{
		bool EnQueueStruct(ref T tick);
		bool DequeueStruct(ref T tick);
		void Clear();
		void Flush();
		void Terminate(Exception ex);
		void Terminate();
		void Pause();
		void Resume();
		void LogStatistics();
		bool CanEnqueue { get; }
		bool CanDequeue { get; }
		int Count { get; }
		long EnqueueConflicts { get; }
		long DequeueConflicts { get; }
		StartEnqueue StartEnqueue { get; set; }
		int Timeout { get; set; }
		bool IsStarted { get; }
		ResumeEnqueue ResumeEnqueue { get; set; }
		PauseEnqueue PauseEnqueue { get; set; }
		bool IsPaused { get; }
		int Capacity { get; }
	}
}


