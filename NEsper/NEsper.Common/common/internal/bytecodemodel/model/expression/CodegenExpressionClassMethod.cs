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
using static com.espertech.esper.common.@internal.bytecodemodel.model.expression.CodegenExpressionBuilder;

namespace com.espertech.esper.common.@internal.bytecodemodel.model.expression
{
    public class CodegenExpressionClassMethod : CodegenExpression
    {
        private readonly string _methodName;
        private readonly CodegenExpression[] _params;

        public CodegenExpressionClassMethod(
            string methodName,
            CodegenExpression[] @params)
        {
            this._methodName = methodName;
            this._params = @params;
        }

        public void Render(
            StringBuilder builder,
            IDictionary<Type, string> imports,
            bool isInnerClass)
        {
            builder.Append(_methodName);
            builder.Append("(");
            CodegenExpressionBuilder.RenderExpressions(builder, _params, imports, isInnerClass);
            builder.Append(")");
        }

        public void MergeClasses(ISet<Type> classes)
        {
            CodegenExpressionBuilder.MergeClassesExpressions(classes, _params);
        }
    }
} // end of namespace