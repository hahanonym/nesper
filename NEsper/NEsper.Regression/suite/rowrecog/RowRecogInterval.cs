///////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) 2006-2015 Esper Team. All rights reserved.                           /
// http://esper.codehaus.org                                                          /
// ---------------------------------------------------------------------------------- /
// The software in this package is published under the terms of the GPL license       /
// a copy of which has been included with this distribution in the license.txt file.  /
///////////////////////////////////////////////////////////////////////////////////////

using System.Collections.Generic;

using com.espertech.esper.common.client.scopetest;
using com.espertech.esper.common.@internal.support;
using com.espertech.esper.compat;
using com.espertech.esper.compat.datetime;
using com.espertech.esper.regressionlib.framework;
using com.espertech.esper.regressionlib.support.rowrecog;

using NUnit.Framework;

namespace com.espertech.esper.regressionlib.suite.rowrecog
{
    public class RowRecogInterval
    {
        public static IList<RegressionExecution> Executions()
        {
            var execs = new List<RegressionExecution>();
            execs.Add(new RowRecogIntervalSimple());
            execs.Add(new RowRecogPartitioned());
            execs.Add(new RowRecogMultiCompleted());
            execs.Add(new RowRecogMonthScoped());
            return execs;
        }

        private static void SendCurrentTime(
            RegressionEnvironment env,
            string time)
        {
            env.AdvanceTime(DateTimeParsingFunctions.ParseDefaultMSec(time));
        }

        private static void SendCurrentTimeWithMinus(
            RegressionEnvironment env,
            string time,
            long minus)
        {
            env.AdvanceTime(DateTimeParsingFunctions.ParseDefaultMSec(time) - minus);
        }

        private static void SendTimer(
            long time,
            RegressionEnvironment env)
        {
            env.AdvanceTime(time);
        }

        internal class RowRecogIntervalSimple : RegressionExecution
        {
            public void Run(RegressionEnvironment env)
            {
                SendTimer(0, env);
                var text = "@Name('s0') select * from SupportRecogBean#keepall " +
                           "match_recognize (" +
                           " measures A.TheString as a, B[0].TheString as b0, B[1].TheString as b1, last(B.TheString) as lastb" +
                           " pattern (A B*)" +
                           " interval 10 seconds" +
                           " define" +
                           " A as A.TheString like \"A%\"," +
                           " B as B.TheString like \"B%\"" +
                           ") order by a, b0, b1, lastb";

                var milestone = new AtomicLong();
                env.CompileDeploy(text).AddListener("s0");
                TryAssertionInterval(env, milestone);
                env.UndeployAll();

                env.EplToModelCompileDeploy(text).AddListener("s0");
                TryAssertionInterval(env, milestone);
                env.UndeployAll();
            }

            private void TryAssertionInterval(
                RegressionEnvironment env,
                AtomicLong milestone)
            {
                var fields = new [] { "a","b0","b1","lastb" };
                SendTimer(1000, env);
                env.SendEventBean(new SupportRecogBean("A1", 1));
                Assert.IsFalse(env.Listener("s0").IsInvoked);

                env.MilestoneInc(milestone);

                SendTimer(10999, env);
                Assert.IsFalse(env.Listener("s0").IsInvoked);
                EPAssertionUtil.AssertPropsPerRow(
                    env.Statement("s0").GetEnumerator(),
                    fields,
                    new[] {new object[] {"A1", null, null, null}});

                env.MilestoneInc(milestone);

                SendTimer(11000, env);
                EPAssertionUtil.AssertPropsPerRow(
                    env.Statement("s0").GetEnumerator(),
                    fields,
                    new[] {new object[] {"A1", null, null, null}});
                EPAssertionUtil.AssertPropsPerRow(
                    env.Listener("s0").GetAndResetLastNewData(),
                    fields,
                    new[] {new object[] {"A1", null, null, null}});

                env.MilestoneInc(milestone);

                SendTimer(13000, env);
                env.SendEventBean(new SupportRecogBean("A2", 2));
                Assert.IsFalse(env.Listener("s0").IsInvoked);

                env.MilestoneInc(milestone);

                SendTimer(15000, env);
                env.SendEventBean(new SupportRecogBean("B1", 3));
                Assert.IsFalse(env.Listener("s0").IsInvoked);

                env.MilestoneInc(milestone);

                SendTimer(22999, env);
                Assert.IsFalse(env.Listener("s0").IsInvoked);

                env.MilestoneInc(milestone);

                SendTimer(23000, env);
                EPAssertionUtil.AssertPropsPerRow(
                    env.Statement("s0").GetEnumerator(),
                    fields,
                    new[] {new object[] {"A1", null, null, null}, new object[] {"A2", "B1", null, "B1"}});
                EPAssertionUtil.AssertPropsPerRow(
                    env.Listener("s0").GetAndResetLastNewData(),
                    fields,
                    new[] {new object[] {"A2", "B1", null, "B1"}});

                env.MilestoneInc(milestone);

                SendTimer(25000, env);
                env.SendEventBean(new SupportRecogBean("A3", 4));
                Assert.IsFalse(env.Listener("s0").IsInvoked);

                SendTimer(26000, env);
                env.SendEventBean(new SupportRecogBean("B2", 5));
                Assert.IsFalse(env.Listener("s0").IsInvoked);

                SendTimer(29000, env);
                env.SendEventBean(new SupportRecogBean("B3", 6));
                Assert.IsFalse(env.Listener("s0").IsInvoked);

                env.MilestoneInc(milestone);

                SendTimer(34999, env);
                env.SendEventBean(new SupportRecogBean("B4", 7));
                Assert.IsFalse(env.Listener("s0").IsInvoked);

                SendTimer(35000, env);
                EPAssertionUtil.AssertPropsPerRow(
                    env.Statement("s0").GetEnumerator(),
                    fields,
                    new[] {
                        new object[] {"A1", null, null, null}, new object[] {"A2", "B1", null, "B1"},
                        new object[] {"A3", "B2", "B3", "B4"}
                    });
                EPAssertionUtil.AssertPropsPerRow(
                    env.Listener("s0").GetAndResetLastNewData(),
                    fields,
                    new[] {new object[] {"A3", "B2", "B3", "B4"}});
            }
        }

