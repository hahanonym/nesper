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
using com.espertech.esper.supportregression.util;
using com.espertech.esper.util.support;

// using static org.junit.Assert.assertEquals;

using NUnit.Framework;

namespace com.espertech.esper.regression.nwtable.tbl
{
    public class ExecTableOnMerge : RegressionExecution {
    
        public override void Run(EPServiceProvider epService) {
            foreach (var clazz in new Type[]{typeof(SupportBean), typeof(SupportBean_S0), typeof(SupportBean_S1)}) {
                epService.EPAdministrator.Configuration.AddEventType(clazz);
            }
    
            RunAssertionMergeWhereWithMethodRead(epService);
            RunAssertionMergeSelectWithAggReadAndEnum(epService);
            RunAssertionOnMergePlainPropsAnyKeyed(epService);
        }
    
        private void RunAssertionMergeWhereWithMethodRead(EPServiceProvider epService) {
            epService.EPAdministrator.CreateEPL("create table varaggMMR (keyOne string primary key, cnt count(*))");
            epService.EPAdministrator.CreateEPL("into table varaggMMR select count(*) as cnt " +
                    "from SupportBean#lastevent group by theString");
    
            var listener = new SupportUpdateListener();
            epService.EPAdministrator.CreateEPL("select varaggMMR[p00].keyOne as c0 from SupportBean_S0").AddListener(listener);
            epService.EPAdministrator.CreateEPL("on SupportBean_S1 merge varaggMMR where cnt = 0 when matched then delete");
    
            epService.EPRuntime.SendEvent(new SupportBean("G1", 0));
            epService.EPRuntime.SendEvent(new SupportBean("G2", 0));
            AssertKeyFound(epService, listener, "G1,G2,G3", new bool[]{true, true, false});
    
            epService.EPRuntime.SendEvent(new SupportBean_S1(0)); // delete
            AssertKeyFound(epService, listener, "G1,G2,G3", new bool[]{false, true, false});
    
            epService.EPRuntime.SendEvent(new SupportBean("G3", 0));
            AssertKeyFound(epService, listener, "G1,G2,G3", new bool[]{false, true, true});
    
            epService.EPRuntime.SendEvent(new SupportBean_S1(0));  // delete
            AssertKeyFound(epService, listener, "G1,G2,G3", new bool[]{false, false, true});
    
            epService.EPAdministrator.DestroyAllStatements();
        }
    
        private void RunAssertionMergeSelectWithAggReadAndEnum(EPServiceProvider epService) {
            var listener = new SupportUpdateListener();
            epService.EPAdministrator.CreateEPL("create table varaggMS (" +
                    "eventset window(*) @Type(SupportBean), total sum(int))");
            epService.EPAdministrator.CreateEPL("into table varaggMS select window(*) as eventset, " +
                    "sum(intPrimitive) as total from SupportBean#length(2)");
            epService.EPAdministrator.CreateEPL("on SupportBean_S0 merge varaggMS " +
                    "when matched then insert into ResultStream select eventset, total, eventset.TakeLast(1) as c0");
            epService.EPAdministrator.CreateEPL("select * from ResultStream").AddListener(listener);
    
            var e1 = new SupportBean("E1", 15);
            epService.EPRuntime.SendEvent(e1);
    
            AssertResultAggRead(epService, listener, new Object[]{e1}, 15);
    
            var e2 = new SupportBean("E2", 20);
            epService.EPRuntime.SendEvent(e2);
    
            AssertResultAggRead(epService, listener, new Object[]{e1, e2}, 35);
    
            var e3 = new SupportBean("E3", 30);
            epService.EPRuntime.SendEvent(e3);
    
            AssertResultAggRead(epService, listener, new Object[]{e2, e3}, 50);
    
            epService.EPAdministrator.DestroyAllStatements();
        }
    
        private void AssertResultAggRead(EPServiceProvider epService, SupportUpdateListener listener, Object[] objects, int total) {
            string[] fields = "eventset,total".Split(',');
            epService.EPRuntime.SendEvent(new SupportBean_S0(0));
            EventBean @event = listener.AssertOneGetNewAndReset();
            EPAssertionUtil.AssertProps(@event, fields, new Object[]{objects, total});
            EPAssertionUtil.AssertEqualsExactOrder(new Object[]{objects[objects.Length - 1]}, ((Collection) @event.Get("c0")).ToArray());
        }
    
