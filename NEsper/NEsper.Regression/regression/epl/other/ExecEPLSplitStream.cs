///////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) 2006-2017 Esper Team. All rights reserved.                           /
// http://esper.codehaus.org                                                          /
// ---------------------------------------------------------------------------------- /
// The software in this package is published under the terms of the GPL license       /
// a copy of which has been included with this distribution in the license.txt file.  /
///////////////////////////////////////////////////////////////////////////////////////

using System.Collections.Generic;

using Avro.Generic;

using com.espertech.esper.client;
using com.espertech.esper.client.deploy;
using com.espertech.esper.client.scopetest;
using com.espertech.esper.client.soda;
using com.espertech.esper.compat;
using com.espertech.esper.compat.collections;
using com.espertech.esper.compat.logging;
using com.espertech.esper.supportregression.bean;
using com.espertech.esper.supportregression.bean.bookexample;
using com.espertech.esper.supportregression.execution;
using com.espertech.esper.supportregression.util;
using com.espertech.esper.util;

using NEsper.Avro.Extensions;

using NUnit.Framework;

using static NEsper.Avro.Extensions.TypeBuilder;

namespace com.espertech.esper.regression.epl.other
{
    using Map = IDictionary<string, object>;

    public class ExecEPLSplitStream : RegressionExecution
    {
        public override void Configure(Configuration configuration)
        {
            configuration.AddEventType<SupportBean>();
            configuration.AddEventType("S0", typeof(SupportBean_S0));
        }
    
        public override void Run(EPServiceProvider epService) {
            RunAssertionInvalid(epService);
            RunAssertionFromClause(epService);
            RunAssertionSplitPremptiveNamedWindow(epService);
            RunAssertion1SplitDefault(epService);
            RunAssertion2SplitNoDefaultOutputFirst(epService);
            RunAssertionSubquery(epService);
            RunAssertion2SplitNoDefaultOutputAll(epService);
            RunAssertion3And4SplitDefaultOutputFirst(epService);
        }
    
        private void RunAssertionInvalid(EPServiceProvider epService) {
            SupportMessageAssertUtil.TryInvalid(epService, "on SupportBean select * where intPrimitive=1 insert into BStream select * where 1=2",
                    "Error starting statement: Required insert-into clause is not provided, the clause is required for split-stream syntax");
    
            SupportMessageAssertUtil.TryInvalid(epService, "on SupportBean insert into AStream select * where intPrimitive=1 group by string insert into BStream select * where 1=2",
                    "Error starting statement: A group-by clause, having-clause or order-by clause is not allowed for the split stream syntax");
    
            SupportMessageAssertUtil.TryInvalid(epService, "on SupportBean insert into AStream select * where intPrimitive=1 insert into BStream select avg(intPrimitive) where 1=2",
                    "Error starting statement: Aggregation functions are not allowed in this context");
        }
    
        private void RunAssertionFromClause(EPServiceProvider epService) {
            epService.EPAdministrator.Configuration.AddEventType("OrderEvent", typeof(OrderBean));
    
            TryAssertionFromClauseBeginBodyEnd(epService);
            TryAssertionFromClauseAsMultiple(epService);
            TryAssertionFromClauseOutputFirstWhere(epService);
            TryAssertionFromClauseDocSample(epService);
        }
    
        private void RunAssertionSplitPremptiveNamedWindow(EPServiceProvider epService) {
            foreach (EventRepresentationChoice rep in EnumHelper.GetValues<EventRepresentationChoice>()) {
                TryAssertionSplitPremptiveNamedWindow(epService, rep);
            }
        }
    
        private void RunAssertion1SplitDefault(EPServiceProvider epService) {
            // test wildcard
            string stmtOrigText = "on SupportBean insert into AStream select *";
            EPStatement stmt = epService.EPAdministrator.CreateEPL(stmtOrigText);
            var listener = new SupportUpdateListener();
            stmt.AddListener(listener);
    
            SupportUpdateListener[] listeners = GetListeners();
            EPStatement stmtOne = epService.EPAdministrator.CreateEPL("select * from AStream");
            stmtOne.AddListener(listeners[0]);
    
            SendSupportBean(epService, "E1", 1);
            AssertReceivedSingle(listeners, 0, "E1");
            Assert.IsFalse(listener.IsInvoked);
    
            // test select
            stmtOrigText = "on SupportBean insert into BStreamABC select 3*intPrimitive as value";
            EPStatement stmtOrig = epService.EPAdministrator.CreateEPL(stmtOrigText);
    
            stmtOne = epService.EPAdministrator.CreateEPL("select value from BStreamABC");
            stmtOne.AddListener(listeners[1]);
    
            SendSupportBean(epService, "E1", 6);
            Assert.AreEqual(18, listeners[1].AssertOneGetNewAndReset().Get("value"));
    
            // assert type is original type
            Assert.AreEqual(typeof(SupportBean), stmtOrig.EventType.UnderlyingType);
            Assert.IsFalse(stmtOrig.HasFirst());
    
            stmtOne.Dispose();
        }
    
