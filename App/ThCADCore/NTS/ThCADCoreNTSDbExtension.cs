using System;
using System.Linq;
using GeoAPI.Geometries;
using System.Collections.Generic;
using NetTopologySuite.Utilities;
using NetTopologySuite.Geometries;
using Autodesk.AutoCAD.DatabaseServices;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Operation.Polygonize;
using NetTopologySuite.Geometries.Utilities;
using NetTopologySuite.Simplify;
using GeometryExtensions;

namespace ThCADCore.NTS
{
    public static class ThCADCoreNTSDbExtension
    {
        public static Polyline ToDbPolyline(this ILineString lineString)
        {
            var pline = new Polyline();
            for(int i = 0; i < lineString.Coordinates.Length; i++)
            {
                pline.AddVertexAt(i, lineString.Coordinates[i].ToAcGePoint2d(), 0, 0, 0);
            }
            pline.Closed = lineString.StartPoint.EqualsExact(lineString.EndPoint);
            return pline;
        }

        public static Line ToDbline(this ILineString lineString)
        {
            var line = new Line
            {
                StartPoint = lineString.StartPoint.ToAcGePoint3d(),
                EndPoint = lineString.EndPoint.ToAcGePoint3d()
            };
            return line;
        }

        public static Curve Simplify(this ILineString lineString)
        {
            var simplifier = new DouglasPeuckerLineSimplifier(lineString.Coordinates);
            var result = ThCADCoreNTSService.Instance.GeometryFactory.CreateLineString(simplifier.Simplify());
            return result.ToDbline();
        }

        public static Polyline ToDbPolyline(this ILinearRing linearRing)
        {
            var pline = new Polyline()
            {
                Closed = true
            };
            for (int i = 0; i < linearRing.Coordinates.Length; i++)
            {
                pline.AddVertexAt(i, linearRing.Coordinates[i].ToAcGePoint2d(), 0, 0, 0);
            }
            return pline;
        }

        public static List<Polyline> ToDbPolylines(this IPolygon polygon)
        {
            var plines = new List<Polyline>();
            plines.Add(polygon.Shell.ToDbPolyline());
            foreach(ILinearRing hole in polygon.Holes)
            {
                plines.Add(hole.ToDbPolyline());
            }
            return plines;
        }

