///////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) 2006-2015 Esper Team. All rights reserved.                           /
// http://esper.codehaus.org                                                          /
// ---------------------------------------------------------------------------------- /
// The software in this package is published under the terms of the GPL license       /
// a copy of which has been included with this distribution in the license.txt file.  /
///////////////////////////////////////////////////////////////////////////////////////

namespace com.espertech.esper.runtime.@internal.metrics.codahale_metrics.metrics.reporting
{
    public partial class JmxReporter
    {
        public class Gauge : AbstractBean,
            GaugeMBean
        {
            private readonly Gauge metric;

            public Gauge(
                Gauge metric,
                string objectName)
                : base(objectName)

            {
                this.metric = metric;
            }

            public object Value => metric.Value;
        }
    }
}