        private void RunAssertion2SplitNoDefaultOutputFirst(EPServiceProvider epService) {
            string stmtOrigText = "@Audit on SupportBean " +
                    "insert into AStream2SP select * where intPrimitive=1 " +
                    "insert into BStream2SP select * where intPrimitive=1 or intPrimitive=2";
            EPStatement stmtOrig = epService.EPAdministrator.CreateEPL(stmtOrigText);
            TryAssertion(epService, stmtOrig);
    
            // statement object model
            var model = new EPStatementObjectModel();
            model.Annotations = Collections.SingletonList(new AnnotationPart("Audit"));
            model.FromClause = FromClause.Create(FilterStream.Create("SupportBean"));
            model.InsertInto = InsertIntoClause.Create("AStream2SP");
            model.SelectClause = SelectClause.CreateWildcard();
            model.WhereClause = Expressions.Eq("intPrimitive", 1);
            OnInsertSplitStreamClause clause = OnClause.CreateOnInsertSplitStream();
            model.OnExpr = clause;
            OnInsertSplitStreamItem item = OnInsertSplitStreamItem.Create(
                    InsertIntoClause.Create("BStream2SP"),
                    SelectClause.CreateWildcard(),
                    Expressions.Or(Expressions.Eq("intPrimitive", 1), Expressions.Eq("intPrimitive", 2)));
            clause.AddItem(item);
            Assert.AreEqual(stmtOrigText, model.ToEPL());
            stmtOrig = epService.EPAdministrator.Create(model);
            TryAssertion(epService, stmtOrig);
    
            EPStatementObjectModel newModel = epService.EPAdministrator.CompileEPL(stmtOrigText);
            stmtOrig = epService.EPAdministrator.Create(newModel);
            Assert.AreEqual(stmtOrigText, newModel.ToEPL());
            TryAssertion(epService, stmtOrig);
    
            SupportModelHelper.CompileCreate(epService, stmtOrigText + " output all");
    
            epService.EPAdministrator.DestroyAllStatements();
        }
    
        private void RunAssertionSubquery(EPServiceProvider epService) {
            string stmtOrigText = "on SupportBean " +
                    "insert into AStreamSub select (select p00 from S0#lastevent) as string where intPrimitive=(select id from S0#lastevent) " +
                    "insert into BStreamSub select (select p01 from S0#lastevent) as string where intPrimitive<>(select id from S0#lastevent) or (select id from S0#lastevent) is null";
            EPStatement stmtOrig = epService.EPAdministrator.CreateEPL(stmtOrigText);
            var listener = new SupportUpdateListener();
            stmtOrig.AddListener(listener);
    
            EPStatement stmtOne = epService.EPAdministrator.CreateEPL("select * from AStreamSub");
            var listenerAStream = new SupportUpdateListener();
            stmtOne.AddListener(listenerAStream);
            EPStatement stmtTwo = epService.EPAdministrator.CreateEPL("select * from BStreamSub");
            var listenerBStream = new SupportUpdateListener();
            stmtTwo.AddListener(listenerBStream);
    
            SendSupportBean(epService, "E1", 1);
            Assert.IsFalse(listenerAStream.GetAndClearIsInvoked());
            Assert.IsNull(listenerBStream.AssertOneGetNewAndReset().Get("string"));
    
            epService.EPRuntime.SendEvent(new SupportBean_S0(10, "x", "y"));
    
            SendSupportBean(epService, "E2", 10);
            Assert.AreEqual("x", listenerAStream.AssertOneGetNewAndReset().Get("string"));
            Assert.IsFalse(listenerBStream.GetAndClearIsInvoked());
    
            SendSupportBean(epService, "E3", 9);
            Assert.IsFalse(listenerAStream.GetAndClearIsInvoked());
            Assert.AreEqual("y", listenerBStream.AssertOneGetNewAndReset().Get("string"));
    
            stmtOne.Dispose();
            stmtTwo.Dispose();
        }
    
