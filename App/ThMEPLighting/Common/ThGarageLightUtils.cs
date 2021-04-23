using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Linq2Acad;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.Common
{
    public static class ThGarageLightUtils
    {
        public static Line ExtendLine(this Line line,double length=5.0)
        {
            var dir = (line.EndPoint - line.StartPoint).GetNormal();
            return new Line(line.StartPoint - dir * length, line.EndPoint + dir * length);
        }
        /// <summary>
        /// Poly只能有线段构成
        /// </summary>
        /// <param name="poly"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static Polyline ExtendPolyline(this Polyline poly, double length = 5.0)
        {
            var sp = poly.GetPoint3dAt(0);
            var spNext = poly.GetPoint3dAt(1);

            Vector3d spVec = sp.GetVectorTo(spNext).GetNormal();
            var startExendPt = sp - spVec.MultiplyBy(length);

            var ep = poly.GetPoint3dAt(poly.NumberOfVertices-1);
            var epPrev = poly.GetPoint3dAt(poly.NumberOfVertices - 2);

            Vector3d epVec = ep.GetVectorTo(epPrev).GetNormal();
            var endExendPt = ep - epVec.MultiplyBy(length);

            Polyline newPoly = new Polyline();
            newPoly.AddVertexAt(0, startExendPt.ToPoint2D(), 0, 0, 0);
            for (int i = 1; i < poly.NumberOfVertices - 1; i++)
            {
                newPoly.AddVertexAt(i, poly.GetPoint3dAt(i).ToPoint2D(), 0, 0, 0);
            }
            newPoly.AddVertexAt(poly.NumberOfVertices - 1, endExendPt.ToPoint2D(), 0, 0, 0);

            return newPoly;
        }        
        public static DBObjectCollection SpatialFilter(this Polyline border, DBObjectCollection dbObjs)
        {
            var pts = border.Vertices();
            var spatialIndex = new ThCADCoreNTSSpatialIndexEx(dbObjs);
            return spatialIndex.SelectCrossingPolygon(pts);
        }
        public static ThCADCoreNTSSpatialIndex BuildSpatialIndex(List<Line> lines)
        {
            DBObjectCollection objs = new DBObjectCollection();
            lines.ForEach(o => objs.Add(o));
            return new ThCADCoreNTSSpatialIndex(objs);
        }
        public static bool IsLink(this Line line, Point3d pt,double tolerance=1.0)
        {
            return line.StartPoint.DistanceTo(pt) <= tolerance ||
                    line.EndPoint.DistanceTo(pt) <= tolerance;
        }
        public static bool IsLink(this Line first, Line second,double tolerance=1.0)
        {            
            if (ThGeometryTool.IsCollinearEx(first.StartPoint, 
                first.EndPoint,second.StartPoint,second.EndPoint))
            {
                double sum = first.Length + second.Length;
                var pairPts = new List<Tuple<Point3d, Point3d>>();
                pairPts.Add(Tuple.Create(first.StartPoint, second.StartPoint));
                pairPts.Add(Tuple.Create(first.StartPoint, second.EndPoint));
                pairPts.Add(Tuple.Create(first.EndPoint, second.StartPoint));
                pairPts.Add(Tuple.Create(first.EndPoint, second.EndPoint));
                var biggest = pairPts.OrderByDescending(o => o.Item1.DistanceTo(o.Item2)).First();
                var bigDistance = biggest.Item1.DistanceTo(biggest.Item2);
                return Math.Abs(sum - bigDistance) <= tolerance;
            }
            else
            {
                return IsLink(first, second.StartPoint, tolerance) ||
                 IsLink(first, second.EndPoint, tolerance);
            }
        }
        public static bool IsCoincide(this Line first,Line second,double tolerance= 1e-4)
        {
            return IsCoincide(first.StartPoint,first.EndPoint,
                second.StartPoint,second.EndPoint, tolerance);
        }
        public static bool IsCoincide(Point3d firstSp,Point3d firstEp,
            Point3d secondSp,Point3d secondEp,double tolerance=1e-4)
        {
            return 
                (firstSp.DistanceTo(secondSp) <= tolerance &&
                firstEp.DistanceTo(secondEp) <= tolerance) ||
                (firstSp.DistanceTo(secondEp) <= tolerance &&
                firstEp.DistanceTo(secondSp) <= tolerance);
        }
        public static List<Point3d> PtOnLines(this List<Point3d> pts,List<Line> lines,double tolerance=1.0)
        {
            var results = new List<Point3d>();
            foreach (var pt in pts)
            {
                foreach(var line in lines)
                {
                    if(line.IsLink(pt, tolerance))
                    {
                        results.Add(pt);
                        break;
                    }
                }
            }
            return results;
        }
        public static Tuple<Point3d, Point3d> GetMaxPts(this List<ThLightEdge> edges)
        {
            var pts = new List<Point3d>();
            edges.ForEach(o =>
            {
                pts.Add(o.Edge.StartPoint);
                pts.Add(o.Edge.EndPoint);
            });
            return pts.GetCollinearMaxPts();
        }
        public static bool IsContains(this List<Line> lines, Line line,double tolerance=1.0)
        {
            return lines.Where(o => line.IsCoincide(o, tolerance)).Any();
        }    
        public static bool IsContains(this List<Point3d> pts,Point3d pt ,double tolerance=1.0)
        {
            return pts.Where(o => pt.DistanceTo(o)<=tolerance).Any();
        }
        public static bool HasCommon(this Line first,Line second,double tolerance=1.0)
        {
            if(first.Length==0.0 || second.Length==0.0)
            {
                return false;
            }
            if (ThGeometryTool.IsParallelToEx(
                first.StartPoint.GetVectorTo(first.EndPoint),
                second.StartPoint.GetVectorTo(second.EndPoint)))
            {
                var newSp = ThGeometryTool.GetProjectPtOnLine(first.StartPoint, second.StartPoint, second.EndPoint);
                var newEp = ThGeometryTool.GetProjectPtOnLine(first.EndPoint, second.StartPoint, second.EndPoint);
                var pts = new List<Point3d>() { newSp, newEp, second.StartPoint, second.EndPoint };
                var maxItem = pts.GetCollinearMaxPts();
                var sum = first.Length + second.Length;           
                if (Math.Abs(maxItem.Item1.DistanceTo(maxItem.Item2)- sum)<= tolerance)
                {
                    return false;
                }
                else if(maxItem.Item1.DistanceTo(maxItem.Item2)< sum)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// 获取灯编号的文字角度
        /// </summary>
        /// <param name="lightEdgeAngle">灯线角度</param>
        /// <returns></returns>
        public static double LightNumberAngle(double lightEdgeAngle)
        {
            if(lightEdgeAngle==0.0 || lightEdgeAngle == 180.0)
            {
                return 0.0;
            }
            if(lightEdgeAngle == 90.0 || lightEdgeAngle == 270.0)
            {
                return 90;
            }
            if(lightEdgeAngle>0.0 && lightEdgeAngle < 90.0)
            {
                return lightEdgeAngle;
            }
            if (lightEdgeAngle > 90.0 && lightEdgeAngle < 180.0)
            {
                return lightEdgeAngle + 180.0;
            }
            if (lightEdgeAngle > 180.0 && lightEdgeAngle < 270.0)
            {
                return lightEdgeAngle - 180.0;
            }
            if (lightEdgeAngle > 270.0 && lightEdgeAngle < 360.0)
            {
                return lightEdgeAngle;
            }
            return 0.0;
        }
        /// <summary>
        /// 判断两根线相连、共线、不重叠
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        public static bool IsCollinearLinkAndNotOverlap(this Line first,Line second, double tolerance = 1.0)
        {
            return first.IsLink(second, tolerance) &&
                ThGeometryTool.IsCollinearEx(
                    first.StartPoint,first.EndPoint,
                    second.StartPoint, second.EndPoint) && 
                !ThGeometryTool.IsOverlapEx(
                    first.StartPoint, first.EndPoint,
                    second.StartPoint, second.EndPoint);
        }
        public static Point3d GetNextLinkPt(Line line, Point3d start)
        {
            Point3d lineSp = line.StartPoint;
            Point3d lineEp = line.EndPoint;
            return start.DistanceTo(lineSp) < start.DistanceTo(lineEp) ?
                lineEp : lineSp;
        }
        public static void ImportLinetype(this Database database, string name, bool replaceIfDuplicate = false)
        {
            using (AcadDatabase currentDb = AcadDatabase.Use(database))
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.LaneLineLightDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                currentDb.Linetypes.Import(blockDb.Linetypes.ElementOrDefault(name), replaceIfDuplicate);
            }
        }

        public static void ImportLayer(this Database database, string name, bool replaceIfDuplicate = false)
        {
            using (AcadDatabase currentDb = AcadDatabase.Use(database))
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.LaneLineLightDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(name), replaceIfDuplicate);
            }
        }

        public static List<Line> DistinctLines(List<Line> lines)
        {
            List<Line> resLines = new List<Line>();
            foreach (var line in lines)
            {
                bool check = resLines.Where(x =>
                (x.StartPoint.IsEqualTo(line.EndPoint, new Tolerance(1, 1)) &&
                x.EndPoint.IsEqualTo(line.StartPoint, new Tolerance(1, 1))) ||
                (x.StartPoint.IsEqualTo(line.StartPoint, new Tolerance(1, 1)) &&
                x.EndPoint.IsEqualTo(line.EndPoint, new Tolerance(1, 1))))
                .Count() > 0;
                if (!check)
                {
                    resLines.Add(line);
                }
            }

            return resLines;
        }
        public static bool IsPointOnLines(Point3d pt, Line line, double tol = 5)
        {
            var closet = line.GetClosestPointTo(pt, false);
            if (closet.DistanceTo(pt) < tol)
            {
                return true;
            }
            return false;   
        }
        public static Line NormalizeLaneLine(Line line, double tolerance = 1.0)
        {
            var newLine = new Line(line.StartPoint, line.EndPoint);
            if (Math.Abs(line.StartPoint.Y - line.EndPoint.Y) <= tolerance)
            {
                //近似沿X轴
                if (line.StartPoint.X < line.EndPoint.X)
                {
                    newLine = new Line(line.StartPoint, line.EndPoint);
                }
                else
                {
                    newLine = new Line(line.EndPoint, line.StartPoint);
                }
            }
            else if(Math.Abs(line.StartPoint.X - line.EndPoint.X) <= tolerance)
            {
                //此线是近似沿Y轴
                if (line.StartPoint.Y < line.EndPoint.Y)
                {
                    newLine = new Line(line.StartPoint, line.EndPoint);
                }
                else
                {
                    newLine = new Line(line.EndPoint, line.StartPoint);
                }
            }
            else
            {
                newLine = newLine.Normalize();
            }
            return newLine;
        }

        public static bool IsLightNumber(string text)
        {
            var sb = new StringBuilder();
            sb.Append('^');
            var str = ThGarageLightCommon.LightNumberPrefix;
            for (int i=0;i< str.Length;i++)
            {
                sb.Append('[');
                sb.Append(str[i]);
                sb.Append(']');
            }
            sb.Append(@"\d+$");
            return Regex.IsMatch(text, sb.ToString());
        }
        public static bool IsGarageLight(Entity e)
        {
            return (e is BlockReference) && (e.Layer == "E-LITE-LITE");
        }
        public static bool IsLightCableCarrierCenterline(Entity e)
        {
            return (e is Line || e is Polyline) && (e.Layer == ThGarageLightCommon.DxCenterLineLayerName);
        }

        public static bool IsNonLightCableCarrierCenterline(Entity e)
        {
            return (e is Line || e is Polyline) && (e.Layer == ThGarageLightCommon.FdxCenterLineLayerName);
        }
        public static List<Line> FilterDistributedEdges(List<Line> edges,List<Line> dxLines)
        {
            var results = new List<Line>();
            var spatialIndex = BuildSpatialIndex(edges);
            foreach (Line line in dxLines)
            {
                var shortenLine = line.ExtendLine(-5.0);
                var dxVec = shortenLine.StartPoint.GetVectorTo(shortenLine.EndPoint);
                var rec = ThDrawTool.ToRectangle(shortenLine.StartPoint, shortenLine.EndPoint, 1.0);
                var objs = spatialIndex.SelectCrossingPolygon(rec);
                foreach(Line edge in objs)
                {
                    var edgeVec = edge.StartPoint.GetVectorTo(edge.EndPoint);
                    if (ThGeometryTool.IsParallelToEx(dxVec, edgeVec) && 
                        HasCommon(shortenLine, edge))
                    {
                        if(results.IndexOf(edge)<0)
                        {
                            results.Add(edge);
                        }
                    }
                }
            }
            return results;
        }
    }
}
