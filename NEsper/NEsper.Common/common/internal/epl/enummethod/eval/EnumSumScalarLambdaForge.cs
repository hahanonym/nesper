///////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) 2006-2019 Esper Team. All rights reserved.                           /
// http://esper.codehaus.org                                                          /
// ---------------------------------------------------------------------------------- /
// The software in this package is published under the terms of the GPL license       /
// a copy of which has been included with this distribution in the license.txt file.  /
///////////////////////////////////////////////////////////////////////////////////////

using System;

using com.espertech.esper.common.@internal.bytecodemodel.@base;
using com.espertech.esper.common.@internal.bytecodemodel.model.expression;
using com.espertech.esper.common.@internal.epl.enummethod.codegen;
using com.espertech.esper.common.@internal.epl.expression.core;
using com.espertech.esper.common.@internal.@event.arr;
using com.espertech.esper.compat;
using com.espertech.esper.compat.collections;

namespace com.espertech.esper.common.@internal.epl.enummethod.eval
{
    public class EnumSumScalarLambdaForge : EnumForgeBase
    {
        internal readonly ExprDotEvalSumMethodFactory sumMethodFactory;
        internal readonly ObjectArrayEventType resultEventType;

        public EnumSumScalarLambdaForge(
            ExprForge innerExpression,
            int streamCountIncoming,
            ExprDotEvalSumMethodFactory sumMethodFactory,
            ObjectArrayEventType resultEventType)
            : base(innerExpression, streamCountIncoming)
        {
            this.sumMethodFactory = sumMethodFactory;
            this.resultEventType = resultEventType;
        }

        public override EnumEval EnumEvaluator {
            get => new EnumSumScalarLambdaForgeEval(this, InnerExpression.ExprEvaluator);
        }

        public override CodegenExpression Codegen(
            EnumForgeCodegenParams premade,
            CodegenMethodScope codegenMethodScope,
            CodegenClassScope codegenClassScope)
        {
            return EnumSumScalarLambdaForgeEval.Codegen(this, premade, codegenMethodScope, codegenClassScope);
        }
    }
} // end of namespace