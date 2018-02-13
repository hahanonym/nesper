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
using com.espertech.esper.core.service;
using com.espertech.esper.epl.named;
using com.espertech.esper.supportregression.bean;
using com.espertech.esper.supportregression.execution;
using com.espertech.esper.util;

// using static org.junit.Assert.*;

using NUnit.Framework;

namespace com.espertech.esper.regression.nwtable.namedwindow
{
    public class ExecNamedWindowOnDelete : RegressionExecution {
        public override void Run(EPServiceProvider epService) {
            RunAssertionFirstUnique(epService);
            RunAssertionStaggeredNamedWindow(epService);
            RunAssertionCoercionKeyMultiPropIndexes(epService);
            RunAssertionCoercionRangeMultiPropIndexes(epService);
            RunAssertionCoercionKeyAndRangeMultiPropIndexes(epService);
        }
    
        private void RunAssertionFirstUnique(EPServiceProvider epService) {
            epService.EPAdministrator.Configuration.AddEventType<SupportBean>();
            epService.EPAdministrator.Configuration.AddEventType("SupportBean_A", typeof(SupportBean_A));
    
            var fields = new string[]{"theString", "intPrimitive"};
            string stmtTextCreateOne = "create window MyWindowFU#Firstunique(theString) as select * from SupportBean";
            EPStatement stmtCreate = epService.EPAdministrator.CreateEPL(stmtTextCreateOne);
            epService.EPAdministrator.CreateEPL("insert into MyWindowFU select * from SupportBean");
            EPStatement stmtDelete = epService.EPAdministrator.CreateEPL("on SupportBean_A a delete from MyWindowFU where theString=a.id");
            var listenerDelete = new SupportUpdateListener();
            stmtDelete.AddListener(listenerDelete);
    
            epService.EPRuntime.SendEvent(new SupportBean("A", 1));
            epService.EPRuntime.SendEvent(new SupportBean("A", 2));
    
            epService.EPRuntime.SendEvent(new SupportBean_A("A"));
            EPAssertionUtil.AssertProps(listenerDelete.AssertOneGetNewAndReset(), fields, new Object[]{"A", 1});
    
            epService.EPRuntime.SendEvent(new SupportBean("A", 3));
            EPAssertionUtil.AssertPropsPerRow(stmtCreate.GetEnumerator(), fields, new Object[][]{new object[] {"A", 3}});
    
            epService.EPRuntime.SendEvent(new SupportBean_A("A"));
            EPAssertionUtil.AssertPropsPerRow(stmtCreate.GetEnumerator(), fields, null);
        }
    
        private void RunAssertionStaggeredNamedWindow(EPServiceProvider epService) {
            foreach (EventRepresentationChoice rep in EnumHelper.GetValues<EventRepresentationChoice>()) {
                TryAssertionStaggered(epService, rep);
            }
        }
    
