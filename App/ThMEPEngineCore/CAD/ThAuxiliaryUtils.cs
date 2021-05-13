using System;
using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.CAD
{
    public static class ThAuxiliaryUtils
    {
        public static void Print(this DBObjectCollection dbObjs, int colorIndex)
        {
            using (var db = Linq2Acad.AcadDatabase.Active())
            {
                for (int i = 0; i < dbObjs.Count; i++)
                {
                    var poly = dbObjs[i].Clone() as Entity;
                    poly.ColorIndex = colorIndex;
                    poly.SetDatabaseDefaults();
                    db.ModelSpace.Add(poly);
                }
            }
        }

        public static bool IsVertical(this Line first, Line second,double radTolerance=1.0)
        {
            var rad = first.LineDirection().GetAngleTo(second.LineDirection());
            var ang = rad * 180 / Math.PI;
            return Math.Abs(ang % 180 - 90.0) <= radTolerance;
        }

        public static List<Line> GetEdges(this Polyline poly)
        {
            List<Line> lines = new List<Line>();
            for (int i = 0; i < poly.NumberOfVertices; i++)
            {
                if (poly.GetSegmentType(i) == SegmentType.Line)
                {
                    var lineSeg = poly.GetLineSegmentAt(i);
                    lines.Add(new Line(lineSeg.StartPoint, lineSeg.EndPoint));
                }
            }
            return lines;
        }

        public static List<Line> FilterShortLines(this List<Line> lines, double length)
        {
            //过滤一端没连接任何物体，一端连接其他物体的线
            Func<Point3d, double, ThCADCoreNTSSpatialIndex, bool> Query = (pt, len, index) =>
            {
                var square = ThDrawTool.CreateSquare(pt, len);
                return index.SelectCrossingPolygon(square).Count > 1;             
            };
            var spatialIndex = new ThCADCoreNTSSpatialIndex(lines.ToCollection());
            return lines.Where(o =>
            {
                if (o.Length >= length)
                {
                    return true;
                }
                else
                {
                    double squareLength = o.Length > 2 ? 1 : 0.25 * o.Length;
                    return Query(o.StartPoint, squareLength, spatialIndex) &&
                    Query(o.EndPoint, squareLength, spatialIndex);
                }
            }).ToList();
        }

        public static List<Line> NodingLines(this DBObjectCollection curves)
        {
            var results = new List<Line>();
            var geometry = curves.ToNTSNodedLineStrings();
            if (geometry is LineString line)
            {
                results.Add(line.ToDbline());
            }
            else if (geometry is MultiLineString lines)
            {
                results.AddRange(lines.Geometries.Cast<LineString>().Select(o => o.ToDbline()));
            }
            else
            {
                throw new NotSupportedException();
            }
            return results;
        }

        public static List<Line> ExplodeLines(this DBObjectCollection curves,double tesslateLength=5.0)
        {
            //支持Line,Polyline,Arc,Circle
            var lines = new List<Line>();
            foreach (Curve curve in curves)
            {
                if (curve is Line line)
                {
                    lines.Add(line.WashClone() as Line);
                }
                else if(curve is Arc arc)
                {
                    var arcPoly = arc.Length > tesslateLength ? arc.TessellateArcWithArc(tesslateLength) :
                        arc.TessellateArcWithArc(arc.Length / 2.0);
                    var objs = new DBObjectCollection();
                    arcPoly.Explode(objs);
                    lines.AddRange(ExplodeLines(objs, tesslateLength));
                }
                else if(curve is Circle circle)
                {
                    var length = 2 * Math.PI * circle.Radius;
                    var circlePoly = length > tesslateLength ? circle.Tessellate(tesslateLength) :
                        circle.Tessellate(length / 5.0);
                    var objs = new DBObjectCollection();
                    circlePoly.Explode(objs);
                    lines.AddRange(ExplodeLines(objs, tesslateLength));

                }
                else if (curve is Polyline polyline)
                {
                    var entitySet = new DBObjectCollection();
                    polyline.Explode(entitySet);
                    lines.AddRange(ExplodeLines(entitySet));
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            return lines;
        }

        public static double RadToAng(this double rad)
        {
            return rad / Math.PI * 180.0;
        }
        public static double AngToRad(this double ang)
        {
            return ang / 180.0 * Math.PI;
        }
    }
}
