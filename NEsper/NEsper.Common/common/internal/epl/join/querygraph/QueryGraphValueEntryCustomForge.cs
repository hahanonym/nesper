///////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) 2006-2019 Esper Team. All rights reserved.                           /
// http://esper.codehaus.org                                                          /
// ---------------------------------------------------------------------------------- /
// The software in this package is published under the terms of the GPL license       /
// a copy of which has been included with this distribution in the license.txt file.  /
///////////////////////////////////////////////////////////////////////////////////////

using System.Collections.Generic;
using com.espertech.esper.common.@internal.bytecodemodel.@base;
using com.espertech.esper.common.@internal.bytecodemodel.model.expression;
using com.espertech.esper.common.@internal.context.aifactory.core;
using com.espertech.esper.common.@internal.util;
using com.espertech.esper.compat.collections;
using static com.espertech.esper.common.@internal.bytecodemodel.model.expression.CodegenExpressionBuilder;

namespace com.espertech.esper.common.@internal.epl.join.querygraph
{
    public class QueryGraphValueEntryCustomForge : QueryGraphValueEntryForge
    {
        private readonly IDictionary<QueryGraphValueEntryCustomKeyForge, QueryGraphValueEntryCustomOperationForge> operations =
            new LinkedHashMap<QueryGraphValueEntryCustomKeyForge, QueryGraphValueEntryCustomOperationForge>();

        public CodegenExpression Make(
            CodegenMethodScope parent,
            SAIFFInitializeSymbol symbols,
            CodegenClassScope classScope)
        {
            var method = parent.MakeChild(typeof(QueryGraphValueEntryCustom), GetType(), classScope);

            CodegenExpression map;
            if (operations.IsEmpty()) {
                map = StaticMethod(typeof(Collections), "emptyMap");
            }
            else {
                method.Block.DeclareVar(
                    typeof(IDictionary<string, object>), "map",
                    NewInstance(typeof(LinkedHashMap<string, object>), Constant(CollectionUtil.CapacityHashMap(operations.Count))));
                foreach (var entry in operations) {
                    method.Block.ExprDotMethod(
                        Ref("map"), "put", entry.Key.Make(parent, symbols, classScope),
                        entry.Value.Make(parent, symbols, classScope));
                }

                map = Ref("map");
            }

            method.Block
                .DeclareVar(
                    typeof(QueryGraphValueEntryCustom), "custom", NewInstance(typeof(QueryGraphValueEntryCustom)))
                .SetProperty(Ref("custom"), "Operations", map)
                .MethodReturn(Ref("custom"));
            return LocalMethod(method);
        }

        public IDictionary<QueryGraphValueEntryCustomKeyForge, QueryGraphValueEntryCustomOperationForge> Operations => operations;

        public void MergeInto(
            IDictionary<QueryGraphValueEntryCustomKeyForge, QueryGraphValueEntryCustomOperationForge> customIndexOps)
        {
            foreach (var operation in operations) {
                var existing = customIndexOps.Get(operation.Key);
                if (existing == null) {
                    customIndexOps.Put(operation.Key, operation.Value);
                    continue;
                }

                existing.PositionalExpressions.PutAll(operation.Value.PositionalExpressions);
            }
        }
    }
} // end of namespace