///////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) 2006-2019 Esper Team. All rights reserved.                           /
// http://esper.codehaus.org                                                          /
// ---------------------------------------------------------------------------------- /
// The software in this package is published under the terms of the GPL license       /
// a copy of which has been included with this distribution in the license.txt file.  /
///////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Text;

namespace com.espertech.esper.common.@internal.bytecodemodel.model.expression
{
    public class CodegenExpressionAndOr : CodegenExpression
    {
        private readonly CodegenExpression _first;
        private readonly bool _isAnd;
        private readonly CodegenExpression[] _optionalMore;
        private readonly CodegenExpression _second;

        public CodegenExpressionAndOr(
            bool isAnd,
            CodegenExpression first,
            CodegenExpression second,
            CodegenExpression[] optionalMore)
        {
            _isAnd = isAnd;
            _first = first;
            _second = second;
            _optionalMore = optionalMore;
        }

        public void Render(
            StringBuilder builder,
            IDictionary<Type, string> imports,
            bool isInnerClass)
        {
            builder.Append("(");
            _first.Render(builder, imports, isInnerClass);
            builder.Append(_isAnd ? "&&" : "||");
            _second.Render(builder, imports, isInnerClass);

            if (_optionalMore != null) {
                foreach (var expr in _optionalMore) {
                    builder.Append(_isAnd ? "&&" : "||");
                    expr.Render(builder, imports, isInnerClass);
                }
            }

            builder.Append(")");
        }

        public void MergeClasses(ISet<Type> classes)
        {
            _first.MergeClasses(classes);
            _second.MergeClasses(classes);

            if (_optionalMore != null) {
                foreach (var expr in _optionalMore) {
                    expr.MergeClasses(classes);
                }
            }
        }
    }
} // end of namespace