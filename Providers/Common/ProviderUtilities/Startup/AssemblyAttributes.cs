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
 * <http://www.tickzoom.org/wiki/Licenses>.
 */
#endregion

using System;
using System.Reflection;
using System.ServiceProcess;

using TickZoom.Api;

namespace TickZoom.ProviderUtilities
{
	public static class AssemblyAttributes {
		
		public static string GetTitle() {
			System.Reflection.Assembly thisAssembly = Assembly.GetEntryAssembly();
			object[] attributes = thisAssembly.GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
			if (attributes.Length == 1)
			{
			   return (((AssemblyTitleAttribute) attributes[0]).Title);
			} else {
				throw new ApplicationException("Found more than one Assembly Title attribute. Unable to choose service name.");
			}
		}
	}
}
