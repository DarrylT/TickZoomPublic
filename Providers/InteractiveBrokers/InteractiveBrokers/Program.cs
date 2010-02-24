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
using TickZoom.Api;
using TickZoom.InteractiveBrokers;

namespace TickZoom.Common
{
	static class Program
	{
		/// <summary>
		/// This method starts the service.
		/// </summary>
		static void Main(string[] args)
		{
			try {
				ServiceConnection connection = Factory.Provider.ConnectionManager();
				connection.OnCreateProvider = () => new IBInterface();
				if( args.Length > 0 ) {
					// Connection port provided on command line.
					ProviderService commandLine = Factory.Utility.CommandLineProcess();
					commandLine.Connection = connection;
					commandLine.Run(args);
				} else {
					// Connection port set via ServicePort in app.config 
					ProviderService service = Factory.Utility.WindowsService();
					service.Connection = connection;
					service.Run(args);
				}
			} catch( Exception ex) {
				string exception = ex.GetType() + ": " + ex.Message + Environment.NewLine + ex.StackTrace;
				System.Diagnostics.Debug.WriteLine( exception);
				Console.WriteLine( exception);
				Environment.Exit(1);
			}
		}
	}
}