        private void TryAssertionStaggered(EPServiceProvider epService, EventRepresentationChoice outputType) {
    
            var fieldsOne = new string[]{"a1", "b1"};
            var fieldsTwo = new string[]{"a2", "b2"};
    
            // create window one
            string stmtTextCreateOne = outputType.GetAnnotationText() + " create window MyWindowSTAG#keepall as select theString as a1, intPrimitive as b1 from " + typeof(SupportBean).FullName;
            EPStatement stmtCreateOne = epService.EPAdministrator.CreateEPL(stmtTextCreateOne);
            var listenerWindow = new SupportUpdateListener();
            stmtCreateOne.AddListener(listenerWindow);
            Assert.AreEqual(0, GetCount(epService, "MyWindowSTAG"));
            Assert.IsTrue(outputType.MatchesClass(stmtCreateOne.EventType.UnderlyingType));
    
            // create window two
            string stmtTextCreateTwo = outputType.GetAnnotationText() + " create window MyWindowSTAGTwo#keepall as select theString as a2, intPrimitive as b2 from " + typeof(SupportBean).FullName;
            EPStatement stmtCreateTwo = epService.EPAdministrator.CreateEPL(stmtTextCreateTwo);
            var listenerWindowTwo = new SupportUpdateListener();
            stmtCreateTwo.AddListener(listenerWindowTwo);
            Assert.AreEqual(0, GetCount(epService, "MyWindowSTAGTwo"));
            Assert.IsTrue(outputType.MatchesClass(stmtCreateTwo.EventType.UnderlyingType));
    
            // create delete stmt
            string stmtTextDelete = "on MyWindowSTAG delete from MyWindowSTAGTwo where a1 = a2";
            EPStatement stmtDelete = epService.EPAdministrator.CreateEPL(stmtTextDelete);
            var listenerDelete = new SupportUpdateListener();
            stmtDelete.AddListener(listenerDelete);
            Assert.AreEqual(StatementType.ON_DELETE, ((EPStatementSPI) stmtDelete).StatementMetadata.StatementType);
    
            // create insert into
            string stmtTextInsert = "insert into MyWindowSTAG select theString as a1, intPrimitive as b1 from " + typeof(SupportBean).FullName + "(intPrimitive > 0)";
            epService.EPAdministrator.CreateEPL(stmtTextInsert);
            stmtTextInsert = "insert into MyWindowSTAGTwo select theString as a2, intPrimitive as b2 from " + typeof(SupportBean).FullName + "(intPrimitive < 0)";
            epService.EPAdministrator.CreateEPL(stmtTextInsert);
    
            SendSupportBean(epService, "E1", -10);
            EPAssertionUtil.AssertProps(listenerWindowTwo.AssertOneGetNewAndReset(), fieldsTwo, new Object[]{"E1", -10});
            EPAssertionUtil.AssertPropsPerRow(stmtCreateTwo.GetEnumerator(), fieldsTwo, new Object[][]{new object[] {"E1", -10}});
            Assert.IsFalse(listenerWindow.IsInvoked);
            Assert.AreEqual(1, GetCount(epService, "MyWindowSTAGTwo"));
    
            SendSupportBean(epService, "E2", 5);
            EPAssertionUtil.AssertProps(listenerWindow.AssertOneGetNewAndReset(), fieldsOne, new Object[]{"E2", 5});
            EPAssertionUtil.AssertPropsPerRow(stmtCreateOne.GetEnumerator(), fieldsOne, new Object[][]{new object[] {"E2", 5}});
            Assert.IsFalse(listenerWindowTwo.IsInvoked);
            Assert.AreEqual(1, GetCount(epService, "MyWindowSTAG"));
    
            SendSupportBean(epService, "E3", -1);
            EPAssertionUtil.AssertProps(listenerWindowTwo.AssertOneGetNewAndReset(), fieldsTwo, new Object[]{"E3", -1});
            EPAssertionUtil.AssertPropsPerRow(stmtCreateTwo.GetEnumerator(), fieldsTwo, new Object[][]{new object[] {"E1", -10}, new object[] {"E3", -1}});
            Assert.IsFalse(listenerWindow.IsInvoked);
            Assert.AreEqual(2, GetCount(epService, "MyWindowSTAGTwo"));
    
            SendSupportBean(epService, "E3", 1);
            EPAssertionUtil.AssertProps(listenerWindow.AssertOneGetNewAndReset(), fieldsOne, new Object[]{"E3", 1});
            EPAssertionUtil.AssertPropsPerRow(stmtCreateOne.GetEnumerator(), fieldsOne, new Object[][]{new object[] {"E2", 5}, new object[] {"E3", 1}});
            EPAssertionUtil.AssertProps(listenerWindowTwo.AssertOneGetOldAndReset(), fieldsTwo, new Object[]{"E3", -1});
            EPAssertionUtil.AssertPropsPerRow(stmtCreateTwo.GetEnumerator(), fieldsTwo, new Object[][]{new object[] {"E1", -10}});
            Assert.AreEqual(2, GetCount(epService, "MyWindowSTAG"));
            Assert.AreEqual(1, GetCount(epService, "MyWindowSTAGTwo"));
    
            stmtDelete.Dispose();
            stmtCreateOne.Dispose();
            stmtCreateTwo.Dispose();
            listenerDelete.Reset();
            listenerWindow.Reset();
            listenerWindowTwo.Reset();
            epService.EPAdministrator.Configuration.RemoveEventType("MyWindowSTAG", true);
            epService.EPAdministrator.Configuration.RemoveEventType("MyWindowSTAGTwo", true);
        }
    
