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
using com.espertech.esper.common.@internal.util;

namespace com.espertech.esper.common.@internal.type
{
    public partial class MathArithTypeEnum
    {
        /// <summary>
        ///     Computer for math op.
        /// </summary>
        public class AddDecimalConvComputer : Computer
        {
            private readonly SimpleNumberDecimalCoercer convOne;
            private readonly SimpleNumberDecimalCoercer convTwo;

            /// <summary>
            ///     Ctor.
            /// </summary>
            /// <param name="convOne">conversion for LHS</param>
            /// <param name="convTwo">conversion for RHS</param>
            public AddDecimalConvComputer(
                SimpleNumberDecimalCoercer convOne,
                SimpleNumberDecimalCoercer convTwo)
            {
                this.convOne = convOne;
                this.convTwo = convTwo;
            }

            public object Compute(
                object d1,
                object d2)
            {
                decimal s1 = convOne.CoerceBoxedDecimal(d1);
                decimal s2 = convTwo.CoerceBoxedDecimal(d2);
                return s1.Add(s2);
            }

            public CodegenExpression Codegen(
                CodegenMethodScope codegenMethodScope,
                CodegenClassScope codegenClassScope,
                CodegenExpressionRef left,
                CodegenExpressionRef right,
                Type ltype,
                Type rtype)
            {
                var leftAsBig = convOne.CoerceBoxedDecimalCodegen(left, ltype);
                var rightAsBig = convTwo.CoerceBoxedDecimalCodegen(right, rtype);
                return CodegenExpressionBuilder.ExprDotMethod(leftAsBig, "add", rightAsBig);
            }
        }
    }
}