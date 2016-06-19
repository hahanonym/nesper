///////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) 2006-2015 Esper Team. All rights reserved.                           /
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
using com.espertech.esper.compat.logging;
using com.espertech.esper.metrics.instrumentation;
using com.espertech.esper.support.bean;
using com.espertech.esper.support.client;
using com.espertech.esper.util;

using NUnit.Framework;

namespace com.espertech.esper.regression.resultset
{
    [TestFixture]
	public class TestGroupByMedianAndDeviation 
	{
        private const string SYMBOL_DELL = "DELL";
        private const string SYMBOL_IBM = "IBM";

        private EPServiceProvider _epService;
	    private SupportUpdateListener _testListener;

        [SetUp]
	    public void SetUp()
	    {
	        _testListener = new SupportUpdateListener();
	        _epService = EPServiceProviderManager.GetDefaultProvider(SupportConfigFactory.GetConfiguration());
	        _epService.Initialize();
	        if (InstrumentationHelper.ENABLED) { InstrumentationHelper.StartTest(_epService, this.GetType(), this.GetType().FullName);}
	    }

        [TearDown]
	    public void TearDown() {
	        if (InstrumentationHelper.ENABLED) { InstrumentationHelper.EndTest();}
	        _testListener = null;
	    }

        [Test]
	    public void TestSumOneView()
	    {
	        var viewExpr = "select irstream Symbol," +
	                                 "median(all Price) as myMedian," +
	                                 "median(distinct Price) as myDistMedian," +
	                                 "stddev(all Price) as myStdev," +
	                                 "avedev(all Price) as myAvedev " +
	                          "from " + typeof(SupportMarketDataBean).FullName + ".win:length(5) " +
	                          "where Symbol='DELL' or Symbol='IBM' or Symbol='GE' " +
	                          "group by Symbol";

	        var selectTestView = _epService.EPAdministrator.CreateEPL(viewExpr);
	        selectTestView.AddListener(_testListener);

	        RunAssertion(selectTestView);

	        // Test NaN sensitivity
	        selectTestView.Dispose();
	        selectTestView = _epService.EPAdministrator.CreateEPL("select stddev(Price) as val from " + typeof(SupportMarketDataBean).FullName + ".win:length(3)");
	        selectTestView.AddListener(_testListener);

	        SendEvent("A", Double.NaN);
	        SendEvent("B", Double.NaN);
	        SendEvent("C", Double.NaN);
	        SendEvent("D", 1d);
	        SendEvent("E", 2d);
	        _testListener.Reset();
	        SendEvent("F", 3d);
            var result = _testListener.AssertOneGetNewAndReset().Get("val").AsDouble();
            Assert.That(result, Is.NaN);
	    }

        [Test]
	    public void TestSumJoin_OM()
	    {
	        var model = new EPStatementObjectModel();
	        model.SelectClause = SelectClause.Create("Symbol")
	                .Add(Expressions.Median("Price"), "myMedian")
	                .Add(Expressions.MedianDistinct("Price"), "myDistMedian")
	                .Add(Expressions.Stddev("Price"), "myStdev")
	                .Add(Expressions.Avedev("Price"), "myAvedev")
                    .SetStreamSelector(StreamSelector.RSTREAM_ISTREAM_BOTH);

	        var fromClause = FromClause.Create(
	                FilterStream.Create(typeof(SupportBeanString).FullName, "one").AddView(View.Create("win", "length", Expressions.Constant(100))),
	                FilterStream.Create(typeof(SupportMarketDataBean).FullName, "two").AddView(View.Create("win", "length", Expressions.Constant(5))));
	        model.FromClause = fromClause;
	        model.WhereClause = Expressions.And().Add(
	                    Expressions.Or()
	                    .Add(Expressions.Eq("Symbol", "DELL"))
	                    .Add(Expressions.Eq("Symbol", "IBM"))
	                    .Add(Expressions.Eq("Symbol", "GE"))
	                )
	                .Add(Expressions.EqProperty("one.TheString", "two.Symbol"));
	        model.GroupByClause = GroupByClause.Create("Symbol");
	        model = (EPStatementObjectModel) SerializableObjectCopier.Copy(model);

	        var viewExpr = "select irstream Symbol, " +
	                                 "median(Price) as myMedian, " +
	                                 "median(distinct Price) as myDistMedian, " +
	                                 "stddev(Price) as myStdev, " +
	                                 "avedev(Price) as myAvedev " +
	                          "from " + typeof(SupportBeanString).FullName + ".win:length(100) as one, " +
	                                    typeof(SupportMarketDataBean).FullName + ".win:length(5) as two " +
	                          "where (Symbol=\"DELL\" or Symbol=\"IBM\" or Symbol=\"GE\") " +
	                          "and one.TheString=two.Symbol " +
	                          "group by Symbol";
	        Assert.AreEqual(viewExpr, model.ToEPL());

	        var selectTestView = _epService.EPAdministrator.Create(model);
	        selectTestView.AddListener(_testListener);

	        _epService.EPRuntime.SendEvent(new SupportBeanString(SYMBOL_DELL));
	        _epService.EPRuntime.SendEvent(new SupportBeanString(SYMBOL_IBM));
	        _epService.EPRuntime.SendEvent(new SupportBeanString("AAA"));

	        RunAssertion(selectTestView);
	    }

