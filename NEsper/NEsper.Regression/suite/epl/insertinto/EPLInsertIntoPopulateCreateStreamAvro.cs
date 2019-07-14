///////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) 2006-2015 Esper Team. All rights reserved.                           /
// http://esper.codehaus.org                                                          /
// ---------------------------------------------------------------------------------- /
// The software in this package is published under the terms of the GPL license       /
// a copy of which has been included with this distribution in the license.txt file.  /
///////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;

using com.espertech.esper.common.client.scopetest;
using com.espertech.esper.common.@internal.support;
using com.espertech.esper.compat.collections;
using com.espertech.esper.regressionlib.framework;

using NEsper.Avro.Core;
using NEsper.Avro.Extensions;
using NEsper.Avro.Util.Support;

using NUnit.Framework;

namespace com.espertech.esper.regressionlib.suite.epl.insertinto
{
    public class EPLInsertIntoPopulateCreateStreamAvro
    {
        public static IList<RegressionExecution> Executions()
        {
            IList<RegressionExecution> execs = new List<RegressionExecution>();
            execs.Add(new EPLInsertIntoCompatExisting());
            execs.Add(new EPLInsertIntoNewSchema());
            return execs;
        }

        public static byte[] MakeByteArray()
        {
            return new byte[] {1, 2, 3};
        }

        public static IDictionary<string, string> MakeMapStringString()
        {
            return Collections.SingletonMap("k1", "v1");
        }

        internal class EPLInsertIntoCompatExisting : RegressionExecution
        {
            public void Run(RegressionEnvironment env)
            {
                var epl = "@Name('s0') insert into AvroExistingType select 1 as myLong," +
                          "{1L, 2L} as myLongArray," +
                          typeof(EPLInsertIntoPopulateCreateStreamAvro).Name +
                          ".makeByteArray() as myByteArray, " +
                          typeof(EPLInsertIntoPopulateCreateStreamAvro).Name +
                          ".makeMapStringString() as myMap " +
                          "from SupportBean";
                env.CompileDeploy(epl).AddListener("s0");

                env.SendEventBean(new SupportBean());
                var @event = env.Listener("s0").AssertOneGetNewAndReset();
                SupportAvroUtil.AvroToJson(@event);
                Assert.AreEqual(1L, @event.Get("myLong"));
                EPAssertionUtil.AssertEqualsExactOrder(
                    new[] {1L, 2L},
                    @event.Get("myLongArray").UnwrapIntoArray<long>());
                Assert.IsTrue(Equals(new byte[] {1, 2, 3}, (byte[]) @event.Get("myByteArray")));
                Assert.AreEqual("{k1=v1}", ((IDictionary<string, object>) @event.Get("myMap")).ToString());

                env.UndeployAll();
            }
        }

        internal class EPLInsertIntoNewSchema : RegressionExecution
        {
            public void Run(RegressionEnvironment env)
            {
                var epl = "@Name('s0') " +
                          EventRepresentationChoice.AVRO.GetAnnotationText() +
                          " select 1 as myInt," +
                          "{1L, 2L} as myLongArray," +
                          typeof(EPLInsertIntoPopulateCreateStreamAvro).FullName +
                          ".makeByteArray() as myByteArray, " +
                          typeof(EPLInsertIntoPopulateCreateStreamAvro).FullName +
                          ".makeMapStringString() as myMap " +
                          "from SupportBean";
                env.CompileDeploy(epl).AddListener("s0");

                env.SendEventBean(new SupportBean());
                var @event = env.Listener("s0").AssertOneGetNewAndReset();
                var json = SupportAvroUtil.AvroToJson(@event);
                Console.Out.WriteLine(json);
                Assert.AreEqual(1, @event.Get("myInt"));
                EPAssertionUtil.AssertEqualsExactOrder(
                    new[] {1L, 2L},
                    @event.Get("myLongArray").UnwrapIntoArray<long>());
                Assert.IsTrue(Equals(new byte[] {1, 2, 3}, (byte[]) @event.Get("myByteArray")));
                Assert.AreEqual("{k1=v1}", ((IDictionary<string, object>) @event.Get("myMap")).ToString());

                var designSchema = SchemaBuilder.Record(
                    "name",
                    TypeBuilder.RequiredInt("myInt"),
                    TypeBuilder.Field(
                        "myLongArray",
                        TypeBuilder.Array(
                            TypeBuilder.Union(
                                TypeBuilder.NullType(),
                                TypeBuilder.LongType()))),
                    TypeBuilder.Field(
                        "myByteArray",
                        TypeBuilder.BytesType()),
                    TypeBuilder.Field(
                        "myMap",
                        TypeBuilder.Map(
                            TypeBuilder.StringType(
                                TypeBuilder.Property(
                                    AvroConstant.PROP_STRING_KEY,
                                    AvroConstant.PROP_STRING_VALUE))))
                );
                var assembledSchema = ((AvroEventType) @event.EventType).SchemaAvro;
                var compareMsg = SupportAvroUtil.CompareSchemas(designSchema, assembledSchema);
                Assert.IsNull(compareMsg, compareMsg);

                env.UndeployAll();
            }
        }
    }
} // end of namespace