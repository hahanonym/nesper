///////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) 2006-2017 Esper Team. All rights reserved.                           /
// http://esper.codehaus.org                                                          /
// ---------------------------------------------------------------------------------- /
// The software in this package is published under the terms of the GPL license       /
// a copy of which has been included with this distribution in the license.txt file.  /
///////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Xml;

using com.espertech.esper.client;
using com.espertech.esper.client.scopetest;
using com.espertech.esper.compat;
using com.espertech.esper.compat.collections;
using com.espertech.esper.compat.logging;
using com.espertech.esper.supportregression.events;
using com.espertech.esper.supportregression.execution;
using com.espertech.esper.util.support;

// using static com.espertech.esper.regression.event.xml.ExecEventXMLSchemaPropertyDynamicDOMGetter.SCHEMA_XML;
// using static com.espertech.esper.regression.event.xml.ExecEventXMLSchemaXPathBacked.CLASSLOADER_SCHEMA_URI;
// using static org.junit.Assert.assertSame;

using NUnit.Framework;

namespace com.espertech.esper.regression.events.xml
{
    public class ExecEventXMLSchemaPropertyDynamicXPathGetter : RegressionExecution {
        public override void Configure(Configuration configuration) {
            var desc = new ConfigurationEventTypeXMLDOM();
            desc.RootElementName = "simpleEvent";
            string schemaUri = typeof(ExecEventXMLSchemaInvalid).ClassLoader.GetResource(CLASSLOADER_SCHEMA_URI).ToString();
            desc.SchemaResource = schemaUri;
            desc.XPathPropertyExpr = true;
            desc.EventSenderValidatesRoot = false;
            desc.AddNamespacePrefix("ss", "samples:schemas:simpleSchema");
            desc.DefaultNamespace = "samples:schemas:simpleSchema";
            configuration.AddEventType("MyEvent", desc);
        }
    
        public override void Run(EPServiceProvider epService) {
            string stmtText = "select type?,dyn[1]?,nested.nes2?,Map('a')? from MyEvent";
            EPStatement stmt = epService.EPAdministrator.CreateEPL(stmtText);
            var listener = new SupportUpdateListener();
            stmt.AddListener(listener);
    
            EPAssertionUtil.AssertEqualsAnyOrder(new Object[]{
                    new EventPropertyDescriptor("type?", typeof(XmlNode), null, false, false, false, false, false),
                    new EventPropertyDescriptor("dyn[1]?", typeof(XmlNode), null, false, false, false, false, false),
                    new EventPropertyDescriptor("nested.nes2?", typeof(XmlNode), null, false, false, false, false, false),
                    new EventPropertyDescriptor("Map('a')?", typeof(XmlNode), null, false, false, false, false, false),
            }, stmt.EventType.PropertyDescriptors);
            SupportEventTypeAssertionUtil.AssertConsistency(stmt.EventType);
    
            EventSender sender = epService.EPRuntime.GetEventSender("MyEvent");
            XmlDocument root = SupportXML.SendEvent(sender, SCHEMA_XML);
    
            EventBean theEvent = listener.AssertOneGetNewAndReset();
            Assert.AreSame(root.DocumentElement.ChildNodes.Item(0), theEvent.Get("type?"));
            Assert.AreSame(root.DocumentElement.ChildNodes.Item(4), theEvent.Get("dyn[1]?"));
            Assert.AreSame(root.DocumentElement.ChildNodes.Item(6).ChildNodes.Item(1), theEvent.Get("nested.nes2?"));
            Assert.AreSame(root.DocumentElement.ChildNodes.Item(8), theEvent.Get("Map('a')?"));
            SupportEventTypeAssertionUtil.AssertConsistency(theEvent);
        }
    }
} // end of namespace