        private void RunAssertion2SplitNoDefaultOutputAll(EPServiceProvider epService) {
            string stmtOrigText = "on SupportBean " +
                    "insert into AStream2S select theString where intPrimitive=1 " +
                    "insert into BStream2S select theString where intPrimitive=1 or intPrimitive=2 " +
                    "output all";
            EPStatement stmtOrig = epService.EPAdministrator.CreateEPL(stmtOrigText);
            var listener = new SupportUpdateListener();
            stmtOrig.AddListener(listener);
    
            SupportUpdateListener[] listeners = GetListeners();
            EPStatement stmtOne = epService.EPAdministrator.CreateEPL("select * from AStream2S");
            stmtOne.AddListener(listeners[0]);
            EPStatement stmtTwo = epService.EPAdministrator.CreateEPL("select * from BStream2S");
            stmtTwo.AddListener(listeners[1]);
    
            Assert.AreNotSame(stmtOne.EventType, stmtTwo.EventType);
            Assert.AreSame(stmtOne.EventType.UnderlyingType, stmtTwo.EventType.UnderlyingType);
    
            SendSupportBean(epService, "E1", 1);
            AssertReceivedEach(listeners, new[]{"E1", "E1"});
            Assert.IsFalse(listener.IsInvoked);
    
            SendSupportBean(epService, "E2", 2);
            AssertReceivedEach(listeners, new[]{null, "E2"});
            Assert.IsFalse(listener.IsInvoked);
    
            SendSupportBean(epService, "E3", 1);
            AssertReceivedEach(listeners, new[]{"E3", "E3"});
            Assert.IsFalse(listener.IsInvoked);
    
            SendSupportBean(epService, "E4", -999);
            AssertReceivedEach(listeners, new string[]{null, null});
            Assert.AreEqual("E4", listener.AssertOneGetNewAndReset().Get("theString"));
    
            stmtOrig.Dispose();
            stmtOrigText = "on SupportBean " +
                    "insert into AStream2S select theString || '_1' as theString where intPrimitive in (1, 2) " +
                    "insert into BStream2S select theString || '_2' as theString where intPrimitive in (2, 3) " +
                    "insert into CStream2S select theString || '_3' as theString " +
                    "output all";
            stmtOrig = epService.EPAdministrator.CreateEPL(stmtOrigText);
            stmtOrig.AddListener(listener);
    
            EPStatement stmtThree = epService.EPAdministrator.CreateEPL("select * from CStream2S");
            stmtThree.AddListener(listeners[2]);
    
            SendSupportBean(epService, "E1", 2);
            AssertReceivedEach(listeners, new[]{"E1_1", "E1_2", "E1_3"});
            Assert.IsFalse(listener.IsInvoked);
    
            SendSupportBean(epService, "E2", 1);
            AssertReceivedEach(listeners, new[]{"E2_1", null, "E2_3"});
            Assert.IsFalse(listener.IsInvoked);
    
            SendSupportBean(epService, "E3", 3);
            AssertReceivedEach(listeners, new[]{null, "E3_2", "E3_3"});
            Assert.IsFalse(listener.IsInvoked);
    
            SendSupportBean(epService, "E4", -999);
            AssertReceivedEach(listeners, new[]{null, null, "E4_3"});
            Assert.IsFalse(listener.IsInvoked);
    
            epService.EPAdministrator.DestroyAllStatements();
        }
    
