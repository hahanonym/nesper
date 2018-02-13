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
using com.espertech.esper.client.dataflow;
using com.espertech.esper.client.scopetest;
using com.espertech.esper.compat;
using com.espertech.esper.compat.collections;
using com.espertech.esper.compat.logging;
using com.espertech.esper.dataflow.util;
using com.espertech.esper.supportregression.dataflow;
using com.espertech.esper.supportregression.execution;

// using static org.junit.Assert.assertEquals;

using NUnit.Framework;

namespace com.espertech.esper.regression.dataflow
{
    public class ExecDataflowExampleWordCount : RegressionExecution {
    
        public override void Run(EPServiceProvider epService) {
            epService.EPAdministrator.Configuration.AddImport(typeof(MyTokenizerCounter).Namespace + ".*");
            epService.EPAdministrator.Configuration.AddImport(typeof(DefaultSupportCaptureOp).Namespace + ".*");
    
            string epl = "create dataflow WordCount " +
                    "MyLineFeedSource -> LineOfTextStream {} " +
                    "MyTokenizerCounter(LineOfTextStream) -> SingleLineCountStream {}" +
                    "MyWordCountAggregator(SingleLineCountStream) -> WordCountStream {}" +
                    "DefaultSupportCaptureOp(WordCountStream) {}";
            epService.EPAdministrator.CreateEPL(epl);
    
            var future = new DefaultSupportCaptureOp<Object>(1);
            var source = new MyLineFeedSource(Collections.List("Test this code", "Test line two").GetEnumerator());
    
            var options = new EPDataFlowInstantiationOptions()
                    .OperatorProvider(new DefaultSupportGraphOpProvider(future, source));
    
            epService.EPRuntime.DataFlowRuntime.Instantiate("WordCount", options).Start();
    
            Object[] received = future.Get(3, TimeUnit.SECONDS);
            Assert.AreEqual(1, received.Length);
            MyWordCountStats stats = (MyWordCountStats) received[0];
            EPAssertionUtil.AssertProps(stats, "lines,words,chars".Split(','), new Object[]{2, 6, 23});
        }
    }
} // end of namespace