        internal class RowRecogPartitioned : RegressionExecution
        {
            public void Run(RegressionEnvironment env)
            {
                SendTimer(0, env);
                var fields = new [] { "a","b0","b1","lastb" };
                var text = "@Name('s0') select * from SupportRecogBean#keepall " +
                           "match_recognize (" +
                           "  partition by Cat " +
                           "  measures A.TheString as a, B[0].TheString as b0, B[1].TheString as b1, last(B.TheString) as lastb" +
                           "  pattern (A B*) " +
                           "  INTERVAL 10 seconds " +
                           "  define " +
                           "    A as A.TheString like 'A%'," +
                           "    B as B.TheString like 'B%'" +
                           ") order by a, b0, b1, lastb";

                env.CompileDeploy(text).AddListener("s0");

                SendTimer(1000, env);
                env.SendEventBean(new SupportRecogBean("A1", "C1", 1));

                env.Milestone(0);

                SendTimer(1000, env);
                env.SendEventBean(new SupportRecogBean("A2", "C2", 2));

                env.Milestone(1);

                SendTimer(2000, env);
                env.SendEventBean(new SupportRecogBean("A3", "C3", 3));

                env.Milestone(2);

                SendTimer(3000, env);
                env.SendEventBean(new SupportRecogBean("A4", "C4", 4));

                env.Milestone(3);

                env.SendEventBean(new SupportRecogBean("B1", "C3", 5));
                env.SendEventBean(new SupportRecogBean("B2", "C1", 6));
                env.SendEventBean(new SupportRecogBean("B3", "C1", 7));
                env.SendEventBean(new SupportRecogBean("B4", "C4", 7));
                EPAssertionUtil.AssertPropsPerRow(
                    env.Statement("s0").GetEnumerator(),
                    fields,
                    new[] {
                        new object[] {"A1", "B2", "B3", "B3"}, new object[] {"A2", null, null, null},
                        new object[] {"A3", "B1", null, "B1"}, new object[] {"A4", "B4", null, "B4"}
                    });

                env.Milestone(4);

                SendTimer(10999, env);
                Assert.IsFalse(env.Listener("s0").IsInvoked);

                env.Milestone(5);

                SendTimer(11000, env);
                EPAssertionUtil.AssertPropsPerRow(
                    env.Listener("s0").GetAndResetLastNewData(),
                    fields,
                    new[] {new object[] {"A1", "B2", "B3", "B3"}, new object[] {"A2", null, null, null}});

                SendTimer(11999, env);
                Assert.IsFalse(env.Listener("s0").IsInvoked);

                env.Milestone(6);

                SendTimer(12000, env);
                EPAssertionUtil.AssertPropsPerRow(
                    env.Listener("s0").GetAndResetLastNewData(),
                    fields,
                    new[] {new object[] {"A3", "B1", null, "B1"}});

                SendTimer(13000, env);
                EPAssertionUtil.AssertPropsPerRow(
                    env.Listener("s0").GetAndResetLastNewData(),
                    fields,
                    new[] {new object[] {"A4", "B4", null, "B4"}});

                env.UndeployAll();
            }
        }

