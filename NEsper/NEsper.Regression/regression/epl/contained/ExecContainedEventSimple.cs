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
using com.espertech.esper.client.soda;
using com.espertech.esper.compat;
using com.espertech.esper.compat.collections;
using com.espertech.esper.compat.logging;
using com.espertech.esper.supportregression.bean.bookexample;
using com.espertech.esper.supportregression.bean.word;
using com.espertech.esper.supportregression.execution;

using static com.espertech.esper.supportregression.bean.bookexample.OrderBeanFactory;

using NUnit.Framework;

namespace com.espertech.esper.regression.epl.contained
{
    public class ExecContainedEventSimple : RegressionExecution {
        private static readonly string NEWLINE = Environment.NewLine;
    
        public override void Run(EPServiceProvider epService) {
            RunAssertionNamedWindowPremptive(epService);
            RunAssertionUnidirectionalJoin(epService);
            RunAssertionUnidirectionalJoinCount(epService);
            RunAssertionJoinCount(epService);
            RunAssertionJoin(epService);
            RunAssertionAloneCount(epService);
            RunAssertionPropertyAccess(epService);
            RunAssertionIRStreamArrayItem(epService);
            RunAssertionSplitWords(epService);
            RunAssertionArrayProperty(epService);
        }
    
        // Assures that the events inserted into the named window are preemptive to events generated by contained-event syntax.
        // This example generates 3 contained-events: One for each book.
        // It then inserts them into a named window to determine the highest price among all.
        // The named window updates first becoming useful to subsequent events (versus last and not useful).
        private void RunAssertionNamedWindowPremptive(EPServiceProvider epService) {
            string[] fields = "bookId".Split(',');
            epService.EPAdministrator.Configuration.AddEventType("OrderEvent", typeof(OrderBean));
            epService.EPAdministrator.Configuration.AddEventType("BookDesc", typeof(BookDesc));
    
            string stmtText = "insert into BookStream select * from OrderEvent[books]";
            EPStatement stmt = epService.EPAdministrator.CreateEPL(stmtText);
            var listener = new SupportUpdateListener();
            stmt.AddListener(listener);
    
            EPStatement stmtNW = epService.EPAdministrator.CreateEPL("create window MyWindow#lastevent as BookDesc");
            epService.EPAdministrator.CreateEPL("insert into MyWindow select * from BookStream bs where not Exists (select * from MyWindow mw where mw.price > bs.price)");
    
            epService.EPRuntime.SendEvent(MakeEventOne());
            EPAssertionUtil.AssertPropsPerRow(listener.LastNewData, fields, new Object[][]{new object[] {"10020"}, new object[] {"10021"}, new object[] {"10022"}});
            listener.Reset();
    
            // higest price (27 is the last value)
            EventBean theEvent = stmtNW.First();
            Assert.AreEqual(35.0, theEvent.Get("price"));
    
            epService.EPAdministrator.DestroyAllStatements();
        }
    
        private void RunAssertionUnidirectionalJoin(EPServiceProvider epService) {
            epService.EPAdministrator.Configuration.AddEventType("OrderEvent", typeof(OrderBean));
            string stmtText = "select * from " +
                    "OrderEvent as orderEvent unidirectional, " +
                    "OrderEvent[select * from books] as book, " +
                    "OrderEvent[select * from orderdetail.items] as item " +
                    "where book.bookId=item.productId " +
                    "order by book.bookId, item.amount";
            string stmtTextFormatted = "select *" + NEWLINE +
                    "from OrderEvent as orderEvent unidirectional," + NEWLINE +
                    "OrderEvent[select * from books] as book," + NEWLINE +
                    "OrderEvent[select * from orderdetail.items] as item" + NEWLINE +
                    "where book.bookId=item.productId" + NEWLINE +
                    "order by book.bookId, item.amount";
    
            EPStatement stmt = epService.EPAdministrator.CreateEPL(stmtText);
            var listener = new SupportUpdateListener();
            stmt.AddListener(listener);
    
            TryAssertionUnidirectionalJoin(epService, listener);
    
            stmt.Dispose();
            EPStatementObjectModel model = epService.EPAdministrator.CompileEPL(stmtText);
            Assert.AreEqual(stmtText, model.ToEPL());
            Assert.AreEqual(stmtTextFormatted, model.ToEPL(new EPStatementFormatter(true)));
            stmt = epService.EPAdministrator.Create(model);
            stmt.AddListener(listener);
    
            TryAssertionUnidirectionalJoin(epService, listener);
    
            stmt.Dispose();
        }
    