        private void RunAssertion3And4SplitDefaultOutputFirst(EPServiceProvider epService) {
            string stmtOrigText = "on SupportBean as mystream " +
                    "insert into AStream34 select mystream.theString||'_1' as theString where intPrimitive=1 " +
                    "insert into BStream34 select mystream.theString||'_2' as theString where intPrimitive=2 " +
                    "insert into CStream34 select theString||'_3' as theString";
            EPStatement stmtOrig = epService.EPAdministrator.CreateEPL(stmtOrigText);
            var listener = new SupportUpdateListener();
            stmtOrig.AddListener(listener);
    
            SupportUpdateListener[] listeners = GetListeners();
            EPStatement stmtOne = epService.EPAdministrator.CreateEPL("select * from AStream34");
            stmtOne.AddListener(listeners[0]);
            EPStatement stmtTwo = epService.EPAdministrator.CreateEPL("select * from BStream34");
            stmtTwo.AddListener(listeners[1]);
            EPStatement stmtThree = epService.EPAdministrator.CreateEPL("select * from CStream34");
            stmtThree.AddListener(listeners[2]);

            Assert.AreNotSame(stmtOne.EventType, stmtTwo.EventType);
            Assert.AreSame(stmtOne.EventType.UnderlyingType, stmtTwo.EventType.UnderlyingType);
    
            SendSupportBean(epService, "E1", 1);
            AssertReceivedSingle(listeners, 0, "E1_1");
            Assert.IsFalse(listener.IsInvoked);
    
            SendSupportBean(epService, "E2", 2);
            AssertReceivedSingle(listeners, 1, "E2_2");
            Assert.IsFalse(listener.IsInvoked);
    
            SendSupportBean(epService, "E3", 1);
            AssertReceivedSingle(listeners, 0, "E3_1");
            Assert.IsFalse(listener.IsInvoked);
    
            SendSupportBean(epService, "E4", -999);
            AssertReceivedSingle(listeners, 2, "E4_3");
            Assert.IsFalse(listener.IsInvoked);
    
            stmtOrigText = "on SupportBean " +
                    "insert into AStream34 select theString||'_1' as theString where intPrimitive=10 " +
                    "insert into BStream34 select theString||'_2' as theString where intPrimitive=20 " +
                    "insert into CStream34 select theString||'_3' as theString where intPrimitive<0 " +
                    "insert into DStream34 select theString||'_4' as theString";
            stmtOrig.Dispose();
            stmtOrig = epService.EPAdministrator.CreateEPL(stmtOrigText);
            stmtOrig.AddListener(listener);
    
            EPStatement stmtFour = epService.EPAdministrator.CreateEPL("select * from DStream34");
            stmtFour.AddListener(listeners[3]);
    
            SendSupportBean(epService, "E5", -999);
            AssertReceivedSingle(listeners, 2, "E5_3");
            Assert.IsFalse(listener.IsInvoked);
    
            SendSupportBean(epService, "E6", 9999);
            AssertReceivedSingle(listeners, 3, "E6_4");
            Assert.IsFalse(listener.IsInvoked);
    
            SendSupportBean(epService, "E7", 20);
            AssertReceivedSingle(listeners, 1, "E7_2");
            Assert.IsFalse(listener.IsInvoked);
    
            SendSupportBean(epService, "E8", 10);
            AssertReceivedSingle(listeners, 0, "E8_1");
            Assert.IsFalse(listener.IsInvoked);
    
            epService.EPAdministrator.DestroyAllStatements();
        }
    
        private void AssertReceivedEach(SupportUpdateListener[] listeners, string[] stringValue) {
            for (int i = 0; i < stringValue.Length; i++) {
                if (stringValue[i] != null) {
                    Assert.AreEqual(stringValue[i], listeners[i].AssertOneGetNewAndReset().Get("theString"));
                } else {
                    Assert.IsFalse(listeners[i].IsInvoked);
                }
            }
        }
    
        private void AssertReceivedSingle(SupportUpdateListener[] listeners, int index, string stringValue) {
            for (int i = 0; i < listeners.Length; i++) {
                if (i == index) {
                    continue;
                }
                Assert.IsFalse(listeners[i].IsInvoked);
            }
            Assert.AreEqual(stringValue, listeners[index].AssertOneGetNewAndReset().Get("theString"));
        }
    
        private void AssertReceivedNone(SupportUpdateListener[] listeners) {
            for (int i = 0; i < listeners.Length; i++) {
                Assert.IsFalse(listeners[i].IsInvoked);
            }
        }
    
        private void SendSupportBean(EPServiceProvider epService, string theString, int intPrimitive) {
            var bean = new SupportBean();
            bean.TheString = theString;
            bean.IntPrimitive = intPrimitive;
            epService.EPRuntime.SendEvent(bean);
        }
    