        internal class RowRecogMultiCompleted : RegressionExecution
        {
            public void Run(RegressionEnvironment env)
            {
                SendTimer(0, env);
                var fields = new [] { "a","b0","b1","lastb" };
                var text = "@Name('s0') select * from SupportRecogBean#keepall " +
                           "match_recognize (" +
                           "  measures A.TheString as a, B[0].TheString as b0, B[1].TheString as b1, last(B.TheString) as lastb" +
                           "  pattern (A B*) " +
                           "  interval 10 seconds " +
                           "  define " +
                           "    A as A.TheString like 'A%'," +
                           "    B as B.TheString like 'B%'" +
                           ") order by a, b0, b1, lastb";

                env.CompileDeploy(text).AddListener("s0");

                SendTimer(1000, env);
                env.SendEventBean(new SupportRecogBean("A1", 1));
                Assert.IsFalse(env.Listener("s0").IsInvoked);

                env.Milestone(0);

                SendTimer(5000, env);
                env.SendEventBean(new SupportRecogBean("A2", 2));
                Assert.IsFalse(env.Listener("s0").IsInvoked);

                env.Milestone(1);

                SendTimer(10999, env);
                Assert.IsFalse(env.Listener("s0").IsInvoked);
                EPAssertionUtil.AssertPropsPerRow(
                    env.Statement("s0").GetEnumerator(),
                    fields,
                    new[] {new object[] {"A1", null, null, null}, new object[] {"A2", null, null, null}});

                env.Milestone(2);

                SendTimer(11000, env);
                EPAssertionUtil.AssertPropsPerRow(
                    env.Listener("s0").GetAndResetLastNewData(),
                    fields,
                    new[] {new object[] {"A1", null, null, null}});

                env.Milestone(3);

                SendTimer(15000, env);
                EPAssertionUtil.AssertPropsPerRow(
                    env.Listener("s0").GetAndResetLastNewData(),
                    fields,
                    new[] {new object[] {"A2", null, null, null}});

                env.Milestone(4);

                SendTimer(21000, env);
                env.SendEventBean(new SupportRecogBean("A3", 3));
                Assert.IsFalse(env.Listener("s0").IsInvoked);

                SendTimer(22000, env);
                env.SendEventBean(new SupportRecogBean("A4", 4));
                Assert.IsFalse(env.Listener("s0").IsInvoked);

                env.Milestone(5);

                SendTimer(23000, env);
                env.SendEventBean(new SupportRecogBean("B1", 5));
                env.SendEventBean(new SupportRecogBean("B2", 6));
                env.SendEventBean(new SupportRecogBean("B3", 7));
                env.SendEventBean(new SupportRecogBean("B4", 8));
                Assert.IsFalse(env.Listener("s0").IsInvoked);

                env.Milestone(6);

                SendTimer(31000, env);
                EPAssertionUtil.AssertPropsPerRow(
                    env.Listener("s0").GetAndResetLastNewData(),
                    fields,
                    new[] {new object[] {"A3", null, null, null}});
                EPAssertionUtil.AssertPropsPerRow(
                    env.Statement("s0").GetEnumerator(),
                    fields,
                    new[] {
                        new object[] {"A1", null, null, null}, new object[] {"A2", null, null, null},
                        new object[] {"A3", null, null, null}, new object[] {"A4", "B1", "B2", "B4"}
                    });

                env.Milestone(7);

                SendTimer(32000, env);
                EPAssertionUtil.AssertPropsPerRow(
                    env.Listener("s0").GetAndResetLastNewData(),
                    fields,
                    new[] {new object[] {"A4", "B1", "B2", "B4"}});

                env.UndeployAll();
            }
        }

        internal class RowRecogMonthScoped : RegressionExecution
        {
            public void Run(RegressionEnvironment env)
            {
                SendCurrentTime(env, "2002-02-01T09:00:00.000");
                var text = "@Name('s0') select * from SupportBean " +
                           "match_recognize (" +
                           " measures A.TheString as a, B[0].TheString as b0, B[1].TheString as b1 " +
                           " pattern (A B*)" +
                           " interval 1 month" +
                           " define" +
                           " A as A.TheString like \"A%\"," +
                           " B as B.TheString like \"B%\"" +
                           ")";
                env.CompileDeploy(text).AddListener("s0");

                env.SendEventBean(new SupportBean("A1", 0));
                env.SendEventBean(new SupportBean("B1", 0));
                SendCurrentTimeWithMinus(env, "2002-03-01T09:00:00.000", 1);
                Assert.IsFalse(env.Listener("s0").GetAndClearIsInvoked());

                env.Milestone(0);

                SendCurrentTime(env, "2002-03-01T09:00:00.000");
                EPAssertionUtil.AssertPropsPerRow(
                    env.Listener("s0").GetAndResetLastNewData(),
                    new [] { "a","b0","b1" },
                    new[] {new object[] {"A1", "B1", null}});

                env.UndeployAll();
            }
        }
    }
} // end of namespace