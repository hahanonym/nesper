///////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) 2006-2017 Esper Team. All rights reserved.                           /
// http://esper.codehaus.org                                                          /
// ---------------------------------------------------------------------------------- /
// The software in this package is published under the terms of the GPL license       /
// a copy of which has been included with this distribution in the license.txt file.  /
///////////////////////////////////////////////////////////////////////////////////////

using System;

using com.espertech.esper.client;
using com.espertech.esper.client.scopetest;
using com.espertech.esper.compat;
using com.espertech.esper.compat.collections;
using com.espertech.esper.compat.logging;
using com.espertech.esper.supportregression.bean;
using com.espertech.esper.supportregression.execution;
using com.espertech.esper.supportregression.util;

// using static org.junit.Assert.*;

using NUnit.Framework;

namespace com.espertech.esper.regression.events.bean
{
    public class ExecEventBeanPropertyResolutionCaseDistinctInsensitive : RegressionExecution {
        public override void Configure(Configuration configuration) {
            configuration.EngineDefaults.EventMeta.ClassPropertyResolutionStyle = Configuration.PropertyResolutionStyle.DISTINCT_CASE_INSENSITIVE;
        }
    
        public override void Run(EPServiceProvider epService) {
            EPStatement stmt = epService.EPAdministrator.CreateEPL("select MYPROPERTY, myproperty, myProperty from " + typeof(SupportBeanDupProperty).FullName);
            var listener = new SupportUpdateListener();
            stmt.AddListener(listener);
    
            epService.EPRuntime.SendEvent(new SupportBeanDupProperty("lowercamel", "uppercamel", "upper", "lower"));
            EventBean result = listener.AssertOneGetNewAndReset();
            Assert.AreEqual("upper", result.Get("MYPROPERTY"));
            Assert.AreEqual("lower", result.Get("myproperty"));
            Assert.IsTrue(result.Get("myProperty").Equals("lowercamel") || result.Get("myProperty").Equals("uppercamel")); // JDK6 versus JDK7 JavaBean inspector
    
            try {
                epService.EPAdministrator.CreateEPL("select MyProperty from " + typeof(SupportBeanDupProperty).FullName);
                Assert.Fail();
            } catch (EPException ex) {
                SupportMessageAssertUtil.AssertMessage(ex, "Unexpected exception starting statement: Unable to determine which property to use for \"MyProperty\" because more than one property matched [");
                // expected
            }
        }
    }
} // end of namespace
