///////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) 2006-2019 Esper Team. All rights reserved.                           /
// http://esper.codehaus.org                                                          /
// ---------------------------------------------------------------------------------- /
// The software in this package is published under the terms of the GPL license       /
// a copy of which has been included with this distribution in the license.txt file.  /
///////////////////////////////////////////////////////////////////////////////////////

using System.Collections.Generic;
using com.espertech.esper.common.client;
using com.espertech.esper.common.@internal.collection;
using com.espertech.esper.common.@internal.context.util;
using com.espertech.esper.common.@internal.epl.@join.@base;
using com.espertech.esper.common.@internal.epl.resultset.core;
using com.espertech.esper.common.@internal.statement.dispatch;
using com.espertech.esper.common.@internal.view.core;
using com.espertech.esper.common.@internal.view.util;

namespace com.espertech.esper.common.@internal.epl.output.core
{
    public class OutputStrategyUtil
    {
        public static void Output(
            bool forceUpdate,
            UniformPair<EventBean[]> result,
            UpdateDispatchView finalView)
        {
            var newEvents = result != null ? result.First : null;
            var oldEvents = result != null ? result.Second : null;
            if (newEvents != null || oldEvents != null) {
                finalView.NewResult(result);
            }
            else if (forceUpdate) {
                finalView.NewResult(result);
            }
        }

        /// <summary>
        ///     Indicate statement result.
        /// </summary>
        /// <param name="newOldEvents">result</param>
        /// <param name="statementContext">context</param>
        public static void IndicateEarlyReturn(
            StatementContext statementContext,
            UniformPair<EventBean[]> newOldEvents)
        {
            // no action
        }

        /// <summary>
        ///     NOTE: Code-generation-invoked method, method name and parameter order matters
        /// </summary>
        /// <param name="joinExecutionStrategy">join strategy</param>
        /// <param name="resultSetProcessor">processor</param>
        /// <param name="parentView">view</param>
        /// <param name="distinct">flag</param>
        /// <returns>iterator</returns>
        public static IEnumerator<EventBean> GetIterator(
            JoinExecutionStrategy joinExecutionStrategy,
            ResultSetProcessor resultSetProcessor,
            Viewable parentView,
            bool distinct)
        {
            IEnumerator<EventBean> iterator;
            EventType eventType;
            if (joinExecutionStrategy != null) {
                var joinSet = joinExecutionStrategy.StaticJoin();
                iterator = resultSetProcessor.GetEnumerator(joinSet);
                eventType = resultSetProcessor.ResultEventType;
            }
            else if (resultSetProcessor != null) {
                iterator = resultSetProcessor.GetEnumerator(parentView);
                eventType = resultSetProcessor.ResultEventType;
            }
            else {
                iterator = parentView.GetEnumerator();
                eventType = parentView.EventType;
            }

            if (!distinct) {
                return iterator;
            }

            return EventDistinctEnumerator.For(iterator, eventType);
        }
    }
} // end of namespace