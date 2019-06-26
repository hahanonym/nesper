///////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) 2006-2015 Esper Team. All rights reserved.                           /
// http://esper.codehaus.org                                                          /
// ---------------------------------------------------------------------------------- /
// The software in this package is published under the terms of the GPL license       /
// a copy of which has been included with this distribution in the license.txt file.  /
///////////////////////////////////////////////////////////////////////////////////////

using System;
using com.espertech.esper.common.@internal.util;
using com.espertech.esper.compat;

namespace com.espertech.esper.runtime.client.util
{
    /// <summary>
    /// Obtains the write lock of the runtime-wide event processing read-write lock by trying the lock
    /// waiting for the timeout and throwing an exception if the lock was not taken.
    /// </summary>
    public class LockStrategyWTimeout : LockStrategy
    {
        private readonly long timeout;
        private readonly TimeUnit unit;
        private readonly TimeSpan timespan;
        private IDisposable _lockDisposable;

        /// <summary>
        /// Ctor.
        /// </summary>
        /// <param name="timeout">timeout value in the unit given</param>
        /// <param name="unit">unit</param>
        public LockStrategyWTimeout(long timeout, TimeUnit unit)
        {
            this.timeout = timeout;
            this.unit = unit;
            this.timespan = TimeUnitHelper.ToTimeSpan(timeout, unit);
        }

        public void Acquire(ManagedReadWriteLock runtimeWideLock)
        {
            try {
                _lockDisposable = runtimeWideLock.Lock.WriteLock.Acquire((long) timespan.TotalMilliseconds);
            } catch (TimeoutException) { 
                throw new LockStrategyException("Failed to obtain write lock of runtime-wide processing read-write lock");
            }
        }

        public void Release(ManagedReadWriteLock runtimeWideLock)
        {
            _lockDisposable?.Dispose();
            //runtimeWideLock.Lock.WriteLock.Release();
        }
    }
} // end of namespace