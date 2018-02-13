///////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) 2006-2017 Esper Team. All rights reserved.                           /
// http://esper.codehaus.org                                                          /
// ---------------------------------------------------------------------------------- /
// The software in this package is published under the terms of the GPL license       /
// a copy of which has been included with this distribution in the license.txt file.  /
///////////////////////////////////////////////////////////////////////////////////////

using System;

using com.espertech.esper.client;
using com.espertech.esper.client.deploy;
using com.espertech.esper.client.scopetest;
using com.espertech.esper.compat.collections;
using com.espertech.esper.supportregression.bean;
using com.espertech.esper.supportregression.client;
using com.espertech.esper.supportregression.execution;

using static com.espertech.esper.supportregression.util.SupportMessageAssertUtil;

using NUnit.Framework;

namespace com.espertech.esper.regression.context
{
    public class ExecContextVariables : RegressionExecution {
    
        public override void Configure(Configuration configuration) {
            configuration.AddEventType<SupportBean>();
            configuration.AddEventType("SupportBean_S0", typeof(SupportBean_S0));
            configuration.AddEventType("SupportBean_S1", typeof(SupportBean_S1));
            configuration.AddEventType("SupportBean_S2", typeof(SupportBean_S2));
        }
    
        public override void Run(EPServiceProvider epService) {
            RunAssertionSegmentedByKey(epService);
            RunAssertionOverlapping(epService);
            RunAssertionIterateAndListen(epService);
            RunAssertionGetSetAPI(epService);
            RunAssertionInvalid(epService);
        }
    
        private void RunAssertionSegmentedByKey(EPServiceProvider epService) {
            string[] fields = "mycontextvar".Split(',');
            epService.EPAdministrator.CreateEPL("create context MyCtx as " +
                    "partition by theString from SupportBean, p00 from SupportBean_S0");
            epService.EPAdministrator.CreateEPL("context MyCtx create variable int mycontextvar = 0");
            epService.EPAdministrator.CreateEPL("context MyCtx on SupportBean(intPrimitive > 0) set mycontextvar = intPrimitive");
            var listener = new SupportUpdateListener();
            epService.EPAdministrator.CreateEPL("context MyCtx select mycontextvar from SupportBean_S0").AddListener(listener);
    
            epService.EPRuntime.SendEvent(new SupportBean("P1", 0));   // allocate partition P1
            epService.EPRuntime.SendEvent(new SupportBean("P1", 10));   // set variable
            epService.EPRuntime.SendEvent(new SupportBean_S0(1, "P1"));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{10});
    
            epService.EPRuntime.SendEvent(new SupportBean("P2", 11));   // allocate and set variable partition E2
            epService.EPRuntime.SendEvent(new SupportBean_S0(2, "P2"));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{11});
    
