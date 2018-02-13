///////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) 2006-2017 Esper Team. All rights reserved.                           /
// http://esper.codehaus.org                                                          /
// ---------------------------------------------------------------------------------- /
// The software in this package is published under the terms of the GPL license       /
// a copy of which has been included with this distribution in the license.txt file.  /
///////////////////////////////////////////////////////////////////////////////////////

using com.espertech.esper.client;
using com.espertech.esper.client.annotation;
using com.espertech.esper.client.deploy;
using com.espertech.esper.client.scopetest;
using com.espertech.esper.client.time;
using com.espertech.esper.collection;
using com.espertech.esper.compat.collections;
using com.espertech.esper.epl.agg.util;
using com.espertech.esper.supportregression.bean;
using com.espertech.esper.supportregression.client;
using com.espertech.esper.supportregression.epl;
using com.espertech.esper.supportregression.execution;
using com.espertech.esper.supportregression.util;

// using static org.junit.Assert.*;

using NUnit.Framework;

namespace com.espertech.esper.regression.resultset.aggregate
{
    public class ExecAggregateLocalGroupBy : RegressionExecution {
        public static readonly string PLAN_CALLBACK_HOOK = "@Hook(type=" + typeof(HookType).FullName + ".INTERNAL_AGGLOCALLEVEL,hook='" + typeof(SupportAggLevelPlanHook).Name + "')";
    
        public override void Run(EPServiceProvider epService) {
            foreach (var clazz in new[]{typeof(SupportBean), typeof(SupportBean_S0), typeof(SupportBean_S1)}) {
                epService.EPAdministrator.Configuration.AddEventType(clazz);
            }
            SupportAggLevelPlanHook.GetAndReset();
    
            RunAssertionInvalid(epService);
            RunAssertionUngroupedAndLocalSyntax(epService);
            RunAssertionGrouped(epService);
            RunAssertionPlanning(epService);
            RunAssertionFullyVersusNotFullyAgg(epService);
        }
    
        private void RunAssertionInvalid(EPServiceProvider epService) {
    
            // not valid with count-min-sketch
            SupportMessageAssertUtil.TryInvalid(epService, "create table MyTable(approx CountMinSketch(group_by:theString) @Type(SupportBean))",
                    "Error starting statement: Failed to validate table-column expression 'CountMinSketch(group_by:theString)': Count-min-sketch aggregation function 'countMinSketch'  expects either no parameter or a single json parameter object");
    
            // not allowed with tables
            SupportMessageAssertUtil.TryInvalid(epService, "create table MyTable(col sum(int, group_by:theString) @Type(SupportBean))",
                    "Error starting statement: Failed to validate table-column expression 'sum(int,group_by:theString)': The 'group_by' and 'filter' parameter is not allowed in create-table statements");
    
            // invalid named parameter
            SupportMessageAssertUtil.TryInvalid(epService, "select sum(intPrimitive, xxx:theString) from SupportBean",
                    "Error starting statement: Failed to validate select-clause expression 'sum(intPrimitive,xxx:theString)': Invalid named parameter 'xxx' (did you mean 'group_by' or 'filter'?) [");
    
            // invalid group-by expression
            SupportMessageAssertUtil.TryInvalid(epService, "select sum(intPrimitive, group_by:sum(intPrimitive)) from SupportBean",
                    "Error starting statement: Failed to validate select-clause expression 'sum(intPrimitive,group_by:sum(intPr...(44 chars)': Group-by expressions cannot contain aggregate functions");
    
            // other functions don't accept this named parameter
            SupportMessageAssertUtil.TryInvalid(epService, "select coalesce(0, 1, group_by:theString) from SupportBean",
                    "Incorrect syntax near ':' at line 1 column 30");
            SupportMessageAssertUtil.TryInvalid(epService, "select " + typeof(SupportStaticMethodLib).FullName + ".StaticMethod(group_by:intPrimitive) from SupportBean",
                    "Error starting statement: Failed to validate select-clause expression 'com.espertech.esper.supportregressi...(100 chars)': Named parameters are not allowed");
    
            // not allowed in combination with roll-up
            SupportMessageAssertUtil.TryInvalid(epService, "select sum(intPrimitive, group_by:theString) from SupportBean group by Rollup(theString)",
                    "Error starting statement: Roll-up and group-by parameters cannot be combined ");
    
            // not allowed in combination with into-table
            epService.EPAdministrator.CreateEPL("create table mytable (thesum sum(int))");
            SupportMessageAssertUtil.TryInvalid(epService, "into table mytable select sum(intPrimitive, group_by:theString) as thesum from SupportBean",
                    "Error starting statement: Into-table and group-by parameters cannot be combined");
    
            // not allowed for match-rezognize measure clauses
            string eplMatchRecog = "select * from SupportBean match_recognize (" +
                    "  measures count(B.intPrimitive, group_by:B.theString) pattern (A B* C))";
            SupportMessageAssertUtil.TryInvalid(epService, eplMatchRecog,
                    "Error starting statement: Match-recognize does not allow aggregation functions to specify a group-by");
    
            // disallow subqueries to specify their own local group-by
            string eplSubq = "select (select sum(intPrimitive, group_by:theString) from SupportBean#keepall) from SupportBean_S0";
            SupportMessageAssertUtil.TryInvalid(epService, eplSubq,
                    "Error starting statement: Failed to plan subquery number 1 querying SupportBean: Subselect aggregations functions cannot specify a group-by");
        }
    
        private void RunAssertionUngroupedAndLocalSyntax(EPServiceProvider epService) {
            TryAssertionUngroupedAggSQLStandard(epService);
            TryAssertionUngroupedAggEvent(epService);
            TryAssertionUngroupedAggAdditionalAndPlugin(epService);
            TryAssertionUngroupedAggIterator(epService);
            TryAssertionUngroupedParenSODA(epService, false);
            TryAssertionUngroupedParenSODA(epService, true);
            TryAssertionColNameRendering(epService);
            TryAssertionUngroupedSameKey(epService);
            TryAssertionUngroupedRowRemove(epService);
            TryAssertionUngroupedHaving(epService);
            TryAssertionUngroupedOrderBy(epService);
            TryAssertionUngroupedUnidirectionalJoin(epService);
            TryAssertionEnumMethods(epService, true);
        }
    
