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
 * Date: 10/12/2009
 * Time: 6:26 PM
 * <http://www.tickzoom.org/wiki/Licenses>.
 */
#endregion

using System;
using TickZoom.Api;

namespace TickZoom.Common
{
	/// <summary>
	/// Description of SymbolFactory.
	/// </summary>
	public class StarterFactoryImpl : StarterFactory
	{
		public ModelProperty ModelProperty(string name,string start1,double start,double end,double increment,bool isActive)
		{
			return new ModelPropertyCommon(name,start1,start,end,increment,isActive);
		}
		/// <summary>
		/// Contructs a new Historical Starter for running a historical 
		/// test pass. 
		/// </summary>
		/// <param name="releaseResources">
		/// Pass false for the Starter it to leave the memory resources from
		/// the last Starter. Pass true for the Starter to release
		/// all memory resources from any previous Starter before beginning.
		/// NOTE: If you pass false, then your code must
		/// call Factory.Engine.Release() to release memory to
		/// avoid a memory leak.
		/// </param>
		/// <returns></returns>
		public Starter HistoricalStarter(bool releaseResources) {
			return new HistoricalStarter(releaseResources);
		}
		/// <summary>
		/// Contructs a new Historical Starter for running a historical 
		/// test pass. Releases all memory resources upon completion.
		/// </summary>
		/// <returns></returns>
		public Starter HistoricalStarter() {
			return new HistoricalStarter();
		}
	}
}
