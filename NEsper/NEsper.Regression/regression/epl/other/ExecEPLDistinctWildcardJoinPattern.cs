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

namespace com.espertech.esper.regression.epl.other
{
    public class ExecEPLDistinctWildcardJoinPattern : RegressionExecution {
        public override void Run(EPServiceProvider epService) {
            epService.EPAdministrator.Configuration.AddEventType<SupportBean>();
    
            string epl = "select distinct * from " +
                    "SupportBean(intPrimitive=0) as fooB unidirectional " +
                    "inner join " +
                    "pattern [" +
                    "every-Distinct(fooA.theString) fooA=SupportBean(intPrimitive=1)" +
                    "->" +
                    "every-Distinct(wooA.theString) wooA=SupportBean(intPrimitive=2)" +
                    " where timer:Within(1 hour)" +
                    "]#Time(1 hour) as fooWooPair " +
                    "on fooB.longPrimitive = fooWooPair.fooA.longPrimitive" +
                    " order by fooWooPair.wooA.theString asc";
    
            EPStatement stmt = epService.EPAdministrator.CreateEPL(epl);
            var subscriber = new SupportSubscriberMRD();
            stmt.Subscriber = subscriber;
    
            SendEvent(epService, "E1", 1, 10L);
            SendEvent(epService, "E2", 2, 10L);
            SendEvent(epService, "E3", 2, 10L);
            SendEvent(epService, "Query", 0, 10L);
    
            Assert.IsTrue(subscriber.IsInvoked);
            Assert.AreEqual(1, subscriber.InsertStreamList.Count);
            Object[][] inserted = subscriber.InsertStreamList[0];
            Assert.AreEqual(2, inserted.Length);
            Assert.AreEqual("Query", ((SupportBean) inserted[0][0]).TheString);
            Assert.AreEqual("Query", ((SupportBean) inserted[1][0]).TheString);
            Map mapOne = (Map) inserted[0][1];
            Assert.AreEqual("E2", ((EventBean) mapOne.Get("wooA")).Get("theString"));
            Assert.AreEqual("E1", ((EventBean) mapOne.Get("fooA")).Get("theString"));
            Map mapTwo = (Map) inserted[1][1];
            Assert.AreEqual("E3", ((EventBean) mapTwo.Get("wooA")).Get("theString"));
            Assert.AreEqual("E1", ((EventBean) mapTwo.Get("fooA")).Get("theString"));
        }
    
        private void SendEvent(EPServiceProvider epService, string theString, int intPrimitive, long longPrimitive) {
            var bean = new SupportBean(theString, intPrimitive);
            bean.LongPrimitive = longPrimitive;
            epService.EPRuntime.SendEvent(bean);
        }
    }
} // end of namespace
