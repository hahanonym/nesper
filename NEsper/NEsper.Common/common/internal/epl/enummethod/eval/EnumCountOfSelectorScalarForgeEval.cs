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
using com.espertech.esper.common.@internal.context.module;
using com.espertech.esper.common.@internal.epl.enummethod.codegen;
using com.espertech.esper.common.@internal.epl.expression.codegen;
using com.espertech.esper.common.@internal.epl.expression.core;
using com.espertech.esper.common.@internal.@event.arr;
using com.espertech.esper.common.@internal.@event.core;
using com.espertech.esper.compat;
using com.espertech.esper.compat.collections;

using static com.espertech.esper.common.@internal.bytecodemodel.model.expression.CodegenExpressionBuilder;

namespace com.espertech.esper.common.@internal.epl.enummethod.eval
{
    public class EnumCountOfSelectorScalarForgeEval : EnumEval
    {
        private readonly EnumCountOfSelectorScalarForge _forge;
        private readonly ExprEvaluator _innerExpression;

        public EnumCountOfSelectorScalarForgeEval(
            EnumCountOfSelectorScalarForge forge,
            ExprEvaluator innerExpression)
        {
            _forge = forge;
            _innerExpression = innerExpression;
        }

        public object EvaluateEnumMethod(
            EventBean[] eventsLambda,
            ICollection<object> enumcoll,
            bool isNewData,
            ExprEvaluatorContext context)
        {
            var count = 0;

            var evalEvent = new ObjectArrayEventBean(new object[1], _forge.type);
            eventsLambda[_forge.StreamNumLambda] = evalEvent;
            var props = evalEvent.Properties;

            foreach (var next in enumcoll) {
                props[0] = next;

                var pass = _innerExpression.Evaluate(eventsLambda, isNewData, context);
                if (pass == null || false.Equals(pass)) {
                    continue;
                }

                count++;
            }

            return count;
        }

        public static CodegenExpression Codegen(
            EnumCountOfSelectorScalarForge forge,
            EnumForgeCodegenParams args,
            CodegenMethodScope codegenMethodScope,
            CodegenClassScope codegenClassScope)
        {
            var typeMember = codegenClassScope.AddDefaultFieldUnshared(
                true,
                typeof(ObjectArrayEventType),
                Cast(
                    typeof(ObjectArrayEventType),
                    EventTypeUtility.ResolveTypeCodegen(forge.type, EPStatementInitServicesConstants.REF)));

            var scope = new ExprForgeCodegenSymbol(false, null);
            var methodNode = codegenMethodScope
                .MakeChildWithScope(typeof(int), typeof(EnumCountOfSelectorScalarForgeEval), scope, codegenClassScope)
                .AddParam(EnumForgeCodegenNames.PARAMS);

            var block = methodNode.Block
                .DeclareVar<int>("count", Constant(0))
                .DeclareVar<ObjectArrayEventBean>(
                    "evalEvent",
                    NewInstance<ObjectArrayEventBean>(NewArrayByLength(typeof(object), Constant(1)), typeMember))
                .AssignArrayElement(EnumForgeCodegenNames.REF_EPS, Constant(forge.StreamNumLambda), Ref("evalEvent"))
                .DeclareVar<object[]>("props", ExprDotName(Ref("evalEvent"), "Properties"));
            var forEach = block.ForEach(typeof(object), "next", EnumForgeCodegenNames.REF_ENUMCOLL)
                .AssignArrayElement(Ref("props"), Constant(0), Ref("next"));
            CodegenLegoBooleanExpression.CodegenContinueIfNotNullAndNotPass(
                forEach,
                forge.InnerExpression.EvaluationType,
                forge.InnerExpression.EvaluateCodegen(typeof(bool?), methodNode, scope, codegenClassScope));
            forEach.Increment("count");
            block.MethodReturn(Ref("count"));
            return LocalMethod(methodNode, args.Eps, args.Enumcoll, args.IsNewData, args.ExprCtx);
        }
    }
} // end of namespace