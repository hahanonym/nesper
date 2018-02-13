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
using com.espertech.esper.compat;
using com.espertech.esper.compat.collections;
using com.espertech.esper.compat.logging;
using com.espertech.esper.supportregression.bean;
using com.espertech.esper.supportregression.execution;

// using static org.junit.Assert.assertEquals;
// using static org.junit.Assert.fail;

using NUnit.Framework;

namespace com.espertech.esper.regression.epl.subselect
{
    public class ExecSubselectAllAnySomeExpr : RegressionExecution {
        public override void Run(EPServiceProvider epService) {
            epService.EPAdministrator.Configuration.AddEventType<SupportBean>();
            epService.EPAdministrator.Configuration.AddEventType("ArrayBean", typeof(SupportBeanArrayCollMap));
    
            RunAssertionRelationalOpAll(epService);
            RunAssertionRelationalOpNullOrNoRows(epService);
            RunAssertionRelationalOpSome(epService);
            RunAssertionEqualsNotEqualsAll(epService);
            RunAssertionEqualsAnyOrSome(epService);
            RunAssertionEqualsInNullOrNoRows(epService);
            RunAssertionInvalid(epService);
        }
    
        private void RunAssertionRelationalOpAll(EPServiceProvider epService) {
            string[] fields = "g,ge,l,le".Split(',');
            string stmtText = "select " +
                    "intPrimitive > all (select intPrimitive from SupportBean(theString like \"S%\")#keepall) as g, " +
                    "intPrimitive >= all (select intPrimitive from SupportBean(theString like \"S%\")#keepall) as ge, " +
                    "intPrimitive < all (select intPrimitive from SupportBean(theString like \"S%\")#keepall) as l, " +
                    "intPrimitive <= all (select intPrimitive from SupportBean(theString like \"S%\")#keepall) as le " +
                    "from SupportBean(theString like \"E%\")";
            EPStatement stmt = epService.EPAdministrator.CreateEPL(stmtText);
            var listener = new SupportUpdateListener();
            stmt.AddListener(listener);
    
            epService.EPRuntime.SendEvent(new SupportBean("E1", 1));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{true, true, true, true});
    
            epService.EPRuntime.SendEvent(new SupportBean("S1", 1));
    
            epService.EPRuntime.SendEvent(new SupportBean("E2", 1));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{false, true, false, true});
    
