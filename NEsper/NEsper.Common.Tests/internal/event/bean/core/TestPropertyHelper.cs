///////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) 2006-2015 Esper Team. All rights reserved.                           /
// http://esper.codehaus.org                                                          /
// ---------------------------------------------------------------------------------- /
// The software in this package is published under the terms of the GPL license       /
// a copy of which has been included with this distribution in the license.txt file.  /
///////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using com.espertech.esper.common.client;
using com.espertech.esper.common.client.scopetest;
using com.espertech.esper.common.@internal.@event.core;
using com.espertech.esper.common.@internal.supportunit.bean;
using com.espertech.esper.common.@internal.supportunit.@event;
using com.espertech.esper.common.@internal.supportunit.util;
using com.espertech.esper.compat.logging;
using com.espertech.esper.compat.magic;
using com.espertech.esper.container;
using NUnit.Framework;

namespace com.espertech.esper.common.@internal.@event.bean.core
{
    [TestFixture]
    public class TestPropertyHelper : AbstractCommonTest
    {
        [Test]
        public void TestAddMappedProperties()
        {
            IList<PropertyStem> result = new List<PropertyStem>();
            PropertyHelper.AddMappedProperties(
                MagicType.GetCachedType<SupportBeanPropertyNames>(), result);
            Assert.AreEqual(6, result.Count);

            IList<string> propertyNames = new List<string>();
            foreach (PropertyStem desc in result)
            {
                log.Debug("desc=" + desc.PropertyName);
                propertyNames.Add(desc.PropertyName);
            }
            EPAssertionUtil.AssertEqualsAnyOrder(new object[] { "a", "AB", "ABC", "ab", "abc", "fooBah" }, propertyNames.ToArray());
        }

        [Test]
        public void TestAddIntrospectProperties()
        {
            IList<PropertyStem> result = new List<PropertyStem>();
            PropertyHelper.AddIntrospectProperties(
                MagicType.GetCachedType<SupportBeanPropertyNames>(), result);

            foreach (PropertyStem desc in result)
            {
                log.Debug("desc=" + desc.PropertyName);
            }

            Assert.AreEqual(9, result.Count); // for "class" is also in there
            Assert.AreEqual("Indexed", result[8].PropertyName);
            Assert.IsNotNull(result[8].ReadMethod);
        }

        [Test]
        public void TestRemoveDuplicateProperties()
        {
            IList<PropertyStem> result = new List<PropertyStem>();
            result.Add(new PropertyStem("x", (MethodInfo) null, null));
            result.Add(new PropertyStem("x", (MethodInfo) null, null));
            result.Add(new PropertyStem("y", (MethodInfo) null, null));

            PropertyHelper.RemoveDuplicateProperties(result);

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("x", result[0].PropertyName);
            Assert.AreEqual("y", result[1].PropertyName);
        }

        [Test]
        public void TestRemoveProperties()
        {
            IList<PropertyStem> result = new List<PropertyStem>();
            result.Add(new PropertyStem("x", (MethodInfo) null, null));

            // Add all methods from System.Object that have no parameters, these
            // would be getters like GetType(), GetHashCode(), ToString().
            foreach (var method in typeof(object)
                .GetMethods()
                .Where(m => m.ReturnType != typeof(void) &&
                            m.GetParameters().Length == 0)) {
                result.Add(new PropertyStem(method.Name, method, null));
            }

            // Ensure that there are at least two properties.
            Assert.That(result.Count, Is.GreaterThan(1));

            PropertyHelper.RemovePlatformProperties(result);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("x", result[0].PropertyName);
        }

        [Test]
        public void TestGetGetter()
        {
            var supportEventTypeFactory = SupportEventTypeFactory.GetInstance(container);
            var bean = SupportEventBeanFactory.CreateObject(supportEventTypeFactory, new SupportBeanPropertyNames());
            var property = typeof(SupportBeanPropertyNames).GetProperty("A");
            var getter = PropertyHelper.GetGetter(property, EventBeanTypedEventFactoryCompileTime.INSTANCE,
                supportEventTypeFactory.BEAN_EVENT_TYPE_FACTORY);
            Assert.AreEqual("", getter.Get(bean));
        }

        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    }
} // end of namespace