        private void RunAssertionGrouped(EPServiceProvider epService) {
            TryAssertionGroupedSolutionPattern(epService);
            TryAssertionGroupedMultiLevelMethod(epService);
            TryAssertionGroupedMultiLevelAccess(epService);
            TryAssertionGroupedMultiLevelNoDefaultLvl(epService);
            TryAssertionGroupedSameKey(epService);
            TryAssertionGroupedRowRemove(epService);
            TryAssertionGroupedOnSelect(epService);
            TryAssertionEnumMethods(epService, false);
        }
    
        private void RunAssertionPlanning(EPServiceProvider epService) {
            AssertNoPlan(epService, "select sum(group_by:(),intPrimitive) as c0 from SupportBean");
            AssertNoPlan(epService, "select sum(group_by:(theString),intPrimitive) as c0 from SupportBean group by theString");
            AssertNoPlan(epService, "select sum(group_by:(theString, intPrimitive),longPrimitive) as c0 from SupportBean group by theString, intPrimitive");
            AssertNoPlan(epService, "select sum(group_by:(intPrimitive, theString),longPrimitive) as c0 from SupportBean group by theString, intPrimitive");
    
            // provide column count stays at 1
            AssertCountColsAndLevels(epService, "select sum(group_by:(theString),intPrimitive) as c0, sum(group_by:(theString),intPrimitive) as c1 from SupportBean",
                    1, 1);
    
            // prove order of group-by expressions does not matter
            AssertCountColsAndLevels(epService, "select sum(group_by:(intPrimitive, theString),longPrimitive) as c0, sum(longPrimitive, group_by:(theString, intPrimitive)) as c1 from SupportBean",
                    1, 1);
    
            // prove the number of levels stays the same even when group-by expressions vary
            AssertCountColsAndLevels(epService, "select sum(group_by:(intPrimitive, theString),longPrimitive) as c0, count(*, group_by:(theString, intPrimitive)) as c1 from SupportBean",
                    2, 1);
    
            // prove there is one shared state factory
            string theEpl = PLAN_CALLBACK_HOOK + "select window(*, group_by:theString), last(*, group_by:theString) from SupportBean#length(2)";
            epService.EPAdministrator.CreateEPL(theEpl);
            Pair<AggregationGroupByLocalGroupDesc, AggregationLocalGroupByPlan> plan = SupportAggLevelPlanHook.GetAndReset();
            Assert.AreEqual(1, plan.Second.AllLevels.Length);
            Assert.AreEqual(1, plan.Second.AllLevels[0].StateFactories.Length);
        }
    
        private void RunAssertionFullyVersusNotFullyAgg(EPServiceProvider epService) {
            string[] colsC0 = "c0".Split(',');
    
            // full-aggregated and un-grouped (row for all)
            TryAssertionAggAndFullyAgg(
                epService, "select sum(group_by:(),intPrimitive) as c0 from SupportBean",
                listener => EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), colsC0, new object[] {60}));
    
            // aggregated and un-grouped (row for event)
            TryAssertionAggAndFullyAgg(epService, "select sum(group_by:theString, intPrimitive) as c0 from SupportBean#keepall",
                listener => EPAssertionUtil.AssertPropsPerRowAnyOrder(listener.GetAndResetLastNewData(), colsC0, new[] {new object[] {10}, new object[] {50}, new object[] {50}}));
    
            // fully aggregated and grouped (row for group)
            TryAssertionAggAndFullyAgg(epService, "select sum(intPrimitive, group_by:()) as c0, sum(group_by:theString, intPrimitive) as c1, theString " +
                            "from SupportBean group by theString",
                listener => EPAssertionUtil.AssertPropsPerRowAnyOrder(listener.GetAndResetLastNewData(), "theString,c0,c1".Split(','), new[] {new object[] {"E1", 60, 10}, new object[] {"E2", 60, 50}}));
    