        private void TryAssertion(EPServiceProvider epService, EPStatement stmtOrig) {
            var listener = new SupportUpdateListener();
            stmtOrig.AddListener(listener);
    
            SupportUpdateListener[] listeners = GetListeners();
            EPStatement stmtOne = epService.EPAdministrator.CreateEPL("select * from AStream2SP");
            stmtOne.AddListener(listeners[0]);
            EPStatement stmtTwo = epService.EPAdministrator.CreateEPL("select * from BStream2SP");
            stmtTwo.AddListener(listeners[1]);

            Assert.AreNotSame(stmtOne.EventType, stmtTwo.EventType);
            Assert.AreSame(stmtOne.EventType.UnderlyingType, stmtTwo.EventType.UnderlyingType);
    
            SendSupportBean(epService, "E1", 1);
            AssertReceivedSingle(listeners, 0, "E1");
            Assert.IsFalse(listener.IsInvoked);
    
            SendSupportBean(epService, "E2", 2);
            AssertReceivedSingle(listeners, 1, "E2");
            Assert.IsFalse(listener.IsInvoked);
    
            SendSupportBean(epService, "E3", 1);
            AssertReceivedSingle(listeners, 0, "E3");
            Assert.IsFalse(listener.IsInvoked);
    
            SendSupportBean(epService, "E4", -999);
            AssertReceivedNone(listeners);
            Assert.AreEqual("E4", listener.AssertOneGetNewAndReset().Get("theString"));
    
            stmtOrig.Dispose();
            stmtOne.Dispose();
            stmtTwo.Dispose();
        }
    
        private void TryAssertionSplitPremptiveNamedWindow(EPServiceProvider epService, EventRepresentationChoice eventRepresentationEnum) {
            epService.EPAdministrator.CreateEPL(eventRepresentationEnum.GetAnnotationText() + " create schema TypeTwo(col2 int)");
            epService.EPAdministrator.CreateEPL(eventRepresentationEnum.GetAnnotationText() + " create schema TypeTrigger(trigger int)");
            epService.EPAdministrator.CreateEPL(eventRepresentationEnum.GetAnnotationText() + " create window WinTwo#keepall as TypeTwo");
    
            string stmtOrigText = "on TypeTrigger " +
                    "insert into OtherStream select 1 " +
                    "insert into WinTwo(col2) select 2 " +
                    "output all";
            epService.EPAdministrator.CreateEPL(stmtOrigText);
    
            EPStatement stmt = epService.EPAdministrator.CreateEPL("on OtherStream select col2 from WinTwo");
            var listener = new SupportUpdateListener();
            stmt.AddListener(listener);
    
            // populate WinOne
            epService.EPRuntime.SendEvent(new SupportBean("E1", 2));
    
            // fire trigger
            if (eventRepresentationEnum.IsObjectArrayEvent()) {
                epService.EPRuntime.GetEventSender("TypeTrigger").SendEvent(new object[]{null});
            } else if (eventRepresentationEnum.IsMapEvent()) {
                epService.EPRuntime.GetEventSender("TypeTrigger").SendEvent(new Dictionary<string, object>());
            } else if (eventRepresentationEnum.IsAvroEvent()) {
                var @event = new GenericRecord(SchemaBuilder.Record("name", OptionalInt("trigger")));
                epService.EPRuntime.GetEventSender("TypeTrigger").SendEvent(@event);
            } else {
                Assert.Fail();
            }
    
            Assert.AreEqual(2, listener.AssertOneGetNewAndReset().Get("col2"));
    
            epService.EPAdministrator.DestroyAllStatements();
            foreach (string name in "TypeTwo,TypeTrigger,WinTwo,OtherStream".Split(',')) {
                epService.EPAdministrator.Configuration.RemoveEventType(name, true);
            }
        }
    
        private void TryAssertionFromClauseBeginBodyEnd(EPServiceProvider epService) {
            TryAssertionFromClauseBeginBodyEnd(epService, false);
            TryAssertionFromClauseBeginBodyEnd(epService, true);
        }
    
        private void TryAssertionFromClauseAsMultiple(EPServiceProvider epService) {
            TryAssertionFromClauseAsMultiple(epService, false);
            TryAssertionFromClauseAsMultiple(epService, true);
        }
    
