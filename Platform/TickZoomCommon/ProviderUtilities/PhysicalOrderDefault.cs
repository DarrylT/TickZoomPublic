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
 * Date: 8/7/2009
 * Time: 7:29 PM
 * <http://www.tickzoom.org/wiki/Licenses>.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Text;
using System.Threading;

using TickZoom.Api;

namespace TickZoom.Common
{
	public struct PhysicalOrderDefault : PhysicalOrder {
		private SymbolInfo symbol;
		private OrderType type;
		private double price;
		private double size;
		private object brokerOrder;
		
		public override string ToString()
		{
			return type + " at " + price + " for " + size;
		}
		
		public PhysicalOrderDefault(SymbolInfo symbol, LogicalOrder logical, double size, object brokerOrder) {
			this.symbol = symbol;
			this.type = logical.Type;
			this.price = logical.Price;
			this.size = size;
			this.brokerOrder = brokerOrder;
		}
		
		public PhysicalOrderDefault(SymbolInfo symbol, OrderType type, double price, double size, object brokerOrder) {
			this.symbol = symbol;
			this.type = type;
			this.price = price;
			this.size = size;
			this.brokerOrder = brokerOrder;
		}
	
		public OrderType Type {
			get { return type; }
		}
		
		public double Price {
			get { return price; }
		}
		
		public double Size {
			get { return size; }
		}
		
		public object BrokerOrder {
			get { return brokerOrder; }
		}
		
		public SymbolInfo Symbol {
			get { return symbol; }
		}
	}
}