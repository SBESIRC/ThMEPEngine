using System;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPEngineCore.BeamInfo.Business
{
    public static class ThBeamGeExtension
    {
        public static bool IsParallelToEx(this Vector3d vector, Vector3d other)
        {
            double angle = vector.GetAngleTo(other) / Math.PI * 180.0;
            return (angle < 1.0) || ((180.0 - angle) < 1.0);
        }
    }
}
