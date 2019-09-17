///////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) 2006-2019 Esper Team. All rights reserved.                           /
// http://esper.codehaus.org                                                          /
// ---------------------------------------------------------------------------------- /
// The software in this package is published under the terms of the GPL license       /
// a copy of which has been included with this distribution in the license.txt file.  /
///////////////////////////////////////////////////////////////////////////////////////

using System;

using com.espertech.esper.common.@internal.epl.expression.core;
using com.espertech.esper.common.@internal.util;
using com.espertech.esper.compat;
using com.espertech.esper.compat.collections;

namespace com.espertech.esper.common.@internal.epl.expression.table
{
    public class ExprTableNodeUtil
    {
        public static void ValidateExpressions(
            string tableName,
            Type[] providedTypes,
            string providedName,
            ExprNode[] providedExpr,
            Type[] expectedTypes,
            string expectedName
        )
        {
            if (expectedTypes.Length != providedTypes.Length) {
                string actual = (providedTypes.Length == 0 ? "no" : "" + providedTypes.Length) +
                                " " +
                                providedName +
                                " expressions";
                string expected = (expectedTypes.Length == 0 ? "no" : "" + expectedTypes.Length) +
                                  " " +
                                  expectedName +
                                  " expressions";
                throw new ExprValidationException(
                    "Incompatible number of " +
                    providedName +
                    " expressions for use with table '" +
                    tableName +
                    "', the table expects " +
                    expected +
                    " and provided are " +
                    actual);
            }

            for (int i = 0; i < expectedTypes.Length; i++) {
                Type actual = Boxing.GetBoxedType(providedTypes[i]);
                Type expected = Boxing.GetBoxedType(expectedTypes[i]);
                if (!TypeHelper.IsSubclassOrImplementsInterface(actual, expected)) {
                    throw new ExprValidationException(
                        "Incompatible type returned by a " +
                        providedName +
                        " expression for use with table '" +
                        tableName +
                        "', the " +
                        providedName +
                        " expression '" +
                        ExprNodeUtilityPrint.ToExpressionStringMinPrecedenceAsList(providedExpr) +
                        "' returns '" +
                        TypeHelper.CleanName(actual) +
                        "' but the table expects '" +
                        TypeHelper.CleanName(expected) +
                        "'");
                }
            }
        }
    }
} // end of namespace