using System;
using Autodesk.AutoCAD.Geometry;
using NetTopologySuite.Algorithm.Match;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThCADCore.NTS
{
    public static class ThCADCoreNTSMatch
    {
        public static double Measure(this Polyline pline, Polyline other)
        {
            var measure = new HausdorffSimilarityMeasure();
            return measure.Measure(pline.ToNTSPolygon(), other.ToNTSPolygon());
        }

        public static bool ContainsDuplication(this DBObjectCollection objs, Polyline other)
        {
            foreach(Polyline polyline in objs)
            {
                if (Math.Abs(polyline.Measure(other) - 1) <= Tolerance.Global.EqualPoint)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