            epService.EPRuntime.SendEvent(new SupportBean_S0(3, "P1"));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{10});
            epService.EPRuntime.SendEvent(new SupportBean_S0(4, "P2"));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{11});
            epService.EPRuntime.SendEvent(new SupportBean_S0(5, "P3"));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{0});
    
            epService.EPRuntime.SendEvent(new SupportBean("P3", 12));
            epService.EPRuntime.SendEvent(new SupportBean_S0(6, "P3"));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{12});
    
            foreach (string statement in epService.EPAdministrator.StatementNames) {
                epService.EPAdministrator.GetStatement(statement).Stop();
            }
    
            epService.EPAdministrator.DestroyAllStatements();
        }
    
        private void RunAssertionOverlapping(EPServiceProvider epService) {
            string[] fields = "mycontextvar".Split(',');
            epService.EPAdministrator.CreateEPL("create context MyCtx as " +
                    "initiated by SupportBean_S0 s0 terminated by SupportBean_S1(p10 = s0.p00)");
            epService.EPAdministrator.CreateEPL("context MyCtx create variable int mycontextvar = 5");
            epService.EPAdministrator.CreateEPL("context MyCtx on SupportBean(theString = context.s0.p00) set mycontextvar = intPrimitive");
            epService.EPAdministrator.CreateEPL("context MyCtx on SupportBean(intPrimitive < 0) set mycontextvar = intPrimitive");
            var listener = new SupportUpdateListener();
            epService.EPAdministrator.CreateEPL("context MyCtx select mycontextvar from SupportBean_S2(p20 = context.s0.p00)").AddListener(listener);
    
            epService.EPRuntime.SendEvent(new SupportBean_S0(0, "P1"));    // allocate partition P1
            epService.EPRuntime.SendEvent(new SupportBean_S2(1, "P1"));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{5});
    
            epService.EPRuntime.SendEvent(new SupportBean_S0(0, "P2"));    // allocate partition P2
            epService.EPRuntime.SendEvent(new SupportBean("P2", 10));
            epService.EPRuntime.SendEvent(new SupportBean_S2(2, "P2"));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{10});
    
            // set all to -1
            epService.EPRuntime.SendEvent(new SupportBean("P2", -1));
            epService.EPRuntime.SendEvent(new SupportBean_S2(2, "P2"));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{-1});
            epService.EPRuntime.SendEvent(new SupportBean_S2(2, "P1"));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{-1});
    
            epService.EPRuntime.SendEvent(new SupportBean("P2", 20));
            epService.EPRuntime.SendEvent(new SupportBean("P1", 21));
            epService.EPRuntime.SendEvent(new SupportBean_S2(2, "P2"));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{20});
            epService.EPRuntime.SendEvent(new SupportBean_S2(2, "P1"));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{21});
    
            // terminate context partitions
            epService.EPRuntime.SendEvent(new SupportBean_S1(0, "P1"));
            epService.EPRuntime.SendEvent(new SupportBean_S1(0, "P2"));
    
            epService.EPRuntime.SendEvent(new SupportBean_S0(0, "P1"));    // allocate partition P1
            epService.EPRuntime.SendEvent(new SupportBean_S2(1, "P1"));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{5});
    
            epService.EPAdministrator.DestroyAllStatements();
    
            // test module deployment and undeployment
            string epl = "@Name(\"context\")\n" +
                    "create context MyContext\n" +
                    "initiated by Distinct(theString) SupportBean as input\n" +
                    "terminated by SupportBean(theString = input.theString);\n" +
                    "\n" +
                    "@Name(\"ctx variable counter\")\n" +
                    "context MyContext create variable integer counter = 0;\n";
            DeploymentResult result = epService.EPAdministrator.DeploymentAdmin.ParseDeploy(epl);
            epService.EPAdministrator.DeploymentAdmin.Undeploy(result.DeploymentId);
            result = epService.EPAdministrator.DeploymentAdmin.ParseDeploy(epl);
            epService.EPAdministrator.DeploymentAdmin.Undeploy(result.DeploymentId);
        }
    
        private void RunAssertionIterateAndListen(EPServiceProvider epService) {
            epService.EPAdministrator.CreateEPL("@Name('ctx') create context MyCtx as initiated by SupportBean_S0 s0 terminated after 24 hours");
    
            string[] fields = "mycontextvar".Split(',');
            var listenerCreateVariable = new SupportUpdateListener();
            EPStatement stmtVar = epService.EPAdministrator.CreateEPL("@Name('var') context MyCtx create variable int mycontextvar = 5");
            stmtVar.AddListener(listenerCreateVariable);
    
            var listenerUpdate = new SupportUpdateListener();
            EPStatement stmtUpd = epService.EPAdministrator.CreateEPL("@Name('upd') context MyCtx on SupportBean(theString = context.s0.p00) set mycontextvar = intPrimitive");
            stmtUpd.AddListener(listenerUpdate);
    
            epService.EPRuntime.SendEvent(new SupportBean_S0(0, "P1"));    // allocate partition P1
            epService.EPRuntime.SendEvent(new SupportBean("P1", 100));    // update
            EPAssertionUtil.AssertProps(listenerUpdate.AssertOneGetNewAndReset(), "mycontextvar".Split(','), new Object[]{100});
            EPAssertionUtil.AssertPropsPerRow(EPAssertionUtil.EnumeratorToArray(stmtUpd.GetEnumerator()), fields, new Object[][]{new object[] {100}});
    
            epService.EPRuntime.SendEvent(new SupportBean_S0(0, "P2"));    // allocate partition P1
            epService.EPRuntime.SendEvent(new SupportBean("P2", 101));    // update
            EPAssertionUtil.AssertProps(listenerUpdate.AssertOneGetNewAndReset(), "mycontextvar".Split(','), new Object[]{101});
            EPAssertionUtil.AssertPropsPerRow(EPAssertionUtil.EnumeratorToArray(stmtUpd.GetEnumerator()), fields, new Object[][]{new object[] {100}, new object[] {101}});
    
            EventBean[] events = EPAssertionUtil.EnumeratorToArray(stmtVar.GetEnumerator());
            EPAssertionUtil.AssertPropsPerRowAnyOrder(events, "mycontextvar".Split(','), new Object[][]{new object[] {100}, new object[] {101}});
            EPAssertionUtil.AssertPropsPerRowAnyOrder(listenerCreateVariable.GetNewDataListFlattened(), "mycontextvar".Split(','), new Object[][]{new object[] {100}, new object[] {101}});
    
            epService.EPAdministrator.DestroyAllStatements();
        }
    
        private void RunAssertionGetSetAPI(EPServiceProvider epService) {
            if (SupportConfigFactory.SkipTest(typeof(ExecContextVariables))) {
                return;
            }
    
            epService.EPAdministrator.CreateEPL("create context MyCtx as initiated by SupportBean_S0 s0 terminated after 24 hours");
            epService.EPAdministrator.CreateEPL("context MyCtx create variable int mycontextvar = 5");
            epService.EPAdministrator.CreateEPL("context MyCtx on SupportBean(theString = context.s0.p00) set mycontextvar = intPrimitive");
    
            epService.EPRuntime.SendEvent(new SupportBean_S0(0, "P1"));    // allocate partition P1
            AssertVariableValues(epService, 0, 5);
    
            epService.EPRuntime.SetVariableValue(Collections.SingletonDataMap("mycontextvar", 10), 0);
            AssertVariableValues(epService, 0, 10);
    
            epService.EPRuntime.SendEvent(new SupportBean_S0(0, "P2"));    // allocate partition P2
            AssertVariableValues(epService, 1, 5);
    
            epService.EPRuntime.SetVariableValue(Collections.SingletonDataMap("mycontextvar", 11), 1);
            AssertVariableValues(epService, 1, 11);
    
            // global variable - trying to set via context partition selection
            epService.EPAdministrator.CreateEPL("create variable int myglobarvar = 0");
            try {
                epService.EPRuntime.SetVariableValue(Collections.SingletonDataMap("myglobarvar", 11), 0);
                Assert.Fail();
            } catch (VariableNotFoundException ex) {
                Assert.AreEqual("Variable by name 'myglobarvar' is a global variable and not context-partitioned", ex.Message);
            }
    
            // global variable - trying to get via context partition selection
            try {
                epService.EPRuntime.GetVariableValue(Collections.SingletonSet("myglobarvar"), new SupportSelectorById(1));
                Assert.Fail();
            } catch (VariableNotFoundException ex) {
                Assert.AreEqual("Variable by name 'myglobarvar' is a global variable and not context-partitioned", ex.Message);
            }
    
            epService.EPAdministrator.DestroyAllStatements();
        }
    
        private void RunAssertionInvalid(EPServiceProvider epService) {
            epService.EPAdministrator.CreateEPL("create context MyCtxOne as partition by theString from SupportBean");
            epService.EPAdministrator.CreateEPL("create context MyCtxTwo as partition by p00 from SupportBean_S0");
            epService.EPAdministrator.CreateEPL("context MyCtxOne create variable int myctxone_int = 0");
    
            // undefined context
            TryInvalid(epService, "context MyCtx create variable int mycontext_invalid1 = 0",
                    "Error starting statement: Context by name 'MyCtx' has not been declared [context MyCtx create variable int mycontext_invalid1 = 0]");
    
            // wrong context uses variable
            TryInvalid(epService, "context MyCtxTwo select myctxone_int from SupportBean_S0",
                    "Variable 'myctxone_int' defined for use with context 'MyCtxOne' is not available for use with context 'MyCtxTwo' [context MyCtxTwo select myctxone_int from SupportBean_S0]");
    
            // variable use outside of context
            TryInvalid(epService, "select myctxone_int from SupportBean_S0",
                    "Variable 'myctxone_int' defined for use with context 'MyCtxOne' can only be accessed within that context [select myctxone_int from SupportBean_S0]");
            TryInvalid(epService, "select * from SupportBean_S0#Expr(myctxone_int > 5)",
                    "Variable 'myctxone_int' defined for use with context 'MyCtxOne' can only be accessed within that context [select * from SupportBean_S0#Expr(myctxone_int > 5)]");
            TryInvalid(epService, "select * from SupportBean_S0#keepall limit myctxone_int",
                    "Error starting statement: Variable 'myctxone_int' defined for use with context 'MyCtxOne' can only be accessed within that context [select * from SupportBean_S0#keepall limit myctxone_int]");
            TryInvalid(epService, "select * from SupportBean_S0#keepall limit 10 offset myctxone_int",
                    "Error starting statement: Variable 'myctxone_int' defined for use with context 'MyCtxOne' can only be accessed within that context [select * from SupportBean_S0#keepall limit 10 offset myctxone_int]");
            TryInvalid(epService, "select * from SupportBean_S0#keepall output every myctxone_int events",
                    "Error starting statement: Error in the output rate limiting clause: Variable 'myctxone_int' defined for use with context 'MyCtxOne' can only be accessed within that context [select * from SupportBean_S0#keepall output every myctxone_int events]");
            TryInvalid(epService, "@Hint('reclaim_group_aged=myctxone_int') select longPrimitive, count(*) from SupportBean group by longPrimitive",
                    "Error starting statement: Variable 'myctxone_int' defined for use with context 'MyCtxOne' can only be accessed within that context [@Hint('reclaim_group_aged=myctxone_int') select longPrimitive, count(*) from SupportBean group by longPrimitive]");
        }
    
        private void AssertVariableValues(EPServiceProvider epService, int agentInstanceId, int expected) {
            var states = epService.EPRuntime.GetVariableValue(Collections.SingletonSet("mycontextvar"), new SupportSelectorById(agentInstanceId));
            Assert.AreEqual(1, states.Count);
            var list = states.Get("mycontextvar");
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual(expected, list[0].State);
        }
    }
} // end of namespace
