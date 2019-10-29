///////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) 2006-2019 Esper Team. All rights reserved.                           /
// http://esper.codehaus.org                                                          /
// ---------------------------------------------------------------------------------- /
// The software in this package is published under the terms of the GPL license       /
// a copy of which has been included with this distribution in the license.txt file.  /
///////////////////////////////////////////////////////////////////////////////////////

using System;
using System.IO;

using com.espertech.esper.common.client.util;
using com.espertech.esper.common.@internal.epl.expression.core;
using com.espertech.esper.common.@internal.util;

namespace com.espertech.esper.common.@internal.epl.expression.ops
{
    /// <summary>
    ///     Represents a string concatenation.
    /// </summary>
    [Serializable]
    public class ExprConcatNode : ExprNodeBase
    {
        [NonSerialized] private ExprConcatNodeForge forge;
        public bool IsConstantResult => false;

        public override ExprPrecedenceEnum Precedence => ExprPrecedenceEnum.CONCAT;

        public ExprEvaluator ExprEvaluator {
            get {
                CheckValidated(forge);
                return forge.ExprEvaluator;
            }
        }

        public override ExprForge Forge {
            get {
                CheckValidated(forge);
                return forge;
            }
        }

        public override ExprNode Validate(ExprValidationContext validationContext)
        {
            if (ChildNodes.Length < 2) {
                throw new ExprValidationException("Concat node must have at least 2 parameters");
            }

            for (var i = 0; i < ChildNodes.Length; i++) {
                var childType = ChildNodes[i].Forge.EvaluationType;
                var childTypeName = childType == null ? "null" : childType.CleanName();
                if (childType != typeof(string)) {
                    throw new ExprValidationException(
                        "Implicit conversion from datatype '" +
                        childTypeName +
                        "' to System.String is not allowed");
                }
            }

            ThreadingProfile threadingProfile = validationContext.StatementCompileTimeService.Configuration.Common
                .Execution.ThreadingProfile;
            forge = new ExprConcatNodeForge(this, threadingProfile);
            return null;
        }

        public override void ToPrecedenceFreeEPL(TextWriter writer)
        {
            var delimiter = "";
            foreach (var child in ChildNodes) {
                writer.Write(delimiter);
                child.ToEPL(writer, Precedence);
                delimiter = "||";
            }
        }

        public override bool EqualsNode(
            ExprNode node,
            bool ignoreStreamPrefix)
        {
            if (!(node is ExprConcatNode)) {
                return false;
            }

            return true;
        }
    }
} // end of namespace