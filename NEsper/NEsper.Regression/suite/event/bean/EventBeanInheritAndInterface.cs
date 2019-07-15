///////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) 2006-2015 Esper Team. All rights reserved.                           /
// http://esper.codehaus.org                                                          /
// ---------------------------------------------------------------------------------- /
// The software in this package is published under the terms of the GPL license       /
// a copy of which has been included with this distribution in the license.txt file.  /
///////////////////////////////////////////////////////////////////////////////////////

using System.Collections.Generic;

using com.espertech.esper.regressionlib.framework;
using com.espertech.esper.regressionlib.support.bean;
using com.espertech.esper.runtime.client;
using com.espertech.esper.runtime.client.scopetest;

using NUnit.Framework;

namespace com.espertech.esper.regressionlib.suite.@event.bean
{
    public class EventBeanInheritAndInterface
    {
        public static IList<RegressionExecution> Executions()
        {
            IList<RegressionExecution> execs = new List<RegressionExecution>();
            execs.Add(new EventBeanInheritAndInterfaceOverridingSubclass());
            execs.Add(new EventBeanInheritAndInterfaceImplementationClass());
            return execs;
        }

        internal class EventBeanInheritAndInterfaceOverridingSubclass : RegressionExecution
        {
            public void Run(RegressionEnvironment env)
            {
                var epl = "@Name('s0') select val as value from SupportOverrideOne#length(10)";
                env.CompileDeployAddListenerMileZero(epl, "s0");

                env.SendEventBean(new SupportOverrideOneA("valA", "valOne", "valBase"));
                var theEvent = env.Listener("s0").GetAndResetLastNewData()[0];
                Assert.AreEqual("valA", theEvent.Get("value"));

                env.SendEventBean(new SupportOverrideBase("x"));
                Assert.IsFalse(env.Listener("s0").IsInvoked);

                env.SendEventBean(new SupportOverrideOneB("valB", "valTwo", "valBase2"));
                theEvent = env.Listener("s0").GetAndResetLastNewData()[0];
                Assert.AreEqual("valB", theEvent.Get("value"));

                env.SendEventBean(new SupportOverrideOne("valThree", "valBase3"));
                theEvent = env.Listener("s0").GetAndResetLastNewData()[0];
                Assert.AreEqual("valThree", theEvent.Get("value"));

                env.UndeployAll();
            }
        }

        internal class EventBeanInheritAndInterfaceImplementationClass : RegressionExecution
        {
            public void Run(RegressionEnvironment env)
            {
                string[] epls = {
                    "select baseAB from ISupportBaseAB#length(10)",
                    "select baseAB, a from ISupportA#length(10)",
                    "select baseAB, b from ISupportB#length(10)",
                    "select c from ISupportC#length(10)",
                    "select baseAB, a, g from ISupportAImplSuperG#length(10)",
                    "select baseAB, a, b, g, c from ISupportAImplSuperGImplPlus#length(10)"
                };

                string[][] expected = {
                    new[] {"baseAB"},
                    new[] {"baseAB", "a"},
                    new[] {"baseAB", "b"},
                    new[] {"c"},
                    new[] {"baseAB", "a", "g"},
                    new[] {"baseAB", "a", "b", "g", "c"}
                };

                var stmts = new EPStatement[epls.Length];
                var listeners = new SupportUpdateListener[epls.Length];
                for (var i = 0; i < epls.Length; i++) {
                    var name = string.Format("@Name('%s')", "stmt_" + i);
                    env.CompileDeploy(name + epls[i]);
                    stmts[i] = env.Statement("stmt_" + i);
                    listeners[i] = new SupportUpdateListener();
                    stmts[i].AddListener(listeners[i]);
                }

                env.SendEventBean(new ISupportAImplSuperGImplPlus("g", "a", "baseAB", "b", "c"));
                for (var i = 0; i < listeners.Length; i++) {
                    Assert.IsTrue(listeners[i].IsInvoked);
                    var theEvent = listeners[i].GetAndResetLastNewData()[0];

                    for (var j = 0; j < expected[i].Length; j++) {
                        Assert.IsTrue(
                            theEvent.EventType.IsProperty(expected[i][j]),
                            "failed property valId check for stmt=" + epls[i]);
                        Assert.AreEqual(
                            expected[i][j],
                            theEvent.Get(expected[i][j]),
                            "failed property check for stmt=" + epls[i]);
                    }
                }

                env.UndeployAll();
            }
        }
    }
} // end of namespace