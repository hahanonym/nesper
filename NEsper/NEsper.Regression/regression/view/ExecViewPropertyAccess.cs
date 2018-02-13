///////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) 2006-2017 Esper Team. All rights reserved.                           /
// http://esper.codehaus.org                                                          /
// ---------------------------------------------------------------------------------- /
// The software in this package is published under the terms of the GPL license       /
// a copy of which has been included with this distribution in the license.txt file.  /
///////////////////////////////////////////////////////////////////////////////////////

using com.espertech.esper.client;
using com.espertech.esper.client.scopetest;
using com.espertech.esper.supportregression.bean;
using com.espertech.esper.supportregression.execution;
using NUnit.Framework;
// using static org.junit.Assert.*;

namespace com.espertech.esper.regression.view
{
    public class ExecViewPropertyAccess : RegressionExecution
    {
        public override void Run(EPServiceProvider epService)
        {
            var epl = "select Mapped('keyOne') as a," +
                      "indexed[1] as b, nested.nestedNested.nestedNestedValue as c, mapProperty, " +
                      "arrayProperty[0] " +
                      "  from " + typeof(SupportBeanComplexProps).FullName + "#length(3) " +
                      " where Mapped('keyOne') = 'valueOne' and " +
                      " indexed[1] = 2 and " +
                      " nested.nestedNested.nestedNestedValue = 'nestedNestedValue'";

            var testView = epService.EPAdministrator.CreateEPL(epl);
            var listener = new SupportUpdateListener();
            testView.AddListener(listener);

            var eventObject = SupportBeanComplexProps.MakeDefaultBean();
            epService.EPRuntime.SendEvent(eventObject);
            var theEvent = listener.GetAndResetLastNewData()[0];
            Assert.AreEqual(eventObject.GetMapped("keyOne"), theEvent.Get("a"));
            Assert.AreEqual(eventObject.GetIndexed(1), theEvent.Get("b"));
            Assert.AreEqual(eventObject.Nested.NestedNested.NestedNestedValue, theEvent.Get("c"));
            Assert.AreEqual(eventObject.MapProperty, theEvent.Get("mapProperty"));
            Assert.AreEqual(eventObject.ArrayProperty[0], theEvent.Get("arrayProperty[0]"));

            eventObject.SetIndexed(1, int.MinValue);
            Assert.IsFalse(listener.IsInvoked);
            epService.EPRuntime.SendEvent(eventObject);
            Assert.IsFalse(listener.IsInvoked);

            eventObject.SetIndexed(1, 2);
            epService.EPRuntime.SendEvent(eventObject);
            Assert.IsTrue(listener.IsInvoked);
        }
    }
} // end of namespace