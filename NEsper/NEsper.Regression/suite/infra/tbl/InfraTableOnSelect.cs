///////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) 2006-2015 Esper Team. All rights reserved.                           /
// http://esper.codehaus.org                                                          /
// ---------------------------------------------------------------------------------- /
// The software in this package is published under the terms of the GPL license       /
// a copy of which has been included with this distribution in the license.txt file.  /
///////////////////////////////////////////////////////////////////////////////////////

using com.espertech.esper.common.@internal.support;
using com.espertech.esper.compat;
using com.espertech.esper.regressionlib.framework;

using NUnit.Framework;

namespace com.espertech.esper.regressionlib.suite.infra.tbl
{
    /// <summary>
    ///     NOTE: More table-related tests in "nwtable"
    /// </summary>
    public class InfraTableOnSelect : RegressionExecution
    {
        public void Run(RegressionEnvironment env)
        {
            var path = new RegressionPath();
            env.CompileDeploy(
                "create table varagg as (" +
                "key string primary key, total sum(int))",
                path);
            env.CompileDeploy(
                "into table varagg " +
                "select sum(IntPrimitive) as total from SupportBean group by TheString",
                path);
            env.CompileDeploy("@Name('s0') on SupportBean_S0 select total as value from varagg where key = P00", path)
                .AddListener("s0");

            AssertValues(env, "G1,G2", new int?[] {null, null});

            env.SendEventBean(new SupportBean("G1", 100));
            AssertValues(env, "G1,G2", new int?[] {100, null});

            env.Milestone(0);

            env.SendEventBean(new SupportBean("G2", 200));
            AssertValues(env, "G1,G2", new int?[] {100, 200});

            env.CompileDeploy("@Name('i1') on SupportBean_S1 select total from varagg where key = P10", path)
                .AddListener("i1");

            env.SendEventBean(new SupportBean("G2", 300));

            env.Milestone(1);

            env.SendEventBean(new SupportBean_S1(0, "G2"));
            Assert.AreEqual(500, env.Listener("i1").AssertOneGetNewAndReset().Get("total"));

            env.UndeployAll();
        }

        private static void AssertValues(
            RegressionEnvironment env,
            string keys,
            int?[] values)
        {
            var keyarr = keys.SplitCsv();
            for (var i = 0; i < keyarr.Length; i++) {
                env.SendEventBean(new SupportBean_S0(0, keyarr[i]));
                if (values[i] == null) {
                    Assert.IsFalse(env.Listener("s0").IsInvoked);
                }
                else {
                    var @event = env.Listener("s0").AssertOneGetNewAndReset();
                    Assert.AreEqual(values[i], @event.Get("value"), "Failed for key '" + keyarr[i] + "'");
                }
            }
        }
    }
} // end of namespace