        private void RunAssertionCoercionKeyMultiPropIndexes(EPServiceProvider epService) {
            // create window
            string stmtTextCreate = "create window MyWindowCK#keepall as select " +
                    "theString, intPrimitive, intBoxed, doublePrimitive, doubleBoxed from " + typeof(SupportBean).FullName;
            EPStatement stmtCreate = epService.EPAdministrator.CreateEPL(stmtTextCreate);
            var listenerWindow = new SupportUpdateListener();
            stmtCreate.AddListener(listenerWindow);
    
            var deleteStatements = new LinkedList<>();
            string stmtTextDelete = "on " + typeof(SupportBean).FullName + "(theString='DB') as s0 delete from MyWindowCK as win where win.intPrimitive = s0.doubleBoxed";
            deleteStatements.Add(epService.EPAdministrator.CreateEPL(stmtTextDelete));
            Assert.AreEqual(1, GetNWMW(epService).GetNamedWindowIndexes("MyWindowCK").Length);
    
            stmtTextDelete = "on " + typeof(SupportBean).FullName + "(theString='DP') as s0 delete from MyWindowCK as win where win.intPrimitive = s0.doublePrimitive";
            deleteStatements.Add(epService.EPAdministrator.CreateEPL(stmtTextDelete));
            Assert.AreEqual(1, GetNWMW(epService).GetNamedWindowIndexes("MyWindowCK").Length);
    
            stmtTextDelete = "on " + typeof(SupportBean).FullName + "(theString='IB') as s0 delete from MyWindowCK where MyWindowCK.intPrimitive = s0.intBoxed";
            deleteStatements.Add(epService.EPAdministrator.CreateEPL(stmtTextDelete));
            Assert.AreEqual(2, GetNWMW(epService).GetNamedWindowIndexes("MyWindowCK").Length);
    
            stmtTextDelete = "on " + typeof(SupportBean).FullName + "(theString='IPDP') as s0 delete from MyWindowCK as win where win.intPrimitive = s0.intPrimitive and win.doublePrimitive = s0.doublePrimitive";
            deleteStatements.Add(epService.EPAdministrator.CreateEPL(stmtTextDelete));
            Assert.AreEqual(2, GetNWMW(epService).GetNamedWindowIndexes("MyWindowCK").Length);
    
            stmtTextDelete = "on " + typeof(SupportBean).FullName + "(theString='IPDP2') as s0 delete from MyWindowCK as win where win.doublePrimitive = s0.doublePrimitive and win.intPrimitive = s0.intPrimitive";
            deleteStatements.Add(epService.EPAdministrator.CreateEPL(stmtTextDelete));
            Assert.AreEqual(2, GetNWMW(epService).GetNamedWindowIndexes("MyWindowCK").Length);
    
            stmtTextDelete = "on " + typeof(SupportBean).FullName + "(theString='IPDPIB') as s0 delete from MyWindowCK as win where win.doublePrimitive = s0.doublePrimitive and win.intPrimitive = s0.intPrimitive and win.intBoxed = s0.intBoxed";
            deleteStatements.Add(epService.EPAdministrator.CreateEPL(stmtTextDelete));
            Assert.AreEqual(2, GetNWMW(epService).GetNamedWindowIndexes("MyWindowCK").Length);
    
            stmtTextDelete = "on " + typeof(SupportBean).FullName + "(theString='CAST') as s0 delete from MyWindowCK as win where win.intBoxed = s0.intPrimitive and win.doublePrimitive = s0.doubleBoxed and win.intPrimitive = s0.intBoxed";
            deleteStatements.Add(epService.EPAdministrator.CreateEPL(stmtTextDelete));
            Assert.AreEqual(2, GetNWMW(epService).GetNamedWindowIndexes("MyWindowCK").Length);
    
            // create insert into
            string stmtTextInsertOne = "insert into MyWindowCK select theString, intPrimitive, intBoxed, doublePrimitive, doubleBoxed "
                    + "from " + typeof(SupportBean).FullName + "(theString like 'E%')";
            epService.EPAdministrator.CreateEPL(stmtTextInsertOne);
    
            SendSupportBean(epService, "E1", 1, 10, 100d, 1000d);
            SendSupportBean(epService, "E2", 2, 20, 200d, 2000d);
            SendSupportBean(epService, "E3", 3, 30, 300d, 3000d);
            SendSupportBean(epService, "E4", 4, 40, 400d, 4000d);
            listenerWindow.Reset();
    
            var fields = new string[]{"theString"};
            EPAssertionUtil.AssertPropsPerRow(stmtCreate.GetEnumerator(), fields, new Object[][]{new object[] {"E1"}, new object[] {"E2"}, new object[] {"E3"}, new object[] {"E4"}});
    
            SendSupportBean(epService, "DB", 0, 0, 0d, null);
            Assert.IsFalse(listenerWindow.IsInvoked);
            SendSupportBean(epService, "DB", 0, 0, 0d, 3d);
            EPAssertionUtil.AssertProps(listenerWindow.AssertOneGetOldAndReset(), fields, new Object[]{"E3"});
            EPAssertionUtil.AssertPropsPerRow(stmtCreate.GetEnumerator(), fields, new Object[][]{new object[] {"E1"}, new object[] {"E2"}, new object[] {"E4"}});
    
            SendSupportBean(epService, "DP", 0, 0, 5d, null);
            Assert.IsFalse(listenerWindow.IsInvoked);
            SendSupportBean(epService, "DP", 0, 0, 4d, null);
            EPAssertionUtil.AssertProps(listenerWindow.AssertOneGetOldAndReset(), fields, new Object[]{"E4"});
            EPAssertionUtil.AssertPropsPerRow(stmtCreate.GetEnumerator(), fields, new Object[][]{new object[] {"E1"}, new object[] {"E2"}});
    
            SendSupportBean(epService, "IB", 0, -1, 0d, null);
            Assert.IsFalse(listenerWindow.IsInvoked);
            SendSupportBean(epService, "IB", 0, 1, 0d, null);
            EPAssertionUtil.AssertProps(listenerWindow.AssertOneGetOldAndReset(), fields, new Object[]{"E1"});
            EPAssertionUtil.AssertPropsPerRow(stmtCreate.GetEnumerator(), fields, new Object[][]{new object[] {"E2"}});
    
            SendSupportBean(epService, "E5", 5, 50, 500d, 5000d);
            SendSupportBean(epService, "E6", 6, 60, 600d, 6000d);
            SendSupportBean(epService, "E7", 7, 70, 700d, 7000d);
            listenerWindow.Reset();
    
            SendSupportBean(epService, "IPDP", 5, 0, 500d, null);
            EPAssertionUtil.AssertProps(listenerWindow.AssertOneGetOldAndReset(), fields, new Object[]{"E5"});
            EPAssertionUtil.AssertPropsPerRow(stmtCreate.GetEnumerator(), fields, new Object[][]{new object[] {"E2"}, new object[] {"E6"}, new object[] {"E7"}});
    
            SendSupportBean(epService, "IPDP2", 6, 0, 600d, null);
            EPAssertionUtil.AssertProps(listenerWindow.AssertOneGetOldAndReset(), fields, new Object[]{"E6"});
            EPAssertionUtil.AssertPropsPerRow(stmtCreate.GetEnumerator(), fields, new Object[][]{new object[] {"E2"}, new object[] {"E7"}});
    
            SendSupportBean(epService, "IPDPIB", 7, 70, 0d, null);
            Assert.IsFalse(listenerWindow.IsInvoked);
            SendSupportBean(epService, "IPDPIB", 7, 70, 700d, null);
            EPAssertionUtil.AssertProps(listenerWindow.AssertOneGetOldAndReset(), fields, new Object[]{"E7"});
            EPAssertionUtil.AssertPropsPerRow(stmtCreate.GetEnumerator(), fields, new Object[][]{new object[] {"E2"}});
    
            SendSupportBean(epService, "E8", 8, 80, 800d, 8000d);
            listenerWindow.Reset();
            EPAssertionUtil.AssertPropsPerRow(stmtCreate.GetEnumerator(), fields, new Object[][]{new object[] {"E2"}, new object[] {"E8"}});
    
            SendSupportBean(epService, "CAST", 80, 8, 0, 800d);
            EPAssertionUtil.AssertProps(listenerWindow.AssertOneGetOldAndReset(), fields, new Object[]{"E8"});
            EPAssertionUtil.AssertPropsPerRow(stmtCreate.GetEnumerator(), fields, new Object[][]{new object[] {"E2"}});
    
            foreach (EPStatement stmt in deleteStatements) {
                stmt.Dispose();
            }
            deleteStatements.Clear();
    
            // late delete on a filled window
            stmtTextDelete = "on " + typeof(SupportBean).FullName + "(theString='LAST') as s0 delete from MyWindowCK as win where win.intPrimitive = s0.intPrimitive and win.doublePrimitive = s0.doublePrimitive";
            deleteStatements.Add(epService.EPAdministrator.CreateEPL(stmtTextDelete));
            SendSupportBean(epService, "LAST", 2, 20, 200, 2000d);
            EPAssertionUtil.AssertProps(listenerWindow.AssertOneGetOldAndReset(), fields, new Object[]{"E2"});
            EPAssertionUtil.AssertPropsPerRow(stmtCreate.GetEnumerator(), fields, null);
    
            foreach (EPStatement stmt in deleteStatements) {
                stmt.Dispose();
            }
    
            // test single-two-field index reuse
            epService.EPAdministrator.Configuration.AddEventType("SupportBean_ST0", typeof(SupportBean_ST0));
            epService.EPAdministrator.Configuration.AddEventType<SupportBean>();
            epService.EPAdministrator.CreateEPL("create window WinOne#keepall as SupportBean");
            epService.EPAdministrator.CreateEPL("on SupportBean_ST0 select * from WinOne where theString = key0");
            Assert.AreEqual(1, GetNWMW(epService).GetNamedWindowIndexes("WinOne").Length);
    
            epService.EPAdministrator.CreateEPL("on SupportBean_ST0 select * from WinOne where theString = key0 and intPrimitive = p00");
            Assert.AreEqual(1, GetNWMW(epService).GetNamedWindowIndexes("WinOne").Length);
    
            epService.EPAdministrator.DestroyAllStatements();
        }
    
