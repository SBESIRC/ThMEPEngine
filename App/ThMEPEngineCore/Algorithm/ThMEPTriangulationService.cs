#if ACAD2016
using System;
using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using DotNetARX;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using AcPolygon = Autodesk.AutoCAD.DatabaseServices.Polyline;
using CLI;

namespace ThMEPEngineCore.Algorithm
{
    public class ThMEPTriangulationService
    {
        public static DBObjectCollection EarCut(AcPolygon shell, AcPolygon[] holes)
        {
            var indices = new List<int>();
            var points = new List<double>();
            points.AddRange(shell.Coordinates2D());
            holes.ForEach(o =>
            {
                indices.Add(points.Count / 2 - 1);
                points.AddRange(o.Coordinates2D());
            });

            var objs = new DBObjectCollection();
            var builder = new ThEarCutTriangulationBuilder();
            var results = builder.EarCut(points.ToArray(), points.Count / 2, indices.ToArray(), indices.Count);
            for(int i = 0; i < results.Count(); i += 3)
            {
                var triangle = ThPolylineExtension.CreateTriangle(
                    new Point2d(points[2 * results[i]], points[2 * results[i]+1]),
                    new Point2d(points[2 * results[i + 1]],points[2 * results[i + 1]+1]),
                    new Point2d(points[2 * results[i + 2]],points[2 * results[i + 2]+1]));
                objs.Add(triangle);
            }
            return objs;
        }
    }
}
#endif
