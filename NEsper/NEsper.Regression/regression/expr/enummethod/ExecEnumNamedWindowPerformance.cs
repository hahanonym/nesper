///////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) 2006-2017 Esper Team. All rights reserved.                           /
// http://esper.codehaus.org                                                          /
// ---------------------------------------------------------------------------------- /
// The software in this package is published under the terms of the GPL license       /
// a copy of which has been included with this distribution in the license.txt file.  /
///////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;

using com.espertech.esper.client;
using com.espertech.esper.client.scopetest;
using com.espertech.esper.compat;
using com.espertech.esper.compat.collections;
using com.espertech.esper.compat.logging;
using com.espertech.esper.supportregression.bean;
using com.espertech.esper.supportregression.execution;

// using static org.junit.Assert.assertEquals;
// using static org.junit.Assert.assertTrue;

using NUnit.Framework;

namespace com.espertech.esper.regression.expr.enummethod
{
    public class ExecEnumNamedWindowPerformance : RegressionExecution {
    
        public override void Configure(Configuration configuration) {
            configuration.AddEventType<SupportBean>();
            configuration.AddEventType("SupportBean_ST0", typeof(SupportBean_ST0));
        }
    
        public override void Run(EPServiceProvider epService) {
    
            epService.EPAdministrator.CreateEPL("create window Win#keepall as SupportBean");
            epService.EPAdministrator.CreateEPL("insert into Win select * from SupportBean");
    
            // preload
            for (int i = 0; i < 10000; i++) {
                epService.EPRuntime.SendEvent(new SupportBean("K" + i % 100, i));
            }
    
            RunAssertiomReuse(epService);
    
            RunAssertiomSubquery(epService);
        }
    
        private void RunAssertiomSubquery(EPServiceProvider epService) {
    
            // test expression reuse
            string epl = "expression q {" +
                    "  x => (select * from Win where intPrimitive = x.p00)" +
                    "}" +
                    "select " +
                    "Q(st0).Where(x => theString = key0) as val0, " +
                    "Q(st0).Where(x => theString = key0) as val1, " +
                    "Q(st0).Where(x => theString = key0) as val2, " +
                    "Q(st0).Where(x => theString = key0) as val3, " +
                    "Q(st0).Where(x => theString = key0) as val4, " +
                    "Q(st0).Where(x => theString = key0) as val5, " +
                    "Q(st0).Where(x => theString = key0) as val6, " +
                    "Q(st0).Where(x => theString = key0) as val7, " +
                    "Q(st0).Where(x => theString = key0) as val8, " +
                    "Q(st0).Where(x => theString = key0) as val9 " +
                    "from SupportBean_ST0 st0";
            EPStatement stmt = epService.EPAdministrator.CreateEPL(epl);
            var listener = new SupportUpdateListener();
            stmt.AddListener(listener);
    
            long start = DateTimeHelper.CurrentTimeMillis;
            for (int i = 0; i < 5000; i++) {
                epService.EPRuntime.SendEvent(new SupportBean_ST0("ID", "K50", 1050));
                EventBean theEvent = listener.AssertOneGetNewAndReset();
                for (int j = 0; j < 10; j++) {
                    Collection coll = (Collection) theEvent.Get("val" + j);
                    Assert.AreEqual(1, coll.Count);
                    SupportBean bean = (SupportBean) coll.First();
                    Assert.AreEqual("K50", bean.TheString);
                    Assert.AreEqual(1050, bean.IntPrimitive);
                }
            }
            long delta = DateTimeHelper.CurrentTimeMillis - start;
            Assert.IsTrue("Delta = " + delta, delta < 1000);
    
            stmt.Dispose();
        }
    
        private void RunAssertiomReuse(EPServiceProvider epService) {
    
            // test expression reuse
            string epl = "expression q {" +
                    "  x => Win(theString = x.key0).Where(y => intPrimitive = x.p00)" +
                    "}" +
                    "select " +
                    "Q(st0) as val0, " +
                    "Q(st0) as val1, " +
                    "Q(st0) as val2, " +
                    "Q(st0) as val3, " +
                    "Q(st0) as val4, " +
                    "Q(st0) as val5, " +
                    "Q(st0) as val6, " +
                    "Q(st0) as val7, " +
                    "Q(st0) as val8, " +
                    "Q(st0) as val9 " +
                    "from SupportBean_ST0 st0";
            EPStatement stmt = epService.EPAdministrator.CreateEPL(epl);
            var listener = new SupportUpdateListener();
            stmt.AddListener(listener);
    
            long start = DateTimeHelper.CurrentTimeMillis;
            for (int i = 0; i < 5000; i++) {
                epService.EPRuntime.SendEvent(new SupportBean_ST0("ID", "K50", 1050));
                EventBean theEvent = listener.AssertOneGetNewAndReset();
                for (int j = 0; j < 10; j++) {
                    Collection coll = (Collection) theEvent.Get("val" + j);
                    Assert.AreEqual(1, coll.Count);
                    SupportBean bean = (SupportBean) coll.First();
                    Assert.AreEqual("K50", bean.TheString);
                    Assert.AreEqual(1050, bean.IntPrimitive);
                }
            }
            long delta = DateTimeHelper.CurrentTimeMillis - start;
            Assert.IsTrue("Delta = " + delta, delta < 1000);
    
            // This will create a single dispatch
            // epService.EPRuntime.SendEvent(new SupportBean("E1", 1));
    
            stmt.Dispose();
        }
    }
} // end of namespace