        private void TryAssertionFromClauseAsMultiple(EPServiceProvider epService, bool soda) {
            string epl = "on OrderEvent as oe " +
                    "insert into StartEvent select oe.orderdetail.orderId as oi " +
                    "insert into ThenEvent select * from [select oe.orderdetail.orderId as oi, itemId from orderdetail.items] as item " +
                    "insert into MoreEvent select oe.orderdetail.orderId as oi, item.itemId as itemId from [select oe, * from orderdetail.items] as item " +
                    "output all";
            SupportModelHelper.CreateByCompileOrParse(epService, soda, epl);
    
            SupportUpdateListener[] listeners = GetListeners();
            epService.EPAdministrator.CreateEPL("select * from StartEvent").AddListener(listeners[0]);
            epService.EPAdministrator.CreateEPL("select * from ThenEvent").AddListener(listeners[1]);
            epService.EPAdministrator.CreateEPL("select * from MoreEvent").AddListener(listeners[2]);
    
            epService.EPRuntime.SendEvent(OrderBeanFactory.MakeEventOne());
            string[] fieldsOrderId = "oi".Split(',');
            string[] fieldsItems = "oi,itemId".Split(',');
            EPAssertionUtil.AssertProps(listeners[0].AssertOneGetNewAndReset(), fieldsOrderId, new object[]{"PO200901"});
            var expected = new[] {new object[] {"PO200901", "A001"}, new object[] {"PO200901", "A002"}, new object[] {"PO200901", "A003"}};
            EPAssertionUtil.AssertPropsPerRow(listeners[1].GetAndResetDataListsFlattened().First, fieldsItems, expected);
            EPAssertionUtil.AssertPropsPerRow(listeners[2].GetAndResetDataListsFlattened().First, fieldsItems, expected);
    
            epService.EPAdministrator.DestroyAllStatements();
        }
    
        private void TryAssertionFromClauseBeginBodyEnd(EPServiceProvider epService, bool soda) {
            string epl = "on OrderEvent " +
                    "insert into BeginEvent select orderdetail.orderId as orderId " +
                    "insert into OrderItem select * from [select orderdetail.orderId as orderId, * from orderdetail.items] " +
                    "insert into EndEvent select orderdetail.orderId as orderId " +
                    "output all";
            SupportModelHelper.CreateByCompileOrParse(epService, soda, epl);
    
            SupportUpdateListener[] listeners = GetListeners();
            epService.EPAdministrator.CreateEPL("select * from BeginEvent").AddListener(listeners[0]);
            epService.EPAdministrator.CreateEPL("select * from OrderItem").AddListener(listeners[1]);
            epService.EPAdministrator.CreateEPL("select * from EndEvent").AddListener(listeners[2]);
    
            EventType typeOrderItem = epService.EPAdministrator.Configuration.GetEventType("OrderItem");
            Assert.AreEqual("[amount, itemId, price, productId, orderId]", CompatExtensions.Render(typeOrderItem.PropertyNames));
    
            epService.EPRuntime.SendEvent(OrderBeanFactory.MakeEventOne());
            AssertFromClauseWContained(listeners, "PO200901", new[] {new object[] {"PO200901", "A001"}, new object[] {"PO200901", "A002"}, new object[] {"PO200901", "A003"}});
    
            epService.EPRuntime.SendEvent(OrderBeanFactory.MakeEventTwo());
            AssertFromClauseWContained(listeners, "PO200902", new[] {new object[] {"PO200902", "B001"}});
    
            epService.EPRuntime.SendEvent(OrderBeanFactory.MakeEventFour());
            AssertFromClauseWContained(listeners, "PO200904", new object[0][]);
    
            epService.EPAdministrator.DestroyAllStatements();
        }
    
        private void TryAssertionFromClauseOutputFirstWhere(EPServiceProvider epService) {
            TryAssertionFromClauseOutputFirstWhere(epService, false);
            TryAssertionFromClauseOutputFirstWhere(epService, true);
        }
    
