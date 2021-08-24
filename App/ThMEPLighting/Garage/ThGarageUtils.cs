using System;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPLighting.Garage
{
    public static class ThGarageUtils
    {
        public static bool IsLessThan45Degree(Vector3d first,Vector3d second)
        {
            if (first.GetAngleTo(second) < Math.PI / 4 - 1e-5) 
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