        private void AssertKeyFound(EPServiceProvider epService, SupportUpdateListener listener, string keyCsv, bool[] expected) {
            string[] split = keyCsv.Split(',');
            for (int i = 0; i < split.Length; i++) {
                string key = split[i];
                epService.EPRuntime.SendEvent(new SupportBean_S0(0, key));
                string expectedString = expected[i] ? key : null;
                Assert.AreEqual("failed for key '" + key + "'", expectedString, listener.AssertOneGetNewAndReset().Get("c0"));
            }
        }
    
        private void RunAssertionOnMergePlainPropsAnyKeyed(EPServiceProvider epService) {
            RunOnMergeInsertUpdDeleteSingleKey(epService, true);
            RunOnMergeInsertUpdDeleteSingleKey(epService, false);
    
            RunOnMergeInsertUpdDeleteTwoKey(epService, true);
            RunOnMergeInsertUpdDeleteTwoKey(epService, false);
    
            RunOnMergeInsertUpdDeleteUngrouped(epService, true);
            RunOnMergeInsertUpdDeleteUngrouped(epService, false);
        }
    
        private void RunOnMergeInsertUpdDeleteUngrouped(EPServiceProvider epService, bool soda) {
            string eplDeclare = "create table varaggIUD (p0 string, sumint sum(int))";
            SupportModelHelper.CreateByCompileOrParse(epService, soda, eplDeclare);
    
            string[] fields = "c0,c1".Split(',');
            string eplRead = "select varaggIUD.p0 as c0, varaggIUD.sumint as c1, varaggIUD as c2 from SupportBean_S0";
            EPStatement stmtRead = SupportModelHelper.CreateByCompileOrParse(epService, soda, eplRead);
            var listener = new SupportUpdateListener();
            stmtRead.AddListener(listener);
    
            // assert selected column types
            var expectedAggType = new Object[][]{new object[] {"c0", typeof(string)}, new object[] {"c1", typeof(int?)}};
            SupportEventTypeAssertionUtil.AssertEventTypeProperties(expectedAggType, stmtRead.EventType, SupportEventTypeAssertionEnum.NAME, SupportEventTypeAssertionEnum.TYPE);
    
            // assert no row
            epService.EPRuntime.SendEvent(new SupportBean_S0(0));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{null, null});
    
            // create merge
            string eplMerge = "on SupportBean merge varaggIUD" +
                    " when not matched then" +
                    " insert select theString as p0" +
                    " when matched and theString like \"U%\" then" +
                    " update set p0=\"updated\"" +
                    " when matched and theString like \"D%\" then" +
                    " delete";
            SupportModelHelper.CreateByCompileOrParse(epService, soda, eplMerge);
    
            // merge for varagg
            epService.EPRuntime.SendEvent(new SupportBean("E1", 0));
    
            // assert
            epService.EPRuntime.SendEvent(new SupportBean_S0(0));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{"E1", null});
    
