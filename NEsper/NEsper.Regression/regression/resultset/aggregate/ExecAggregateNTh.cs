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
using com.espertech.esper.client.soda;
using com.espertech.esper.supportregression.bean;
using com.espertech.esper.supportregression.execution;

using static com.espertech.esper.supportregression.util.SupportMessageAssertUtil;
// using static org.junit.Assert.assertEquals;
// using static org.junit.Assert.assertFalse;

using NUnit.Framework;

namespace com.espertech.esper.regression.resultset.aggregate
{
    public class ExecAggregateNTh : RegressionExecution {
    
        public override void Configure(Configuration configuration) {
            configuration.AddEventType<SupportBean>();
        }
    
        public override void Run(EPServiceProvider epService) {
    
            string epl = "select " +
                    "theString, " +
                    "Nth(intPrimitive,0) as int1, " +  // current
                    "Nth(intPrimitive,1) as int2 " +   // one before
                    "from SupportBean#keepall group by theString output last every 3 events order by theString";
            EPStatement stmt = epService.EPAdministrator.CreateEPL(epl);
            var listener = new SupportUpdateListener();
            stmt.AddListener(listener);
    
            RunAssertion(epService, listener);
    
            stmt.Dispose();
            EPStatementObjectModel model = epService.EPAdministrator.CompileEPL(epl);
            stmt = epService.EPAdministrator.Create(model);
            stmt.AddListener(listener);
            Assert.AreEqual(epl, model.ToEPL());
    
            RunAssertion(epService, listener);
    
            TryInvalid(epService, "select Nth() from SupportBean",
                    "Error starting statement: Failed to validate select-clause expression 'Nth(*)': The nth aggregation function requires two parameters, an expression returning aggregation values and a numeric index constant [select Nth() from SupportBean]");
        }
    
        private void RunAssertion(EPServiceProvider epService, SupportUpdateListener listener) {
            string[] fields = "theString,int1,int2".Split(',');
    
            epService.EPRuntime.SendEvent(new SupportBean("G1", 10));
            epService.EPRuntime.SendEvent(new SupportBean("G2", 11));
            Assert.IsFalse(listener.IsInvoked);
    
            epService.EPRuntime.SendEvent(new SupportBean("G1", 12));
            EPAssertionUtil.AssertPropsPerRow(listener.GetAndResetLastNewData(), fields, new Object[][]{new object[] {"G1", 12, 10}, new object[] {"G2", 11, null}});
    
            epService.EPRuntime.SendEvent(new SupportBean("G2", 30));
            epService.EPRuntime.SendEvent(new SupportBean("G2", 20));
            Assert.IsFalse(listener.IsInvoked);
    
            epService.EPRuntime.SendEvent(new SupportBean("G2", 25));
            EPAssertionUtil.AssertPropsPerRow(listener.GetAndResetLastNewData(), fields, new Object[][]{new object[] {"G2", 25, 20}});
    
            epService.EPRuntime.SendEvent(new SupportBean("G1", -1));
            epService.EPRuntime.SendEvent(new SupportBean("G1", -2));
            Assert.IsFalse(listener.IsInvoked);
    
            epService.EPRuntime.SendEvent(new SupportBean("G2", 8));
            EPAssertionUtil.AssertPropsPerRow(listener.GetAndResetLastNewData(), fields, new Object[][]{new object[] {"G1", -2, -1}, new object[] {"G2", 8, 25}});
        }
    }
} // end of namespace