        private void TryAssertionUnidirectionalJoin(EPServiceProvider epService, SupportUpdateListener listener) {
            string[] fields = "orderEvent.orderdetail.orderId,book.bookId,book.title,item.amount".Split(',');
            epService.EPRuntime.SendEvent(MakeEventOne());
            Assert.AreEqual(3, listener.LastNewData.Length);
            EPAssertionUtil.AssertPropsPerRow(listener.LastNewData, fields, new Object[][]{new object[] {"PO200901", "10020", "Enders Game", 10}, new object[] {"PO200901", "10020", "Enders Game", 30}, new object[] {"PO200901", "10021", "Foundation 1", 25}});
            listener.Reset();
    
            epService.EPRuntime.SendEvent(MakeEventTwo());
            Assert.AreEqual(1, listener.LastNewData.Length);
            EPAssertionUtil.AssertPropsPerRow(listener.LastNewData, fields, new Object[][]{new object[] {"PO200902", "10022", "Stranger in a Strange Land", 5}});
            listener.Reset();
    
            epService.EPRuntime.SendEvent(MakeEventThree());
            Assert.AreEqual(1, listener.LastNewData.Length);
            EPAssertionUtil.AssertPropsPerRow(listener.LastNewData, fields, new Object[][]{new object[] {"PO200903", "10021", "Foundation 1", 50}});
        }
    
        private void RunAssertionUnidirectionalJoinCount(EPServiceProvider epService) {
            epService.EPAdministrator.Configuration.AddEventType("OrderEvent", typeof(OrderBean));
            string stmtText = "select count(*) from " +
                    "OrderEvent orderEvent unidirectional, " +
                    "OrderEvent[books] as book, " +
                    "OrderEvent[orderdetail.items] item " +
                    "where book.bookId = item.productId order by book.bookId asc, item.amount asc";
    
            EPStatement stmt = epService.EPAdministrator.CreateEPL(stmtText);
            var listener = new SupportUpdateListener();
            stmt.AddListener(listener);
    
            epService.EPRuntime.SendEvent(MakeEventOne());
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), "count(*)".Split(','), new Object[]{3L});
    
