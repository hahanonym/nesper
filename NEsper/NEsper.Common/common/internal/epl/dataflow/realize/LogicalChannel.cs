///////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) 2006-2019 Esper Team. All rights reserved.                           /
// http://esper.codehaus.org                                                          /
// ---------------------------------------------------------------------------------- /
// The software in this package is published under the terms of the GPL license       /
// a copy of which has been included with this distribution in the license.txt file.  /
///////////////////////////////////////////////////////////////////////////////////////

using com.espertech.esper.common.@internal.bytecodemodel.@base;
using com.espertech.esper.common.@internal.bytecodemodel.model.expression;
using com.espertech.esper.common.@internal.context.aifactory.core;

namespace com.espertech.esper.common.@internal.epl.dataflow.realize
{
    /// <summary>Models a pipe between two operators. </summary>
    public class LogicalChannel
    {
        public LogicalChannel(
            int channelId,
            string consumingOpName,
            int consumingOpNum,
            int consumingOpStreamNum,
            string consumingOpStreamName,
            string consumingOptStreamAliasName,
            string consumingOpPrettyPrint,
            LogicalChannelProducingPortCompiled outputPort)
        {
            ChannelId = channelId;
            ConsumingOpName = consumingOpName;
            ConsumingOpNum = consumingOpNum;
            ConsumingOpStreamNum = consumingOpStreamNum;
            ConsumingOpStreamName = consumingOpStreamName;
            ConsumingOptStreamAliasName = consumingOptStreamAliasName;
            ConsumingOpPrettyPrint = consumingOpPrettyPrint;
            OutputPort = outputPort;
        }

        public int ChannelId { get; }

        public string ConsumingOpName { get; }

        public string ConsumingOpStreamName { get; }

        public string ConsumingOptStreamAliasName { get; }

        public int ConsumingOpStreamNum { get; }

        public int ConsumingOpNum { get; }

        public LogicalChannelProducingPortCompiled OutputPort { get; }

        public string ConsumingOpPrettyPrint { get; }

        public override string ToString()
        {
            return "LogicalChannel{" +
                   "channelId=" + ChannelId +
                   ", produced=" + OutputPort +
                   ", consumed='" + ConsumingOpPrettyPrint + '\'' +
                   '}';
        }

        public CodegenExpression Make(
            CodegenMethodScope parent,
            SAIFFInitializeSymbol symbols,
            CodegenClassScope classScope)
        {
            return new SAIFFInitializeBuilder(typeof(LogicalChannel), GetType(), "lc", parent, symbols, classScope)
                .Constant("channelId", ChannelId)
                .Constant("consumingOpName", ConsumingOpName)
                .Constant("consumingOpNum", ConsumingOpNum)
                .Constant("consumingOpStreamNum", ConsumingOpStreamNum)
                .Constant("consumingOpStreamName", ConsumingOpStreamName)
                .Constant("consumingOptStreamAliasName", ConsumingOptStreamAliasName)
                .Constant("consumingOpPrettyPrint", ConsumingOpPrettyPrint)
                .Method("outputPort", method => OutputPort.Make(method, symbols, classScope))
                .Build();
        }
    }
}