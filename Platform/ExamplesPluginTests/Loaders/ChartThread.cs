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
 * Date: 5/25/2009
 * Time: 3:36 PM
 * <http://www.tickzoom.org/wiki/Licenses>.
 */
#endregion


using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;
using TickZoom;
using TickZoom.Api;
using TickZoom.Common;
using TickZoom.TickUtil;
using ZedGraph;

namespace Loaders
{

	
	public class ChartThread {
		Log log = Factory.Log.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		private PortfolioDoc portfolioDoc;
		public Thread thread;
		public ChartThread() {
			log.Debug("Starting Chart Thread");
			ThreadStart job = new ThreadStart(Run);
			thread = new Thread(job);
			thread.IsBackground = true;
			thread.Name = "ChartTest";
			thread.Start();
			while( portfolioDoc == null) {
				Thread.Sleep(0);
			}
			log.Debug("Returning Chart Created by Thread");
		}
		
		private void ClosedEvent(object sender, EventArgs args) {
			stop = true;
		}
		
		public void Run() {
			try {
   				log.Debug("Chart Thread Started");
   				portfolioDoc = new PortfolioDoc();
   				portfolioDoc.Closed += ClosedEvent;
   				while( !stop ) {
   					Application.DoEvents();
   					Thread.Sleep(10);
   				}
   				stop = true;
			} catch( Exception ex) {
				log.Error("ERROR: Thread had an exception:",ex);
			}
		}
		
		private bool stop = false;
		
		public void Stop() {
			if(portfolioDoc!=null) {
		   		portfolioDoc.Invoke(new MethodInvoker(portfolioDoc.Hide));
		   		portfolioDoc=null;
			}
			if( thread!=null) {
				stop = true;
				thread.Join();
			}
		}
		
		public bool IsAlive {
			get { return !stop; }
		}
		
		public PortfolioDoc PortfolioDoc {
			get { return portfolioDoc; }
		}
	}
}
