///////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) 2006-2017 Esper Team. All rights reserved.                           /
// http://esper.codehaus.org                                                          /
// ---------------------------------------------------------------------------------- /
// The software in this package is published under the terms of the GPL license       /
// a copy of which has been included with this distribution in the license.txt file.  /
///////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Threading;
using com.espertech.esper.client;
using com.espertech.esper.compat;
using com.espertech.esper.compat.collections;
using com.espertech.esper.compat.logging;
using com.espertech.esper.supportregression.bean;
using com.espertech.esper.supportregression.client;
using com.espertech.esper.supportregression.execution;

// using static org.junit.Assert.assertEquals;
// using static org.junit.Assert.assertTrue;

using NUnit.Framework;

namespace com.espertech.esper.regression.client
{
    public class ExecClientThreadedConfigOutbound : RegressionExecution {
        public override void Configure(Configuration configuration) {
            configuration.EngineDefaults.Threading.InternalTimerEnabled = false;
            configuration.EngineDefaults.Expression.UdfCache = false;
            configuration.EngineDefaults.Threading.ThreadPoolOutbound = true;
            configuration.EngineDefaults.Threading.ThreadPoolOutboundNumThreads = 5;
            configuration.AddEventType<SupportBean>();
        }
    
        public override void Run(EPServiceProvider epService) {
            var listener = new SupportListenerSleeping(200);
            EPStatement stmt = epService.EPAdministrator.CreateEPL("select * from SupportBean");
            stmt.AddListener(listener);
    
            long start = PerformanceObserver.NanoTime;
            for (int i = 0; i < 5; i++) {
                epService.EPRuntime.SendEvent(new SupportBean());
            }
            long end = PerformanceObserver.NanoTime;
            long delta = (end - start) / 1000000;
            Assert.IsTrue("Delta is " + delta, delta < 100);
    
            Thread.Sleep(1000);
            Assert.AreEqual(5, listener.NewEvents.Count);
        }
    }
} // end of namespace
