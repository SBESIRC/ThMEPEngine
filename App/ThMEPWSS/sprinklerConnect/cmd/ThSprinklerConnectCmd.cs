﻿using System.Collections.Generic;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using NetTopologySuite.Operation.Relate;

using ThCADCore.NTS;
using ThMEPWSS.DrainageSystemDiagram;

namespace ThMEPWSS.SprinklerConnect.Cmd
{

    public partial class ThSprinklerConnectNoUICmd
    {
        [CommandMethod("TIANHUACAD", "testLineOverlap", CommandFlags.Modal)]
        public void TestLineOverlap()
        {
            var aa = new Line(new Point3d(100, 0, 0), new Point3d(200, 0, 0));
            var ab = new Line(new Point3d(250, 0, 0), new Point3d(150, 0, 0));

            var ba = new Line(new Point3d(100, 200, 0), new Point3d(200, 200, 0));
            var bb = new Line(new Point3d(200, 200, 0), new Point3d(300, 200, 0));

            var ca = new Line(new Point3d(50, 300, 0), new Point3d(250, 300, 0));
            var cb = new Line(new Point3d(100, 300, 0), new Point3d(200, 300, 0));

            var da = new Line(new Point3d(100, 400, 0), new Point3d(200, 400, 0));
            var db = new Line(new Point3d(250, 400, 0), new Point3d(50, 400, 0));

            var ea = new Line(new Point3d(100, 500, 0), new Point3d(200, 500, 0));
            var eb = new Line(new Point3d(200, 500, 0), new Point3d(100, 500, 0));

            var intersectCheck = new Line(new Point3d(75, -100, 0), new Point3d(75, 600, 0));

            DrawUtils.ShowGeometry(aa, "l0test", 1);
            DrawUtils.ShowGeometry(ab, "l0test", 2);
            DrawUtils.ShowGeometry(ba, "l0test", 1);
            DrawUtils.ShowGeometry(bb, "l0test", 2);
            DrawUtils.ShowGeometry(ca, "l0test", 1);
            DrawUtils.ShowGeometry(cb, "l0test", 2);
            DrawUtils.ShowGeometry(da, "l0test", 1);
            DrawUtils.ShowGeometry(db, "l0test", 2);
            DrawUtils.ShowGeometry(ea, "l0test", 1);
            DrawUtils.ShowGeometry(eb, "l0test", 2);
            DrawUtils.ShowGeometry(intersectCheck, "l0test", 1);

            var a = new List<(Line, Line)> { (aa, ab), (ab, aa), (ba, bb), (ca, cb), (da, db), (ea, eb), (ca, intersectCheck) };
            //var a = new List<(Line, Line)> { (aa, ab), (ab, aa), (ca, intersectCheck) };
            for (int i = 0; i < a.Count; i++)
            {
                var am = RelateOp.Relate(a[i].Item1.ToNTSLineString(), a[i].Item2.ToNTSLineString());

                var an1 = am.IsCrosses(NetTopologySuite.Geometries.Dimension.Curve, NetTopologySuite.Geometries.Dimension.Curve);
                var an2 = am.IsOverlaps(NetTopologySuite.Geometries.Dimension.Curve, NetTopologySuite.Geometries.Dimension.Curve);
                var an3 = am.IsContains();
                var an4 = am.IsDisjoint();
                var an5 = am.IsIntersects();
                var an6 = am.IsEquals(NetTopologySuite.Geometries.Dimension.Curve, NetTopologySuite.Geometries.Dimension.Curve);
                var an7 = am.IsCoveredBy();
                var an8 = am.IsCovers();
                var an9 = am.IsTouches(NetTopologySuite.Geometries.Dimension.Curve, NetTopologySuite.Geometries.Dimension.Curve);
                var an10 = am.IsWithin();
            }
        }
    }
}
