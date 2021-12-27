﻿using System;
using AcHelper;
using NFox.Cad;
using Linq2Acad;
using DotNetARX;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;

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
        public static string PointToString(this Point3d pt)
        {
            return pt.X + "," + pt.Y + "," + pt.Z;
        }
        public static string PointToString(this Point2d pt)
        {
            return pt.X + "," + pt.Y;
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
        public static DBObjectCollection BufferZeroPolyline(this DBObjectCollection objectCollection, double distance=0.1)
        {
            DBObjectCollection result = new DBObjectCollection();
            foreach(DBObject obj in objectCollection)
            {
                if(obj is Polyline polyline && DoubleEquals(polyline.Area,0.0))
                {
                    var temp = polyline.Buffer(distance/2);
                    foreach(DBObject dBObject in temp)
                    {
                        result.Add(dBObject);
                    }
                }
                else
                {
                    result.Add(obj);
                }
            }
            return result;
        }
        public static double GetArea(this Entity polygon)
        {
            if (polygon is Polyline polyline)
            {
                return polyline.Area;
            }
            else if (polygon is MPolygon mPolygon)
            {
                return mPolygon.Area;
            }
            else if (polygon is Circle circle)
            {
                return circle.Area;
            }
            else if (polygon is Ellipse ellipse)
            {
                return ellipse.Area;
            }
            else
            {
                throw new System.NotSupportedException();
            }
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
    }
}
