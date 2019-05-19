///////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) 2006-2019 Esper Team. All rights reserved.                           /
// http://esper.codehaus.org                                                          /
// ---------------------------------------------------------------------------------- /
// The software in this package is published under the terms of the GPL license       /
// a copy of which has been included with this distribution in the license.txt file.  /
///////////////////////////////////////////////////////////////////////////////////////

using com.espertech.esper.common.client;
using com.espertech.esper.common.@internal.compile.stage1.spec;
using com.espertech.esper.common.@internal.context.util;
using com.espertech.esper.common.@internal.epl.output.condition;
using com.espertech.esper.common.@internal.epl.output.core;
using com.espertech.esper.common.@internal.epl.resultset.core;

namespace com.espertech.esper.common.@internal.epl.output.view
{
    /// <summary>
    ///     A view that handles the "output snapshot" keyword in output rate stabilizing.
    /// </summary>
    public class OutputProcessViewConditionFactory : OutputProcessViewDirectDistinctOrAfterFactory
    {
        private readonly ResultSetProcessorOutputConditionType conditionType;
        private readonly EventType[] eventTypes;

        public OutputProcessViewConditionFactory(OutputProcessViewConditionSpec spec)
            : base(spec.PostProcessFactory, spec.IsDistinct, spec.AfterTimePeriod, spec.AfterConditionNumberOfEvents, spec.ResultEventType)
        {
            OutputConditionFactory = spec.OutputConditionFactory;
            StreamCount = spec.StreamCount;
            conditionType = spec.ConditionType;
            IsTerminable = spec.IsTerminable;
            IsAfter = spec.HasAfter;
            IsUnaggregatedUngrouped = spec.IsUnaggregatedUngrouped;
            SelectClauseStreamSelectorEnum = spec.SelectClauseStreamSelector;
            eventTypes = spec.EventTypes;
        }

        public OutputConditionFactory OutputConditionFactory { get; }

        public int StreamCount { get; }

        public bool IsTerminable { get; }

        public bool IsAfter { get; }

        public bool IsUnaggregatedUngrouped { get; }

        public SelectClauseStreamSelectorEnum SelectClauseStreamSelectorEnum { get; }

        public override OutputProcessView MakeView(
            ResultSetProcessor resultSetProcessor,
            AgentInstanceContext agentInstanceContext)
        {
            // determine after-stuff
            var isAfterConditionSatisfied = true;
            long? afterConditionTime = null;
            if (afterConditionNumberOfEvents != null) {
                isAfterConditionSatisfied = false;
            }
            else if (afterTimePeriod != null) {
                isAfterConditionSatisfied = false;
                var time = agentInstanceContext.TimeProvider.Time;
                var delta = afterTimePeriod.DeltaAdd(time, null, true, agentInstanceContext);
                afterConditionTime = time + delta;
            }

            if (conditionType == ResultSetProcessorOutputConditionType.SNAPSHOT) {
                if (postProcessFactory == null) {
                    return new OutputProcessViewConditionSnapshot(
                        resultSetProcessor, afterConditionTime, afterConditionNumberOfEvents, isAfterConditionSatisfied, this, agentInstanceContext);
                }

                var postProcess = postProcessFactory.Make(agentInstanceContext);
                return new OutputProcessViewConditionSnapshotPostProcess(
                    resultSetProcessor, afterConditionTime, afterConditionNumberOfEvents, isAfterConditionSatisfied, this, agentInstanceContext,
                    postProcess);
            }

            if (conditionType == ResultSetProcessorOutputConditionType.POLICY_FIRST) {
                if (postProcessFactory == null) {
                    return new OutputProcessViewConditionFirst(
                        resultSetProcessor, afterConditionTime, afterConditionNumberOfEvents, isAfterConditionSatisfied, this, agentInstanceContext);
                }

                var postProcess = postProcessFactory.Make(agentInstanceContext);
                return new OutputProcessViewConditionFirstPostProcess(
                    resultSetProcessor, afterConditionTime, afterConditionNumberOfEvents, isAfterConditionSatisfied, this, agentInstanceContext,
                    postProcess);
            }

            if (conditionType == ResultSetProcessorOutputConditionType.POLICY_LASTALL_UNORDERED) {
                if (postProcessFactory == null) {
                    return new OutputProcessViewConditionLastAllUnord(
                        resultSetProcessor, afterConditionTime, afterConditionNumberOfEvents, isAfterConditionSatisfied, this, agentInstanceContext);
                }

                var postProcess = postProcessFactory.Make(agentInstanceContext);
                return new OutputProcessViewConditionLastAllUnordPostProcessAll(
                    resultSetProcessor, afterConditionTime, afterConditionNumberOfEvents, isAfterConditionSatisfied, this, agentInstanceContext,
                    postProcess);
            }
            else {
                if (postProcessFactory == null) {
                    return new OutputProcessViewConditionDefault(
                        resultSetProcessor, afterConditionTime, afterConditionNumberOfEvents, isAfterConditionSatisfied, this, agentInstanceContext,
                        StreamCount > 1, eventTypes);
                }

                var postProcess = postProcessFactory.Make(agentInstanceContext);
                return new OutputProcessViewConditionDefaultPostProcess(
                    resultSetProcessor, afterConditionTime, afterConditionNumberOfEvents, isAfterConditionSatisfied, this, agentInstanceContext,
                    postProcess, StreamCount > 1, eventTypes);
            }
        }
    }
} // end of namespace