            epService.EPRuntime.SendEvent(new SupportBean("E2", 2));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{true, true, false, false});
    
            epService.EPRuntime.SendEvent(new SupportBean("S2", 2));
    
            epService.EPRuntime.SendEvent(new SupportBean("E3", 3));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{true, true, false, false});
    
            epService.EPRuntime.SendEvent(new SupportBean("E4", 2));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{false, true, false, false});
    
            epService.EPRuntime.SendEvent(new SupportBean("E5", 1));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{false, false, false, true});
    
            epService.EPRuntime.SendEvent(new SupportBean("E6", 0));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{false, false, true, true});
    
            try {
                epService.EPAdministrator.CreateEPL("select intArr > all (select intPrimitive from SupportBean#keepall) from ArrayBean");
                Assert.Fail();
            } catch (EPStatementException ex) {
                Assert.AreEqual("Error starting statement: Failed to validate select-clause expression subquery number 1 querying SupportBean: Collection or array comparison is not allowed for the IN, ANY, SOME or ALL keywords [select intArr > all (select intPrimitive from SupportBean#keepall) from ArrayBean]", ex.Message);
            }
    
            // test OM
            stmt.Dispose();
            EPStatementObjectModel model = epService.EPAdministrator.CompileEPL(stmtText);
            Assert.AreEqual(stmtText, model.ToEPL());
            stmt = epService.EPAdministrator.Create(model);
            stmt.AddListener(listener);
            epService.EPRuntime.SendEvent(new SupportBean("E1", 1));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{true, true, true, true});
            stmt.Dispose();
        }
    
        private void RunAssertionRelationalOpNullOrNoRows(EPServiceProvider epService) {
            string[] fields = "vall,vany".Split(',');
            string stmtText = "select " +
                    "intBoxed >= all (select doubleBoxed from SupportBean(theString like 'S%')#keepall) as vall, " +
                    "intBoxed >= any (select doubleBoxed from SupportBean(theString like 'S%')#keepall) as vany " +
                    " from SupportBean(theString like 'E%')";
            EPStatement stmt = epService.EPAdministrator.CreateEPL(stmtText);
            var listener = new SupportUpdateListener();
            stmt.AddListener(listener);
    
            // subs is empty
            // select  null >= all (select val from subs), null >= any (select val from subs)
            SendEvent(epService, "E1", null, null);
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{true, false});
    
            // select  1 >= all (select val from subs), 1 >= any (select val from subs)
            SendEvent(epService, "E2", 1, null);
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{true, false});
    
            // subs is {null}
            SendEvent(epService, "S1", null, null);
    
            SendEvent(epService, "E3", null, null);
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{null, null});
            SendEvent(epService, "E4", 1, null);
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{null, null});
    
            // subs is {null, 1}
            SendEvent(epService, "S2", null, 1d);
    
            SendEvent(epService, "E5", null, null);
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{null, null});
            SendEvent(epService, "E6", 1, null);
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{null, true});
    
            SendEvent(epService, "E7", 0, null);
            EventBean theEvent = listener.AssertOneGetNewAndReset();
            EPAssertionUtil.AssertProps(theEvent, fields, new Object[]{false, false});
    
            stmt.Dispose();
        }
    
        private void RunAssertionRelationalOpSome(EPServiceProvider epService) {
            string[] fields = "g,ge,l,le".Split(',');
            string stmtText = "select " +
                    "intPrimitive > any (select intPrimitive from SupportBean(theString like 'S%')#keepall) as g, " +
                    "intPrimitive >= any (select intPrimitive from SupportBean(theString like 'S%')#keepall) as ge, " +
                    "intPrimitive < any (select intPrimitive from SupportBean(theString like 'S%')#keepall) as l, " +
                    "intPrimitive <= any (select intPrimitive from SupportBean(theString like 'S%')#keepall) as le " +
                    " from SupportBean(theString like 'E%')";
            EPStatement stmt = epService.EPAdministrator.CreateEPL(stmtText);
            var listener = new SupportUpdateListener();
            stmt.AddListener(listener);
    
            epService.EPRuntime.SendEvent(new SupportBean("E1", 1));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{false, false, false, false});
    
            epService.EPRuntime.SendEvent(new SupportBean("S1", 1));
    
            epService.EPRuntime.SendEvent(new SupportBean("E2", 1));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{false, true, false, true});
    
            epService.EPRuntime.SendEvent(new SupportBean("E2", 2));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{true, true, false, false});
    
            epService.EPRuntime.SendEvent(new SupportBean("E2a", 0));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{false, false, true, true});
    
            epService.EPRuntime.SendEvent(new SupportBean("S2", 2));
    
            epService.EPRuntime.SendEvent(new SupportBean("E3", 3));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{true, true, false, false});
    
            epService.EPRuntime.SendEvent(new SupportBean("E4", 2));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{true, true, false, true});
    
            epService.EPRuntime.SendEvent(new SupportBean("E5", 1));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{false, true, true, true});
    
            epService.EPRuntime.SendEvent(new SupportBean("E6", 0));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{false, false, true, true});
    
            stmt.Dispose();
        }
    
        private void RunAssertionEqualsNotEqualsAll(EPServiceProvider epService) {
            string[] fields = "eq,neq,sqlneq,nneq".Split(',');
            string stmtText = "select " +
                    "intPrimitive=All(select intPrimitive from SupportBean(theString like 'S%')#keepall) as eq, " +
                    "intPrimitive != all (select intPrimitive from SupportBean(theString like 'S%')#keepall) as neq, " +
                    "intPrimitive <> all (select intPrimitive from SupportBean(theString like 'S%')#keepall) as sqlneq, " +
                    "not intPrimitive = all (select intPrimitive from SupportBean(theString like 'S%')#keepall) as nneq " +
                    " from SupportBean(theString like 'E%')";
            EPStatement stmt = epService.EPAdministrator.CreateEPL(stmtText);
            var listener = new SupportUpdateListener();
            stmt.AddListener(listener);
    
            epService.EPRuntime.SendEvent(new SupportBean("E1", 10));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{true, true, true, false});
    
            epService.EPRuntime.SendEvent(new SupportBean("S1", 11));
    
            epService.EPRuntime.SendEvent(new SupportBean("E2", 11));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{true, false, false, false});
    
            epService.EPRuntime.SendEvent(new SupportBean("E3", 10));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{false, true, true, true});
    
            epService.EPRuntime.SendEvent(new SupportBean("S1", 12));
    
            epService.EPRuntime.SendEvent(new SupportBean("E4", 11));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{false, false, false, true});
    
            epService.EPRuntime.SendEvent(new SupportBean("E5", 14));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{false, true, true, true});
    
            stmt.Dispose();
        }
    
        // Test "value = SOME (subselect)" which is the same as "value IN (subselect)"
        private void RunAssertionEqualsAnyOrSome(EPServiceProvider epService) {
            string[] fields = "r1,r2,r3,r4".Split(',');
            string stmtText = "select " +
                    "intPrimitive = SOME (select intPrimitive from SupportBean(theString like 'S%')#keepall) as r1, " +
                    "intPrimitive = ANY (select intPrimitive from SupportBean(theString like 'S%')#keepall) as r2, " +
                    "intPrimitive != SOME (select intPrimitive from SupportBean(theString like 'S%')#keepall) as r3, " +
                    "intPrimitive <> ANY (select intPrimitive from SupportBean(theString like 'S%')#keepall) as r4 " +
                    "from SupportBean(theString like 'E%')";
            EPStatement stmt = epService.EPAdministrator.CreateEPL(stmtText);
            var listener = new SupportUpdateListener();
            stmt.AddListener(listener);
    
            epService.EPRuntime.SendEvent(new SupportBean("E1", 10));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{false, false, false, false});
    
            epService.EPRuntime.SendEvent(new SupportBean("S1", 11));
            epService.EPRuntime.SendEvent(new SupportBean("E2", 11));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{true, true, false, false});
    
            epService.EPRuntime.SendEvent(new SupportBean("E3", 12));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{false, false, true, true});
    
            epService.EPRuntime.SendEvent(new SupportBean("S2", 12));
            epService.EPRuntime.SendEvent(new SupportBean("E4", 12));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{true, true, true, true});
    
            epService.EPRuntime.SendEvent(new SupportBean("E5", 13));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{false, false, true, true});
    
            stmt.Dispose();
        }
    
        private void RunAssertionEqualsInNullOrNoRows(EPServiceProvider epService) {
            string[] fields = "eall,eany,neall,neany,isin".Split(',');
            string stmtText = "select " +
                    "intBoxed = all (select doubleBoxed from SupportBean(theString like 'S%')#keepall) as eall, " +
                    "intBoxed = any (select doubleBoxed from SupportBean(theString like 'S%')#keepall) as eany, " +
                    "intBoxed != all (select doubleBoxed from SupportBean(theString like 'S%')#keepall) as neall, " +
                    "intBoxed != any (select doubleBoxed from SupportBean(theString like 'S%')#keepall) as neany, " +
                    "intBoxed in (select doubleBoxed from SupportBean(theString like 'S%')#keepall) as isin " +
                    " from SupportBean(theString like 'E%')";
            EPStatement stmt = epService.EPAdministrator.CreateEPL(stmtText);
            var listener = new SupportUpdateListener();
            stmt.AddListener(listener);
    
            // subs is empty
            // select  null = all (select val from subs), null = any (select val from subs), null != all (select val from subs), null != any (select val from subs), null in (select val from subs)
            SendEvent(epService, "E1", null, null);
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{true, false, true, false, false});
    
            // select  1 = all (select val from subs), 1 = any (select val from subs), 1 != all (select val from subs), 1 != any (select val from subs), 1 in (select val from subs)
            SendEvent(epService, "E2", 1, null);
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{true, false, true, false, false});
    
            // subs is {null}
            SendEvent(epService, "S1", null, null);
    
            SendEvent(epService, "E3", null, null);
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{null, null, null, null, null});
            SendEvent(epService, "E4", 1, null);
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{null, null, null, null, null});
    
            // subs is {null, 1}
            SendEvent(epService, "S2", null, 1d);
    
            SendEvent(epService, "E5", null, null);
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{null, null, null, null, null});
            SendEvent(epService, "E6", 1, null);
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{null, true, false, null, true});
            SendEvent(epService, "E7", 0, null);
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{false, null, null, true, null});
    
            stmt.Dispose();
        }
    
        private void RunAssertionInvalid(EPServiceProvider epService) {
            try {
                string stmtText = "select intArr = all (select intPrimitive from SupportBean#keepall) as r1 from ArrayBean";
                epService.EPAdministrator.CreateEPL(stmtText);
                Assert.Fail();
            } catch (EPStatementException ex) {
                Assert.AreEqual("Error starting statement: Failed to validate select-clause expression subquery number 1 querying SupportBean: Collection or array comparison is not allowed for the IN, ANY, SOME or ALL keywords [select intArr = all (select intPrimitive from SupportBean#keepall) as r1 from ArrayBean]", ex.Message);
            }
        }
    
        private void SendEvent(EPServiceProvider epService, string theString, int? intBoxed, double? doubleBoxed) {
            var bean = new SupportBean(theString, -1);
            bean.IntBoxed = intBoxed;
            bean.DoubleBoxed = doubleBoxed;
            epService.EPRuntime.SendEvent(bean);
        }
    }
} // end of namespace
