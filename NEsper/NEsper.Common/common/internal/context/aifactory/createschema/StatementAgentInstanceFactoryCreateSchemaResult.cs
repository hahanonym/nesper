///////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) 2006-2019 Esper Team. All rights reserved.                           /
// http://esper.codehaus.org                                                          /
// ---------------------------------------------------------------------------------- /
// The software in this package is published under the terms of the GPL license       /
// a copy of which has been included with this distribution in the license.txt file.  /
///////////////////////////////////////////////////////////////////////////////////////

using com.espertech.esper.common.@internal.context.aifactory.core;
using com.espertech.esper.common.@internal.context.util;
using com.espertech.esper.common.@internal.epl.subselect;
using com.espertech.esper.common.@internal.view.core;
using com.espertech.esper.compat.collections;

namespace com.espertech.esper.common.@internal.context.aifactory.createschema
{
    public class StatementAgentInstanceFactoryCreateSchemaResult : StatementAgentInstanceFactoryResult
    {
        public StatementAgentInstanceFactoryCreateSchemaResult(
            Viewable finalView,
            AgentInstanceStopCallback stopCallback,
            AgentInstanceContext agentInstanceContext)
            : base(
                finalView,
                stopCallback,
                agentInstanceContext,
                null,
                new EmptyDictionary<int, SubSelectFactoryResult>(),
                null,
                null,
                null,
                null,
                new EmptyList<StatementAgentInstancePreload>())
        {
        }
    }
} // end of namespace