        public static List<Polyline> ToDbPolylines(this IMultiLineString geometries)
        {
            var plines = new List<Polyline>();
            foreach(var geometry in geometries.Geometries)
            {
                if (geometry is ILineString lineString)
                {
                    plines.Add(lineString.ToDbPolyline());
                }
                else if (geometry is ILinearRing linearRing)
                {
                    plines.Add(linearRing.ToDbPolyline());
                }
                else if (geometry is IPolygon polygon)
                {
                    plines.AddRange(polygon.ToDbPolylines());
                }
                else if (geometry is IMultiLineString multiLineString)
                {
                    plines.AddRange(multiLineString.ToDbPolylines());
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            return plines;
        }

        public static Line ToDbLine(this LineSegment segment)
        {
            return new Line()
            {
                StartPoint = segment.P0.ToAcGePoint3d(),
                EndPoint = segment.P1.ToAcGePoint3d()
            };
        }

        public static Region ToDbRegion(this IPolygon polygon)
        {
            try
            {
                // 暂时不考虑有“洞”的情况
                var curves = new DBObjectCollection
                {
                    polygon.Shell.ToDbPolyline()
                };
                return Region.CreateFromCurves(curves)[0] as Region;
            }
            catch
            {
                // 未知错误
                return null;
            }
        }

        public static List<Region> ToDbRegions(this IMultiPolygon mPolygon)
        {
            var regions = new List<Region>();
            foreach (IPolygon polygon in mPolygon.Geometries)
            {
                regions.Add(polygon.ToDbRegion());
            }
            return regions;
        }

        public static List<Polyline> ToDbPolylines(this IMultiPolygon mPolygon)
        {
            var plines = new List<Polyline>();
            foreach (IPolygon polygon in mPolygon.Geometries)
            {
                plines.Add(polygon.Shell.ToDbPolyline());
            }
            return plines;
        }

        public static IGeometry ToNTSLineString(this Polyline polyLine)
        {
            var points = new List<Coordinate>();
            for (int i = 0; i < polyLine.NumberOfVertices; i++)
            {
                // 暂时不考虑“圆弧”的情况
                points.Add(polyLine.GetPoint3dAt(i).ToNTSCoordinate());
            }

            // 对于处于“闭合”状态的多段线，要保证其首尾点一致
            if(polyLine.Closed && !points[0].Equals(points[points.Count - 1]))
            {
                points.Add(points[0]);
            }

            if (points[0].Equals(points[points.Count - 1]))
            {
                // 三个点，其中起点和终点重合
                // 多段线退化成一根线段
                if (points.Count == 3)
                {
                    return ThCADCoreNTSService.Instance.GeometryFactory.CreateLineString(points.ToArray());
                }

                // 三个点，其中起点和终点重合
                // 多段线退化成一个点
                if (points.Count == 2)
                {
                    return ThCADCoreNTSService.Instance.GeometryFactory.CreateMultiPointFromCoords(points.ToArray());
                }

                // 首尾端点一致的情况
                // LinearRings are the fundamental building block for Polygons.
                // LinearRings may not be degenerate; that is, a LinearRing must have at least 3 points.
                // Other non-degeneracy criteria are implied by the requirement that LinearRings be simple. 
                // For instance, not all the points may be collinear, and the ring may not self - intersect.
                return ThCADCoreNTSService.Instance.GeometryFactory.CreateLinearRing(points.ToArray());
            }
            else
            {
                // 首尾端点不一致的情况
                return ThCADCoreNTSService.Instance.GeometryFactory.CreateLineString(points.ToArray());
            }
        }

        public static List<LineSegment> LineSegments(this Polyline polyLine)
        {
            var lineSegments = new List<LineSegment>();
            foreach(var segment in new PolylineSegmentCollection(polyLine))
            {
                lineSegments.Add(new LineSegment(segment.StartPoint.ToNTSCoordinate(),
                    segment.EndPoint.ToNTSCoordinate()));
            }
            return lineSegments;
        }

        public static IPolygon ToNTSPolygon(this Polyline polyLine)
        {
            var polygons = polyLine.Polygonize();
            if (polygons.Count == 1)
            {
                return polygons.First() as IPolygon;
            }
            else if (polygons.Count == 0)
            {
                return ThCADCoreNTSService.Instance.GeometryFactory.CreatePolygon();
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public static IPolygon ToNTSPolygon(this Circle circle)
        {
            // 获取圆的外接矩形
            var shapeFactory = new GeometricShapeFactory(ThCADCoreNTSService.Instance.GeometryFactory)
            {
                NumPoints = 4,
                Size = 2 * circle.Radius,
                Centre = circle.Center.ToNTSCoordinate(),
            };
            return shapeFactory.CreateCircle();
        }

        public static ILineString ToNTSLineString(this Line line)
        {
            var points = new List<Coordinate>
            {
                line.StartPoint.ToNTSCoordinate(),
                line.EndPoint.ToNTSCoordinate()
            };
            return ThCADCoreNTSService.Instance.GeometryFactory.CreateLineString(points.ToArray());
        }

        public static IPolygon ToPolygon(this ILinearRing linearRing)
        {
            return ThCADCoreNTSService.Instance.GeometryFactory.CreatePolygon(linearRing);
        }

        public static IPolygon ToNTSPolygon(this Region region)
        {
            // 暂时不支持"复杂面域"
            var plines = region.ToPolylines();
            if (plines.Count != 1)
            {
                throw new NotSupportedException();
            }

            // 返回由面域外轮廓线封闭的多边形区域
            var pline = plines[0] as Polyline;
            return pline.ToNTSPolygon();
        }

        public static IMultiPolygon ToNTSPolygons(this DBObjectCollection objs)
        {
            var polygons = new List<IPolygon>();
            foreach(Entity entity in objs)
            {
                if (entity is Polyline polyline)
                {
                    polygons.Add(polyline.ToNTSPolygon());
                }
                else if (entity is Region region)
                {
                    polygons.Add(region.ToNTSPolygon());
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            return ThCADCoreNTSService.Instance.GeometryFactory.CreateMultiPolygon(polygons.ToArray());
        }

        public static IMultiPolygon ToNTSPolygons(this DBObject obj)
        {
            var polygons = new List<IPolygon>();
            if (obj is Polyline polyline)
            {
                polygons.Add(polyline.ToNTSPolygon());
            }
            else if (obj is Region region)
            {
                polygons.Add(region.ToNTSPolygon());
            }
            else
            {
                throw new NotSupportedException();
            }

            return ThCADCoreNTSService.Instance.GeometryFactory.CreateMultiPolygon(polygons.ToArray());
        }

        public static IGeometryCollection ToNTSPolygonCollection(this DBObjectCollection curves)
        {
            var polygons = new List<IPolygon>();
            foreach (Polyline polyline in curves)
            {
                polygons.Add(polyline.ToNTSPolygon());
            }
            return ThCADCoreNTSService.Instance.GeometryFactory.CreateGeometryCollection(polygons.ToArray());
        }

        public static ILineString ToNTSLineString(this Arc arc, int numPoints)
        {
            var shapeFactory = new GeometricShapeFactory(ThCADCoreNTSService.Instance.GeometryFactory)
            {
                Centre = arc.Center.ToNTSCoordinate(),
                Size = 2 * arc.Radius,
                NumPoints = numPoints
            };
            return shapeFactory.CreateArc(arc.StartAngle, arc.TotalAngle);
        }

        /// <summary>
        /// 按弦长细化
        /// </summary>
        /// <param name="arc"></param>
        /// <param name="chord"></param>
        /// <returns></returns>
        public static ILineString TessellateWithChord(this Arc arc, double chord)
        {
            // 根据弦长，半径，计算对应的弧长
            // Chord Length = 2 * Radius * sin(angle / 2.0)
            // Arc Length = Radius * angle (angle in radians)
            if (chord > 2 * arc.Radius )
            {
                return null;
            }

            double radius = arc.Radius;
            double angle = 2 * Math.Asin(chord / (2 * radius));
            return arc.TessellateWithArc(radius * angle);
        }

        /// <summary>
        /// 按弧长细化
        /// </summary>
        /// <param name="arc"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static ILineString TessellateWithArc(this Arc arc, double length)
        {
            if (arc.Length < length)
            {
                return null;
            }

            return arc.ToNTSLineString(Convert.ToInt32(Math.Floor(arc.Length / length)) + 1);
        }

        public static IGeometry ToNTSNodedLineStrings(this DBObjectCollection curves, double chord = 5.0)
        {
            IGeometry nodedLineString = ThCADCoreNTSService.Instance.GeometryFactory.CreateLineString();
            foreach(DBObject curve in curves)
            {
                if (curve is Line line)
                {
                    nodedLineString = nodedLineString.Union(line.ToNTSLineString());
                }
                else if (curve is Polyline polyline)
                {
                    nodedLineString = nodedLineString.Union(polyline.ToNTSLineString());
                }
                else if (curve is Arc arc)
                {
                    nodedLineString = nodedLineString.Union(arc.TessellateWithChord(chord));
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            return nodedLineString;
        }

        public static List<IGeometry> ToNTSLineStrings(this DBObjectCollection curves, double chord = 5.0)
        {
            var geometries = new List<IGeometry>();
            foreach(DBObject curve in curves)
            {
                if (curve is Line line)
                {
                    geometries.Add(line.ToNTSLineString());
                }
                else if (curve is Arc arc)
                {
                    geometries.Add(arc.TessellateWithChord(chord));
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            return geometries;
        }

        public static bool IsCCW(this Polyline pline)
        {
            return Orientation.IsCCW(pline.ToNTSLineString().Coordinates);
        }

        public static Envelope ToEnvelope(this Extents3d extents)
        {
            return new Envelope(extents.MinPoint.ToNTSCoordinate(),
                extents.MaxPoint.ToNTSCoordinate());
        }
    }
}
