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
using com.espertech.esper.supportregression.bean;
using com.espertech.esper.supportregression.bean.lambda;
using com.espertech.esper.supportregression.execution;

namespace com.espertech.esper.regression.expr.datetime
{
    public class ExecDTNested : RegressionExecution
    {
        public override void Configure(Configuration configuration) {
            configuration.AddEventType("SupportDateTime", typeof(SupportDateTime));
        }
    
        public override void Run(EPServiceProvider epService) {
            string[] fields = "val0,val1,val2,val3,val4".Split(',');
            string eplFragment = "select " +
                    "utildate.Set('hour', 1).Set('minute', 2).Set('second', 3) as val0," +
                    "longdate.Set('hour', 1).Set('minute', 2).Set('second', 3) as val1" +
                    " from SupportDateTime";
            EPStatement stmtFragment = epService.EPAdministrator.CreateEPL(eplFragment);
            var listener = new SupportUpdateListener();
            stmtFragment.AddListener(listener);
            LambdaAssertionUtil.AssertTypes(stmtFragment.EventType, fields, new Type[]
            {
                typeof(DateTimeOffset?), typeof(long?)
            });
    
            string startTime = "2002-05-30T09:00:00.000";
            string expectedTime = "2002-05-30T01:02:03.000";
            epService.EPRuntime.SendEvent(SupportDateTime.Make(startTime));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, SupportDateTime.GetArrayCoerced(expectedTime, "util", "long", "cal", "ldt", "zdt"));
    
            stmtFragment.Dispose();
            eplFragment = "select " +
                    "utildate.Set('hour', 1).Set('minute', 2).Set('second', 3).ToCalendar() as val0," +
                    "longdate.Set('hour', 1).Set('minute', 2).Set('second', 3).ToCalendar() as val1" +
                    " from SupportDateTime";
            stmtFragment = epService.EPAdministrator.CreateEPL(eplFragment);
            stmtFragment.AddListener(listener);
            LambdaAssertionUtil.AssertTypes(stmtFragment.EventType, fields, new Type[]
            {
                typeof(DateTimeEx), typeof(DateTimeEx)
            });
    
            epService.EPRuntime.SendEvent(SupportDateTime.Make(startTime));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, SupportDateTime.GetArrayCoerced(expectedTime, "cal", "cal", "cal", "cal", "cal"));
        }
    }
} // end of namespace
