using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Geometry;

namespace ThMEPElectrical.FireAlarmFixLayout.Service
{
    public static class ThAFASFixUtil
    {
        public static bool IsPositiveInfinity(this Point3d pt)
        {
            return double.IsPositiveInfinity(pt.X) ||
                double.IsPositiveInfinity(pt.Y) ||
                double.IsPositiveInfinity(pt.Z);
        }

    }
}