            epService.EPRuntime.SendEvent(MakeEventTwo());
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), "count(*)".Split(','), new Object[]{1L});
    
            epService.EPRuntime.SendEvent(MakeEventThree());
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), "count(*)".Split(','), new Object[]{1L});
    
            epService.EPRuntime.SendEvent(MakeEventFour());
            Assert.IsFalse(listener.IsInvoked);
    
            stmt.Dispose();
        }
    
        private void RunAssertionJoinCount(EPServiceProvider epService) {
            string[] fields = "count(*)".Split(',');
            epService.EPAdministrator.Configuration.AddEventType("OrderEvent", typeof(OrderBean));
            string stmtText = "select count(*) from " +
                    "OrderEvent[books]#unique(bookId) book, " +
                    "OrderEvent[orderdetail.items]#keepall item " +
                    "where book.bookId = item.productId";
    
            EPStatement stmt = epService.EPAdministrator.CreateEPL(stmtText);
            var listener = new SupportUpdateListener();
            stmt.AddListener(listener);
    
            epService.EPRuntime.SendEvent(MakeEventOne());
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{3L});
            EPAssertionUtil.AssertPropsPerRow(stmt.GetEnumerator(), fields, new Object[][]{new object[] {3L}});
    
            epService.EPRuntime.SendEvent(MakeEventTwo());
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), "count(*)".Split(','), new Object[]{4L});
            EPAssertionUtil.AssertPropsPerRow(stmt.GetEnumerator(), fields, new Object[][]{new object[] {4L}});
    
            epService.EPRuntime.SendEvent(MakeEventThree());
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), "count(*)".Split(','), new Object[]{5L});
            EPAssertionUtil.AssertPropsPerRow(stmt.GetEnumerator(), fields, new Object[][]{new object[] {5L}});
    
            epService.EPRuntime.SendEvent(MakeEventFour());
            Assert.IsFalse(listener.IsInvoked);
    
            epService.EPRuntime.SendEvent(MakeEventOne());
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), "count(*)".Split(','), new Object[]{8L});
            EPAssertionUtil.AssertPropsPerRow(stmt.GetEnumerator(), fields, new Object[][]{new object[] {8L}});
    
            stmt.Dispose();
        }
    
        private void RunAssertionJoin(EPServiceProvider epService) {
            string[] fields = "book.bookId,item.itemId,amount".Split(',');
            epService.EPAdministrator.Configuration.AddEventType("OrderEvent", typeof(OrderBean));
            string stmtText = "select book.bookId,item.itemId,amount from " +
                    "OrderEvent[books]#Firstunique(bookId) book, " +
                    "OrderEvent[orderdetail.items]#keepall item " +
                    "where book.bookId = item.productId " +
                    "order by book.bookId, item.itemId";
    
            EPStatement stmt = epService.EPAdministrator.CreateEPL(stmtText);
            var listener = new SupportUpdateListener();
            stmt.AddListener(listener);
    
            epService.EPRuntime.SendEvent(MakeEventOne());
            EPAssertionUtil.AssertPropsPerRow(listener.LastNewData, fields, new Object[][]{new object[] {"10020", "A001", 10}, new object[] {"10020", "A003", 30}, new object[] {"10021", "A002", 25}});
            EPAssertionUtil.AssertPropsPerRow(stmt.GetEnumerator(), fields, new Object[][]{new object[] {"10020", "A001", 10}, new object[] {"10020", "A003", 30}, new object[] {"10021", "A002", 25}});
            listener.Reset();
    
            epService.EPRuntime.SendEvent(MakeEventTwo());
            EPAssertionUtil.AssertPropsPerRow(listener.LastNewData, fields, new Object[][]{new object[] {"10022", "B001", 5}});
            EPAssertionUtil.AssertPropsPerRow(stmt.GetEnumerator(), fields, new Object[][]{new object[] {"10020", "A001", 10}, new object[] {"10020", "A003", 30}, new object[] {"10021", "A002", 25}, new object[] {"10022", "B001", 5}});
            listener.Reset();
    
            epService.EPRuntime.SendEvent(MakeEventThree());
            EPAssertionUtil.AssertPropsPerRow(listener.LastNewData, fields, new Object[][]{new object[] {"10021", "C001", 50}});
            EPAssertionUtil.AssertPropsPerRow(stmt.GetEnumerator(), fields, new Object[][]{new object[] {"10020", "A001", 10}, new object[] {"10020", "A003", 30}, new object[] {"10021", "A002", 25}, new object[] {"10021", "C001", 50}, new object[] {"10022", "B001", 5}});
            listener.Reset();
    
            epService.EPRuntime.SendEvent(MakeEventFour());
            Assert.IsFalse(listener.IsInvoked);
    
            stmt.Dispose();
        }
    
        private void RunAssertionAloneCount(EPServiceProvider epService) {
            string[] fields = "count(*)".Split(',');
            epService.EPAdministrator.Configuration.AddEventType("OrderEvent", typeof(OrderBean));
    
            string stmtText = "select count(*) from OrderEvent[books]";
            EPStatement stmt = epService.EPAdministrator.CreateEPL(stmtText);
            var listener = new SupportUpdateListener();
            stmt.AddListener(listener);
    
            epService.EPRuntime.SendEvent(MakeEventOne());
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{3L});
            EPAssertionUtil.AssertPropsPerRow(stmt.GetEnumerator(), fields, new Object[][]{new object[] {3L}});
    
            epService.EPRuntime.SendEvent(MakeEventFour());
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{5L});
            EPAssertionUtil.AssertPropsPerRow(stmt.GetEnumerator(), fields, new Object[][]{new object[] {5L}});
    
            stmt.Dispose();
        }
    
        private void RunAssertionPropertyAccess(EPServiceProvider epService) {
            epService.EPAdministrator.Configuration.AddEventType("OrderEvent", typeof(OrderBean));
    
            EPStatement stmtOne = epService.EPAdministrator.CreateEPL("@IterableUnbound select bookId from OrderEvent[books]");
            var listener = new SupportUpdateListener();
            stmtOne.AddListener(listener);
            EPStatement stmtTwo = epService.EPAdministrator.CreateEPL("@IterableUnbound select books[0].author as val from OrderEvent(books[0].bookId = '10020')");
    
            epService.EPRuntime.SendEvent(MakeEventOne());
            EPAssertionUtil.AssertPropsPerRow(listener.LastNewData, "bookId".Split(','), new Object[][]{new object[] {"10020"}, new object[] {"10021"}, new object[] {"10022"}});
            listener.Reset();
            EPAssertionUtil.AssertPropsPerRow(stmtOne.GetEnumerator(), "bookId".Split(','), new Object[][]{new object[] {"10020"}, new object[] {"10021"}, new object[] {"10022"}});
            EPAssertionUtil.AssertPropsPerRow(stmtTwo.GetEnumerator(), "val".Split(','), new Object[][]{new object[] {"Orson Scott Card"}});
    
            epService.EPRuntime.SendEvent(MakeEventFour());
            EPAssertionUtil.AssertPropsPerRow(listener.LastNewData, "bookId".Split(','), new Object[][]{new object[] {"10031"}, new object[] {"10032"}});
            listener.Reset();
            EPAssertionUtil.AssertPropsPerRow(stmtOne.GetEnumerator(), "bookId".Split(','), new Object[][]{new object[] {"10031"}, new object[] {"10032"}});
            EPAssertionUtil.AssertPropsPerRow(stmtTwo.GetEnumerator(), "val".Split(','), new Object[][]{new object[] {"Orson Scott Card"}});
    
            // add where clause
            stmtOne.Dispose();
            stmtTwo.Dispose();
            stmtOne = epService.EPAdministrator.CreateEPL("select bookId from OrderEvent[books where author='Orson Scott Card']");
            stmtOne.AddListener(listener);
            epService.EPRuntime.SendEvent(MakeEventOne());
            EPAssertionUtil.AssertPropsPerRow(listener.LastNewData, "bookId".Split(','), new Object[][]{new object[] {"10020"}});
            listener.Reset();
    
            stmtOne.Dispose();
        }
    
        private void RunAssertionIRStreamArrayItem(EPServiceProvider epService) {
            epService.EPAdministrator.Configuration.AddEventType("OrderEvent", typeof(OrderBean));
            string stmtText = "@IterableUnbound select irstream bookId from OrderEvent[books[0]]";
    
            EPStatement stmt = epService.EPAdministrator.CreateEPL(stmtText);
            var listener = new SupportUpdateListener();
            stmt.AddListener(listener);
    
            epService.EPRuntime.SendEvent(MakeEventOne());
            EPAssertionUtil.AssertPropsPerRow(listener.LastNewData, "bookId".Split(','), new Object[][]{new object[] {"10020"}});
            Assert.IsNull(listener.LastOldData);
            listener.Reset();
            EPAssertionUtil.AssertPropsPerRow(stmt.GetEnumerator(), "bookId".Split(','), new Object[][]{new object[] {"10020"}});
    
            epService.EPRuntime.SendEvent(MakeEventFour());
            Assert.IsNull(listener.LastOldData);
            EPAssertionUtil.AssertPropsPerRow(listener.LastNewData, "bookId".Split(','), new Object[][]{new object[] {"10031"}});
            listener.Reset();
            EPAssertionUtil.AssertPropsPerRow(stmt.GetEnumerator(), "bookId".Split(','), new Object[][]{new object[] {"10031"}});
    
            stmt.Dispose();
        }
    
        private void RunAssertionSplitWords(EPServiceProvider epService) {
            epService.EPAdministrator.Configuration.AddEventType(typeof(SentenceEvent));
            string stmtText = "insert into WordStream select * from SentenceEvent[words]";
    
            string[] fields = "word".Split(',');
            EPStatement stmt = epService.EPAdministrator.CreateEPL(stmtText);
            var listener = new SupportUpdateListener();
            stmt.AddListener(listener);
    
            epService.EPRuntime.SendEvent(new SentenceEvent("I am testing this"));
            EPAssertionUtil.AssertPropsPerRow(listener.GetAndResetLastNewData(), fields, new Object[][]{new object[] {"I"}, new object[] {"am"}, new object[] {"testing"}, new object[] {"this"}});
    
            stmt.Dispose();
        }
    
        private void RunAssertionArrayProperty(EPServiceProvider epService) {
            epService.EPAdministrator.Configuration.AddEventType(typeof(MyBeanWithArray));
            epService.EPAdministrator.CreateEPL("create objectarray schema ContainedId(id string)");
            EPStatement stmt = epService.EPAdministrator.CreateEPL("select * from MyBeanWithArray[select topId, * from containedIds @Type(ContainedId)]");
            var listener = new SupportUpdateListener();
            stmt.AddListener(listener);
            epService.EPRuntime.SendEvent(new MyBeanWithArray("A", "one,two,three".Split(',')));
            EPAssertionUtil.AssertPropsPerRow(listener.GetAndResetLastNewData(), "topId,id".Split(','),
                    new Object[][]{new object[] {"A", "one"}, new object[] {"A", "two"}, new object[] {"A", "three"}});
            stmt.Dispose();
        }
    
        public class MyBeanWithArray {
            private readonly string topId;
            private readonly string[] containedIds;
    
            public MyBeanWithArray(string topId, string[] containedIds) {
                this.topId = topId;
                this.containedIds = containedIds;
            }
    
            public string GetTopId() {
                return topId;
            }
    
            public string[] GetContainedIds() {
                return containedIds;
            }
        }
    }
} // end of namespace
