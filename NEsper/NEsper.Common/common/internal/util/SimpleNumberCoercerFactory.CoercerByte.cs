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
using com.espertech.esper.compat;

namespace com.espertech.esper.common.@internal.util
{
    public partial class SimpleNumberCoercerFactory
    {
        public class CoercerByte : SimpleNumberCoercer
        {
            public static readonly CoercerByte INSTANCE = new CoercerByte();

            private CoercerByte()
            {
            }

            public object CoerceBoxed(object numToCoerce)
            {
                return numToCoerce.AsByte();
            }

            public Type ReturnType => typeof(byte);

            public CodegenExpression CoerceCodegen(
                CodegenExpression value,
                Type valueType)
            {
                return CodegenByte(value, valueType);
            }

            public CodegenExpression CoerceCodegenMayNullBoxed(
                CodegenExpression value,
                Type valueTypeMustNumeric,
                CodegenMethodScope codegenMethodScope,
                CodegenClassScope codegenClassScope)
            {
                return CodegenCoerceMayNull(
                    typeof(byte), typeof(byte), "bteValue", value, valueTypeMustNumeric, codegenMethodScope,
                    typeof(CoercerByte), codegenClassScope);
            }

            public static CodegenExpression CodegenByte(
                CodegenExpression input,
                Type inputType)
            {
                return CodegenCoerceNonNull(typeof(byte), typeof(byte), "byteValue", input, inputType);
            }
        }
    }
}