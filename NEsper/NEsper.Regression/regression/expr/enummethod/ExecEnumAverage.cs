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
using com.espertech.esper.supportregression.bean.lambda;
using com.espertech.esper.supportregression.execution;
using com.espertech.esper.supportregression.util;

using NUnit.Framework;

namespace com.espertech.esper.regression.expr.enummethod
{
    public class ExecEnumAverage : RegressionExecution {
    
        public override void Configure(Configuration configuration) {
            configuration.AddEventType("Bean", typeof(SupportBean_Container));
            configuration.AddEventType("SupportCollection", typeof(SupportCollection));
        }
    
        public override void Run(EPServiceProvider epService) {
            RunAssertionAverageEvents(epService);
            RunAssertionAverageScalar(epService);
            RunAssertionInvalid(epService);
        }
    
        private void RunAssertionAverageEvents(EPServiceProvider epService) {
    
            var fields = "val0,val1,val2,val3".Split(',');
            var eplFragment = "select " +
                    "beans.Average(x => intBoxed) as val0," +
                    "beans.Average(x => doubleBoxed) as val1," +
                    "beans.Average(x => longBoxed) as val2," +
                    "beans.Average(x => decimalBoxed) as val3 " +
                    "from Bean";
            var stmtFragment = epService.EPAdministrator.CreateEPL(eplFragment);
            var listener = new SupportUpdateListener();
            stmtFragment.AddListener(listener);
            LambdaAssertionUtil.AssertTypes(stmtFragment.EventType, fields, new Type[]{typeof(double?), typeof(double?), typeof(double?), typeof(decimal?)});
    
            epService.EPRuntime.SendEvent(new SupportBean_Container(null));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{null, null, null, null});
    
            epService.EPRuntime.SendEvent(new SupportBean_Container(Collections.GetEmptyList<SupportBean>()));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{null, null, null, null});
    
            var list = new List<SupportBean>();
            list.Add(Make(2, 3d, 4L, 5));
            epService.EPRuntime.SendEvent(new SupportBean_Container(list));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{2d, 3d, 4d, 5.0m});
    
            list.Add(Make(4, 6d, 8L, 10));
            epService.EPRuntime.SendEvent(new SupportBean_Container(list));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{(2 + 4) / 2d, (3d + 6d) / 2d, (4L + 8L) / 2d, (decimal) ((5 + 10) / 2d)});
    
            stmtFragment.Dispose();
        }
    
        private void RunAssertionAverageScalar(EPServiceProvider epService) {
    
            var fields = "val0,val1".Split(',');
            var eplFragment = "select " +
                    "intvals.Average() as val0," +
                    "bdvals.Average() as val1 " +
                    "from SupportCollection";
            var stmtFragment = epService.EPAdministrator.CreateEPL(eplFragment);
            var listener = new SupportUpdateListener();
            stmtFragment.AddListener(listener);
            LambdaAssertionUtil.AssertTypes(stmtFragment.EventType, fields, new Type[]{typeof(double?), typeof(decimal?)});
    
            epService.EPRuntime.SendEvent(SupportCollection.MakeNumeric("1,2,3"));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{2d, 2m});
    
            epService.EPRuntime.SendEvent(SupportCollection.MakeNumeric("1,null,3"));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{2d, 2m});
    
            epService.EPRuntime.SendEvent(SupportCollection.MakeNumeric("4"));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{4d, 4m});
            stmtFragment.Dispose();
    
            // test average with lambda
            epService.EPAdministrator.Configuration.AddPlugInSingleRowFunction("extractNum", typeof(ExecEnumMinMax.MyService).FullName, "ExtractNum");
            epService.EPAdministrator.Configuration.AddPlugInSingleRowFunction("extractBigDecimal", typeof(ExecEnumMinMax.MyService).FullName, "ExtractBigDecimal");
    
            var fieldsLambda = "val0,val1".Split(',');
            var eplLambda = "select " +
                    "strvals.Average(v => ExtractNum(v)) as val0, " +
                    "strvals.Average(v => ExtractBigDecimal(v)) as val1 " +
                    "from SupportCollection";
            var stmtLambda = epService.EPAdministrator.CreateEPL(eplLambda);
            stmtLambda.AddListener(listener);
            LambdaAssertionUtil.AssertTypes(stmtLambda.EventType, fieldsLambda, new Type[]{typeof(double?), typeof(decimal?)});
    
            epService.EPRuntime.SendEvent(SupportCollection.MakeString("E2,E1,E5,E4"));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fieldsLambda, new Object[]{(2 + 1 + 5 + 4) / 4d, (decimal) ((2 + 1 + 5 + 4) / 4d)});
    
            epService.EPRuntime.SendEvent(SupportCollection.MakeString("E1"));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fieldsLambda, new Object[]{1d, 1m});
    
            epService.EPRuntime.SendEvent(SupportCollection.MakeString(null));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fieldsLambda, new Object[]{null, null});
    
            epService.EPRuntime.SendEvent(SupportCollection.MakeString(""));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fieldsLambda, new Object[]{null, null});
    
            stmtLambda.Dispose();
        }
    
        private void RunAssertionInvalid(EPServiceProvider epService) {
            string epl;
    
            epl = "select Strvals.Average() from SupportCollection";
            SupportMessageAssertUtil.TryInvalid(epService, epl, "Error starting statement: Failed to validate select-clause expression 'strvals.Average()': Invalid input for built-in enumeration method 'average' and 0-parameter footprint, expecting collection of numeric values as input, received collection of string [select Strvals.Average() from SupportCollection]");
    
            epl = "select Beans.Average() from Bean";
            SupportMessageAssertUtil.TryInvalid(epService, epl, "Error starting statement: Failed to validate select-clause expression 'beans.Average()': Invalid input for built-in enumeration method 'average' and 0-parameter footprint, expecting collection of values (typically scalar values) as input, received collection of events of type '" + typeof(SupportBean).FullName + "'");
        }
    
        private SupportBean Make(int? intBoxed, double? doubleBoxed, long longBoxed, int decimalBoxed) {
            var bean = new SupportBean();
            bean.IntBoxed = intBoxed;
            bean.DoubleBoxed = doubleBoxed;
            bean.LongBoxed = longBoxed;
            bean.DecimalBoxed = decimalBoxed;
            return bean;
        }
    }
} // end of namespace
