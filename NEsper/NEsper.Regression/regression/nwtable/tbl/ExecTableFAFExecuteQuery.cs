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

// using static org.junit.Assert.assertEquals;
// using static org.junit.Assert.assertSame;

using NUnit.Framework;

namespace com.espertech.esper.regression.nwtable.tbl
{
    public class ExecTableFAFExecuteQuery : IndexBackingTableInfo, IRegressionExecution
    {
        public bool ExcludeWhenInstrumented() {
            return false;
        }

        public void Configure(Configuration configuration) {
            configuration.EngineDefaults.Logging.IsEnableQueryPlan = true;
            configuration.AddEventType("SupportBean", typeof(SupportBean).FullName);
            configuration.AddEventType("SupportBean_A", typeof(SupportBean_A).Name);
        }
    
        public void Run(EPServiceProvider epService) {
            RunAssertionFAFInsert(epService);
            RunAssertionFAFDelete(epService);
            RunAssertionFAFUpdate(epService);
            RunAssertionFAFSelect(epService);
        }
    
        private void RunAssertionFAFInsert(EPServiceProvider epService) {
            string[] propertyNames = "p0,p1".Split(',');
            EPStatement stmt = epService.EPAdministrator.CreateEPL("create table MyTableINS as (p0 string, p1 int)");
    
            string eplInsertInto = "insert into MyTableINS (p0, p1) select 'a', 1";
            EPOnDemandQueryResult resultOne = epService.EPRuntime.ExecuteQuery(eplInsertInto);
            AssertFAFInsertResult(resultOne, propertyNames, stmt);
            EPAssertionUtil.AssertPropsPerRow(stmt.GetEnumerator(), propertyNames, new Object[][]{new object[] {"a", 1}});
    
            epService.EPAdministrator.DestroyAllStatements();
        }
    
        private void RunAssertionFAFDelete(EPServiceProvider epService) {
            EPStatement stmt = epService.EPAdministrator.CreateEPL("create table MyTableDEL as (p0 string primary key, thesum sum(int))");
            epService.EPAdministrator.CreateEPL("into table MyTableDEL select theString, sum(intPrimitive) as thesum from SupportBean group by theString");
            for (int i = 0; i < 10; i++) {
                epService.EPRuntime.SendEvent(new SupportBean("G" + i, i));
            }
            Assert.AreEqual(10L, GetTableCount(stmt));
            epService.EPRuntime.ExecuteQuery("delete from MyTableDEL");
            Assert.AreEqual(0L, GetTableCount(stmt));
    
            epService.EPAdministrator.DestroyAllStatements();
        }
    
        private void RunAssertionFAFUpdate(EPServiceProvider epService) {
            string[] fields = "p0,p1".Split(',');
            epService.EPAdministrator.CreateEPL("@Name('TheTable') create table MyTableUPD as (p0 string primary key, p1 string, thesum sum(int))");
            epService.EPAdministrator.CreateEPL("into table MyTableUPD select theString, sum(intPrimitive) as thesum from SupportBean group by theString");
            epService.EPRuntime.SendEvent(new SupportBean("E1", 1));
            epService.EPRuntime.SendEvent(new SupportBean("E2", 2));
            epService.EPRuntime.ExecuteQuery("update MyTableUPD set p1 = 'ABC'");
            EPAssertionUtil.AssertPropsPerRowAnyOrder(epService.EPAdministrator.GetStatement("TheTable").GetEnumerator(), fields, new Object[][]{new object[] {"E1", "ABC"}, new object[] {"E2", "ABC"}});
            epService.EPAdministrator.DestroyAllStatements();
        }
    
        private void RunAssertionFAFSelect(EPServiceProvider epService) {
            string[] fields = "p0".Split(',');
            epService.EPAdministrator.CreateEPL("@Name('TheTable') create table MyTableSEL as (p0 string primary key, thesum sum(int))");
            epService.EPAdministrator.CreateEPL("into table MyTableSEL select theString, sum(intPrimitive) as thesum from SupportBean group by theString");
            epService.EPRuntime.SendEvent(new SupportBean("E1", 1));
            epService.EPRuntime.SendEvent(new SupportBean("E2", 2));
            EPOnDemandQueryResult result = epService.EPRuntime.ExecuteQuery("select * from MyTableSEL");
            EPAssertionUtil.AssertPropsPerRowAnyOrder(result.Array, fields, new Object[][]{new object[] {"E1"}, new object[] {"E2"}});
            epService.EPAdministrator.DestroyAllStatements();
        }
    
        private long GetTableCount(EPStatement stmt) {
            return EPAssertionUtil.EnumeratorCount(stmt.GetEnumerator());
        }
    
        private void AssertFAFInsertResult(EPOnDemandQueryResult resultOne, string[] propertyNames, EPStatement stmt) {
            Assert.AreEqual(0, resultOne.Array.Length);
            Assert.AreSame(resultOne.EventType, stmt.EventType);
        }
    }
} // end of namespace
