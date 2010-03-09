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
 * Date: 2/24/2010
 * Time: 11:14 AM
 * <http://www.tickzoom.org/wiki/Licenses>.
 */
#endregion

using System;
using TickZoom.Api;

namespace TickZoom.Common
{
	public class UtilityFactoryDefault : UtilityFactory
	{
		public ProviderService CommandLineProcess() {
			return new CommandLineProcess();
		}
		public ProviderService WindowsService() {
			return new WindowsService();
		}
		public LogicalOrderHandler LogicalOrderHandler(SymbolInfo symbol, PhysicalOrderHandler handler) {
			return new LogicalOrderHandlerDefault(symbol,handler);
		}
		public SymbolHandler SymbolHandler(SymbolInfo symbol, Receiver receiver) {
			return new SymbolHandlerDefault(symbol,receiver);
		}
		public VerifyFeed VerifyFeed() {
			return new VerifyFeedDefault();
		}
		public FillSimulator FillSimulator() {
			return new FillSimulatorDefault();
		}
		public FillSimulator FillSimulator(StrategyInterface strategy) {
			return new FillSimulatorDefault(strategy);
		}
		public BreakPointInterface BreakPoint() {
			return new BreakPoint();
		}
	}
}