        private void RunAssertionCoercionRangeMultiPropIndexes(EPServiceProvider epService) {
            epService.EPAdministrator.Configuration.AddEventType<SupportBean>();
            epService.EPAdministrator.Configuration.AddEventType<SupportBeanTwo>();
    
            // create window
            string stmtTextCreate = "create window MyWindowCR#keepall as select " +
                    "theString, intPrimitive, intBoxed, doublePrimitive, doubleBoxed from SupportBean";
            EPStatement stmtCreate = epService.EPAdministrator.CreateEPL(stmtTextCreate);
            var listenerWindow = new SupportUpdateListener();
            stmtCreate.AddListener(listenerWindow);
            string stmtText = "insert into MyWindowCR select theString, intPrimitive, intBoxed, doublePrimitive, doubleBoxed from SupportBean";
            epService.EPAdministrator.CreateEPL(stmtText);
            var fields = new string[]{"theString"};
    
            SendSupportBean(epService, "E1", 1, 10, 100d, 1000d);
            SendSupportBean(epService, "E2", 2, 20, 200d, 2000d);
            SendSupportBean(epService, "E3", 3, 30, 3d, 30d);
            SendSupportBean(epService, "E4", 4, 40, 4d, 40d);
            SendSupportBean(epService, "E5", 5, 50, 500d, 5000d);
            SendSupportBean(epService, "E6", 6, 60, 600d, 6000d);
            listenerWindow.Reset();
    
            var deleteStatements = new LinkedList<>();
            string stmtTextDelete = "on SupportBeanTwo as s2 delete from MyWindowCR as win where win.intPrimitive between s2.doublePrimitiveTwo and s2.doubleBoxedTwo";
            deleteStatements.Add(epService.EPAdministrator.CreateEPL(stmtTextDelete));
            Assert.AreEqual(1, GetNWMW(epService).GetNamedWindowIndexes("MyWindowCR").Length);
    
            SendSupportBeanTwo(epService, "T", 0, 0, 0d, null);
            Assert.IsFalse(listenerWindow.IsInvoked);
            SendSupportBeanTwo(epService, "T", 0, 0, -1d, 1d);
            EPAssertionUtil.AssertProps(listenerWindow.AssertOneGetOldAndReset(), fields, new Object[]{"E1"});
    
            stmtTextDelete = "on SupportBeanTwo as s2 delete from MyWindowCR as win where win.intPrimitive between s2.intPrimitiveTwo and s2.intBoxedTwo";
            deleteStatements.Add(epService.EPAdministrator.CreateEPL(stmtTextDelete));
            Assert.AreEqual(2, GetNWMW(epService).GetNamedWindowIndexes("MyWindowCR").Length);
    
            SendSupportBeanTwo(epService, "T", -2, 2, 0d, 0d);
            EPAssertionUtil.AssertProps(listenerWindow.AssertOneGetOldAndReset(), fields, new Object[]{"E2"});
    
            stmtTextDelete = "on SupportBeanTwo as s2 delete from MyWindowCR as win " +
                    "where win.intPrimitive between s2.intPrimitiveTwo and s2.intBoxedTwo and win.doublePrimitive between s2.intPrimitiveTwo and s2.intBoxedTwo";
            deleteStatements.Add(epService.EPAdministrator.CreateEPL(stmtTextDelete));
            Assert.AreEqual(2, GetNWMW(epService).GetNamedWindowIndexes("MyWindowCR").Length);
    
            SendSupportBeanTwo(epService, "T", -3, 3, -3d, 3d);
            EPAssertionUtil.AssertProps(listenerWindow.AssertOneGetOldAndReset(), fields, new Object[]{"E3"});
    
            stmtTextDelete = "on SupportBeanTwo as s2 delete from MyWindowCR as win " +
                    "where win.doublePrimitive between s2.intPrimitiveTwo and s2.intPrimitiveTwo and win.intPrimitive between s2.intPrimitiveTwo and s2.intPrimitiveTwo";
            deleteStatements.Add(epService.EPAdministrator.CreateEPL(stmtTextDelete));
            Assert.AreEqual(2, GetNWMW(epService).GetNamedWindowIndexes("MyWindowCR").Length);
    
            SendSupportBeanTwo(epService, "T", -4, 4, -4, 4d);
            EPAssertionUtil.AssertProps(listenerWindow.AssertOneGetOldAndReset(), fields, new Object[]{"E4"});
    
            stmtTextDelete = "on SupportBeanTwo as s2 delete from MyWindowCR as win where win.intPrimitive <= doublePrimitiveTwo";
            deleteStatements.Add(epService.EPAdministrator.CreateEPL(stmtTextDelete));
            Assert.AreEqual(2, GetNWMW(epService).GetNamedWindowIndexes("MyWindowCR").Length);
    
            SendSupportBeanTwo(epService, "T", 0, 0, 5, 1d);
            EPAssertionUtil.AssertProps(listenerWindow.AssertOneGetOldAndReset(), fields, new Object[]{"E5"});
    
            stmtTextDelete = "on SupportBeanTwo as s2 delete from MyWindowCR as win where win.intPrimitive not between s2.intPrimitiveTwo and s2.intBoxedTwo";
            deleteStatements.Add(epService.EPAdministrator.CreateEPL(stmtTextDelete));
            Assert.AreEqual(2, GetNWMW(epService).GetNamedWindowIndexes("MyWindowCR").Length);
    
            SendSupportBeanTwo(epService, "T", 100, 200, 0, 0d);
            EPAssertionUtil.AssertProps(listenerWindow.AssertOneGetOldAndReset(), fields, new Object[]{"E6"});
    
            // delete
            foreach (EPStatement stmt in deleteStatements) {
                stmt.Dispose();
            }
            deleteStatements.Clear();
            Assert.AreEqual(0, GetNWMW(epService).GetNamedWindowIndexes("MyWindowCR").Length);
        }
    
