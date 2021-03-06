///////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) 2006-2019 Esper Team. All rights reserved.                           /
// http://esper.codehaus.org                                                          /
// ---------------------------------------------------------------------------------- /
// The software in this package is published under the terms of the GPL license       /
// a copy of which has been included with this distribution in the license.txt file.  /
///////////////////////////////////////////////////////////////////////////////////////

using System.Collections.Generic;
using System.Linq;

using com.espertech.esper.common.client;
using com.espertech.esper.common.@internal.collection;
using com.espertech.esper.compat.collections;

namespace com.espertech.esper.common.@internal.epl.resultset.rowpergroup
{
    public class ResultSetProcessorRowPerGroupOutputAllHelperImpl : ResultSetProcessorRowPerGroupOutputAllHelper
    {
        private readonly IDictionary<object, EventBean[]> groupReps = new LinkedHashMap<object, EventBean[]>();

        private readonly IDictionary<object, EventBean> groupRepsOutputLastUnordRStream =
            new LinkedHashMap<object, EventBean>();

        internal readonly ResultSetProcessorRowPerGroup processor;
        private bool first;

        public ResultSetProcessorRowPerGroupOutputAllHelperImpl(ResultSetProcessorRowPerGroup processor)
        {
            this.processor = processor;
        }

        public void ProcessView(
            EventBean[] newData,
            EventBean[] oldData,
            bool isGenerateSynthetic)
        {
            GenerateRemoveStreamJustOnce(isGenerateSynthetic, false);

            if (newData != null) {
                foreach (var aNewData in newData) {
                    EventBean[] eventsPerStream = {aNewData};
                    var mk = processor.GenerateGroupKeySingle(eventsPerStream, true);
                    groupReps.Put(mk, eventsPerStream);

                    if (processor.IsSelectRStream && !groupRepsOutputLastUnordRStream.ContainsKey(mk)) {
                        var @event = processor.GenerateOutputBatchedNoSortWMap(
                            false,
                            mk,
                            eventsPerStream,
                            true,
                            isGenerateSynthetic);
                        if (@event != null) {
                            groupRepsOutputLastUnordRStream.Put(mk, @event);
                        }
                    }

                    processor.AggregationService.ApplyEnter(eventsPerStream, mk, processor.GetAgentInstanceContext());
                }
            }

            if (oldData != null) {
                foreach (var anOldData in oldData) {
                    EventBean[] eventsPerStream = {anOldData};
                    var mk = processor.GenerateGroupKeySingle(eventsPerStream, true);

                    if (processor.IsSelectRStream && !groupRepsOutputLastUnordRStream.ContainsKey(mk)) {
                        var @event = processor.GenerateOutputBatchedNoSortWMap(
                            false,
                            mk,
                            eventsPerStream,
                            false,
                            isGenerateSynthetic);
                        if (@event != null) {
                            groupRepsOutputLastUnordRStream.Put(mk, @event);
                        }
                    }

                    processor.AggregationService.ApplyLeave(eventsPerStream, mk, processor.GetAgentInstanceContext());
                }
            }
        }

        public void ProcessJoin(
            ISet<MultiKey<EventBean>> newData,
            ISet<MultiKey<EventBean>> oldData,
            bool isGenerateSynthetic)
        {
            GenerateRemoveStreamJustOnce(isGenerateSynthetic, true);

            if (newData != null) {
                foreach (var aNewData in newData) {
                    var mk = processor.GenerateGroupKeySingle(aNewData.Array, true);
                    groupReps.Put(mk, aNewData.Array);

                    if (processor.IsSelectRStream && !groupRepsOutputLastUnordRStream.ContainsKey(mk)) {
                        var @event = processor.GenerateOutputBatchedNoSortWMap(
                            true,
                            mk,
                            aNewData.Array,
                            true,
                            isGenerateSynthetic);
                        if (@event != null) {
                            groupRepsOutputLastUnordRStream.Put(mk, @event);
                        }
                    }

                    processor.AggregationService.ApplyEnter(aNewData.Array, mk, processor.GetAgentInstanceContext());
                }
            }

            if (oldData != null) {
                foreach (var anOldData in oldData) {
                    var mk = processor.GenerateGroupKeySingle(anOldData.Array, false);
                    if (processor.IsSelectRStream && !groupRepsOutputLastUnordRStream.ContainsKey(mk)) {
                        var @event = processor.GenerateOutputBatchedNoSortWMap(
                            true,
                            mk,
                            anOldData.Array,
                            false,
                            isGenerateSynthetic);
                        if (@event != null) {
                            groupRepsOutputLastUnordRStream.Put(mk, @event);
                        }
                    }

                    processor.AggregationService.ApplyLeave(anOldData.Array, mk, processor.GetAgentInstanceContext());
                }
            }
        }

        public UniformPair<EventBean[]> OutputView(bool isSynthesize)
        {
            GenerateRemoveStreamJustOnce(isSynthesize, false);
            return Output(isSynthesize, false);
        }

        public UniformPair<EventBean[]> OutputJoin(bool isSynthesize)
        {
            GenerateRemoveStreamJustOnce(isSynthesize, true);
            return Output(isSynthesize, true);
        }

        public void Destroy()
        {
            // no action required
        }

        private UniformPair<EventBean[]> Output(
            bool isSynthesize,
            bool join)
        {
            // generate latest new-events from group representatives
            IList<EventBean> newEvents = new List<EventBean>(4);
            processor.GenerateOutputBatchedArrFromIterator(
                join,
                groupReps.GetEnumerator(),
                true,
                isSynthesize,
                newEvents,
                null);
            var newEventsArr = newEvents.IsEmpty() ? null : newEvents.ToArray();

            // use old-events as retained, if any
            EventBean[] oldEventsArr = null;
            if (!groupRepsOutputLastUnordRStream.IsEmpty()) {
                var oldEvents = groupRepsOutputLastUnordRStream.Values;
                oldEventsArr = oldEvents.ToArray();
                groupRepsOutputLastUnordRStream.Clear();
            }

            first = true;

            if (newEventsArr == null && oldEventsArr == null) {
                return null;
            }

            return new UniformPair<EventBean[]>(newEventsArr, oldEventsArr);
        }

        private void GenerateRemoveStreamJustOnce(
            bool isSynthesize,
            bool join)
        {
            if (first && processor.IsSelectRStream) {
                foreach (var groupRep in groupReps) {
                    var mk = processor.GenerateGroupKeySingle(groupRep.Value, false);
                    var @event = processor.GenerateOutputBatchedNoSortWMap(
                        join,
                        mk,
                        groupRep.Value,
                        false,
                        isSynthesize);
                    if (@event != null) {
                        groupRepsOutputLastUnordRStream.Put(mk, @event);
                    }
                }
            }

            first = false;
        }
    }
} // end of namespace