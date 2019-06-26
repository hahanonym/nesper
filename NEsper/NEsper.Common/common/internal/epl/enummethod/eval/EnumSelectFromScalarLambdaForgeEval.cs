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
    public class EnumSelectFromScalarLambdaForgeEval : EnumEval
    {
        private readonly EnumSelectFromScalarLambdaForge forge;
        private readonly ExprEvaluator innerExpression;

        public EnumSelectFromScalarLambdaForgeEval(
            EnumSelectFromScalarLambdaForge forge,
            ExprEvaluator innerExpression)
        {
            this.forge = forge;
            this.innerExpression = innerExpression;
        }

        public object EvaluateEnumMethod(
            EventBean[] eventsLambda,
            ICollection<object> enumcoll,
            bool isNewData,
            ExprEvaluatorContext context)
        {
            if (enumcoll.IsEmpty()) {
                return enumcoll;
            }

            ObjectArrayEventBean resultEvent = new ObjectArrayEventBean(new object[1], forge.resultEventType);
            eventsLambda[forge.streamNumLambda] = resultEvent;
            object[] props = resultEvent.Properties;
            Deque<object> result = new ArrayDeque<object>(enumcoll.Count);

            ICollection<object> values = (ICollection<object>) enumcoll;
            foreach (object next in values) {
                props[0] = next;

                object item = innerExpression.Evaluate(eventsLambda, isNewData, context);
                if (item != null) {
                    result.Add(item);
                }
            }

            return result;
        }

        public static CodegenExpression Codegen(
            EnumSelectFromScalarLambdaForge forge,
            EnumForgeCodegenParams args,
            CodegenMethodScope codegenMethodScope,
            CodegenClassScope codegenClassScope)
        {
            CodegenExpressionField resultTypeMember = codegenClassScope.AddFieldUnshared(
                true, typeof(ObjectArrayEventType),
                Cast(typeof(ObjectArrayEventType), EventTypeUtility.ResolveTypeCodegen(forge.resultEventType, EPStatementInitServicesConstants.REF)));

            ExprForgeCodegenSymbol scope = new ExprForgeCodegenSymbol(false, null);
            CodegenMethod methodNode = codegenMethodScope.MakeChildWithScope(
                    typeof(ICollection<object>), typeof(EnumSelectFromScalarLambdaForgeEval), scope, codegenClassScope)
                .AddParam(EnumForgeCodegenNames.PARAMS);

            CodegenBlock block = methodNode.Block
                .IfCondition(ExprDotMethod(EnumForgeCodegenNames.REF_ENUMCOLL, "isEmpty"))
                .BlockReturn(EnumForgeCodegenNames.REF_ENUMCOLL)
                .DeclareVar(
                    typeof(ArrayDeque<object>), "result",
                    NewInstance<ArrayDeque<object>>(ExprDotMethod(EnumForgeCodegenNames.REF_ENUMCOLL, "size")))
                .DeclareVar(
                    typeof(ObjectArrayEventBean), "resultEvent",
                    NewInstance<ObjectArrayEventBean>(NewArrayByLength(typeof(object), Constant(1)), resultTypeMember))
                .AssignArrayElement(EnumForgeCodegenNames.REF_EPS, Constant(forge.streamNumLambda), @Ref("resultEvent"))
                .DeclareVar(typeof(object[]), "props", ExprDotMethod(@Ref("resultEvent"), "getProperties"));
            CodegenBlock forEach = block.ForEach(typeof(object), "next", EnumForgeCodegenNames.REF_ENUMCOLL)
                .AssignArrayElement("props", Constant(0), @Ref("next"))
                .DeclareVar(typeof(object), "item", forge.innerExpression.EvaluateCodegen(typeof(object), methodNode, scope, codegenClassScope))
                .IfCondition(NotEqualsNull(@Ref("item")))
                .Expression(ExprDotMethod(@Ref("result"), "add", @Ref("item")));
            block.MethodReturn(@Ref("result"));
            return LocalMethod(methodNode, args.Eps, args.Enumcoll, args.IsNewData, args.ExprCtx);
        }
    }
} // end of namespace