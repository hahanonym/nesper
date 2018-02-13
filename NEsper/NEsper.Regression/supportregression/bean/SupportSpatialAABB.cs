///////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) 2006-2017 Esper Team. All rights reserved.                           /
// http://esper.codehaus.org                                                          /
// ---------------------------------------------------------------------------------- /
// The software in this package is published under the terms of the GPL license       /
// a copy of which has been included with this distribution in the license.txt file.  /
///////////////////////////////////////////////////////////////////////////////////////

using System;
using System.IO;

using com.espertech.esper.compat;
using com.espertech.esper.compat.collections;
using com.espertech.esper.compat.logging;

using NUnit.Framework;

namespace com.espertech.esper.supportregression.bean
{
    [Serializable]
    public class SupportSpatialAABB  {
        private string id;
        private double x;
        private double y;
        private double width;
        private double height;
        private string category;
    
        public SupportSpatialAABB(string id, double x, double y, double width, double height, string category) {
            this.id = id;
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
            this.category = category;
        }
    
        public SupportSpatialAABB(string id, double x, double y, double width, double height) {
            This(id, x, y, width, height, null);
        }
    
        public string GetId() {
            return id;
        }
    
        public double GetX() {
            return x;
        }
    
        public double GetY() {
            return y;
        }
    
        public double GetWidth() {
            return width;
        }
    
        public double GetHeight() {
            return height;
        }
    
        public string GetCategory() {
            return category;
        }
    }
} // end of namespace
