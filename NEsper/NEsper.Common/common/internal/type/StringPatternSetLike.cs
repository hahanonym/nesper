///////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) 2006-2019 Esper Team. All rights reserved.                           /
// http://esper.codehaus.org                                                          /
// ---------------------------------------------------------------------------------- /
// The software in this package is published under the terms of the GPL license       /
// a copy of which has been included with this distribution in the license.txt file.  /
///////////////////////////////////////////////////////////////////////////////////////

using System;
using com.espertech.esper.common.@internal.util;
using com.espertech.esper.compat;
using com.espertech.esper.compat.collections;

namespace com.espertech.esper.common.@internal.type
{
    /// <summary>
    /// </summary>
    [Serializable]
    public class StringPatternSetLike : StringPatternSet
    {
        private readonly string likeString;
        private readonly LikeUtil likeUtil;

        /// <summary>
        /// Ctor.
        /// </summary>
        /// <param name="likeString">pattern to match</param>
        public StringPatternSetLike(string likeString)
        {
            this.likeString = likeString;
            likeUtil = new LikeUtil(likeString, '\\', false);
        }

        /// <summary>
        /// Match the string returning true for a match, using SQL-like semantics.
        /// </summary>
        /// <param name="stringToMatch">string to match</param>
        /// <returns>true for match</returns>
        public bool Match(string stringToMatch)
        {
            if (stringToMatch == null) {
                return false;
            }

            return likeUtil.Compare(stringToMatch);
        }

        protected bool Equals(StringPatternSetLike other)
        {
            return string.Equals(likeString, other.likeString);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) {
                return false;
            }

            if (ReferenceEquals(this, obj)) {
                return true;
            }

            if (obj.GetType() != this.GetType()) {
                return false;
            }

            return Equals((StringPatternSetLike) obj);
        }

        public override int GetHashCode()
        {
            return (likeString != null ? likeString.GetHashCode() : 0);
        }
    }
} // end of namespace