        private void TryAssertionFromClauseOutputFirstWhere(EPServiceProvider epService, bool soda) {
            string[] fieldsOrderId = "oe.orderdetail.orderId".Split(',');
            string epl = "on OrderEvent as oe " +
                    "insert into HeaderEvent select orderdetail.orderId as orderId where 1=2 " +
                    "insert into StreamOne select * from [select oe, * from orderdetail.items] where productId=\"10020\" " +
                    "insert into StreamTwo select * from [select oe, * from orderdetail.items] where productId=\"10022\" " +
                    "insert into StreamThree select * from [select oe, * from orderdetail.items] where productId in (\"10020\",\"10025\",\"10022\")";
            SupportModelHelper.CreateByCompileOrParse(epService, soda, epl);
    
            SupportUpdateListener[] listeners = GetListeners();
            var listenerEPL = new[]{"select * from StreamOne", "select * from StreamTwo", "select * from StreamThree"};
            for (int i = 0; i < listenerEPL.Length; i++) {
                epService.EPAdministrator.CreateEPL(listenerEPL[i]).AddListener(listeners[i]);
                listeners[i].Reset();
            }
    
            epService.EPRuntime.SendEvent(OrderBeanFactory.MakeEventOne());
            EPAssertionUtil.AssertProps(listeners[0].AssertOneGetNewAndReset(), fieldsOrderId, new object[]{"PO200901"});
            Assert.IsFalse(listeners[1].IsInvoked);
            Assert.IsFalse(listeners[2].IsInvoked);
    
            epService.EPRuntime.SendEvent(OrderBeanFactory.MakeEventTwo());
            Assert.IsFalse(listeners[0].IsInvoked);
            EPAssertionUtil.AssertProps(listeners[1].AssertOneGetNewAndReset(), fieldsOrderId, new object[]{"PO200902"});
            Assert.IsFalse(listeners[2].IsInvoked);
    
            epService.EPRuntime.SendEvent(OrderBeanFactory.MakeEventThree());
            Assert.IsFalse(listeners[0].IsInvoked);
            Assert.IsFalse(listeners[1].IsInvoked);
            EPAssertionUtil.AssertProps(listeners[2].AssertOneGetNewAndReset(), fieldsOrderId, new object[]{"PO200903"});
    
            epService.EPRuntime.SendEvent(OrderBeanFactory.MakeEventFour());
            Assert.IsFalse(listeners[0].IsInvoked);
            Assert.IsFalse(listeners[1].IsInvoked);
            Assert.IsFalse(listeners[2].IsInvoked);
    
            epService.EPAdministrator.DestroyAllStatements();
        }
    
        private void TryAssertionFromClauseDocSample(EPServiceProvider epService) {
            string epl =
                    "create schema MyOrderItem(itemId string);\n" +
                            "create schema MyOrderEvent(orderId string, items MyOrderItem[]);\n" +
                            "on MyOrderEvent\n" +
                            "  insert into MyOrderBeginEvent select orderId\n" +
                            "  insert into MyOrderItemEvent select * from [select orderId, * from items]\n" +
                            "  insert into MyOrderEndEvent select orderId\n" +
                            "  output all;\n" +
                            "create context MyOrderContext \n" +
                            "  initiated by MyOrderBeginEvent as obe\n" +
                            "  terminated by MyOrderEndEvent(orderId = obe.orderId);\n" +
                            "@Name('count') context MyOrderContext select count(*) as orderItemCount from MyOrderItemEvent output when terminated;\n";
            DeploymentResult result = epService.EPAdministrator.DeploymentAdmin.ParseDeploy(epl);
    
            var listener = new SupportUpdateListener();
            epService.EPAdministrator.GetStatement("count").AddListener(listener);
    
            IDictionary<string, object> @event = new Dictionary<string, object>();
            @event.Put("orderId", "1010");
            @event.Put("items", new[] {Collections.SingletonDataMap("itemId", "A0001")});
            epService.EPRuntime.SendEvent(@event, "MyOrderEvent");
    
            Assert.AreEqual(1L, listener.AssertOneGetNewAndReset().Get("orderItemCount"));
    
            epService.EPAdministrator.DeploymentAdmin.Undeploy(result.DeploymentId);
        }
    
        private void AssertFromClauseWContained(SupportUpdateListener[] listeners, string orderId, object[][] expected) {
            string[] fieldsOrderId = "orderId".Split(',');
            string[] fieldsItems = "orderId,itemId".Split(',');
            EPAssertionUtil.AssertProps(listeners[0].AssertOneGetNewAndReset(), fieldsOrderId, new object[]{orderId});
            EPAssertionUtil.AssertPropsPerRow(listeners[1].GetAndResetDataListsFlattened().First, fieldsItems, expected);
            EPAssertionUtil.AssertProps(listeners[2].AssertOneGetNewAndReset(), fieldsOrderId, new object[]{orderId});
        }
    
        private SupportUpdateListener[] GetListeners() {
            var listeners = new SupportUpdateListener[10];
            for (int i = 0; i < listeners.Length; i++) {
                listeners[i] = new SupportUpdateListener();
            }
            return listeners;
        }
    }
} // end of namespace