        [Test]
	    public void TestSumJoin()
	    {
	        var viewExpr = "select irstream Symbol," +
	                                 "median(Price) as myMedian," +
	                                 "median(distinct Price) as myDistMedian," +
	                                 "stddev(Price) as myStdev," +
	                                 "avedev(Price) as myAvedev " +
	                          "from " + typeof(SupportBeanString).FullName + ".win:length(100) as one, " +
	                                    typeof(SupportMarketDataBean).FullName + ".win:length(5) as two " +
	                          "where (Symbol='DELL' or Symbol='IBM' or Symbol='GE') " +
	                          "       and one.TheString = two.Symbol " +
	                          "group by Symbol";

	        var selectTestView = _epService.EPAdministrator.CreateEPL(viewExpr);
	        selectTestView.AddListener(_testListener);

	        _epService.EPRuntime.SendEvent(new SupportBeanString(SYMBOL_DELL));
	        _epService.EPRuntime.SendEvent(new SupportBeanString(SYMBOL_IBM));
	        _epService.EPRuntime.SendEvent(new SupportBeanString("AAA"));

	        RunAssertion(selectTestView);
	    }

	    private void RunAssertion(EPStatement selectTestView)
	    {
	        // assert select result type
	        Assert.AreEqual(typeof(string), selectTestView.EventType.GetPropertyType("Symbol"));
	        Assert.AreEqual(typeof(double?), selectTestView.EventType.GetPropertyType("myMedian"));
	        Assert.AreEqual(typeof(double?), selectTestView.EventType.GetPropertyType("myDistMedian"));
	        Assert.AreEqual(typeof(double?), selectTestView.EventType.GetPropertyType("myStdev"));
	        Assert.AreEqual(typeof(double?), selectTestView.EventType.GetPropertyType("myAvedev"));

	        SendEvent(SYMBOL_DELL, 10);
	        AssertEvents(SYMBOL_DELL,
	                null, null, null, null,
	                10d, 10d, null, 0d);

	        SendEvent(SYMBOL_DELL, 20);
	        AssertEvents(SYMBOL_DELL,
	                10d, 10d, null, 0d,
	                15d, 15d, 7.071067812d, 5d);

	        SendEvent(SYMBOL_DELL, 20);
	        AssertEvents(SYMBOL_DELL,
	                15d, 15d, 7.071067812d, 5d,
	                20d, 15d, 5.773502692, 4.444444444444444);

	        SendEvent(SYMBOL_DELL, 90);
	        AssertEvents(SYMBOL_DELL,
	                20d, 15d, 5.773502692, 4.444444444444444,
	                20d, 20d, 36.96845502d, 27.5d);

	        SendEvent(SYMBOL_DELL, 5);
	        AssertEvents(SYMBOL_DELL,
	                20d, 20d, 36.96845502d, 27.5d,
	                20d, 15d, 34.71310992d, 24.4d);

	        SendEvent(SYMBOL_DELL, 90);
	        AssertEvents(SYMBOL_DELL,
	                20d, 15d, 34.71310992d, 24.4d,
	                20d, 20d, 41.53311931d, 36d);

	        SendEvent(SYMBOL_DELL, 30);
	        AssertEvents(SYMBOL_DELL,
	                20d, 20d, 41.53311931d, 36d,
	                30d, 25d, 40.24922359d, 34.4d);
	    }

        private void AssertEvents(
            string symbol,
            double? oldMedian,
            double? oldDistMedian,
            double? oldStdev,
            double? oldAvedev,
            double? newMedian,
            double? newDistMedian,
            double? newStdev,
            double? newAvedev)
	    {
	        var oldData = _testListener.LastOldData;
	        var newData = _testListener.LastNewData;

	        Assert.AreEqual(1, oldData.Length);
	        Assert.AreEqual(1, newData.Length);

	        Assert.AreEqual(symbol, oldData[0].Get("Symbol"));
	        Assert.AreEqual(oldMedian, oldData[0].Get("myMedian"));
	        Assert.AreEqual(oldDistMedian, oldData[0].Get("myDistMedian"));
	        Assert.AreEqual(oldAvedev, oldData[0].Get("myAvedev"));

	        var oldStdevResult = oldData[0].Get("myStdev").AsBoxedDouble();
	        if (oldStdevResult  == null)
	        {
	            Assert.IsNull(oldStdev);
	        }
	        else
	        {
	            Assert.AreEqual(Math.Round(oldStdev.Value * 1000), Math.Round(oldStdevResult.Value * 1000));
	        }

	        Assert.AreEqual(symbol, newData[0].Get("Symbol"));
	        Assert.AreEqual(newMedian, newData[0].Get("myMedian"));
	        Assert.AreEqual(newDistMedian, newData[0].Get("myDistMedian"));
	        Assert.AreEqual(newAvedev, newData[0].Get("myAvedev"));

	        var newStdevResult = newData[0].Get("myStdev").AsBoxedDouble();
	        if (newStdevResult == null)
	        {
	            Assert.IsNull(newStdev);
	        }
	        else
	        {
	            Assert.AreEqual(Math.Round(newStdev.Value * 1000), Math.Round(newStdevResult.Value * 1000));
	        }

	        _testListener.Reset();
	        Assert.IsFalse(_testListener.IsInvoked);
	    }

	    private void SendEvent(string symbol, double price)
	    {
	        var bean = new SupportMarketDataBean(symbol, price, 0L, null);
	        _epService.EPRuntime.SendEvent(bean);
	    }

        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	}
} // end of namespace
