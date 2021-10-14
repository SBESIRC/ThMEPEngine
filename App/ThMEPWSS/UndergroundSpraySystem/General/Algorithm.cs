﻿using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPWSS.UndergroundSpraySystem.General
{
    public static class Algorithm
    {
        public static Point3d Point3dZ0(this Point3d pt)
        {
            return new Point3d(pt.X, pt.Y, 0);
        }
    }
}
