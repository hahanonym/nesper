///////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) 2006-2019 Esper Team. All rights reserved.                           /
// http://esper.codehaus.org                                                          /
// ---------------------------------------------------------------------------------- /
// The software in this package is published under the terms of the GPL license       /
// a copy of which has been included with this distribution in the license.txt file.  /
///////////////////////////////////////////////////////////////////////////////////////

using System;

using com.espertech.esper.common.@internal.epl.expression.core;
using com.espertech.esper.compat;
using com.espertech.esper.compat.collections;

namespace com.espertech.esper.common.@internal.compile.stage1.spec
{
    public class NewItem
    {
        private readonly string name;
        private readonly ExprNode optExpression;

        public NewItem(
            string name,
            ExprNode optExpression)
        {
            this.name = name;
            this.optExpression = optExpression;
        }

        public string Name {
            get => name;
        }

        public ExprNode OptExpression {
            get => optExpression;
        }
    }
} // end of namespace