            // aggregated and grouped (row for event)
            TryAssertionAggAndFullyAgg(epService, "select sum(longPrimitive, group_by:()) as c0," +
                            " sum(longPrimitive, group_by:theString) as c1, " +
                            " sum(longPrimitive, group_by:intPrimitive) as c2, " +
                            " theString " +
                            "from SupportBean#keepall group by theString",
                listener => EPAssertionUtil.AssertPropsPerRowAnyOrder(listener.GetAndResetLastNewData(),
                                    "theString,c0,c1,c2".Split(','), new[] {new object[] {"E1", 600L, 100L, 100L}, new object[] {"E2", 600L, 500L, 200L}, new object[] {"E2", 600L, 500L, 300L}}));
        }
    
        private void TryAssertionUngroupedRowRemove(EPServiceProvider epService) {
            string[] cols = "theString,intPrimitive,c0,c1".Split(',');
            string epl = "create window MyWindow#keepall as SupportBean;\n" +
                    "insert into MyWindow select * from SupportBean;\n" +
                    "on SupportBean_S0 delete from MyWindow where p00 = theString and id = intPrimitive;\n" +
                    "on SupportBean_S1 delete from MyWindow;\n" +
                    "@Name('out') select theString, intPrimitive, sum(longPrimitive) as c0, " +
                    "  sum(longPrimitive, group_by:theString) as c1 from MyWindow;\n";
            DeploymentResult result = epService.EPAdministrator.DeploymentAdmin.ParseDeploy(epl);
            var listener = new SupportUpdateListener();
            epService.EPAdministrator.GetStatement("out").AddListener(listener);
    
            MakeSendEvent(epService, "E1", 10, 101);
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), cols, new object[]{"E1", 10, 101L, 101L});
    
            epService.EPRuntime.SendEvent(new SupportBean_S0(10, "E1")); // delete event {"E1", 10}
            Assert.IsFalse(listener.IsInvoked);
    
            MakeSendEvent(epService, "E1", 20, 102);
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), cols, new object[]{"E1", 20, 102L, 102L});
    
            MakeSendEvent(epService, "E2", 30, 103);
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), cols, new object[]{"E2", 30, 102 + 103L, 103L});
    
            MakeSendEvent(epService, "E1", 40, 104);
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), cols, new object[]{"E1", 40, 102 + 103 + 104L, 102 + 104L});
    
            epService.EPRuntime.SendEvent(new SupportBean_S0(40, "E1")); // delete event {"E1", 40}
            Assert.IsFalse(listener.IsInvoked);
    
            MakeSendEvent(epService, "E1", 50, 105);
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), cols, new object[]{"E1", 50, 102 + 103 + 105L, 102 + 105L});
    
            epService.EPRuntime.SendEvent(new SupportBean_S1(-1)); // delete all
            Assert.IsFalse(listener.IsInvoked);
    
            MakeSendEvent(epService, "E1", 60, 106);
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), cols, new object[]{"E1", 60, 106L, 106L});
    
            epService.EPAdministrator.DeploymentAdmin.Undeploy(result.DeploymentId);
        }
    
        private void TryAssertionGroupedRowRemove(EPServiceProvider epService) {
            string[] cols = "theString,intPrimitive,c0,c1".Split(',');
            string epl = "create window MyWindow#keepall as SupportBean;\n" +
                    "insert into MyWindow select * from SupportBean;\n" +
                    "on SupportBean_S0 delete from MyWindow where p00 = theString and id = intPrimitive;\n" +
                    "on SupportBean_S1 delete from MyWindow;\n" +
                    "@Name('out') select theString, intPrimitive, sum(longPrimitive) as c0, " +
                    "  sum(longPrimitive, group_by:theString) as c1 " +
                    "  from MyWindow group by theString, intPrimitive;\n";
            DeploymentResult result = epService.EPAdministrator.DeploymentAdmin.ParseDeploy(epl);
            var listener = new SupportUpdateListener();
            epService.EPAdministrator.GetStatement("out").AddListener(listener);
    
            MakeSendEvent(epService, "E1", 10, 101);
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), cols, new object[]{"E1", 10, 101L, 101L});
    
            epService.EPRuntime.SendEvent(new SupportBean_S0(10, "E1")); // delete event {"E1", 10}
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), cols, new object[]{"E1", 10, null, null});
    
            MakeSendEvent(epService, "E1", 20, 102);
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), cols, new object[]{"E1", 20, 102L, 102L});
    
            MakeSendEvent(epService, "E2", 30, 103);
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), cols, new object[]{"E2", 30, 103L, 103L});
    
            MakeSendEvent(epService, "E1", 40, 104);
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), cols, new object[]{"E1", 40, 104L, 102 + 104L});
    
            epService.EPRuntime.SendEvent(new SupportBean_S0(40, "E1")); // delete event {"E1", 40}
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), cols, new object[]{"E1", 40, null, 102L});
    
            MakeSendEvent(epService, "E1", 50, 105);
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), cols, new object[]{"E1", 50, 105L, 102 + 105L});
    
            epService.EPRuntime.SendEvent(new SupportBean_S1(-1)); // delete all
            listener.Reset();
    
            MakeSendEvent(epService, "E1", 60, 106);
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), cols, new object[]{"E1", 60, 106L, 106L});
    
            epService.EPAdministrator.DeploymentAdmin.Undeploy(result.DeploymentId);
        }
    
        private void TryAssertionGroupedMultiLevelMethod(EPServiceProvider epService) {
            SendTime(epService, 0);
            string[] fields = "theString,intPrimitive,c0,c1,c2,c3,c4".Split(',');
            string epl = "select" +
                    "   theString, intPrimitive," +
                    "   sum(longPrimitive, group_by:(intPrimitive, theString)) as c0," +
                    "   sum(longPrimitive) as c1," +
                    "   sum(longPrimitive, group_by:(theString)) as c2," +
                    "   sum(longPrimitive, group_by:(intPrimitive)) as c3," +
                    "   sum(longPrimitive, group_by:()) as c4" +
                    " from SupportBean" +
                    " group by theString, intPrimitive" +
                    " output snapshot every 10 seconds";
            var listener = new SupportUpdateListener();
            epService.EPAdministrator.CreateEPL(epl).AddListener(listener);
    
            MakeSendEvent(epService, "E1", 10, 100);
            MakeSendEvent(epService, "E1", 20, 202);
            MakeSendEvent(epService, "E2", 10, 303);
            MakeSendEvent(epService, "E1", 10, 404);
            MakeSendEvent(epService, "E2", 10, 505);
            SendTime(epService, 10000);
    
            EPAssertionUtil.AssertPropsPerRowAnyOrder(listener.GetAndResetLastNewData(), fields, new[]
            {
                    new object[]{"E1", 10, 504L, 504L, 706L, 1312L, 1514L}, new object[]{"E1", 20, 202L, 202L, 706L, 202L, 1514L}, new object[]{"E2", 10, 808L, 808L, 808L, 1312L, 1514L}});
    
            MakeSendEvent(epService, "E1", 10, 1);
            SendTime(epService, 20000);
    
            EPAssertionUtil.AssertPropsPerRowAnyOrder(listener.GetAndResetLastNewData(), fields, new[]
            {
                new object[]{"E1", 10, 505L, 505L, 707L, 1313L, 1515L}, new object[]{"E1", 20, 202L, 202L, 707L, 202L, 1515L}, new object[]{"E2", 10, 808L, 808L, 808L, 1313L, 1515L}});
    
            epService.EPAdministrator.DestroyAllStatements();
        }
    
        private void TryAssertionGroupedMultiLevelAccess(EPServiceProvider epService) {
            SendTime(epService, 0);
            string[] fields = "theString,intPrimitive,c0,c1,c2,c3,c4".Split(',');
            string epl = "select" +
                    "   theString, intPrimitive," +
                    "   window(*, group_by:(intPrimitive, theString)) as c0," +
                    "   window(*) as c1," +
                    "   window(*, group_by:theString) as c2," +
                    "   window(*, group_by:intPrimitive) as c3," +
                    "   window(*, group_by:()) as c4" +
                    " from SupportBean#keepall" +
                    " group by theString, intPrimitive" +
                    " output snapshot every 10 seconds" +
                    " order by theString, intPrimitive";
            var listener = new SupportUpdateListener();
            epService.EPAdministrator.CreateEPL(epl).AddListener(listener);
    
            SupportBean b1 = MakeSendEvent(epService, "E1", 10, 100);
            SupportBean b2 = MakeSendEvent(epService, "E1", 20, 202);
            SupportBean b3 = MakeSendEvent(epService, "E2", 10, 303);
            SupportBean b4 = MakeSendEvent(epService, "E1", 10, 404);
            SupportBean b5 = MakeSendEvent(epService, "E2", 10, 505);
            SendTime(epService, 10000);
    
            var all = new object[]{b1, b2, b3, b4, b5};
            EPAssertionUtil.AssertProps(listener.LastNewData[0], fields,
                    new object[]{"E1", 10, new object[]{b1, b4}, new object[]{b1, b4}, new object[]{b1, b2, b4},
                            new object[]{b1, b3, b4, b5}, all});
            EPAssertionUtil.AssertProps(listener.LastNewData[1], fields,
                    new object[]{"E1", 20, new object[]{b2}, new object[]{b2}, new object[]{b1, b2, b4},
                            new object[]{b2}, all});
            EPAssertionUtil.AssertProps(listener.LastNewData[2], fields,
                    new object[]{"E2", 10, new object[]{b3, b5}, new object[]{b3, b5}, new object[]{b3, b5},
                            new object[]{b1, b3, b4, b5}, all});
    
            epService.EPAdministrator.DestroyAllStatements();
        }
    
        private void TryAssertionGroupedMultiLevelNoDefaultLvl(EPServiceProvider epService) {
            SendTime(epService, 0);
            string[] fields = "theString,intPrimitive,c0,c1,c2".Split(',');
            string epl = "select" +
                    "   theString, intPrimitive," +
                    "   sum(longPrimitive, group_by:(theString)) as c0," +
                    "   sum(longPrimitive, group_by:(intPrimitive)) as c1," +
                    "   sum(longPrimitive, group_by:()) as c2" +
                    " from SupportBean" +
                    " group by theString, intPrimitive" +
                    " output snapshot every 10 seconds";
            var listener = new SupportUpdateListener();
            epService.EPAdministrator.CreateEPL(epl).AddListener(listener);
    
            MakeSendEvent(epService, "E1", 10, 100);
            MakeSendEvent(epService, "E1", 20, 202);
            MakeSendEvent(epService, "E2", 10, 303);
            MakeSendEvent(epService, "E1", 10, 404);
            MakeSendEvent(epService, "E2", 10, 505);
            SendTime(epService, 10000);
    
            EPAssertionUtil.AssertPropsPerRowAnyOrder(listener.GetAndResetLastNewData(), fields, new[]
            {
                new object[]{"E1", 10, 706L, 1312L, 1514L}, new object[]{"E1", 20, 706L, 202L, 1514L}, new object[]{"E2", 10, 808L, 1312L, 1514L}});
    
            MakeSendEvent(epService, "E1", 10, 1);
            SendTime(epService, 20000);
    
            EPAssertionUtil.AssertPropsPerRowAnyOrder(listener.GetAndResetLastNewData(), fields, new[]
            {
                new object[]{"E1", 10, 707L, 1313L, 1515L}, new object[]{"E1", 20, 707L, 202L, 1515L}, new object[]{"E2", 10, 808L, 1313L, 1515L}});
    
            epService.EPAdministrator.DestroyAllStatements();
        }
    
        private void TryAssertionGroupedSolutionPattern(EPServiceProvider epService) {
            SendTime(epService, 0);
            string[] fields = "theString,pct".Split(',');
            string epl = "select theString, count(*) / count(*, group_by:()) as pct" +
                    " from SupportBean#Time(30 sec)" +
                    " group by theString" +
                    " output snapshot every 10 seconds";
            var listener = new SupportUpdateListener();
            epService.EPAdministrator.CreateEPL(epl).AddListener(listener);
    
            SendEventMany(epService, "A", "B", "C", "B", "B", "C");
            SendTime(epService, 10000);
    
            EPAssertionUtil.AssertPropsPerRowAnyOrder(listener.GetAndResetLastNewData(), fields, new[]
            {
                new object[]{"A", 1 / 6d}, new object[]{"B", 3 / 6d}, new object[]{"C", 2 / 6d}});
    
            SendEventMany(epService, "A", "B", "B", "B", "B", "A");
            SendTime(epService, 20000);
    
            EPAssertionUtil.AssertPropsPerRowAnyOrder(listener.GetAndResetLastNewData(), fields, new[]
            {
                new object[]{"A", 3 / 12d}, new object[]{"B", 7 / 12d}, new object[]{"C", 2 / 12d}});
    
            SendEventMany(epService, "C", "A", "A", "A", "B", "A");
            SendTime(epService, 30000);
    
            EPAssertionUtil.AssertPropsPerRowAnyOrder(listener.GetAndResetLastNewData(), fields, new[]
            {
                new object[]{"A", 6 / 12d}, new object[]{"B", 5 / 12d}, new object[]{"C", 1 / 12d}});
    
            epService.EPAdministrator.DestroyAllStatements();
        }
    
        private void TryAssertionAggAndFullyAgg(EPServiceProvider epService, string selected, MyAssertion assertion) {
            string epl = "create context StartS0EndS1 start SupportBean_S0 end SupportBean_S1;" +
                    "@Name('out') context StartS0EndS1 " +
                    selected +
                    " output snapshot when terminated;";
            DeploymentResult deployed = epService.EPAdministrator.DeploymentAdmin.ParseDeploy(epl);
            var listener = new SupportUpdateListener();
            epService.EPAdministrator.GetStatement("out").AddListener(listener);
    
            epService.EPRuntime.SendEvent(new SupportBean_S0(0));
            MakeSendEvent(epService, "E1", 10, 100);
            MakeSendEvent(epService, "E2", 20, 200);
            MakeSendEvent(epService, "E2", 30, 300);
            epService.EPRuntime.SendEvent(new SupportBean_S1(0));
    
            assertion.Invoke(listener);
    
            // try an empty batch
            epService.EPRuntime.SendEvent(new SupportBean_S0(1));
            epService.EPRuntime.SendEvent(new SupportBean_S1(1));
    
            epService.EPAdministrator.DeploymentAdmin.Undeploy(deployed.DeploymentId);
        }
    
        private void TryAssertionUngroupedParenSODA(EPServiceProvider epService, bool soda) {
            string[] cols = "c0,c1,c2,c3,c4".Split(',');
            string epl = "select longPrimitive, " +
                    "sum(longPrimitive) as c0, " +
                    "sum(group_by:(),longPrimitive) as c1, " +
                    "sum(longPrimitive,group_by:()) as c2, " +
                    "sum(longPrimitive,group_by:theString) as c3, " +
                    "sum(longPrimitive,group_by:(theString,intPrimitive)) as c4" +
                    " from SupportBean";
            var listener = new SupportUpdateListener();
            SupportModelHelper.CreateByCompileOrParse(epService, soda, epl).AddListener(listener);
    
            MakeSendEvent(epService, "E1", 1, 10);
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), cols, new object[]{10L, 10L, 10L, 10L, 10L});
    
            MakeSendEvent(epService, "E1", 2, 11);
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), cols, new object[]{21L, 21L, 21L, 21L, 11L});
    
            MakeSendEvent(epService, "E2", 1, 12);
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), cols, new object[]{33L, 33L, 33L, 12L, 12L});
    
            MakeSendEvent(epService, "E2", 2, 13);
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), cols, new object[]{46L, 46L, 46L, 25L, 13L});
    
            epService.EPAdministrator.DestroyAllStatements();
        }
    
        private void TryAssertionUngroupedAggAdditionalAndPlugin(EPServiceProvider epService) {
    
            epService.EPAdministrator.Configuration.AddPlugInAggregationFunctionFactory("concatstring", typeof(MyConcatAggregationFunctionFactory).Name);
            var mfAggConfig = new ConfigurationPlugInAggregationMultiFunction(SupportAggMFFuncExtensions.GetFunctionNames(), typeof(SupportAggMFFactory).Name);
            epService.EPAdministrator.Configuration.AddPlugInAggregationMultiFunction(mfAggConfig);
    
            string[] cols = "c0,c1,c2,c3,c4,c5,c8,c9,c10,c11,c12,c13".Split(',');
            string epl = "select intPrimitive, " +
                    " countever(*, intPrimitive>0, group_by:(theString)) as c0," +
                    " countever(*, intPrimitive>0, group_by:()) as c1," +
                    " countever(*, group_by:(theString)) as c2," +
                    " countever(*, group_by:()) as c3," +
                    " Concatstring(Convert.ToString(intPrimitive), group_by:(theString)) as c4," +
                    " Concatstring(Convert.ToString(intPrimitive), group_by:()) as c5," +
                    " Sc(intPrimitive, group_by:(theString)) as c6," +
                    " Sc(intPrimitive, group_by:()) as c7," +
                    " leaving(group_by:(theString)) as c8," +
                    " leaving(group_by:()) as c9," +
                    " rate(3, group_by:(theString)) as c10," +
                    " rate(3, group_by:()) as c11," +
                    " Nth(intPrimitive, 1, group_by:(theString)) as c12," +
                    " Nth(intPrimitive, 1, group_by:()) as c13" +
                    " from SupportBean as sb";
            var listener = new SupportUpdateListener();
            epService.EPAdministrator.CreateEPL(epl).AddListener(listener);
    
            MakeSendEvent(epService, "E1", 10);
            AssertScalarColl(listener.LastNewData[0], new[]{10}, new[]{10});
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), cols, new object[]{1L, 1L, 1L, 1L, "10", "10", false, false,
                    null, null, null, null});
    
            MakeSendEvent(epService, "E2", 20);
            AssertScalarColl(listener.LastNewData[0], new[]{20}, new[]{10, 20});
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), cols, new object[]{1L, 2L, 1L, 2L, "20", "10 20", false, false,
                    null, null, null, 10});
    
            MakeSendEvent(epService, "E1", -1);
            AssertScalarColl(listener.LastNewData[0], new[]{10, -1}, new[]{10, 20, -1});
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), cols, new object[]{1L, 2L, 2L, 3L, "10 -1", "10 20 -1", false, false,
                    null, null, 10, 20});
    
            MakeSendEvent(epService, "E2", 30);
            AssertScalarColl(listener.LastNewData[0], new[]{20, 30}, new[]{10, 20, -1, 30});
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), cols, new object[]{2L, 3L, 2L, 4L, "20 30", "10 20 -1 30", false, false,
                    null, null, 20, -1});
    
            // plug-in aggregation function can also take other parameters
            epService.EPAdministrator.CreateEPL("select Sc(intPrimitive, dummy:1)," +
                    "Concatstring(Convert.ToString(intPrimitive), dummy2:(1,2,3)) from SupportBean");
    
            epService.EPAdministrator.DestroyAllStatements();
        }
    
        private void TryAssertionUngroupedAggEvent(EPServiceProvider epService) {
            string[] cols = "first0,first1,last0,last1,window0,window1,maxby0,maxby1,minby0,minby1,sorted0,sorted1,maxbyever0,maxbyever1,minbyever0,minbyever1,firstever0,firstever1,lastever0,lastever1".Split(',');
            string epl = "select intPrimitive as c0, " +
                    " First(sb, group_by:(theString)) as first0," +
                    " First(sb, group_by:()) as first1," +
                    " last(sb, group_by:(theString)) as last0," +
                    " last(sb, group_by:()) as last1," +
                    " window(sb, group_by:(theString)) as window0," +
                    " window(sb, group_by:()) as window1," +
                    " maxby(intPrimitive, group_by:(theString)) as maxby0," +
                    " maxby(intPrimitive, group_by:()) as maxby1," +
                    " minby(intPrimitive, group_by:(theString)) as minby0," +
                    " minby(intPrimitive, group_by:()) as minby1," +
                    " sorted(intPrimitive, group_by:(theString)) as sorted0," +
                    " sorted(intPrimitive, group_by:()) as sorted1," +
                    " maxbyever(intPrimitive, group_by:(theString)) as maxbyever0," +
                    " maxbyever(intPrimitive, group_by:()) as maxbyever1," +
                    " minbyever(intPrimitive, group_by:(theString)) as minbyever0," +
                    " minbyever(intPrimitive, group_by:()) as minbyever1," +
                    " firstever(sb, group_by:(theString)) as firstever0," +
                    " firstever(sb, group_by:()) as firstever1," +
                    " lastever(sb, group_by:(theString)) as lastever0," +
                    " lastever(sb, group_by:()) as lastever1" +
                    " from SupportBean#length(3) as sb";
            var listener = new SupportUpdateListener();
            epService.EPAdministrator.CreateEPL(epl).AddListener(listener);
    
            SupportBean b1 = MakeSendEvent(epService, "E1", 10);
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), cols, new object[]{b1, b1, b1, b1, new object[]{b1}, new object[]{b1},
                    b1, b1, b1, b1, new object[]{b1}, new object[]{b1}, b1, b1, b1, b1,
                    b1, b1, b1, b1});
    
            SupportBean b2 = MakeSendEvent(epService, "E2", 20);
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), cols, new object[]{b2, b1, b2, b2, new object[]{b2}, new object[]{b1, b2},
                    b2, b2, b2, b1, new object[]{b2}, new object[]{b1, b2}, b2, b2, b2, b1,
                    b2, b1, b2, b2});
    
            SupportBean b3 = MakeSendEvent(epService, "E1", 15);
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), cols, new object[]{b1, b1, b3, b3, new object[]{b1, b3}, new object[]{b1, b2, b3},
                    b3, b2, b1, b1, new object[]{b1, b3}, new object[]{b1, b3, b2}, b3, b2, b1, b1,
                    b1, b1, b3, b3});
    
            SupportBean b4 = MakeSendEvent(epService, "E3", 16);
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), cols, new object[]{b4, b2, b4, b4, new object[]{b4}, new object[]{b2, b3, b4},
                    b4, b2, b4, b3, new object[]{b4}, new object[]{b3, b4, b2}, b4, b2, b4, b1,
                    b4, b1, b4, b4});
    
            epService.EPAdministrator.DestroyAllStatements();
        }
    
        private void TryAssertionUngroupedAggSQLStandard(EPServiceProvider epService) {
            string[] fields = "c0,sum0,sum1,avedev0,avg0,max0,fmax0,min0,fmin0,maxever0,fmaxever0,minever0,fminever0,median0,stddev0".Split(',');
            string epl = "select intPrimitive as c0, " +
                    "sum(intPrimitive, group_by:()) as sum0, " +
                    "sum(intPrimitive, group_by:(theString)) as sum1," +
                    "avedev(intPrimitive, group_by:(theString)) as avedev0," +
                    "avg(intPrimitive, group_by:(theString)) as avg0," +
                    "max(intPrimitive, group_by:(theString)) as max0," +
                    "fmax(intPrimitive, intPrimitive>0, group_by:(theString)) as fmax0," +
                    "min(intPrimitive, group_by:(theString)) as min0," +
                    "fmin(intPrimitive, intPrimitive>0, group_by:(theString)) as fmin0," +
                    "maxever(intPrimitive, group_by:(theString)) as maxever0," +
                    "fmaxever(intPrimitive, intPrimitive>0, group_by:(theString)) as fmaxever0," +
                    "minever(intPrimitive, group_by:(theString)) as minever0," +
                    "fminever(intPrimitive, intPrimitive>0, group_by:(theString)) as fminever0," +
                    "median(intPrimitive, group_by:(theString)) as median0," +
                    "Math.Round(coalesce(stddev(intPrimitive, group_by:(theString)), 0)) as stddev0" +
                    " from SupportBean#keepall";
            EPStatement stmt = epService.EPAdministrator.CreateEPL(epl);
            var listener = new SupportUpdateListener();
            stmt.AddListener(listener);
    
            epService.EPRuntime.SendEvent(new SupportBean("E1", 10));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new object[]{10, 10, 10,
                    0.0d, 10d, 10, 10, 10, 10, 10, 10, 10, 10, 10.0, 0L});
    
            epService.EPRuntime.SendEvent(new SupportBean("E2", 20));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new object[]{20, 10 + 20, 20,
                    0.0d, 20d, 20, 20, 20, 20, 20, 20, 20, 20, 20.0, 0L});
    
            epService.EPRuntime.SendEvent(new SupportBean("E1", 30));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new object[]{30, 10 + 20 + 30, 10 + 30,
                    10.0d, 20d, 30, 30, 10, 10, 30, 30, 10, 10, 20.0, 14L});
    
            epService.EPRuntime.SendEvent(new SupportBean("E2", 40));
            var expected = new object[]{40, 10 + 20 + 30 + 40, 20 + 40,
                    10.0d, 30d, 40, 40, 20, 20, 40, 40, 20, 20, 30.0, 14L};
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, expected);
    
            epService.EPAdministrator.DestroyAllStatements();
        }
    
        private void TryAssertionUngroupedSameKey(EPServiceProvider epService) {
            epService.EPAdministrator.CreateEPL("create objectarray schema MyEventOne (d1 string, d2 string, val int)");
            string epl = "select sum(val, group_by: d1) as c0, sum(val, group_by: d2) as c1 from MyEventOne";
            var listener = new SupportUpdateListener();
            epService.EPAdministrator.CreateEPL(epl).AddListener(listener);
            string[] cols = "c0,c1".Split(',');
    
            epService.EPRuntime.SendEvent(new object[]{"E1", "E1", 10}, "MyEventOne");
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), cols, new object[]{10, 10});
    
            epService.EPRuntime.SendEvent(new object[]{"E1", "E2", 11}, "MyEventOne");
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), cols, new object[]{21, 11});
    
            epService.EPRuntime.SendEvent(new object[]{"E2", "E1", 12}, "MyEventOne");
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), cols, new object[]{12, 22});
    
            epService.EPRuntime.SendEvent(new object[]{"E3", "E1", 13}, "MyEventOne");
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), cols, new object[]{13, 35});
    
            epService.EPRuntime.SendEvent(new object[]{"E3", "E3", 14}, "MyEventOne");
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), cols, new object[]{27, 14});
    
            epService.EPAdministrator.DestroyAllStatements();
        }
    
        private void TryAssertionGroupedSameKey(EPServiceProvider epService) {
            epService.EPAdministrator.CreateEPL("create objectarray schema MyEventTwo (g1 string, d1 string, d2 string, val int)");
            string epl = "select sum(val) as c0, sum(val, group_by: d1) as c1, sum(val, group_by: d2) as c2 from MyEventTwo group by g1";
            var listener = new SupportUpdateListener();
            epService.EPAdministrator.CreateEPL(epl).AddListener(listener);
            string[] cols = "c0,c1,c2".Split(',');
    
            epService.EPRuntime.SendEvent(new object[]{"E1", "E1", "E1", 10}, "MyEventTwo");
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), cols, new object[]{10, 10, 10});
    
            epService.EPRuntime.SendEvent(new object[]{"E1", "E1", "E2", 11}, "MyEventTwo");
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), cols, new object[]{21, 21, 11});
    
            epService.EPRuntime.SendEvent(new object[]{"E1", "E2", "E1", 12}, "MyEventTwo");
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), cols, new object[]{33, 12, 22});
    
            epService.EPRuntime.SendEvent(new object[]{"X", "E1", "E1", 13}, "MyEventTwo");
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), cols, new object[]{13, 10 + 11 + 13, 10 + 12 + 13});
    
            epService.EPRuntime.SendEvent(new object[]{"E1", "E2", "E3", 14}, "MyEventTwo");
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), cols, new object[]{10 + 11 + 12 + 14, 12 + 14, 14});
    
            epService.EPAdministrator.DestroyAllStatements();
        }
    
        private void TryAssertionUngroupedAggIterator(EPServiceProvider epService) {
            string[] fields = "c0,sum0,sum1".Split(',');
            string epl = "select intPrimitive as c0, " +
                    "sum(intPrimitive, group_by:()) as sum0, " +
                    "sum(intPrimitive, group_by:(theString)) as sum1 " +
                    " from SupportBean#keepall";
            EPStatement stmt = epService.EPAdministrator.CreateEPL(epl);
    
            epService.EPRuntime.SendEvent(new SupportBean("E1", 10));
            EPAssertionUtil.AssertPropsPerRowAnyOrder(stmt.GetEnumerator(), fields, new[] {new object[] {10, 10, 10}});
    
            epService.EPRuntime.SendEvent(new SupportBean("E2", 20));
            EPAssertionUtil.AssertPropsPerRowAnyOrder(stmt.GetEnumerator(), fields, new[] {new object[] {10, 30, 10}, new object[] {20, 30, 20}});
    
            epService.EPRuntime.SendEvent(new SupportBean("E1", 30));
            EPAssertionUtil.AssertPropsPerRowAnyOrder(stmt.GetEnumerator(), fields, new[] {new object[] {10, 60, 40}, new object[] {20, 60, 20}, new object[] {30, 60, 40}});
    
            epService.EPAdministrator.DestroyAllStatements();
        }
    
        private void TryAssertionUngroupedHaving(EPServiceProvider epService) {
            string epl = "select * from SupportBean having sum(intPrimitive, group_by:theString) > 100";
            var listener = new SupportUpdateListener();
            epService.EPAdministrator.CreateEPL(epl).AddListener(listener);
    
            MakeSendEvent(epService, "E1", 95);
            MakeSendEvent(epService, "E2", 10);
            Assert.IsFalse(listener.IsInvoked);
    
            MakeSendEvent(epService, "E1", 10);
            Assert.IsTrue(listener.IsInvoked);
            listener.Reset();
    
            epService.EPAdministrator.DestroyAllStatements();
        }
    
        private void TryAssertionUngroupedOrderBy(EPServiceProvider epService) {
            string epl = "create context StartS0EndS1 start SupportBean_S0 end SupportBean_S1;" +
                    "@Name('out') context StartS0EndS1 select theString, sum(intPrimitive, group_by:theString) as c0 " +
                    " from SupportBean#keepall " +
                    " output snapshot when terminated" +
                    " order by sum(intPrimitive, group_by:theString)" +
                    ";";
            DeploymentResult deployed = epService.EPAdministrator.DeploymentAdmin.ParseDeploy(epl);
            var listener = new SupportUpdateListener();
            epService.EPAdministrator.GetStatement("out").AddListener(listener);
    
            epService.EPRuntime.SendEvent(new SupportBean_S0(0));
            MakeSendEvent(epService, "E1", 10);
            MakeSendEvent(epService, "E2", 20);
            MakeSendEvent(epService, "E1", 30);
            MakeSendEvent(epService, "E3", 40);
            MakeSendEvent(epService, "E2", 50);
            epService.EPRuntime.SendEvent(new SupportBean_S1(0));
    
            EPAssertionUtil.AssertPropsPerRow(listener.GetAndResetLastNewData(), "theString,c0".Split(','), new[]
            {
                new object[]{"E1", 40}, new object[]{"E1", 40}, new object[]{"E3", 40}, new object[]{"E2", 70}, new object[]{"E2", 70}});
    
            // try an empty batch
            epService.EPRuntime.SendEvent(new SupportBean_S0(1));
            epService.EPRuntime.SendEvent(new SupportBean_S1(1));
    
            epService.EPAdministrator.DeploymentAdmin.Undeploy(deployed.DeploymentId);
        }
    
        private void TryAssertionGroupedOnSelect(EPServiceProvider epService) {
            string epl = "create window MyWindow#keepall as SupportBean;" +
                    "insert into MyWindow select * from SupportBean;" +
                    "@Name('out') on SupportBean_S0 select theString, sum(intPrimitive) as c0, sum(intPrimitive, group_by:()) as c1" +
                    " from MyWindow group by theString;";
            DeploymentResult deployed = epService.EPAdministrator.DeploymentAdmin.ParseDeploy(epl);
            var listener = new SupportUpdateListener();
            epService.EPAdministrator.GetStatement("out").AddListener(listener);
    
            MakeSendEvent(epService, "E1", 10);
            MakeSendEvent(epService, "E2", 20);
            MakeSendEvent(epService, "E1", 30);
            MakeSendEvent(epService, "E3", 40);
            MakeSendEvent(epService, "E2", 50);
    
            epService.EPRuntime.SendEvent(new SupportBean_S0(0));
            EPAssertionUtil.AssertPropsPerRowAnyOrder(listener.GetAndResetLastNewData(), "theString,c0,c1".Split(','), new[]
            {
                new object[]{"E1", 40, 150}, new object[]{"E2", 70, 150}, new object[]{"E3", 40, 150}});
    
            MakeSendEvent(epService, "E1", 60);
    
            epService.EPRuntime.SendEvent(new SupportBean_S0(0));
            EPAssertionUtil.AssertPropsPerRowAnyOrder(listener.GetAndResetLastNewData(), "theString,c0,c1".Split(','), new[]
            {
                new object[]{"E1", 100, 210}, new object[]{"E2", 70, 210}, new object[]{"E3", 40, 210}});
    
            epService.EPAdministrator.DeploymentAdmin.Undeploy(deployed.DeploymentId);
        }
    
        private void TryAssertionUngroupedUnidirectionalJoin(EPServiceProvider epService) {
            string epl = "select theString, sum(intPrimitive, group_by:theString) as c0 from SupportBean#keepall, SupportBean_S0 unidirectional";
            var listener = new SupportUpdateListener();
            epService.EPAdministrator.CreateEPL(epl).AddListener(listener);
    
            MakeSendEvent(epService, "E1", 10);
            MakeSendEvent(epService, "E2", 20);
            MakeSendEvent(epService, "E1", 30);
    
            epService.EPRuntime.SendEvent(new SupportBean_S0(1));
            EPAssertionUtil.AssertPropsPerRowAnyOrder(listener.GetAndResetLastNewData(), "theString,c0".Split(','),
                    new[] {new object[] {"E1", 40}, new object[] {"E1", 40}, new object[] {"E2", 20}});
    
            MakeSendEvent(epService, "E1", 40);
    
            epService.EPRuntime.SendEvent(new SupportBean_S0(1));
            EPAssertionUtil.AssertPropsPerRowAnyOrder(listener.GetAndResetLastNewData(), "theString,c0".Split(','),
                    new[] {new object[] {"E1", 80}, new object[] {"E1", 80}, new object[] {"E1", 80}, new object[] {"E2", 20}});
    
            epService.EPAdministrator.DestroyAllStatements();
        }
    
        private void TryAssertionEnumMethods(EPServiceProvider epService, bool grouped) {
            string epl =
                    "select" +
                            " window(*, group_by:()).FirstOf() as c0," +
                            " window(*, group_by:theString).FirstOf() as c1," +
                            " window(intPrimitive, group_by:()).FirstOf() as c2," +
                            " window(intPrimitive, group_by:theString).FirstOf() as c3," +
                            " First(*, group_by:()).intPrimitive as c4," +
                            " First(*, group_by:theString).intPrimitive as c5 " +
                            " from SupportBean#keepall " +
                            (grouped ? "group by theString, intPrimitive" : "");
            var listener = new SupportUpdateListener();
            epService.EPAdministrator.CreateEPL(epl).AddListener(listener);
    
            SupportBean b1 = MakeSendEvent(epService, "E1", 10);
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), "c0,c1,c2,c3,c4,c5".Split(','),
                    new object[]{b1, b1, 10, 10, 10, 10});
    
            epService.EPAdministrator.DestroyAllStatements();
        }
    
        private void SendTime(EPServiceProvider epService, long msec) {
            epService.EPRuntime.SendEvent(new CurrentTimeEvent(msec));
        }
    
        private void SendEventMany(EPServiceProvider epService, params string[] theString) {
            foreach (string value in theString) {
                SendEvent(epService, value);
            }
        }
    
        private void SendEvent(EPServiceProvider epService, string theString) {
            epService.EPRuntime.SendEvent(new SupportBean(theString, 0));
        }
    
        private SupportBean MakeSendEvent(EPServiceProvider epService, string theString, int intPrimitive) {
            var b = new SupportBean(theString, intPrimitive);
            epService.EPRuntime.SendEvent(b);
            return b;
        }
    
        private SupportBean MakeSendEvent(EPServiceProvider epService, string theString, int intPrimitive, long longPrimitive) {
            var b = new SupportBean(theString, intPrimitive);
            b.LongPrimitive = longPrimitive;
            epService.EPRuntime.SendEvent(b);
            return b;
        }
    
        public delegate void MyAssertion(SupportUpdateListener listener);

        private void AssertCountColsAndLevels(EPServiceProvider epService, string epl, int colCount, int lvlCount) {
            string theEpl = PLAN_CALLBACK_HOOK + epl;
            epService.EPAdministrator.CreateEPL(theEpl);
            Pair<AggregationGroupByLocalGroupDesc, AggregationLocalGroupByPlan> plan = SupportAggLevelPlanHook.GetAndReset();
            Assert.AreEqual(colCount, plan.First.NumColumns);
            Assert.AreEqual(lvlCount, plan.First.Levels.Length);
        }
    
        private void AssertNoPlan(EPServiceProvider epService, string epl) {
            string theEpl = PLAN_CALLBACK_HOOK + epl;
            epService.EPAdministrator.CreateEPL(theEpl);
            Assert.IsNull(SupportAggLevelPlanHook.GetAndReset());
        }
    
        private void TryAssertionColNameRendering(EPServiceProvider epService) {
            EPStatement stmt = epService.EPAdministrator.CreateEPL("select " +
                    "count(*, group_by:(theString, intPrimitive)), " +
                    "count(group_by:theString, *) " +
                    "from SupportBean");
            Assert.AreEqual("count(*,group_by:(theString,intPrimitive))", stmt.EventType.PropertyNames[0]);
            Assert.AreEqual("count(group_by:theString,*)", stmt.EventType.PropertyNames[1]);
        }
    
        private void AssertScalarColl(EventBean eventBean, int[] expectedC6, int[] expectedC7)
        {
            var c6 = eventBean.Get("c6").Unwrap<int>();
            var c7 = eventBean.Get("c7").Unwrap<int>();
            EPAssertionUtil.AssertEqualsExactOrder(expectedC6, c6);
            EPAssertionUtil.AssertEqualsExactOrder(expectedC7, c7);
        }
    }
} // end of namespace
