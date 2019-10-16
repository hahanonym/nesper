///////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) 2006-2019 Esper Team. All rights reserved.                           /
// http://esper.codehaus.org                                                          /
// ---------------------------------------------------------------------------------- /
// The software in this package is published under the terms of the GPL license       /
// a copy of which has been included with this distribution in the license.txt file.  /
///////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;

using com.espertech.esper.common.client;
using com.espertech.esper.compat.collections;

namespace com.espertech.esper.common.@internal.collection
{
    public class EventUnderlyingCollection : TransformCollection<EventBean, object>
    {
        public EventUnderlyingCollection(ICollection<EventBean> events) 
            : base(events, _ => throw new NotSupportedException(), i => i.Underlying)
        {
        }
    }
} // end of namespace