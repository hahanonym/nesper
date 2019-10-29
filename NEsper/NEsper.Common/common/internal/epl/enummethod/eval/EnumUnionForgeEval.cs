///////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) 2006-2019 Esper Team. All rights reserved.                           /
// http://esper.codehaus.org                                                          /
// ---------------------------------------------------------------------------------- /
// The software in this package is published under the terms of the GPL license       /
// a copy of which has been included with this distribution in the license.txt file.  /
///////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;

using com.espertech.esper.common.client;
using com.espertech.esper.common.@internal.bytecodemodel.@base;
using com.espertech.esper.common.@internal.bytecodemodel.model.expression;
using com.espertech.esper.common.@internal.epl.enummethod.codegen;
using com.espertech.esper.common.@internal.epl.expression.codegen;
using com.espertech.esper.common.@internal.epl.expression.core;
using com.espertech.esper.compat;
using com.espertech.esper.compat.collections;

using static com.espertech.esper.common.@internal.bytecodemodel.model.expression.CodegenExpressionBuilder;

namespace com.espertech.esper.common.@internal.epl.enummethod.eval
{
    public class EnumUnionForgeEval : EnumEval
    {
        private readonly EnumUnionForge forge;
        private readonly ExprEnumerationEval evaluator;

        public EnumUnionForgeEval(
            EnumUnionForge forge,
            ExprEnumerationEval evaluator)
        {
            this.forge = forge;
            this.evaluator = evaluator;
        }

        private object EvaluateEnumMethodInternal<T>(
            ICollection<T> other,
            ICollection<object> enumcoll)
        {
            if (other == null || other.IsEmpty()) {
                return enumcoll;
            }

            var result = new List<object>(enumcoll.Count + other.Count);
            result.AddAll(enumcoll);
            result.AddAll(other.UnwrapEnumerable<object>());

            return result;
        }

        public object EvaluateEnumMethod(
            EventBean[] eventsLambda,
            ICollection<object> enumcoll,
            bool isNewData,
            ExprEvaluatorContext context)
        {
            if (forge.scalar) {
                return EvaluateEnumMethodInternal(
                    evaluator.EvaluateGetROCollectionScalar(eventsLambda, isNewData, context),
                    enumcoll);
            }

            return EvaluateEnumMethodInternal(
                evaluator.EvaluateGetROCollectionEvents(eventsLambda, isNewData, context),
                enumcoll);
        }

        public static CodegenExpression Codegen(
            EnumUnionForge forge,
            EnumForgeCodegenParams args,
            CodegenMethodScope codegenMethodScope,
            CodegenClassScope codegenClassScope)
        {
            var scope = new ExprForgeCodegenSymbol(false, null);
            var methodNode = codegenMethodScope
                .MakeChildWithScope(typeof(ICollection<EventBean>), typeof(EnumUnionForgeEval), scope, codegenClassScope)
                .AddParam(EnumForgeCodegenNames.PARAMS_EVENTBEAN);

            var block = methodNode.Block;
            if (forge.scalar) {
                block.DeclareVar<ICollection<object>>(
                    "other",
                    forge.evaluatorForge.EvaluateGetROCollectionScalarCodegen(methodNode, scope, codegenClassScope));
            }
            else {
                block.DeclareVar<ICollection<EventBean>>(
                    "other",
                    forge.evaluatorForge.EvaluateGetROCollectionEventsCodegen(methodNode, scope, codegenClassScope));
            }

            block.IfCondition(Or(EqualsNull(@Ref("other")), ExprDotMethod(@Ref("other"), "IsEmpty")))
                .BlockReturn(EnumForgeCodegenNames.REF_ENUMCOLL);
            block.DeclareVar<List<EventBean>>("result", NewInstance<List<EventBean>>())
                .Expression(ExprDotMethod(@Ref("result"), "AddAll", EnumForgeCodegenNames.REF_ENUMCOLL))
                .Expression(ExprDotMethod(@Ref("result"), "AddAll", @Ref("other")))
                .MethodReturn(@Ref("result"));
            return LocalMethod(methodNode, args.Eps, args.Enumcoll, args.IsNewData, args.ExprCtx);
        }
    }
} // end of namespace