        private void RunAssertionCoercionKeyAndRangeMultiPropIndexes(EPServiceProvider epService) {
            epService.EPAdministrator.Configuration.AddEventType<SupportBean>();
            epService.EPAdministrator.Configuration.AddEventType<SupportBeanTwo>();
    
            // create window
            string stmtTextCreate = "create window MyWindowCKR#keepall as select " +
                    "theString, intPrimitive, intBoxed, doublePrimitive, doubleBoxed from SupportBean";
            EPStatement stmtCreate = epService.EPAdministrator.CreateEPL(stmtTextCreate);
            var listenerWindow = new SupportUpdateListener();
            stmtCreate.AddListener(listenerWindow);
            string stmtText = "insert into MyWindowCKR select theString, intPrimitive, intBoxed, doublePrimitive, doubleBoxed from SupportBean";
            epService.EPAdministrator.CreateEPL(stmtText);
            var fields = new string[]{"theString"};
    
            SendSupportBean(epService, "E1", 1, 10, 100d, 1000d);
            SendSupportBean(epService, "E2", 2, 20, 200d, 2000d);
            SendSupportBean(epService, "E3", 3, 30, 300d, 3000d);
            SendSupportBean(epService, "E4", 4, 40, 400d, 4000d);
            listenerWindow.Reset();
    
            var deleteStatements = new LinkedList<>();
            string stmtTextDelete = "on SupportBeanTwo delete from MyWindowCKR where theString = stringTwo and intPrimitive between doublePrimitiveTwo and doubleBoxedTwo";
            deleteStatements.Add(epService.EPAdministrator.CreateEPL(stmtTextDelete));
            Assert.AreEqual(1, GetNWMW(epService).GetNamedWindowIndexes("MyWindowCKR").Length);
    
            SendSupportBeanTwo(epService, "T", 0, 0, 1d, 200d);
            Assert.IsFalse(listenerWindow.IsInvoked);
            SendSupportBeanTwo(epService, "E1", 0, 0, 1d, 200d);
            EPAssertionUtil.AssertProps(listenerWindow.AssertOneGetOldAndReset(), fields, new Object[]{"E1"});
    
            stmtTextDelete = "on SupportBeanTwo delete from MyWindowCKR where theString = stringTwo and intPrimitive = intPrimitiveTwo and intBoxed between doublePrimitiveTwo and doubleBoxedTwo";
            deleteStatements.Add(epService.EPAdministrator.CreateEPL(stmtTextDelete));
            Assert.AreEqual(2, GetNWMW(epService).GetNamedWindowIndexes("MyWindowCKR").Length);
    
            SendSupportBeanTwo(epService, "E2", 2, 0, 19d, 21d);
            EPAssertionUtil.AssertProps(listenerWindow.AssertOneGetOldAndReset(), fields, new Object[]{"E2"});
    
            stmtTextDelete = "on SupportBeanTwo delete from MyWindowCKR where intBoxed between doubleBoxedTwo and doublePrimitiveTwo and intPrimitive = intPrimitiveTwo and theString = stringTwo ";
            deleteStatements.Add(epService.EPAdministrator.CreateEPL(stmtTextDelete));
            Assert.AreEqual(2, GetNWMW(epService).GetNamedWindowIndexes("MyWindowCKR").Length);
    
            SendSupportBeanTwo(epService, "E3", 3, 0, 29d, 34d);
            EPAssertionUtil.AssertProps(listenerWindow.AssertOneGetOldAndReset(), fields, new Object[]{"E3"});
    
            stmtTextDelete = "on SupportBeanTwo delete from MyWindowCKR where intBoxed between intBoxedTwo and intBoxedTwo and intPrimitive = intPrimitiveTwo and theString = stringTwo ";
            deleteStatements.Add(epService.EPAdministrator.CreateEPL(stmtTextDelete));
            Assert.AreEqual(3, GetNWMW(epService).GetNamedWindowIndexes("MyWindowCKR").Length);
    
            SendSupportBeanTwo(epService, "E4", 4, 40, 0d, null);
            EPAssertionUtil.AssertProps(listenerWindow.AssertOneGetOldAndReset(), fields, new Object[]{"E4"});
    
            // delete
            foreach (EPStatement stmt in deleteStatements) {
                stmt.Dispose();
            }
            deleteStatements.Clear();
            Assert.AreEqual(0, GetNWMW(epService).GetNamedWindowIndexes("MyWindowCKR").Length);
        }
    
