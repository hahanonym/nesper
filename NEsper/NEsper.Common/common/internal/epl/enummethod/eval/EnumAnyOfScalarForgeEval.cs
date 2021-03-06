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
    public class EnumAnyOfScalarForgeEval : EnumEval
    {
        private readonly EnumAnyOfScalarForge _forge;
        private readonly ExprEvaluator _innerExpression;

        public EnumAnyOfScalarForgeEval(
            EnumAnyOfScalarForge forge,
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
            if (enumcoll.IsEmpty()) {
                return false;
            }

            var evalEvent = new ObjectArrayEventBean(new object[1], _forge.type);
            eventsLambda[_forge.StreamNumLambda] = evalEvent;
            var props = evalEvent.Properties;

            foreach (var next in enumcoll) {
                props[0] = next;

                var pass = _innerExpression.Evaluate(eventsLambda, isNewData, context);
                if (pass != null && ((Boolean) pass)) {
                    return true;
                }
            }

            return false;
        }

        public static CodegenExpression Codegen(
            EnumAnyOfScalarForge forge,
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

            var namedParams = EnumForgeCodegenNames.PARAMS;
            var scope = new ExprForgeCodegenSymbol(false, null);
            var methodNode = codegenMethodScope
                .MakeChildWithScope(typeof(bool), typeof(EnumAllOfScalarForgeEval), scope, codegenClassScope)
                .AddParam(namedParams);

            var block = methodNode.Block
                .IfConditionReturnConst(ExprDotMethod(EnumForgeCodegenNames.REF_ENUMCOLL, "IsEmpty"), false)
                .DeclareVar<ObjectArrayEventBean>(
                    "evalEvent",
                    NewInstance<ObjectArrayEventBean>(
                        NewArrayByLength(typeof(object), Constant(1)),
                        typeMember));
            block.AssignArrayElement(EnumForgeCodegenNames.REF_EPS, Constant(forge.StreamNumLambda), Ref("evalEvent"))
                .DeclareVar<object[]>("props", ExprDotName(Ref("evalEvent"), "Properties"));

            var forEach = block.ForEach(typeof(object), "next", EnumForgeCodegenNames.REF_ENUMCOLL)
                .AssignArrayElement("props", Constant(0), Ref("next"));
            CodegenLegoBooleanExpression.CodegenReturnBoolIfNullOrBool(
                forEach,
                forge.InnerExpression.EvaluationType,
                forge.InnerExpression.EvaluateCodegen(typeof(bool?), methodNode, scope, codegenClassScope),
                false,
                null,
                true,
                true);
            block.MethodReturn(ConstantFalse());
            return LocalMethod(methodNode, args.Eps, args.Enumcoll, args.IsNewData, args.ExprCtx);
        }
    }
} // end of namespace