using Dreambuild.AutoCAD;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using System;

namespace ThMEPLighting.Garage.Service.LayoutResult
{
    internal class ThBuildLightLineService
    {
        public static DBObjectCollection Build(Dictionary<Point3d, Tuple<double, string>> lightPos,double lampLength)
        {
            var results = new DBObjectCollection();
            lightPos.ForEach(o => results.Add(CreateLine(o.Key, o.Value.Item1, lampLength)));
            return results; 
        }
        public static Line CreateLine(Point3d pos, double rad,double lampLength)
        {
            var lineVec = GetLineVector(rad);
            var sp = pos + lineVec.MultiplyBy(lampLength / 2.0);
            var ep = pos - lineVec.MultiplyBy(lampLength / 2.0);
            return new Line(sp, ep);
        }
        private static Vector3d GetLineVector(double rad)
        {
            return Vector3d.XAxis.RotateBy(rad, Vector3d.ZAxis).GetNormal();
        }
    }
}
