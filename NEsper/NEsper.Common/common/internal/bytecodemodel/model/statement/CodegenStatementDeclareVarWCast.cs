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

using com.espertech.esper.common.@internal.bytecodemodel.util;

using static com.espertech.esper.common.@internal.bytecodemodel.core.CodeGenerationHelper;

namespace com.espertech.esper.common.@internal.bytecodemodel.model.statement
{
    public class CodegenStatementDeclareVarWCast : CodegenStatementBase
    {
        private readonly Type clazz;
        private readonly string rhsName;
        private readonly string var;

        public CodegenStatementDeclareVarWCast(
            Type clazz,
            string var,
            string rhsName)
        {
            this.var = var;
            this.clazz = clazz;
            this.rhsName = rhsName;
        }

        public override void RenderStatement(
            StringBuilder builder,
            bool isInnerClass)
        {
            AppendClassName(builder, clazz);
            builder
                .Append(" ")
                .Append(var)
                .Append("=")
                .Append("(");

            AppendClassName(builder, clazz);

            builder
                .Append(")")
                .Append(rhsName);
        }

        public override void MergeClasses(ISet<Type> classes)
        {
            classes.AddToSet(clazz);
        }
    }
} // end of namespace