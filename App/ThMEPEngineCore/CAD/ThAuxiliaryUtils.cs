using System;
using AcHelper;
using NFox.Cad;
using Linq2Acad;
using DotNetARX;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;

namespace ThMEPEngineCore.CAD
{
    public static class ThAuxiliaryUtils
    {
        public static void CreateGroup(this List<Entity> entities, Database database, int colorIndex)
        {
            using (var db = AcadDatabase.Use(database))
            {
                var objIds = new ObjectIdList();
                entities.ForEach(o =>
                {
                    o.ColorIndex = colorIndex;
                    o.SetDatabaseDefaults();
                    objIds.Add(db.ModelSpace.Add(o));
                });
                if (objIds.Count > 0)
                {
                    GroupTools.CreateGroup(db.Database, Guid.NewGuid().ToString(), objIds);
                }
            }
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
            foreach (Entity curve in curves)
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
                else if(curve is Mline mLine)
                {
                    var mlines = new DBObjectCollection();
                    mLine.Explode(mlines);
                    lines.AddRange(lines.Cast<Line>());
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
        public static Extents2d GetCurrentViewBound(double shrinkScale = 1.0)
        {
            Point2d vSize = GetCurrentViewSize();
            Point3d center = ((Point3d)Application.GetSystemVariable("VIEWCTR")).
                    TransformBy(Active.Editor.CurrentUserCoordinateSystem);
            double w = vSize.X * shrinkScale;
            double h = vSize.Y * shrinkScale;
            Point2d minPoint = new Point2d(center.X - w / 2.0, center.Y - h / 2.0);
            Point2d maxPoint = new Point2d(center.X + w / 2.0, center.Y + h / 2.0);
            return new Extents2d(minPoint, maxPoint);
        }
        public static Point2d GetCurrentViewSize()
        {
            double h = (double)Application.GetSystemVariable("VIEWSIZE");
            Point2d screen = (Point2d)Application.GetSystemVariable("SCREENSIZE");
            double w = h * (screen.X / screen.Y);
            return new Point2d(w, h);
        }        
        public static bool DoubleEquals(double value1, double value2,double DOUBLE_DELTA = 1E-6)
        {
            return value1 == value2 || Math.Abs(value1 - value2) < DOUBLE_DELTA;
        }
        public static DBObjectCollection FilterSmallArea(this DBObjectCollection polygons,double areaTolerance)
        {
            return polygons.Cast<Entity>().Where(o =>
            {
                if (o is Polyline polygon)
                {
                    return polygon.Area >= areaTolerance;
                }
                else if (o is MPolygon mPolygon)
                {
                    return mPolygon.Area >= areaTolerance;
                }
                else
                {
                    return false;
                }
            }).ToCollection();
        }
        public static DBObjectCollection Clone(this DBObjectCollection objs)
        {
            return objs
                .Cast<Entity>()
                .Select(e => e.Clone() as Entity)
                .ToCollection();
        }
        public static string GetCurrentLayer(this Database database)
        {
            using (var acdb = AcadDatabase.Use(database))
            {
                return acdb.Element<LayerTableRecord>(acdb.Database.Clayer).Name;
            }
        }
        public static double OverlapDis(this Line first, Line second)
        {
            if (first.Length == 0.0 || second.Length == 0.0)
            {
                return 0.0;
            }
            var newSp = first.StartPoint.GetProjectPtOnLine(second.StartPoint, second.EndPoint);
            var newEp = first.EndPoint.GetProjectPtOnLine(second.StartPoint, second.EndPoint);
            var pts = new List<Point3d> { newSp, newEp, second.StartPoint, second.EndPoint };
            var maxPair = pts.GetCollinearMaxPts();
            var maxLength = maxPair.Item1.DistanceTo(maxPair.Item2);
            var sum = newSp.DistanceTo(newEp) + second.Length;
            return maxLength >= sum ? 0.0 : sum - maxLength;
        }
        public static void MDispose(this DBObjectCollection objs)
        {
            objs.OfType<Entity>()
                .Where(e => !e.IsDisposed)
                .ForEach(e => e.Dispose());
        }
    }
}