            // also aggregate-into the same key
            SupportModelHelper.CreateByCompileOrParse(epService, soda, "into table varaggIUD select sum(50) as sumint from SupportBean_S1");
            epService.EPRuntime.SendEvent(new SupportBean_S1(0));
            epService.EPRuntime.SendEvent(new SupportBean_S0(0));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{"E1", 50});
    
            // update for varagg
            epService.EPRuntime.SendEvent(new SupportBean("U2", 10));
            epService.EPRuntime.SendEvent(new SupportBean_S0(0));
            EventBean received = listener.AssertOneGetNewAndReset();
            EPAssertionUtil.AssertProps(received, fields, new Object[]{"updated", 50});
            EPAssertionUtil.AssertPropsMap((Map) received.Get("c2"), "p0,sumint".Split(','), new Object[]{"updated", 50});
    
            // delete for varagg
            epService.EPRuntime.SendEvent(new SupportBean("D3", 0));
            epService.EPRuntime.SendEvent(new SupportBean_S0(0));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{null, null});
    
            epService.EPAdministrator.DestroyAllStatements();
        }
    
        public void RunOnMergeInsertUpdDeleteSingleKey(EPServiceProvider epService, bool soda) {
            string[] fieldsTable = "key,p0,p1,p2,sumint".Split(',');
            string eplDeclare = "create table varaggMIU (key int primary key, p0 string, p1 int, p2 int[], sumint sum(int))";
            SupportModelHelper.CreateByCompileOrParse(epService, soda, eplDeclare);
    
            string[] fields = "c0,c1,c2,c3".Split(',');
            string eplRead = "select varaggMIU[id].p0 as c0, varaggMIU[id].p1 as c1, varaggMIU[id].p2 as c2, varaggMIU[id].sumint as c3 from SupportBean_S0";
            EPStatement stmtRead = SupportModelHelper.CreateByCompileOrParse(epService, soda, eplRead);
            var listener = new SupportUpdateListener();
            stmtRead.AddListener(listener);
    
            // assert selected column types
            var expectedAggType = new Object[][]{new object[] {"c0", typeof(string)}, new object[] {"c1", typeof(int?)}, new object[] {"c2", typeof(int?[])}, new object[] {"c3", typeof(int?)}};
            SupportEventTypeAssertionUtil.AssertEventTypeProperties(expectedAggType, stmtRead.EventType, SupportEventTypeAssertionEnum.NAME, SupportEventTypeAssertionEnum.TYPE);
    
            // assert no row
            epService.EPRuntime.SendEvent(new SupportBean_S0(10));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{null, null, null, null});
    
            // create merge
            string eplMerge = "on SupportBean merge varaggMIU" +
                    " where intPrimitive=key" +
                    " when not matched then" +
                    " insert select intPrimitive as key, \"v1\" as p0, 1000 as p1, {1,2} as p2" +
                    " when matched and theString like \"U%\" then" +
                    " update set p0=\"v2\", p1=2000, p2={3,4}" +
                    " when matched and theString like \"D%\" then" +
                    " delete";
            EPStatement stmtMerge = SupportModelHelper.CreateByCompileOrParse(epService, soda, eplMerge);
            var listenerMerge = new SupportUpdateListener();
            stmtMerge.AddListener(listenerMerge);
    
            // merge for varagg[10]
            epService.EPRuntime.SendEvent(new SupportBean("E1", 10));
            EPAssertionUtil.AssertProps(listenerMerge.AssertOneGetNewAndReset(), fieldsTable, new Object[]{10, "v1", 1000, new int[]{1, 2}, null});
    
            // assert key "10"
            epService.EPRuntime.SendEvent(new SupportBean_S0(10));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{"v1", 1000, new int?[]{1, 2}, null});
    
            // also aggregate-into the same key
            SupportModelHelper.CreateByCompileOrParse(epService, soda, "into table varaggMIU select sum(50) as sumint from SupportBean_S1 group by id");
            epService.EPRuntime.SendEvent(new SupportBean_S1(10));
            epService.EPRuntime.SendEvent(new SupportBean_S0(10));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{"v1", 1000, new int?[]{1, 2}, 50});
    
            // update for varagg[10]
            epService.EPRuntime.SendEvent(new SupportBean("U2", 10));
            EPAssertionUtil.AssertProps(listenerMerge.LastNewData[0], fieldsTable, new Object[]{10, "v2", 2000, new int[]{3, 4}, 50});
            EPAssertionUtil.AssertProps(listenerMerge.GetAndResetLastOldData()[0], fieldsTable, new Object[]{10, "v1", 1000, new int[]{1, 2}, 50});
    
            epService.EPRuntime.SendEvent(new SupportBean_S0(10));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{"v2", 2000, new int?[]{3, 4}, 50});
    
            // delete for varagg[10]
            epService.EPRuntime.SendEvent(new SupportBean("D3", 10));
            EPAssertionUtil.AssertProps(listenerMerge.AssertOneGetOldAndReset(), fieldsTable, new Object[]{10, "v2", 2000, new int[]{3, 4}, 50});
    
            epService.EPRuntime.SendEvent(new SupportBean_S0(10));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{null, null, null, null});
    
            epService.EPAdministrator.DestroyAllStatements();
            epService.EPAdministrator.Configuration.RemoveEventType("table_varaggMIU__internal", false);
            epService.EPAdministrator.Configuration.RemoveEventType("table_varaggMIU__public", false);
        }
    
        public void RunOnMergeInsertUpdDeleteTwoKey(EPServiceProvider epService, bool soda) {
            string eplDeclare = "create table varaggMIUD (keyOne int primary key, keyTwo string primary key, prop string)";
            SupportModelHelper.CreateByCompileOrParse(epService, soda, eplDeclare);
    
            string[] fields = "c0,c1,c2".Split(',');
            string eplRead = "select varaggMIUD[id,p00].keyOne as c0, varaggMIUD[id,p00].keyTwo as c1, varaggMIUD[id,p00].prop as c2 from SupportBean_S0";
            EPStatement stmtRead = SupportModelHelper.CreateByCompileOrParse(epService, soda, eplRead);
            var listener = new SupportUpdateListener();
            stmtRead.AddListener(listener);
    
            // assert selected column types
            var expectedAggType = new Object[][]{new object[] {"c0", typeof(int?)}, new object[] {"c1", typeof(string)}, new object[] {"c2", typeof(string)}};
            SupportEventTypeAssertionUtil.AssertEventTypeProperties(expectedAggType, stmtRead.EventType, SupportEventTypeAssertionEnum.NAME, SupportEventTypeAssertionEnum.TYPE);
    
            // assert no row
            epService.EPRuntime.SendEvent(new SupportBean_S0(10, "A"));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{null, null, null});
    
            // create merge
            string eplMerge = "on SupportBean merge varaggMIUD" +
                    " where intPrimitive=keyOne and theString=keyTwo" +
                    " when not matched then" +
                    " insert select intPrimitive as keyOne, theString as keyTwo, \"inserted\" as prop" +
                    " when matched and longPrimitive>0 then" +
                    " update set prop=\"updated\"" +
                    " when matched and longPrimitive<0 then" +
                    " delete";
            EPStatement stmtMerge = SupportModelHelper.CreateByCompileOrParse(epService, soda, eplMerge);
            var expectedType = new Object[][]{new object[] {"keyOne", typeof(int?)}, new object[] {"keyTwo", typeof(string)}, new object[] {"prop", typeof(string)}};
            SupportEventTypeAssertionUtil.AssertEventTypeProperties(expectedType, stmtMerge.EventType, SupportEventTypeAssertionEnum.NAME, SupportEventTypeAssertionEnum.TYPE);
    
    
            // merge for varagg[10, "A"]
            epService.EPRuntime.SendEvent(new SupportBean("A", 10));
    
            // assert key {"10", "A"}
            epService.EPRuntime.SendEvent(new SupportBean_S0(10, "A"));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{10, "A", "inserted"});
    
            // update for varagg[10, "A"]
            epService.EPRuntime.SendEvent(MakeSupportBean("A", 10, 1));
            epService.EPRuntime.SendEvent(new SupportBean_S0(10, "A"));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{10, "A", "updated"});
    
            // test typable output
            epService.EPAdministrator.Configuration.AddEventType(typeof(LocalBean));
            EPStatement stmtConvert = epService.EPAdministrator.CreateEPL("insert into LocalBean select varaggMIUD[10, 'A'] as val0 from SupportBean_S1");
            stmtConvert.AddListener(listener);
            epService.EPRuntime.SendEvent(new SupportBean_S1(2));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), "val0.keyOne".Split(','), new Object[]{10});
    
            // delete for varagg[10, "A"]
            epService.EPRuntime.SendEvent(MakeSupportBean("A", 10, -1));
            epService.EPRuntime.SendEvent(new SupportBean_S0(10, "A"));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{null, null, null});
    
            epService.EPAdministrator.DestroyAllStatements();
            epService.EPAdministrator.Configuration.RemoveEventType("table_varaggMIUD__internal", false);
            epService.EPAdministrator.Configuration.RemoveEventType("table_varaggMIUD__public", false);
        }
    
        private SupportBean MakeSupportBean(string theString, int intPrimitive, long longPrimitive) {
            var bean = new SupportBean(theString, intPrimitive);
            bean.LongPrimitive = longPrimitive;
            return bean;
        }
    
        public class LocalSubBean {
            private int keyOne;
            private string keyTwo;
            private string prop;
    
            public int GetKeyOne() {
                return keyOne;
            }
    
            public void SetKeyOne(int keyOne) {
                this.keyOne = keyOne;
            }
    
            public string GetKeyTwo() {
                return keyTwo;
            }
    
            public void SetKeyTwo(string keyTwo) {
                this.keyTwo = keyTwo;
            }
    
            public string GetProp() {
                return prop;
            }
    
            public void SetProp(string prop) {
                this.prop = prop;
            }
        }
    
        public class LocalBean {
            private LocalSubBean val0;
    
            public LocalSubBean GetVal0() {
                return val0;
            }
    
            public void SetVal0(LocalSubBean val0) {
                this.val0 = val0;
            }
        }
    }
} // end of namespace
