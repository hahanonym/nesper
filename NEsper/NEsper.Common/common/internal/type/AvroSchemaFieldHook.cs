///////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) 2006-2019 Esper Team. All rights reserved.                           /
// http://esper.codehaus.org                                                          /
// ---------------------------------------------------------------------------------- /
// The software in this package is published under the terms of the GPL license       /
// a copy of which has been included with this distribution in the license.txt file.  /
///////////////////////////////////////////////////////////////////////////////////////

using System;
using com.espertech.esper.common.client.annotation;

namespace com.espertech.esper.common.@internal.type
{
    public class AvroSchemaFieldHook : AvroSchemaFieldAttribute
    {
        private readonly string name;
        private readonly string schema;

        public AvroSchemaFieldHook(
            string name,
            string schema)
        {
            this.name = name;
            this.schema = schema;
        }

        public override string Name => name;

        public override string Schema => schema;

        public Type AnnotationType() => typeof(AvroSchemaFieldAttribute);
    }
} // end of namespace