        private void SendSupportBean(EPServiceProvider epService, string theString, int intPrimitive, int? intBoxed,
                                     double doublePrimitive, double? doubleBoxed) {
            var bean = new SupportBean();
            bean.TheString = theString;
            bean.IntPrimitive = intPrimitive;
            bean.IntBoxed = intBoxed;
            bean.DoublePrimitive = doublePrimitive;
            bean.DoubleBoxed = doubleBoxed;
            epService.EPRuntime.SendEvent(bean);
        }
    
        private void SendSupportBeanTwo(EPServiceProvider epService, string theString, int intPrimitive, int? intBoxed,
                                        double doublePrimitive, double? doubleBoxed) {
            var bean = new SupportBeanTwo();
            bean.StringTwo = theString;
            bean.IntPrimitiveTwo = intPrimitive;
            bean.IntBoxedTwo = intBoxed;
            bean.DoublePrimitiveTwo = doublePrimitive;
            bean.DoubleBoxedTwo = doubleBoxed;
            epService.EPRuntime.SendEvent(bean);
        }
    
        private void SendSupportBean(EPServiceProvider epService, string theString, int intPrimitive) {
            var bean = new SupportBean();
            bean.TheString = theString;
            bean.IntPrimitive = intPrimitive;
            epService.EPRuntime.SendEvent(bean);
        }
    
        private long GetCount(EPServiceProvider epService, string windowName) {
            NamedWindowProcessor processor = GetNWMW(epService).GetProcessor(windowName);
            return Processor.GetProcessorInstance(null).CountDataWindow;
        }
    
        private NamedWindowMgmtService GetNWMW(EPServiceProvider epService) {
            return ((EPServiceProviderSPI) epService).NamedWindowMgmtService;
        }
    }
} // end of namespace
