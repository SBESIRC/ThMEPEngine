namespace ThMEPWSS.Pipe.Service
{
    using TypeDescriptor = System.ComponentModel.TypeDescriptor;
    using System;
    using System.Linq;
    using System.Text;
    using System.Reflection;
    using System.Collections.Generic;
    using System.Windows.Forms;
    using ThMEPWSS.JsonExtensionsNs;
    using Dbg = ThMEPWSS.DebugNs.ThDebugTool;
    using DU = ThMEPWSS.Assistant.DrawUtils;
    using Autodesk.AutoCAD.EditorInput;
    using AcHelper;
    using Autodesk.AutoCAD.Geometry;
    using Linq2Acad;
    using ThMEPWSS.Pipe.Model;
    using ThMEPWSS.Pipe.Engine;
    using Autodesk.AutoCAD.DatabaseServices;
    using System.Diagnostics;
    using Autodesk.AutoCAD.ApplicationServices;
    using Dreambuild.AutoCAD;
    using DotNetARX;
    using Autodesk.AutoCAD.Internal;
    using static ThMEPWSS.DebugNs.ThPublicMethods;
    using ThMEPWSS.CADExtensionsNs;
    using ThMEPWSS.Uitl;
    using ThMEPWSS.Uitl.DebugNs;
    using ThMEPWSS.Uitl.ExtensionsNs;
    using ThMEPWSS.Assistant;
    using ThMEPWSS.Pipe.Service;
    using NFox.Cad;
    using ThCADCore.NTS;
    using Autodesk.AutoCAD.Colors;
    using System.Runtime.Remoting;
    using PolylineTools = Pipe.Service.PolylineTools;
    using CircleTools = Pipe.Service.CircleTools;
    using System.IO;
    using Autodesk.AutoCAD.Runtime;
    using ThMEPWSS.Pipe;
    using Newtonsoft.Json;
    using System.Text.RegularExpressions;
    using ThCADExtension;
    using System.Collections;
    using ThCADCore.NTS.IO;
    using Newtonsoft.Json.Linq;
    using ThMEPEngineCore.Engine;
    using NetTopologySuite.Geometries;
    using NetTopologySuite.Operation.Linemerge;
    using Microsoft.CSharp;
    using System.CodeDom.Compiler;
    using System.Linq.Expressions;
    using ThMEPEngineCore.Algorithm;
    using ThMEPWSS.DebugNs;

    public class RainSystemDrawingData
    {
        public List<string> RoofLabels = new List<string>();
        public List<string> BalconyLabels = new List<string>();
        public List<string> CondenseLabels = new List<string>();
        public List<string> GetAllLabels()
        {
            return RoofLabels.Concat(BalconyLabels).Concat(CondenseLabels).Distinct().ToList();
        }
        public List<string> CommentLabels = new List<string>();
        public List<string> LongTranslatorLabels = new List<string>();
        public List<string> ShortTranslatorLabels = new List<string>();
        public List<string> GravityWaterBucketTranslatorLabels = new List<string>();
        public List<KeyValuePair<string, int>> FloorDrains = new List<KeyValuePair<string, int>>();
        public List<KeyValuePair<string, int>> FloorDrainsWrappingPipes = new List<KeyValuePair<string, int>>();
        public List<KeyValuePair<string, int>> WaterWellWrappingPipes = new List<KeyValuePair<string, int>>();
        public List<KeyValuePair<string, int>> RainPortWrappingPipes = new List<KeyValuePair<string, int>>();
        public List<KeyValuePair<string, KeyValuePair<int, bool>>> CondensePipes = new List<KeyValuePair<string, KeyValuePair<int, bool>>>();
        public List<KeyValuePair<string, RainOutputTypeEnum>> OutputTypes = new List<KeyValuePair<string, RainOutputTypeEnum>>();
        public List<KeyValuePair<string, string>> PipeLabelToWaterWellLabels = new List<KeyValuePair<string, string>>();
        public List<KeyValuePair<string, GRect>> VerticalPipes = new List<KeyValuePair<string, GRect>>();
        public List<KeyValuePair<string, GRect>> GravityWaterBuckets = new List<KeyValuePair<string, GRect>>();
    }
    public static class GeoNTSConvertion
    {
        public static Coordinate[] ConvertToCoordinateArray(GRect gRect)
        {
            var pt = gRect.LeftTop.ToPoint3d().ToNTSCoordinate();
            return new Coordinate[]
      {
pt,
gRect.RightTop.ToPoint3d().ToNTSCoordinate(),
gRect.RightButtom.ToPoint3d().ToNTSCoordinate(),
gRect.LeftButtom.ToPoint3d().ToNTSCoordinate(),
pt,
      };
        }
    }
    public static class GeoFac
    {
        static readonly NetTopologySuite.Index.Strtree.GeometryItemDistance itemDist = new NetTopologySuite.Index.Strtree.GeometryItemDistance();
        public static List<Point2d> GetAlivePoints(List<GLineSegment> segs, double radius)
        {
            var points = GetSegsPoints(segs);
            return GetAlivePoints(points, radius);
        }

        public static List<Point2d> GetSegsPoints(List<GLineSegment> segs)
        {
            return segs.Select(x => x.StartPoint).Concat(segs.Select(x => x.EndPoint)).ToList();
        }

        public static List<Point2d> GetAlivePoints(List<Point2d> points, double radius)
        {
            var flags = new bool[points.Count];
            for (int i = 0; i < points.Count; i++)
            {
                if (!flags[i])
                {
                    for (int j = 0; j < points.Count; j++)
                    {
                        if (!flags[j])
                        {
                            if (i != j)
                            {
                                if (points[i].GetDistanceTo(points[j]) < radius)
                                {
                                    flags[i] = true;
                                    flags[j] = true;
                                }
                            }
                        }
                    }
                }
            }
            return flags.ToList(points, true);
        }
        public static List<Point2d> GetLabelLineEndPoints(List<GLineSegment> lines, Geometry killer)
        {
            var radius = 5;
            var points = GetAlivePoints(lines, radius);
            var pts = points.Select(x => new GCircle(x, radius).ToCirclePolygon(6, false)).ToGeometryList();
            var list = lines.Where(x => x.IsHorizontal(5)).Select(x => x.ToLineString()).ToGeometryList();
            list.Add(killer);
            return points.Except(GeoFac.CreateGeometrySelector(pts)(GeoFac.CreateGeometry(list)).Select(pts).ToList(points)).ToList();
        }
        public static Func<Point3d, Geometry> NearestNeighbourPoint3dF(List<Geometry> geos)
        {
            if (geos.Count == 0) return geometry => null;
            else if (geos.Count == 1) return geometry => geos[0];
            var f = NearestNeighbourGeometryF(geos);
            return pt => f(pt.ToNTSPoint());
        }
        public static Func<Geometry, Geometry> NearestNeighbourGeometryF(List<Geometry> geos)
        {
            if (geos.Count == 0) return geometry => null;
            else if (geos.Count == 1) return geometry => geos[0];
            var engine = new NetTopologySuite.Index.Strtree.STRtree<Geometry>();
            foreach (var geo in geos) engine.Insert(geo.EnvelopeInternal, geo);
            return geometry => engine.NearestNeighbour(geometry.EnvelopeInternal, geometry, itemDist);
        }
        public static Func<Point3d, int, List<Geometry>> NearestNeighboursPoint3dF(List<Geometry> geos)
        {
            if (geos.Count == 0) return (pt, num) => new List<Geometry>();
            var f = NearestNeighboursGeometryF(geos);
            return (pt, num) => f(pt.ToNTSPoint(), num);
        }
        public static Func<Geometry, int, List<Geometry>> NearestNeighboursGeometryF(List<Geometry> geos)
        {
            if (geos.Count == 0) return (geometry, num) => new List<Geometry>();
            var engine = new NetTopologySuite.Index.Strtree.STRtree<Geometry>();
            foreach (var geo in geos) engine.Insert(geo.EnvelopeInternal, geo);
            return (geometry, num) =>
            {
                if (num <= geos.Count) return geos.ToList();
                var neighbours = engine.NearestNeighbour(geometry.EnvelopeInternal, geometry, itemDist, num)
        .Where(o => !o.EqualsExact(geometry)).ToList();
                return neighbours;
            };
        }
        public static IEnumerable<KeyValuePair<int, int>> GroupGeometriesFast(List<Geometry> geos)
        {
            if (geos.Count == 0) yield break;
            var engine = new NetTopologySuite.Index.Strtree.STRtree<Geometry>();
            foreach (var geo in geos) engine.Insert(geo.EnvelopeInternal, geo);
            for (int i = 0; i < geos.Count; i++)
            {
                var geo = geos[i];
                var gf = ThCADCoreNTSService.Instance.PreparedGeometryFactory.Create(geo);
                foreach (var j in engine.Query(geo.EnvelopeInternal).Where(g => gf.Intersects(g)).Select(g => geos.BinarySearch(g)).Where(j => i < j))
                {
                    yield return new KeyValuePair<int, int>(i, j);
                }
            }
        }
        public class LineGrouppingHelper
        {
            public List<GLineSegment> LineSegs;
            public List<Geometry> DoublePoints;
            public List<Geometry> Points1;
            public List<Geometry> Points2;
            public List<Geometry> Buffers;
            public List<List<Geometry>> GeoGroupsByPoint;
            public List<List<Geometry>> GeoGroupsByBuffer;
            private LineGrouppingHelper() { }
            public static LineGrouppingHelper Create(List<GLineSegment> lineSegs)
            {
                var o = new LineGrouppingHelper();
                o.LineSegs = lineSegs;
                return o;
            }
            public void DoGroupingByPoint()
            {
                GeoGroupsByPoint = GroupGeometries(DoublePoints);
            }
            public void DoGroupingByBuffer()
            {
                GeoGroupsByBuffer = GroupGeometries(Buffers);
            }
            public List<Geometry> AlonePoints;
            public void CalcAlonePoints()
            {
                var hs = new HashSet<Geometry>();
                var tokill = new HashSet<Geometry>();
                var lst = Points1.Concat(Points2).ToList();
                foreach (var r in lst)
                {
                    if (!hs.Contains(r))
                    {
                        hs.Add(r);
                    }
                    else
                    {
                        tokill.Add(r);
                    }
                }
                var gs = GroupGeometries(lst.Except(tokill).ToList());
                AlonePoints = gs.Where(g => g.Count == 1).Select(g => g[0]).ToList();
            }
            public bool[] IsAlone1;
            public bool[] IsAlone2;
            public IEnumerable<Geometry> YieldAloneRings()
            {
                for (int i = 0; i < IsAlone1.Length; i++)
                {
                    if (IsAlone1[i]) yield return Points1[i];
                    if (IsAlone2[i]) yield return Points2[i];
                }
            }
            public void DistinguishAlonePoints()
            {
                IsAlone1 = new bool[LineSegs.Count];
                IsAlone2 = new bool[LineSegs.Count];
                foreach (var aloneRing in AlonePoints)
                {
                    int i;
                    i = Points1.IndexOf(aloneRing);
                    if (i >= 0)
                    {
                        IsAlone1[i] = true;
                        continue;
                    }
                    i = Points2.IndexOf(aloneRing);
                    if (i >= 0)
                    {
                        IsAlone2[i] = true;
                        continue;
                    }
                }
                AlonePoints = null;
            }
            public IEnumerable<GLineSegment> GetExtendedGLineSegmentsByFlags(double ext)
            {
                for (int i = 0; i < LineSegs.Count; i++)
                {
                    var b1 = IsAlone1[i];
                    var b2 = IsAlone2[i];
                    if (b1 && b2)
                    {
                        var seg = LineSegs[i];
                        //yield return seg.Extend(ext);
                        {
                            //var seg = LineSegs[i];
                            var sp = seg.StartPoint;
                            var ep = seg.EndPoint;
                            var vec = sp - ep;
                            if (vec.Length == 0) continue;
                            var k = ext / vec.Length;
                            ep = sp;
                            sp += vec * k;
                            yield return new GLineSegment(ep, sp);
                        }
                        {
                            //var seg = LineSegs[i];
                            var sp = seg.StartPoint;
                            var ep = seg.EndPoint;
                            var vec = ep - sp;
                            if (vec.Length == 0) continue;
                            var k = ext / vec.Length;
                            sp = ep;
                            ep += vec * k;
                            yield return new GLineSegment(sp, ep);
                        }
                    }
                    if (b1)
                    {
                        var seg = LineSegs[i];
                        var sp = seg.StartPoint;
                        var ep = seg.EndPoint;
                        var vec = sp - ep;
                        if (vec.Length == 0) continue;
                        var k = ext / vec.Length;
                        ep = sp;
                        sp += vec * k;
                        yield return new GLineSegment(ep, sp);
                    }
                    if (b2)
                    {
                        var seg = LineSegs[i];
                        var sp = seg.StartPoint;
                        var ep = seg.EndPoint;
                        var vec = ep - sp;
                        if (vec.Length == 0) continue;
                        var k = ext / vec.Length;
                        sp = ep;
                        ep += vec * k;
                        yield return new GLineSegment(sp, ep);
                    }
                }
            }
            public IEnumerable<Geometry> GetAlonePoints()
            {
                for (int i = 0; i < IsAlone1.Length; i++)
                {
                    if (IsAlone1[i]) yield return Points1[i];
                    if (IsAlone2[i]) yield return Points2[i];
                }
            }
            public int GetAlonePointsCount()
            {
                var s = 0;
                for (int i = 0; i < IsAlone1.Length; i++)
                {
                    if (IsAlone1[i]) s++;
                    if (IsAlone2[i]) s++;
                }
                return s;
            }
            public void KillAloneRings(Geometry[] geos)
            {
                if (geos.Length == 0) return;
                var engine = new NetTopologySuite.Index.Strtree.STRtree<Geometry>();
                for (int i = 0; i < IsAlone1.Length; i++)
                {
                    if (IsAlone1[i])
                    {
                        var geo = Points1[i];
                        engine.Insert(geo.EnvelopeInternal, geo);
                    }
                    if (IsAlone2[i])
                    {
                        var geo = Points2[i];
                        engine.Insert(geo.EnvelopeInternal, geo);
                    }
                }
                {
                    var geo = ThCADCoreNTSService.Instance.GeometryFactory.BuildGeometry(geos);
                    var gf = ThCADCoreNTSService.Instance.PreparedGeometryFactory.Create(geo);
                    foreach (var r in engine.Query(geo.EnvelopeInternal).Where(g => gf.Intersects(g)))
                    {
                        for (int i = 0; i < IsAlone1.Length; i++)
                        {
                            if (Points1[i] == r) IsAlone1[i] = false;
                            if (Points2[i] == r) IsAlone2[i] = false;
                        }
                    }
                }
            }
            public void InitBufferGeos(double dis)
            {
                Buffers = new List<Geometry>(LineSegs.Count);
                foreach (var seg in LineSegs)
                {
                    Buffers.Add(seg.Buffer(dis));
                }
            }
            public void InitPointGeos(double radius, int numPoints = 6)
            {
                DoublePoints = new List<Geometry>(LineSegs.Count);
                Points1 = new List<Geometry>(LineSegs.Count);
                Points2 = new List<Geometry>(LineSegs.Count);
                GeometricShapeFactory.NumPoints = numPoints;
                GeometricShapeFactory.Size = 2 * radius;
                foreach (var seg in LineSegs)
                {
                    GeometricShapeFactory.Centre = seg.StartPoint.ToNTSCoordinate();
                    //var point1 = GeometricShapeFactory.CreateCircle().Shell;
                    var point1 = GeometricShapeFactory.CreateCircle();
                    GeometricShapeFactory.Centre = seg.EndPoint.ToNTSCoordinate();
                    //var point2 = GeometricShapeFactory.CreateCircle().Shell;
                    var point2 = GeometricShapeFactory.CreateCircle();
                    var geo = ThCADCoreNTSService.Instance.GeometryFactory.BuildGeometry(new Geometry[] { point1, point2 });
                    DoublePoints.Add(geo);
                    Points1.Add(point1);
                    Points2.Add(point2);
                }
            }
            public static List<GLineSegment> TryConnect(List<GLineSegment> segs, double dis)
            {
                var lst = new List<GLineSegment>();
                ThRainSystemService.Triangle(segs.Count, (i, j) =>
                {
                    var seg1 = segs[i];
                    var seg2 = segs[j];
                    var angleTol = 5;
                    if (seg1.IsHorizontal(angleTol) && seg2.IsHorizontal(angleTol) || seg1.IsVertical(angleTol) && seg2.IsVertical(angleTol))
                    {
                        if (GeoAlgorithm.GetMinConnectionDistance(seg1, seg2) < dis)
                        {
                            lst.Add(new GLineSegment(seg1.EndPoint, seg2.StartPoint));
                        }
                    }
                });
                return lst;
            }
        }
        public static readonly NetTopologySuite.Utilities.GeometricShapeFactory GeometricShapeFactory = new NetTopologySuite.Utilities.GeometricShapeFactory(ThCADCoreNTSService.Instance.GeometryFactory);
        public static List<Geometry> CreateGeometries(IList<GLineSegment> lineSegments, double radius, int numPoints = 6)
        {
            var ret = new List<Geometry>(lineSegments.Count);
            GeometricShapeFactory.NumPoints = numPoints;
            GeometricShapeFactory.Size = 2 * radius;
            foreach (var seg in lineSegments)
            {
                GeometricShapeFactory.Centre = seg.StartPoint.ToNTSCoordinate();
                var ring1 = GeometricShapeFactory.CreateCircle().Shell;
                GeometricShapeFactory.Centre = seg.EndPoint.ToNTSCoordinate();
                var ring2 = GeometricShapeFactory.CreateCircle().Shell;
                var geo = ThCADCoreNTSService.Instance.GeometryFactory.BuildGeometry(new Geometry[] { ring1, ring2 });
                ret.Add(geo);
            }
            return ret;
        }

        public static IEnumerable<KeyValuePair<int, int>> GroupGLineSegmentsToKVIndex(IList<GLineSegment> lineSegments, double radius, int numPoints = 6)
        {
            var geos = CreateGeometries(lineSegments, radius, numPoints);
            return _GroupGeometriesToKVIndex(geos);
        }
        public static Geometry CreateGeometry(IEnumerable<Geometry> geomList)
        {
            return ThCADCoreNTSService.Instance.GeometryFactory.BuildGeometry(geomList);
        }
        public static Geometry CreateGeometryEx(List<Geometry> geomList) => CreateGeometry(GeoFac.GroupGeometries(geomList).Select(x => (x.Count > 1 ? (x.Aggregate((x, y) => x.Union(y))) : x[0])).Distinct().ToList());
        public static Geometry CreateGeometry(params Geometry[] geos)
        {
            return ThCADCoreNTSService.Instance.GeometryFactory.BuildGeometry(geos);
        }
        public static List<List<Geometry>> GroupGeometries(List<Geometry> geos)
        {
            var geosGroup = new List<List<Geometry>>();
            GroupGeometries(geos, geosGroup);
            return geosGroup;
        }
        public static void GroupGeometries(List<Geometry> geos, List<List<Geometry>> geosGroup)
        {
            if (geos.Count == 0) return;
            var pairs = _GroupGeometriesToKVIndex(geos).ToArray();
            var dict = new ListDict<int>();
            var h = new BFSHelper()
            {
                Pairs = pairs,
                TotalCount = geos.Count,
                Callback = (g, i) => dict.Add(g.root, i),
            };
            h.BFS();
            dict.ForEach((_i, l) =>
            {
                geosGroup.Add(l.Select(i => geos[i]).ToList());
            });
        }
        public static Geometry ToNTSGeometry(GLineSegment seg, double radius)
        {
            var points1 = GeoNTSConvertion.ConvertToCoordinateArray(GRect.Create(seg.StartPoint, radius));
            var points2 = GeoNTSConvertion.ConvertToCoordinateArray(GRect.Create(seg.EndPoint, radius));
            var ring1 = new LinearRing(points1);
            var ring2 = new LinearRing(points2);
            var geo = ThCADCoreNTSService.Instance.GeometryFactory.BuildGeometry(new Geometry[] { ring1, ring2 });
            return geo;
        }
        public static Polygon CreateCirclePolygon(Point3d center, double radius, int numPoints, bool larger = true)
        {
            radius = larger ? radius / Math.Cos(Math.PI / numPoints) : radius;
            var shapeFactory = new NetTopologySuite.Utilities.GeometricShapeFactory(ThCADCoreNTSService.Instance.GeometryFactory)
            {
                NumPoints = numPoints,
                Size = 2 * radius,
                Centre = center.ToNTSCoordinate(),
            };
            return shapeFactory.CreateCircle();
        }
        public static Polygon CreateCirclePolygon(Point2d center, double radius, int numPoints, bool larger = true)
        {
            radius = larger ? radius / Math.Cos(Math.PI / numPoints) : radius;
            var shapeFactory = new NetTopologySuite.Utilities.GeometricShapeFactory(ThCADCoreNTSService.Instance.GeometryFactory)
            {
                NumPoints = numPoints,
                Size = 2 * radius,
                Centre = center.ToNTSCoordinate(),
            };
            return shapeFactory.CreateCircle();
        }
        public static Func<GRect, List<Geometry>> CreateGRectContainsSelector(List<Geometry> geos)
        {
            if (geos.Count == 0) return r => new List<Geometry>();
            var engine = new NetTopologySuite.Index.Strtree.STRtree<Geometry>();
            foreach (var geo in geos) engine.Insert(geo.EnvelopeInternal, geo);
            return r =>
            {
                if (!r.IsValid) return new List<Geometry>();
                var poly = new Polygon(r.ToLinearRing());
                var gf = ThCADCoreNTSService.Instance.PreparedGeometryFactory.Create(poly);
                return engine.Query(poly.EnvelopeInternal).Where(g => gf.Contains(g)).ToList();
            };
        }
        public static Func<GRect, List<Geometry>> CreateGRectSelector(List<Geometry> geos)
        {
            if (geos.Count == 0) return r => new List<Geometry>();
            var engine = new NetTopologySuite.Index.Strtree.STRtree<Geometry>();
            foreach (var geo in geos) engine.Insert(geo.EnvelopeInternal, geo);
            return r =>
            {
                if (!r.IsValid) return new List<Geometry>();
                var poly = new Polygon(r.ToLinearRing());
                var gf = ThCADCoreNTSService.Instance.PreparedGeometryFactory.Create(poly);
                return engine.Query(poly.EnvelopeInternal).Where(g => gf.Intersects(g)).ToList();
            };
        }
        public static Func<Geometry, List<Geometry>> CreateGeometrySelector(List<Geometry> geos)
        {
            if (geos.Count == 0) return r => new List<Geometry>();
            var engine = new NetTopologySuite.Index.Strtree.STRtree<Geometry>();
            foreach (var geo in geos) engine.Insert(geo.EnvelopeInternal, geo);
            return geo =>
            {
                if (geo == null) throw new ArgumentNullException();
                var gf = ThCADCoreNTSService.Instance.PreparedGeometryFactory.Create(geo);
                return engine.Query(geo.EnvelopeInternal).Where(g => gf.Intersects(g)).ToList();
            };
        }
        public static Func<LinearRing, List<Geometry>> CreateLinearRingSelector(List<Geometry> geos)
        {
            if (geos.Count == 0) return r => new List<Geometry>();
            var engine = new NetTopologySuite.Index.Strtree.STRtree<Geometry>();
            foreach (var geo in geos) engine.Insert(geo.EnvelopeInternal, geo);
            return r =>
            {
                if (r == null) throw new ArgumentNullException();
                var poly = new Polygon(r);
                var gf = ThCADCoreNTSService.Instance.PreparedGeometryFactory.Create(poly);
                return engine.Query(poly.EnvelopeInternal).Where(g => gf.Intersects(g)).ToList();
            };
        }
        public static Func<Polygon, List<Geometry>> CreatePolygonSelector(List<Geometry> geos)
        {
            if (geos.Count == 0) return r => new List<Geometry>();
            var engine = new NetTopologySuite.Index.Strtree.STRtree<Geometry>();
            foreach (var geo in geos) engine.Insert(geo.EnvelopeInternal, geo);
            return poly =>
            {
                if (poly == null) throw new ArgumentNullException();
                var gf = ThCADCoreNTSService.Instance.PreparedGeometryFactory.Create(poly);
                return engine.Query(poly.EnvelopeInternal).Where(g => gf.Intersects(g)).ToList();
            };
        }
        public static IEnumerable<KeyValuePair<int, int>> _GroupGeometriesToKVIndex(List<Geometry> geos)
        {
            if (geos.Count == 0) yield break;
            geos = geos.Distinct().ToList();
            var engine = new NetTopologySuite.Index.Strtree.STRtree<Geometry>();
            foreach (var geo in geos) engine.Insert(geo.EnvelopeInternal, geo);
            for (int i = 0; i < geos.Count; i++)
            {
                var geo = geos[i];
                var gf = ThCADCoreNTSService.Instance.PreparedGeometryFactory.Create(geo);
                foreach (var j in engine.Query(geo.EnvelopeInternal).Where(g => gf.Intersects(g)).Select(g => geos.IndexOf(g)).Where(j => i < j))
                {
                    yield return new KeyValuePair<int, int>(i, j);
                }
            }
        }
        public static IEnumerable<GLineSegment> AutoConn(List<GLineSegment> lines, Geometry killer, int maxDis, int angleTolleranceDegree = 1)
        {
            var pts = GetAlivePoints(lines, radius: 10);
            if (killer != null)
            {
                var _pts = pts.Select(pt => pt.ToNTSPoint()).Cast<Geometry>().ToList();
                var ptsf = CreateGeometrySelector(_pts);
                pts = ptsf(killer).Select(_pts).ToList(pts, reverse: true);
            }
            var gvs = new List<GVector>(pts.Count);
            var hs = new HashSet<Point2d>(pts);
            foreach (var line in lines)
            {
                if (hs.Contains(line.StartPoint))
                {
                    gvs.Add(new GVector(line.StartPoint, (line.StartPoint - line.EndPoint).GetNormal()));
                }
                if (hs.Contains(line.EndPoint))
                {
                    gvs.Add(new GVector(line.EndPoint, (line.EndPoint - line.StartPoint).GetNormal()));
                }
            }
            bool f(GVector gv1, GVector gv2)
            {
                var dis1 = gv1.StartPoint.GetDistanceTo(gv2.StartPoint);
                if (dis1 > maxDis) return false;
                var dis2 = gv1.EndPoint.GetDistanceTo(gv2.EndPoint);
                if (dis1 < dis2) return false;
                if (!gv1.Vector.GetAngleTo(gv2.Vector).AngleToDegree().EqualsTo(180, angleTolleranceDegree)) return false;
                if ((gv1.StartPoint - gv2.StartPoint).GetAngleTo(gv1.Vector).AngleToDegree().EqualsTo(180, angleTolleranceDegree)) return true;
                return false;
            }
            var flags = new bool[gvs.Count];
            for (int i = 0; i < gvs.Count; i++)
            {
                if (!flags[i])
                {
                    for (int j = 0; j < gvs.Count; j++)
                    {
                        if (!flags[j])
                        {
                            if (i != j)
                            {
                                if (f(gvs[i], gvs[j]))
                                {
                                    flags[i] = true;
                                    flags[j] = true;
                                    var seg = new GLineSegment(gvs[i].StartPoint, gvs[j].StartPoint);
                                    if (seg.Length > 0)
                                    {
                                        yield return seg;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
#pragma warning disable
    public class DrainageCadData
    {
        public List<Geometry> Storeys;
        public List<Geometry> Labels;
        public List<Geometry> LabelLines;
        public List<Geometry> DLines;
        public List<Geometry> VLines;
        public List<Geometry> VerticalPipes;
        public List<Geometry> FloorDrains;
        public List<Geometry> WaterPorts;
        public List<Geometry> WashingMachines;
        public List<Geometry> CleaningPorts;
        public void Init()
        {
            Storeys ??= new List<Geometry>();
            Labels ??= new List<Geometry>();
            LabelLines ??= new List<Geometry>();
            DLines ??= new List<Geometry>();
            VLines ??= new List<Geometry>();
            VerticalPipes ??= new List<Geometry>();
            FloorDrains ??= new List<Geometry>();
            WaterPorts ??= new List<Geometry>();
            WashingMachines ??= new List<Geometry>();
            CleaningPorts ??= new List<Geometry>();
        }
        public static DrainageCadData Create(DrainageGeoData data)
        {
            var bfSize = 10;
            var o = new DrainageCadData();
            o.Init();
            o.Storeys.AddRange(data.Storeys.Select(x => x.ToPolygon()));
            o.Labels.AddRange(data.Labels.Select(x => x.Boundary.ToPolygon()));

            if (false) o.LabelLines.AddRange(data.LabelLines.Select(NewMethod(bfSize)));
            else o.LabelLines.AddRange(data.LabelLines.Select(ConvertLabelLinesF()));
            o.DLines.AddRange(data.DLines.Select(ConvertDLinesF()));
            o.VLines.AddRange(data.VLines.Select(ConvertVLinesF()));
            if (false) o.VerticalPipes.AddRange(data.VerticalPipes.Select(ConvertVerticalPipesPreciseF()));
            else o.VerticalPipes.AddRange(data.VerticalPipes.Select(ConvertVerticalPipesF()));
            o.FloorDrains.AddRange(data.FloorDrains.Select(ConvertFloorDrainsF()));
            o.WaterPorts.AddRange(data.WaterPorts.Select(ConvertWaterPortsF()));
            o.WashingMachines.AddRange(data.WashingMachines.Select(ConvertWashingMachinesF()));
            o.CleaningPorts.AddRange(data.CleaningPorts.Select(ConvertCleaningPortsF()));
            return o;
        }

        private static Func<GLineSegment, Geometry> NewMethod(int bfSize)
        {
            return x => x.Buffer(bfSize);
        }

        public static Func<Point2d, Polygon> ConvertCleaningPortsF()
        {
            return x => new GCircle(x, 40).ToCirclePolygon(36);
        }

        public static Func<GRect, Polygon> ConvertWashingMachinesF()
        {
            return x => x.ToPolygon();
        }

        public static Func<GRect, Polygon> ConvertWaterPortsLargerF()
        {
            return x => x.Center.ToGCircle(1500).ToCirclePolygon(6);
        }

        private static Func<GRect, Polygon> ConvertWaterPortsF()
        {
            return x => x.ToPolygon();
        }

        public static Func<GRect, Polygon> ConvertFloorDrainsF()
        {
            return x => x.ToPolygon();
        }

        public static Func<GRect, Polygon> ConvertVerticalPipesF()
        {
            return x => x.ToPolygon();
        }

        private static Func<GRect, Polygon> ConvertVerticalPipesPreciseF()
        {
            return x => new GCircle(x.Center, x.InnerRadius).ToCirclePolygon(36);
        }

        public static Func<GLineSegment, LineString> ConvertVLinesF()
        {
            return x => x.ToLineString();
        }

        public static Func<GLineSegment, LineString> ConvertDLinesF()
        {
            return x => x.ToLineString();
        }

        public static Func<GLineSegment, LineString> ConvertLabelLinesF()
        {
            return x => x.Extend(.1).ToLineString();
        }

        public List<Geometry> GetAllEntities()
        {
            var ret = new List<Geometry>(4096);
            ret.AddRange(Storeys);
            ret.AddRange(Labels);
            ret.AddRange(LabelLines);
            ret.AddRange(DLines);
            ret.AddRange(VLines);
            ret.AddRange(VerticalPipes);
            ret.AddRange(FloorDrains);
            ret.AddRange(WaterPorts);
            ret.AddRange(WashingMachines);
            ret.AddRange(CleaningPorts);
            return ret;
        }
        public List<DrainageCadData> SplitByStorey()
        {
            var lst = new List<DrainageCadData>(this.Storeys.Count);
            if (this.Storeys.Count == 0) return lst;
            var f = GeoFac.CreateGeometrySelector(GetAllEntities());
            foreach (var storey in this.Storeys)
            {
                var objs = f(storey);
                var o = new DrainageCadData();
                o.Init();
                o.Labels.AddRange(objs.Where(x => this.Labels.Contains(x)));
                o.LabelLines.AddRange(objs.Where(x => this.LabelLines.Contains(x)));
                o.DLines.AddRange(objs.Where(x => this.DLines.Contains(x)));
                o.VLines.AddRange(objs.Where(x => this.VLines.Contains(x)));
                o.VerticalPipes.AddRange(objs.Where(x => this.VerticalPipes.Contains(x)));
                o.FloorDrains.AddRange(objs.Where(x => this.FloorDrains.Contains(x)));
                o.WaterPorts.AddRange(objs.Where(x => this.WaterPorts.Contains(x)));
                o.WashingMachines.AddRange(objs.Where(x => this.WashingMachines.Contains(x)));
                o.CleaningPorts.AddRange(objs.Where(x => this.CleaningPorts.Contains(x)));
                lst.Add(o);
            }
            return lst;
        }
        public DrainageCadData Clone()
        {
            return (DrainageCadData)MemberwiseClone();
        }
    }

    public class RainSystemCadData
    {
        public List<Geometry> Storeys;
        public List<Geometry> LabelLines;
        public List<Geometry> WLines;
        public List<Geometry> WLinesAddition;
        public List<Geometry> Labels;
        public List<Geometry> VerticalPipes;
        public List<Geometry> CondensePipes;
        public List<Geometry> FloorDrains;
        public List<Geometry> WaterWells;
        public List<Geometry> WaterPortSymbols;
        public List<Geometry> WaterPort13s;
        public List<Geometry> WrappingPipes;
        public List<Geometry> SideWaterBuckets;
        public List<Geometry> GravityWaterBuckets;
        public List<Geometry> _87WaterBuckets;
        public void Init()
        {
            Storeys ??= new List<Geometry>();
            LabelLines ??= new List<Geometry>();
            WLines ??= new List<Geometry>();
            WLinesAddition ??= new List<Geometry>();
            Labels ??= new List<Geometry>();
            VerticalPipes ??= new List<Geometry>();
            CondensePipes ??= new List<Geometry>();
            FloorDrains ??= new List<Geometry>();
            WaterWells ??= new List<Geometry>();
            WaterPortSymbols ??= new List<Geometry>();
            WaterPort13s ??= new List<Geometry>();
            WrappingPipes ??= new List<Geometry>();
            SideWaterBuckets ??= new List<Geometry>();
            GravityWaterBuckets ??= new List<Geometry>();
            _87WaterBuckets ??= new List<Geometry>();
        }
        public static RainSystemCadData Create(RainSystemGeoData data)
        {
            var bfSize = 10;
            var o = new RainSystemCadData();
            o.Init();
            o.Storeys.AddRange(data.Storeys.Select(x => x.ToPolygon()));
            o.LabelLines.AddRange(data.LabelLines.Select(x => x.Buffer(bfSize)));
            o.WLines.AddRange(data.WLines.Select(x => x.Buffer(bfSize)));
            o.WLinesAddition.AddRange(data.WLinesAddition.Select(x => x.Buffer(bfSize)));
            o.Labels.AddRange(data.Labels.Select(x => x.Boundary.ToPolygon()));
            o.VerticalPipes.AddRange(data.VerticalPipes.Select(x => x.ToPolygon()));
            o.CondensePipes.AddRange(data.CondensePipes.Select(x => x.ToPolygon()));
            o.FloorDrains.AddRange(data.FloorDrains.Select(x => x.ToPolygon()));
            o.WaterWells.AddRange(data.WaterWells.Select(x => x.ToPolygon()));
            o.WaterPortSymbols.AddRange(data.WaterPortSymbols.Select(x => x.ToPolygon()));
            o.WaterPort13s.AddRange(data.WaterPort13s.Select(x => x.ToPolygon()));
            o.WrappingPipes.AddRange(data.WrappingPipes.Select(x => x.ToPolygon()));
            o.SideWaterBuckets.AddRange(data.SideWaterBuckets.Select(x => x.ToPolygon()));
            o.GravityWaterBuckets.AddRange(data.GravityWaterBuckets.Select(x => x.ToPolygon()));
            o._87WaterBuckets.AddRange(data._87WaterBuckets.Select(x => x.ToPolygon()));
            return o;
        }
        public List<Geometry> GetAllEntities()
        {
            var ret = new List<Geometry>(4096);
            ret.AddRange(Storeys);
            ret.AddRange(LabelLines);
            ret.AddRange(WLines);
            ret.AddRange(WLinesAddition);
            ret.AddRange(Labels);
            ret.AddRange(VerticalPipes);
            ret.AddRange(CondensePipes);
            ret.AddRange(FloorDrains);
            ret.AddRange(WaterWells);
            ret.AddRange(WaterPortSymbols);
            ret.AddRange(WaterPort13s);
            ret.AddRange(WrappingPipes);
            ret.AddRange(SideWaterBuckets);
            ret.AddRange(GravityWaterBuckets);
            ret.AddRange(_87WaterBuckets);
            return ret;
        }
        public List<RainSystemCadData> SplitByStorey()
        {
            var lst = new List<RainSystemCadData>(this.Storeys.Count);
            if (this.Storeys.Count == 0) return lst;
            var f = GeoFac.CreateGeometrySelector(GetAllEntities());
            foreach (var storey in this.Storeys)
            {
                var objs = f(storey);
                var o = new RainSystemCadData();
                o.Init();
                o.LabelLines.AddRange(objs.Where(x => this.LabelLines.Contains(x)));
                o.WLines.AddRange(objs.Where(x => this.WLines.Contains(x)));
                o.WLinesAddition.AddRange(objs.Where(x => this.WLinesAddition.Contains(x)));
                o.Labels.AddRange(objs.Where(x => this.Labels.Contains(x)));
                o.VerticalPipes.AddRange(objs.Where(x => this.VerticalPipes.Contains(x)));
                o.CondensePipes.AddRange(objs.Where(x => this.CondensePipes.Contains(x)));
                o.FloorDrains.AddRange(objs.Where(x => this.FloorDrains.Contains(x)));
                o.WaterWells.AddRange(objs.Where(x => this.WaterWells.Contains(x)));
                o.WaterPortSymbols.AddRange(objs.Where(x => this.WaterPortSymbols.Contains(x)));
                o.WaterPort13s.AddRange(objs.Where(x => this.WaterPort13s.Contains(x)));
                o.WrappingPipes.AddRange(objs.Where(x => this.WrappingPipes.Contains(x)));
                o.SideWaterBuckets.AddRange(objs.Where(x => this.SideWaterBuckets.Contains(x)));
                o.GravityWaterBuckets.AddRange(objs.Where(x => this.GravityWaterBuckets.Contains(x)));
                o._87WaterBuckets.AddRange(objs.Where(x => this._87WaterBuckets.Contains(x)));
                lst.Add(o);
            }
            return lst;
        }
        public RainSystemCadData Clone()
        {
            return (RainSystemCadData)MemberwiseClone();
        }
    }
    public class DrainageGeoData
    {
        public List<GRect> Storeys;
        public List<CText> Labels;
        public List<GLineSegment> LabelLines;
        public List<GLineSegment> DLines;//排水立管专用转管
        public List<GLineSegment> VLines;//通气立管专用转管
        public List<GRect> VerticalPipes;
        public List<GRect> FloorDrains;
        public List<GRect> WaterPorts;
        public List<string> WaterPortLabels;
        public List<GRect> WashingMachines;
        public List<Point2d> CleaningPorts;
        public void Init()
        {
            Storeys ??= new List<GRect>();
            Labels ??= new List<CText>();
            LabelLines ??= new List<GLineSegment>();
            DLines ??= new List<GLineSegment>();
            VLines ??= new List<GLineSegment>();
            VerticalPipes ??= new List<GRect>();
            FloorDrains ??= new List<GRect>();
            WaterPorts ??= new List<GRect>();
            WaterPortLabels ??= new List<string>();
            WashingMachines ??= new List<GRect>();
            CleaningPorts ??= new List<Point2d>();
        }
        public void FixData()
        {
            Storeys = Storeys.Where(x => x.IsValid).Distinct().ToList();
            Labels = Labels.Where(x => x.Boundary.IsValid).Distinct().ToList();
            LabelLines = LabelLines.Where(x => x.Length > 0).Distinct().ToList();
            DLines = DLines.Where(x => x.Length > 0).Distinct().ToList();
            VLines = VLines.Where(x => x.Length > 0).Distinct().ToList();
            VerticalPipes = VerticalPipes.Where(x => x.IsValid).Distinct().ToList();
            FloorDrains = FloorDrains.Where(x => x.IsValid).Distinct().ToList();
            WaterPorts = WaterPorts.Where(x => x.IsValid).Distinct().ToList();
            WashingMachines = WashingMachines.Where(x => x.IsValid).Distinct().ToList();
        }
        public DrainageGeoData Clone()
        {
            return (DrainageGeoData)MemberwiseClone();
        }
        public DrainageGeoData DeepClone()
        {
            return this.ToCadJson().FromCadJson<DrainageGeoData>();
        }
    }
    public class RainSystemGeoData
    {
        public List<GRect> Storeys;

        public List<GLineSegment> LabelLines;
        public List<GLineSegment> WLines;
        public List<GLineSegment> WLinesAddition;
        public List<CText> Labels;
        public List<GRect> VerticalPipes;
        public List<GRect> CondensePipes;
        public List<GRect> FloorDrains;
        public List<GRect> WaterWells;
        public List<string> WaterWellLabels;
        public List<GRect> WaterPortSymbols;
        public List<GRect> WaterPort13s;
        public List<GRect> WrappingPipes;

        public List<GRect> SideWaterBuckets;
        public List<GRect> GravityWaterBuckets;
        public List<GRect> _87WaterBuckets;

        public void Init()
        {
            LabelLines ??= new List<GLineSegment>();
            WLines ??= new List<GLineSegment>();
            WLinesAddition ??= new List<GLineSegment>();
            Labels ??= new List<CText>();
            VerticalPipes ??= new List<GRect>();
            Storeys ??= new List<GRect>();
            CondensePipes ??= new List<GRect>();
            FloorDrains ??= new List<GRect>();
            WaterWells ??= new List<GRect>();
            WaterWellLabels ??= new List<string>();
            WaterPortSymbols ??= new List<GRect>();
            WaterPort13s ??= new List<GRect>();
            WrappingPipes ??= new List<GRect>();
            SideWaterBuckets ??= new List<GRect>();
            GravityWaterBuckets ??= new List<GRect>();
            _87WaterBuckets ??= new List<GRect>();
        }
        public void FixData()
        {
            LabelLines = LabelLines.Where(x => x.Length > 0).Distinct().ToList();
            WLines = WLines.Where(x => x.Length > 0).Distinct().ToList();
            WLinesAddition = WLinesAddition.Where(x => x.Length > 0).Distinct().ToList();
            Labels = Labels.Where(x => x.Boundary.IsValid).Distinct().ToList();
            VerticalPipes = VerticalPipes.Where(x => x.IsValid).Distinct().ToList();
            Storeys = Storeys.Where(x => x.IsValid).Distinct().ToList();
            CondensePipes = CondensePipes.Where(x => x.IsValid).Distinct().ToList();
            FloorDrains = FloorDrains.Where(x => x.IsValid).Distinct().ToList();
            WaterWells = WaterWells.Where(x => x.IsValid).Distinct().ToList();
            WaterPortSymbols = WaterPortSymbols.Where(x => x.IsValid).Distinct().ToList();
            WaterPort13s = WaterPort13s.Where(x => x.IsValid).Distinct().ToList();
            WrappingPipes = WrappingPipes.Where(x => x.IsValid).Distinct().ToList();
            SideWaterBuckets = SideWaterBuckets.Where(x => x.IsValid).Distinct().ToList();
            GravityWaterBuckets = GravityWaterBuckets.Where(x => x.IsValid).Distinct().ToList();
            _87WaterBuckets = _87WaterBuckets.Where(x => x.IsValid).Distinct().ToList();
        }
        public RainSystemGeoData Clone()
        {
            return (RainSystemGeoData)MemberwiseClone();
        }
        public RainSystemGeoData DeepClone()
        {
            return this.ToCadJson().FromCadJson<RainSystemGeoData>();
        }
    }
    public class ThStoreysData
    {
        public GRect Boundary;
        public List<int> Storeys;
        public ThMEPEngineCore.Model.Common.StoreyType StoreyType;
    }
    public abstract class DrainageText
    {
        public string RawString;
        public class TLDrainageText : DrainageText
        {
            public static readonly Regex re = new Regex(@"TL(\d+)\-(\d+)");
        }
        public class FLDrainageText : DrainageText
        {
            public static readonly Regex re = new Regex(@"FL(\d+)\-(\d+)");
        }
        public class PLDrainageText : DrainageText
        {
            public static readonly Regex re = new Regex(@"PL(\d+)\-(\d+)");
        }
        public class ToiletDrainageText : DrainageText
        {
            public static readonly Regex re = new Regex(@"(接自)?(\d+)F卫生间单排");
        }
        public class KitchenDrainageText : DrainageText
        {
            public static readonly Regex re = new Regex(@"(接自)?(\d+)F厨房单排");
        }
        public class FallingBoardAreaFloorDrainText : DrainageText
        {
            //仅31F顶层板下设置乙字弯
            //一层底板设置乙字弯
            //这个不管
        }
        public class UnderboardShortTranslatorSettingsDrainageText : DrainageText
        {
            public static readonly Regex re = new Regex(@"降板区地漏DN(\d+)");

        }
    }


    public class ThDrainageSystemServiceGeoCollector
    {
        public AcadDatabase adb;
        public DrainageGeoData geoData;
        public List<Entity> entities;
        List<GLineSegment> labelLines => geoData.LabelLines;
        List<CText> cts => geoData.Labels;
        List<GLineSegment> dlines => geoData.DLines;
        List<GLineSegment> vlines => geoData.VLines;
        List<GRect> pipes => geoData.VerticalPipes;
        List<GRect> floorDrains => geoData.FloorDrains;
        List<GRect> waterPorts => geoData.WaterPorts;
        List<string> waterPortLabels => geoData.WaterPortLabels;
        List<GRect> storeys => geoData.Storeys;
        List<Point2d> cleaningPorts => geoData.CleaningPorts;
        public void CollectStoreys(Point3dCollection range)
        {
            var storeysRecEngine = new ThStoreysRecognitionEngine();
            storeysRecEngine.Recognize(adb.Database, range);
            foreach (var item in storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>())
            {
                var bd = adb.Element<Entity>(item.ObjectId).Bounds.ToGRect();
                storeys.Add(bd);
            }
        }
        public void CollectEntities()
        {
            IEnumerable<Entity> GetEntities()
            {
                foreach (var ent in adb.ModelSpace.OfType<Entity>())
                {
                    if (ent is BlockReference br)
                    {
                        if (br.Layer == "0")
                        {
                            var r = br.Bounds.ToGRect();
                            if (r.Width > 20000 && r.Width < 80000 && r.Height > 5000)
                            {
                                foreach (var e in br.ExplodeToDBObjectCollection().OfType<Entity>())
                                {
                                    yield return e;
                                }
                            }
                        }
                        else if (br.Layer == "块")
                        {
                            var r = br.Bounds.ToGRect();
                            if (r.Width > 35000 && r.Width < 80000 && r.Height > 15000)
                            {
                                foreach (var e in br.ExplodeToDBObjectCollection().OfType<Entity>())
                                {
                                    yield return e;
                                }
                            }
                        }
                        else
                        {
                            yield return ent;
                        }
                    }
                    else
                    {
                        yield return ent;
                    }
                }
            }
            var entities = GetEntities().ToList();
            this.entities = entities;
        }
        public void CollectCleaningPorts()
        {
            static bool f(string layer) => layer == "W-DRAI-EQPM";
            Point3d? pt = null;
            foreach (var e in entities.OfType<BlockReference>().Where(x => f(x.Layer) && x.ObjectId.IsValid && x.GetEffectiveName() == "清扫口系统"))
            {
                if (!pt.HasValue)
                {
                    var blockTableRecord = adb.Blocks.Element(e.BlockTableRecord);
                    var r = blockTableRecord.GeometricExtents().ToGRect();
                    pt = GeoAlgorithm.MidPoint(r.LeftButtom, r.RightButtom).ToPoint3d();
                }
                cleaningPorts.Add(pt.Value.TransformBy(e.BlockTransform).ToPoint2d());
            }
        }
        public void CollectLabelLines()
        {
            static bool f(string layer) => layer == "W-DRAI-EQPM";
            foreach (var e in entities.OfType<Line>().Where(e => f(e.Layer) && e.Length > 0))
            {
                labelLines.Add(e.ToGLineSegment());
            }
        }
        public void CollectDLines()
        {
            dlines.AddRange(GetLines(entities, layer => layer == "W-DRAI-DOME-PIPE"));
        }
        public void CollectVLines()
        {
            vlines.AddRange(GetLines(entities, layer => layer == "W-DRAI-VENT-PIPE"));
        }
        public static IEnumerable<GLineSegment> GetLines(IEnumerable<Entity> entities, Func<string, bool> f)
        {
            foreach (var e in entities.OfType<Entity>().Where(e => f(e.Layer)).ToList())
            {
                if (e is Line line && line.Length > 0)
                {
                    //wLines.Add(line.ToGLineSegment());
                    yield return line.ToGLineSegment();
                }
                else if (ThRainSystemService.IsTianZhengElement(e))
                {
                    //有些天正线炸开是两条，看上去是一条，这里当成一条来处理

                    //var lst = e.ExplodeToDBObjectCollection().OfType<Entity>().ToList();
                    //if (lst.Count == 1 && lst[0] is Line ln && ln.Length > 0)
                    //{
                    //    wLines.Add(ln.ToGLineSegment());
                    //}

                    if (GeoAlgorithm.TryConvertToLineSegment(e, out GLineSegment seg))
                    {
                        //wLines.Add(seg);
                        if (seg.Length > 0) yield return seg;
                    }
                    else foreach (var ln in e.ExplodeToDBObjectCollection().OfType<Line>())
                        {
                            if (ln.Length > 0)
                            {
                                //wLines.Add(ln.ToGLineSegment());
                                yield return ln.ToGLineSegment();
                            }
                        }
                }
            }
        }
        int distinguishDiameter = 35;
        public void CollectVerticalPipes()
        {
            static bool f(string layer) => layer == "W-DRAI-EQPM";

            {
                var pps = new List<Entity>();
                pps.AddRange(entities.OfType<BlockReference>()
                .Where(e => f(e.Layer))
                .Where(e => e.ObjectId.IsValid && e.ToDataItem().EffectiveName == "立管编号"));
                static GRect getRealBoundaryForPipe(Entity ent)
                {
                    var ents = ent.ExplodeToDBObjectCollection().OfType<Circle>().ToList();
                    var et = ents.FirstOrDefault(e => Convert.ToInt32(GeoAlgorithm.GetBoundaryRect(e).Width) == 100);
                    if (et != null) return GeoAlgorithm.GetBoundaryRect(et);
                    return GeoAlgorithm.GetBoundaryRect(ent);
                }

                foreach (var pp in pps)
                {
                    pipes.Add(getRealBoundaryForPipe(pp));
                }
            }

            {
                var pps = new List<Circle>();
                pps.AddRange(entities.OfType<Circle>()
                .Where(x => f(x.Layer))
                .Where(c => distinguishDiameter <= c.Radius && c.Radius <= 100));
                static GRect getRealBoundaryForPipe(Circle c)
                {
                    return c.Bounds.ToGRect();
                }
                foreach (var pp in pps.Distinct())
                {
                    pipes.Add(getRealBoundaryForPipe(pp));
                }
            }
        }

        public void CollectCTexts()
        {
            static bool f(string layer) => layer == "W-DRAI-EQPM";
            foreach (var e in entities.OfType<DBText>().Where(e => f(e.Layer)))
            {
                cts.Add(new CText() { Text = e.TextString, Boundary = e.Bounds.ToGRect() });
            }
            foreach (var ent in adb.ModelSpace.OfType<Entity>().Where(e => f(e.Layer) && ThRainSystemService.IsTianZhengElement(e)))
            {
                foreach (var e in ent.ExplodeToDBObjectCollection().OfType<DBText>())
                {
                    var ct = new CText() { Text = e.TextString, Boundary = e.Bounds.ToGRect() };
                    if (!ct.Boundary.IsValid)
                    {
                        var p = e.Position.ToPoint2d();
                        var h = e.Height;
                        var w = h * e.WidthFactor * e.WidthFactor * e.TextString.Length;
                        var r = new GRect(p, p.OffsetXY(w, h));
                        ct.Boundary = r;
                    }
                    cts.Add(ct);
                }
            }
        }
        public void CollectWaterPorts()
        {
            var ents = new List<BlockReference>();
            ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && x.GetEffectiveName() == "污废合流井编号"));
            waterPorts.AddRange(ents.Select(e => e.Bounds.ToGRect()));
            waterPortLabels.AddRange(ents.Select(e => e.GetAttributesStrValue("-") ?? ""));
        }
        public void CollectFloorDrains()
        {
            var ents = new List<Entity>();
            ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && (x.GetEffectiveName()?.Contains("地漏") ?? false)));
            ents.AddRange(entities.OfType<BlockReference>().Where(x => x.Layer == "W-DRAI-FLDR"));
            floorDrains.AddRange(ents.Distinct().Select(e => e.Bounds.ToGRect()));
        }
    }
    public class DrainageDrawingData
    {
        public List<string> LongTranslatorLabels;
        public List<string> ShortTranslatorLabels;
        public void Init()
        {
            LongTranslatorLabels ??= new List<string>();
            ShortTranslatorLabels ??= new List<string>();
        }
    }

    public class DrainageService
    {
        public AcadDatabase adb;
        public DrainageSystemDiagram DrainageSystemDiagram;
        public List<ThStoreysData> Storeys;
        public DrainageGeoData GeoData;
        public DrainageCadData CadDataMain;
        public List<DrainageCadData> CadDatas;
        public List<DrainageDrawingData> drawingDatas;
        public List<KeyValuePair<string, Geometry>> roomData;
        public static void TestDrawingDatasCreation(DrainageGeoData geoData)
        {
            ThDrainageService.PreFixGeoData(geoData);
            ThDrainageService.ConnectLabelToLabelLine(geoData);
            geoData.FixData();
            var cadDataMain = DrainageCadData.Create(geoData);
            var cadDatas = cadDataMain.SplitByStorey();
            TestDrawingDatasCreation(geoData, cadDataMain, cadDatas);
        }
        public static DrainageGeoData CollectGeoData()
        {
            if (commandContext != null) return null;
            Dbg.FocusMainWindow();
            var range = Dbg.TrySelectRange();
            if (range == null) return null;
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            {
                var storeys = ThRainSystemService.GetStoreys(range, adb);
                var geoData = new DrainageGeoData();
                geoData.Init();
                CollectGeoData(range, adb, geoData);
                return geoData;
            }
        }
        public static void DrawDrainageSystemDiagram2()
        {
            if (commandContext != null) return;
            Dbg.FocusMainWindow();
            var range = Dbg.TrySelectRange();
            if (range == null) return;
            //if (!Dbg.TrySelectPoint(out Point3d basePt)) return;
            if (!ThRainSystemService.ImportElementsFromStdDwg()) return;
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                try
                {
                    DU.Dispose();
                    var storeys = ThRainSystemService.GetStoreys(range, adb);
                    var geoData = new DrainageGeoData();
                    geoData.Init();
                    CollectGeoData(range, adb, geoData);
                    ThDrainageService.PreFixGeoData(geoData);
                    ThDrainageService.ConnectLabelToLabelLine(geoData);
                    geoData.FixData();
                    var cadDataMain = DrainageCadData.Create(geoData);
                    var cadDatas = cadDataMain.SplitByStorey();
                    var sv = new DrainageService()
                    {
                        adb = adb,
                        Storeys = storeys,
                        GeoData = geoData,
                        CadDataMain = cadDataMain,
                        CadDatas = cadDatas,
                    };
                    sv.CreateDrawingDatas();
                    //DU.Draw();
                    //DU.Dispose();
                    if (sv.DrainageSystemDiagram == null) sv.CreateDrainageSystemDiagram();

                    //DU.Dispose();
                    //sv.RainSystemDiagram.Draw(basePt);
                    //DU.Draw(adb);
                    //Dbg.PrintText(sv.DrawingDatas.ToCadJson());
                }
                finally
                {
                    DU.Dispose();
                }
            }
        }

        private static void CollectGeoData(Point3dCollection range, AcadDatabase adb, DrainageGeoData geoData)
        {
            var cl = new ThDrainageSystemServiceGeoCollector() { adb = adb, geoData = geoData };
            cl.CollectEntities();
            cl.CollectDLines();
            cl.CollectVLines();
            cl.CollectLabelLines();
            cl.CollectCTexts();
            cl.CollectVerticalPipes();
            cl.CollectWaterPorts();
            cl.CollectFloorDrains();
            cl.CollectCleaningPorts();
            cl.CollectStoreys(range);
        }

        public static void DrawDrainageSystemDiagram3()
        {
            Dbg.FocusMainWindow();
            if (!Dbg.TrySelectPoint(out Point3d basePt)) return;
            DU.Dispose();
            if (commandContext == null) return;
            if (commandContext.StoreyContext == null) return;
            if (commandContext.range == null) return;
            if (commandContext.StoreyContext.thStoreysDatas == null) return;
            if (!ThRainSystemService.ImportElementsFromStdDwg()) return;
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                try
                {
                    DU.Dispose();
                    var range = commandContext.range;
                    var storeys = commandContext.StoreyContext.thStoreysDatas;
                    var geoData = new DrainageGeoData();
                    geoData.Init();
                    CollectGeoData(range, adb, geoData);
                    ThDrainageService.PreFixGeoData(geoData);
                    ThDrainageService.ConnectLabelToLabelLine(geoData);
                    geoData.FixData();
                    var cadDataMain = DrainageCadData.Create(geoData);
                    var cadDatas = cadDataMain.SplitByStorey();
                    var sv = new DrainageService()
                    {
                        Storeys = storeys,
                        GeoData = geoData,
                        CadDataMain = cadDataMain,
                        CadDatas = cadDatas,
                    };
                    sv.CreateDrawingDatas();

                }
                finally
                {
                    DU.Dispose();
                }

            }

        }
        static bool AllNotEmpty(params List<Geometry>[] plss)
        {
            foreach (var pls in plss)
            {
                if (pls.Count == 0) return false;
            }
            return true;
        }
        public static ThRainSystemService.CommandContext commandContext => ThRainSystemService.commandContext;
        public void CreateDrawingDatas()
        {
            //roomData ??= CollectRoomData(adb);
            TestDrawingDatasCreation(GeoData, CadDataMain, CadDatas);
        }
        static List<GLineSegment> ExplodeGLineSegments(Geometry geo)
        {
            static IEnumerable<GLineSegment> enumerate(Geometry geo)
            {
                if (geo is LineString ls)
                {
                    if (ls.NumPoints == 2) yield return new GLineSegment(ls[0].ToPoint2d(), ls[1].ToPoint2d());
                    else if (ls.NumPoints > 2)
                    {
                        for (int i = 0; i < ls.NumPoints - 1; i++)
                        {
                            yield return new GLineSegment(ls[i].ToPoint2d(), ls[i + 1].ToPoint2d());
                        }
                    }
                }
                else if (geo is GeometryCollection colle)
                {
                    foreach (var _geo in colle.Geometries)
                    {
                        foreach (var __geo in enumerate(_geo))
                        {
                            yield return __geo;
                        }
                    }
                }
            }
            return enumerate(geo).ToList();
        }
        public static void DrawGeoData(DrainageGeoData geoData)
        {
            foreach (var s in geoData.Storeys) DU.DrawRectLazy(s).ColorIndex = 1;
            foreach (var o in geoData.LabelLines) DU.DrawLineSegmentLazy(o).ColorIndex = 1;
            foreach (var o in geoData.Labels)
            {
                DU.DrawTextLazy(o.Text, o.Boundary.LeftButtom).ColorIndex = 2;
                DU.DrawRectLazy(o.Boundary).ColorIndex = 2;
            }
            foreach (var o in geoData.VerticalPipes) DU.DrawRectLazy(o).ColorIndex = 3;
            foreach (var o in geoData.FloorDrains) DU.DrawRectLazy(o).ColorIndex = 6;
            foreach (var o in geoData.WaterPorts) DU.DrawRectLazy(o).ColorIndex = 7;
            {
                var cl = Color.FromRgb(4, 229, 230);
                foreach (var o in geoData.DLines) DU.DrawLineSegmentLazy(o).Color = cl;
            }
        }
        public static void TestDrawingDatasCreation(DrainageGeoData geoData, DrainageCadData cadDataMain, List<DrainageCadData> cadDatas)
        {
            Func<List<Geometry>, Func<Geometry, List<Geometry>>> F = GeoFac.CreateGeometrySelector;
            Func<IEnumerable<Geometry>, Geometry> G = GeoFac.CreateGeometry;
            Func<List<Geometry>, List<List<Geometry>>> GG = GeoFac.GroupGeometries;
            static List<Geometry> GeosGroupToGeos(List<List<Geometry>> geosGrp) => geosGrp.Select(lst => GeoFac.CreateGeometry(lst)).ToList();
            FengDbgTesting.AddLazyAction("画骨架", adb =>
            {
                foreach (var s in geoData.Storeys)
                {
                    var e = DU.DrawRectLazy(s);
                    e.ColorIndex = 1;
                }
                for (int storeyI = 0; storeyI < cadDatas.Count; storeyI++)
                {
                    var item = cadDatas[storeyI];
                    foreach (var o in item.LabelLines)
                    {
                        var j = cadDataMain.LabelLines.IndexOf(o);
                        var m = geoData.LabelLines[j];
                        var e = DU.DrawLineSegmentLazy(m);
                        e.ColorIndex = 1;
                    }
                    foreach (var pl in item.Labels)
                    {
                        var j = cadDataMain.Labels.IndexOf(pl);
                        var m = geoData.Labels[j];
                        var e = DU.DrawTextLazy(m.Text, m.Boundary.LeftButtom);
                        e.ColorIndex = 2;
                        var _pl = DU.DrawRectLazy(m.Boundary);
                        _pl.ColorIndex = 2;
                    }
                    foreach (var o in item.VerticalPipes)
                    {
                        var j = cadDataMain.VerticalPipes.IndexOf(o);
                        var m = geoData.VerticalPipes[j];
                        var e = DU.DrawRectLazy(m);
                        e.ColorIndex = 3;
                    }
                    foreach (var o in item.FloorDrains)
                    {
                        var j = cadDataMain.FloorDrains.IndexOf(o);
                        var m = geoData.FloorDrains[j];
                        var e = DU.DrawRectLazy(m);
                        e.ColorIndex = 6;
                    }
                    foreach (var o in item.WaterPorts)
                    {
                        var j = cadDataMain.WaterPorts.IndexOf(o);
                        var m = geoData.WaterPorts[j];
                        var e = DU.DrawRectLazy(m);
                        e.ColorIndex = 7;
                    }
                    foreach (var o in item.WashingMachines)
                    {
                        var j = cadDataMain.WashingMachines.IndexOf(o);
                        var m = geoData.WashingMachines[j];
                        var e = DU.DrawRectLazy(m);
                        e.ColorIndex = 1;
                    }
                    foreach (var o in item.CleaningPorts)
                    {
                        var j = cadDataMain.CleaningPorts.IndexOf(o);
                        var m = geoData.CleaningPorts[j];
                        if (false) DU.DrawGeometryLazy(new GCircle(m, 50).ToCirclePolygon(36), ents => ents.ForEach(e => e.ColorIndex = 7));
                        DU.DrawRectLazy(GRect.Create(m, 50));
                    }
                    {
                        var cl = Color.FromRgb(4, 229, 230);
                        foreach (var o in item.DLines)
                        {
                            var j = cadDataMain.DLines.IndexOf(o);
                            var m = geoData.DLines[j];
                            var e = DU.DrawLineSegmentLazy(m);
                            e.Color = cl;
                        }
                    }
                }
            });
            FengDbgTesting.AddLazyAction("开始分析", adb =>
            {
                foreach (var s in geoData.Storeys)
                {
                    var e = DU.DrawRectLazy(s).ColorIndex = 1;
                }
                var sb = new StringBuilder(8192);
                for (int storeyI = 0; storeyI < cadDatas.Count; storeyI++)
                {
                    sb.AppendLine($"===框{storeyI}===");
                    var drData = new DrainageDrawingData();
                    drData.Init();
                    var item = cadDatas[storeyI];

                    {
                        var maxDis = 8000;
                        var angleTolleranceDegree = 1;
                        var waterPortCvt = DrainageCadData.ConvertWaterPortsLargerF();
                        var lines = GeoFac.AutoConn(item.DLines.Where(x => x.Length > 0).Distinct().ToList().Select(cadDataMain.DLines).ToList(geoData.DLines).ToList(),
                            GeoFac.CreateGeometryEx(item.VerticalPipes.Concat(item.FloorDrains).Concat(item.WaterPorts.Select(cadDataMain.WaterPorts).ToList(geoData.WaterPorts).Select(waterPortCvt)).ToList()),
                            maxDis, angleTolleranceDegree).ToList();
                        geoData.DLines.AddRange(lines);
                        var dlineCvt = DrainageCadData.ConvertDLinesF();
                        var _lines = lines.Select(dlineCvt).ToList();
                        cadDataMain.DLines.AddRange(_lines);
                        item.DLines.AddRange(_lines);
                    }

                    var lbDict = new Dictionary<Geometry, string>();
                    var notedPipesDict = new Dictionary<Geometry, string>();
                    var labelLinesGroup = GG(item.LabelLines);
                    var labelLinesGeos = GeosGroupToGeos(labelLinesGroup);
                    var labellinesf = F(labelLinesGeos);
                    var shortTranslatorLabels = new HashSet<string>();
                    var longTranslatorLabels = new HashSet<string>();
                    var dlinesGroups = GG(item.DLines);
                    var dlinesGeos = GeosGroupToGeos(dlinesGroups);
                    var vlinesGroups = GG(item.VLines);
                    var vlinesGeos = GeosGroupToGeos(vlinesGroups);

                    {
                        var f = F(item.VerticalPipes);
                        foreach (var label in item.Labels)
                        {
                            if (!ThDrainageService.IsWantedLabelText(geoData.Labels[cadDataMain.Labels.IndexOf(label)].Text)) continue;
                            var lst = labellinesf(label);
                            if (lst.Count == 1)
                            {
                                var labelline = lst[0];
                                if (f(GeoFac.CreateGeometry(label, labelline)).Count == 0)
                                {
                                    var lines = ExplodeGLineSegments(labelline);
                                    var points = GeoFac.GetLabelLineEndPoints(lines, label);
                                    if (points.Count == 1)
                                    {
                                        var pt = points[0];
                                        var r = GRect.Create(pt, 50);
                                        geoData.VerticalPipes.Add(r);
                                        var pl = r.ToPolygon();
                                        cadDataMain.VerticalPipes.Add(pl);
                                        item.VerticalPipes.Add(pl);
                                    }
                                }

                            }
                        }
                    }


                    DU.DrawTextLazy($"===框{storeyI}===", geoData.Storeys[storeyI].LeftTop);
                    foreach (var o in item.LabelLines)
                    {
                        DU.DrawLineSegmentLazy(geoData.LabelLines[cadDataMain.LabelLines.IndexOf(o)]).ColorIndex = 1;
                    }
                    foreach (var pl in item.Labels)
                    {
                        var m = geoData.Labels[cadDataMain.Labels.IndexOf(pl)];
                        var e = DU.DrawTextLazy(m.Text, m.Boundary.LeftButtom.ToPoint3d());
                        e.ColorIndex = 2;
                        var _pl = DU.DrawRectLazy(m.Boundary);
                        _pl.ColorIndex = 2;
                    }
                    foreach (var o in item.VerticalPipes)
                    {
                        DU.DrawRectLazy(geoData.VerticalPipes[cadDataMain.VerticalPipes.IndexOf(o)]).ColorIndex = 3;
                    }
                    foreach (var o in item.FloorDrains)
                    {
                        DU.DrawRectLazy(geoData.FloorDrains[cadDataMain.FloorDrains.IndexOf(o)]).ColorIndex = 6;
                    }
                    foreach (var o in item.WaterPorts)
                    {
                        DU.DrawRectLazy(geoData.WaterPorts[cadDataMain.WaterPorts.IndexOf(o)]).ColorIndex = 7;
                        DU.DrawTextLazy(geoData.WaterPortLabels[cadDataMain.WaterPorts.IndexOf(o)], o.GetCenter());
                    }
                    foreach (var o in item.WashingMachines)
                    {
                        var e = DU.DrawRectLazy(geoData.WashingMachines[cadDataMain.WashingMachines.IndexOf(o)]).ColorIndex = 1;
                    }
                    foreach (var o in item.CleaningPorts)
                    {
                        var m = geoData.CleaningPorts[cadDataMain.CleaningPorts.IndexOf(o)];
                        if (false) DU.DrawGeometryLazy(new GCircle(m, 50).ToCirclePolygon(36), ents => ents.ForEach(e => e.ColorIndex = 7));
                        DU.DrawRectLazy(GRect.Create(m, 40));
                    }
                    {
                        var cl = Color.FromRgb(4, 229, 230);
                        foreach (var o in item.DLines)
                        {
                            var e = DU.DrawLineSegmentLazy(geoData.DLines[cadDataMain.DLines.IndexOf(o)]).Color = cl;
                        }
                    }
                    {
                        var cl = Color.FromRgb(211, 213, 111);
                        foreach (var o in item.VLines)
                        {
                            DU.DrawLineSegmentLazy(geoData.VLines[cadDataMain.VLines.IndexOf(o)]).Color = cl;
                        }
                    }



                    {
                        //通过引线进行标注
                        var ok_ents = new HashSet<Geometry>();
                        for (int i = 0; i < 3; i++)
                        {
                            //先处理最简单的case
                            var ok = false;
                            var labelsf = F(item.Labels.Except(ok_ents).ToList());
                            var pipesf = F(item.VerticalPipes.Except(ok_ents).ToList());
                            foreach (var labelLinesGeo in labelLinesGeos)
                            {
                                var labels = labelsf(labelLinesGeo);
                                var pipes = pipesf(labelLinesGeo);
                                if (labels.Count == 1 && pipes.Count == 1)
                                {
                                    var lb = labels[0];
                                    var pp = pipes[0];
                                    var label = geoData.Labels[cadDataMain.Labels.IndexOf(lb)].Text ?? "";
                                    if (ThDrainageService.IsWantedLabelText(label))
                                    {
                                        lbDict[pp] = label;
                                        ok_ents.Add(pp);
                                        ok_ents.Add(lb);
                                        ok = true;
                                    }
                                    else if (ThDrainageService.IsNotedLabel(label))
                                    {
                                        notedPipesDict[pp] = label;
                                        ok_ents.Add(lb);
                                        ok = true;
                                    }
                                }
                            }
                            if (!ok) break;
                        }

                        for (int i = 0; i < 3; i++)
                        {
                            //再处理多个一起串的case
                            var ok = false;
                            var labelsf = F(item.Labels.Except(ok_ents).ToList());
                            var pipesf = F(item.VerticalPipes.Except(ok_ents).ToList());
                            foreach (var labelLinesGeo in labelLinesGeos)
                            {
                                var labels = labelsf(labelLinesGeo);
                                var pipes = pipesf(labelLinesGeo);
                                if (labels.Count == pipes.Count && labels.Count > 0)
                                {
                                    var labelsTxts = labels.Select(lb => geoData.Labels[cadDataMain.Labels.IndexOf(lb)].Text ?? "").ToList();
                                    if (labelsTxts.All(txt => ThDrainageService.IsWantedLabelText(txt)))
                                    {
                                        pipes = ThRainSystemService.SortGeometrysBy2DSpacePosition(pipes).ToList();
                                        labels = ThRainSystemService.SortGeometrysBy2DSpacePosition(labels).ToList();
                                        for (int k = 0; k < pipes.Count; k++)
                                        {
                                            var pp = pipes[k];
                                            var lb = labels[k];
                                            var j = cadDataMain.Labels.IndexOf(lb);
                                            var m = geoData.Labels[j];
                                            var label = m.Text;
                                            lbDict[pp] = label;
                                        }
                                        //OK，识别成功
                                        ok_ents.AddRange(pipes);
                                        ok_ents.AddRange(labels);
                                        ok = true;
                                    }
                                }
                            }
                            if (!ok) break;
                        }

                        {
                            //对付擦边球case
                            foreach (var label in item.Labels.Except(ok_ents).ToList())
                            {
                                var lb = geoData.Labels[cadDataMain.Labels.IndexOf(label)].Text ?? "";
                                if (!ThDrainageService.IsWantedLabelText(lb)) continue;
                                var lst = labellinesf(label);
                                if (lst.Count == 1)
                                {
                                    var labelline = lst[0];
                                    var lines = ExplodeGLineSegments(labelline);
                                    var points = GeoFac.GetLabelLineEndPoints(lines, label);
                                    if (points.Count == 1)
                                    {
                                        var pipes = F(item.VerticalPipes.Except(lbDict.Keys).ToList())(points[0].ToNTSPoint());
                                        if (pipes.Count == 1)
                                        {
                                            var pp = pipes[0];
                                            lbDict[pp] = lb;
                                            ok_ents.Add(pp);
                                            ok_ents.Add(label);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    (List<Geometry>, List<Geometry>) getPipes()
                    {
                        var pipes1 = new List<Geometry>(lbDict.Count);
                        var pipes2 = new List<Geometry>(lbDict.Count);
                        foreach (var pipe in item.VerticalPipes) if (lbDict.ContainsKey(pipe)) pipes1.Add(pipe); else pipes2.Add(pipe);
                        return (pipes1, pipes2);
                    }
                    {
                        //识别转管，顺便进行标注

                        bool recognise1()
                        {
                            var ok = false;
                            for (int i = 0; i < 3; i++)
                            {
                                var (pipes1, pipes2) = getPipes();
                                var pipes1f = F(pipes1);
                                var pipes2f = F(pipes2);
                                foreach (var dlinesGeo in dlinesGeos)
                                {
                                    var lst1 = pipes1f(dlinesGeo);
                                    var lst2 = pipes2f(dlinesGeo);
                                    if (lst1.Count == 1 && lst2.Count > 0)
                                    {
                                        var pp1 = lst1[0];
                                        var label = lbDict[pp1];
                                        var c = pp1.GetCenter();
                                        foreach (var pp2 in lst2)
                                        {
                                            var dis = c.GetDistanceTo(pp2.GetCenter());
                                            if (10 < dis && dis <= MAX_SHORTTRANSLATOR_DISTANCE)
                                            {
                                                //通气立管没有乙字弯
                                                if (!label.StartsWith("TL"))
                                                {
                                                    shortTranslatorLabels.Add(label);
                                                    lbDict[pp2] = label;
                                                    ok = true;
                                                }
                                            }
                                            else if (dis > MAX_SHORTTRANSLATOR_DISTANCE)
                                            {
                                                longTranslatorLabels.Add(label);
                                                lbDict[pp2] = label;
                                                ok = true;
                                            }
                                        }
                                    }
                                }
                                if (!ok) break;
                            }
                            return ok;
                        }
                        bool recognise2()
                        {
                            var ok = false;
                            for (int i = 0; i < 3; i++)
                            {
                                var (pipes1, pipes2) = getPipes();
                                var pipes1f = F(pipes1);
                                foreach (var pp2 in pipes2)
                                {
                                    var pps1 = pipes1f(pp2.ToGRect().Expand(5).ToGCircle(false).ToCirclePolygon(6));
                                    var fs = new List<Action>();
                                    foreach (var pp1 in pps1)
                                    {
                                        var label = lbDict[pp1];
                                        //通气立管没有乙字弯
                                        if (!label.StartsWith("TL"))
                                        {
                                            if (pp1.GetCenter().GetDistanceTo(pp2.GetCenter()) > 1)
                                            {
                                                fs.Add(() =>
                                                {
                                                    shortTranslatorLabels.Add(label);
                                                    lbDict[pp2] = label;
                                                    ok = true;
                                                });
                                            }
                                        }
                                    }
                                    if (fs.Count == 1) fs[0]();
                                }
                                if (!ok) break;
                            }
                            return ok;
                        }
                        for (int i = 0; i < 3; i++)
                        {
                            if (!(recognise1() && recognise2())) break;
                        }
                    }
                    {
                        var pipes1f = F(lbDict.Where(kv => kv.Value.StartsWith("TL")).Select(kv => kv.Key).ToList());
                        var pipes2f = F(item.VerticalPipes.Where(p => !lbDict.ContainsKey(p)).ToList());
                        foreach (var vlinesGeo in vlinesGeos)
                        {
                            var lst = pipes1f(vlinesGeo);
                            if (lst.Count == 1)
                            {
                                var pp1 = lst[0];
                                lst = pipes2f(vlinesGeo);
                                if (lst.Count == 1)
                                {
                                    var pp2 = lst[0];
                                    if (pp1.GetCenter().GetDistanceTo(pp2.GetCenter()) > MAX_SHORTTRANSLATOR_DISTANCE)
                                    {
                                        var label = lbDict[pp1];
                                        longTranslatorLabels.Add(label);
                                        lbDict[pp2] = label;
                                    }
                                }
                            }
                        }
                    }


                    //“仅31F顶层板下设置乙字弯”的处理（😉不处理）

                    //标出所有的立管编号（看看识别成功了没）
                    foreach (var pp in item.VerticalPipes)
                    {
                        lbDict.TryGetValue(pp, out string label);
                        if (label != null)
                        {
                            DU.DrawTextLazy(label, pp.ToGRect().LeftTop.ToPoint3d());
                        }
                    }



                    {
                        //获取排出编号

                        var f1 = F(item.WaterPorts);

                        var ok_ents = new HashSet<Geometry>();
                        var d = new Dictionary<string, string>();


                        {
                            //先提取直接连接的
                            var f2 = F(item.VerticalPipes.Except(ok_ents).ToList());
                            foreach (var dlinesGeo in dlinesGeos)
                            {
                                var waterPorts = f1(dlinesGeo);
                                if (waterPorts.Count == 1)
                                {
                                    var waterPort = waterPorts[0];
                                    var pipes = f2(dlinesGeo);
                                    ok_ents.AddRange(pipes);
                                    foreach (var pipe in pipes)
                                    {
                                        if (lbDict.TryGetValue(pipe, out string label))
                                        {
                                            d[label] = geoData.WaterPortLabels[cadDataMain.WaterPorts.IndexOf(waterPort)];
                                        }
                                    }
                                }
                            }
                        }
                        {
                            //再处理没直接连接的
                            var f2 = F(item.VerticalPipes.Except(ok_ents).ToList());
                            var radius = 10;
                            var f5 = GeoFac.NearestNeighbourPoint3dF(item.WaterPorts);
                            foreach (var dlinesGeo in dlinesGeos)
                            {
                                var segs = ExplodeGLineSegments(dlinesGeo);
                                var pts = GeoFac.GetAlivePoints(segs.Distinct().ToList(), radius: radius);
                                {
                                    var _pts = pts.Select(x => new GCircle(x, radius).ToCirclePolygon(6, false)).ToGeometryList();
                                    var killer = GeoFac.CreateGeometryEx(item.VerticalPipes.Concat(item.WaterPorts).Concat(item.CleaningPorts).Concat(item.FloorDrains).Distinct().ToList());
                                    pts = pts.Except(F(_pts)(killer).Select(_pts).ToList(pts)).ToList();
                                }
                                foreach (var pt in pts)
                                {
                                    var waterPort = f5(pt.ToPoint3d());
                                    if (waterPort != null)
                                    {
                                        if (waterPort.GetCenter().GetDistanceTo(pt) <= 1500)
                                        {
                                            var waterPortLabel = geoData.WaterPortLabels[cadDataMain.WaterPorts.IndexOf(waterPort)];
                                            foreach (var pipe in f2(dlinesGeo))
                                            {
                                                if (lbDict.TryGetValue(pipe, out string label))
                                                {
                                                    d[label] = waterPortLabel;
                                                    ok_ents.Add(pipe);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        {
                            sb.AppendLine("排出：" + d.ToJson());


                            d.Join(lbDict, kv => kv.Key, kv => kv.Value, (kv1, kv2) =>
                            {
                                var num = kv1.Value;
                                var pipe = kv2.Key;
                                DU.DrawTextLazy(num, pipe.ToGRect().RightButtom);
                                return 666;
                            }).Count();
                        }


                    }

                    {
                        var _longTranslatorLabels = longTranslatorLabels.Distinct().ToList();
                        _longTranslatorLabels.Sort();
                        sb.AppendLine("长转管:" + _longTranslatorLabels.JoinWith(","));
                        drData.LongTranslatorLabels.AddRange(_longTranslatorLabels);
                    }

                    {
                        var _shortTranslatorLabels = shortTranslatorLabels.ToList();
                        _shortTranslatorLabels.Sort();
                        sb.AppendLine("短转管:" + _shortTranslatorLabels.JoinWith(","));
                        drData.ShortTranslatorLabels.AddRange(_shortTranslatorLabels);
                    }


                }
                Dbg.PrintText(sb.ToString());
            });
        }
        const double MAX_SHORTTRANSLATOR_DISTANCE = 150;

        public void CreateDrainageSystemDiagram()
        {

        }
        public static List<T> ToList<T>(IEnumerable<T> source1, IEnumerable<T> source2)
        {
            return source1.Concat(source2).ToList();
        }
        public static List<T> ToList<T>(IEnumerable<T> source1, IEnumerable<T> source2, IEnumerable<T> source3)
        {
            return source1.Concat(source2).Concat(source3).ToList();
        }
        public static List<KeyValuePair<string, Geometry>> CollectRoomData(AcadDatabase adb)
        {
            var ranges = adb.ModelSpace.OfType<Polyline>().Where(x => x.Layer == "AI-空间框线").Select(x => x.ToNTSPolygon()).Cast<Geometry>().ToList();
            var names = adb.ModelSpace.OfType<MText>().Where(x => x.Layer == "AI-空间名称").Select(x => new CText() { Text = x.Text, Boundary = x.Bounds.ToGRect() }).ToList();
            var f = GeoFac.CreateGeometrySelector(ranges);
            var list = new List<KeyValuePair<string, Geometry>>(names.Count);
            foreach (var name in names)
            {
                if (name.Boundary.IsValid)
                {
                    var l = f(name.Boundary.ToPolygon());
                    if (l.Count == 1)
                    {
                        list.Add(new KeyValuePair<string, Geometry>(name.Text, l[0]));
                    }
                    else
                    {
                        foreach (var geo in l)
                        {
                            ranges.Remove(geo);
                        }
                    }
                }
            }
            foreach (var range in ranges.Except(list.Select(kv => kv.Value)))
            {
                list.Add(new KeyValuePair<string, Geometry>("", range));
            }
            return list;
        }
        public static List<Geometry> GetKitchenOnlyFLs(List<Geometry> FLs, List<Geometry> kitchens, List<Geometry> nonames, List<Geometry> balconies)
        {
            //6.3.2	只负责厨房的FL
            //-	判断方法
            //找到所有的FL立管，若：
            //1）	FL的500范围内有厨房空间，或
            //2）	以FL圆心为起点，以图层为W-DRAI-DOME-PIPE或W-DRAI-WAST-PIPE水平水管的管线结构的任意一个结束点在厨房空间内
            //且以FL圆心为起点，以图层为W-DRAI-DOME-PIPE或W-DRAI-WAST-PIPE水平水管的管线结构的任意所有结束点不在没有名称的空间或不在阳台空间内。
            var kitchensGeo = GeoFac.CreateGeometry(kitchens);
            var list = new List<Geometry>(FLs.Count);
            foreach (var fl in FLs)
            {
                List<Geometry> endpoints = null;
                Geometry endpointsGeo = null;
                List<Geometry> _GetEndPoints()
                {
                    return GetEndPoints(fl.GetCenter().ToNTSPoint());
                }
                bool test1()
                {
                    return kitchensGeo.Intersects(GeoFac.CreateCirclePolygon(fl.GetCenter().ToPoint3d(), 500, 36));
                }
                bool test2()
                {
                    endpoints ??= _GetEndPoints();
                    endpointsGeo ??= GeoFac.CreateGeometry(endpoints);
                    return kitchensGeo.Intersects(endpointsGeo);
                }
                bool test3()
                {
                    endpoints ??= _GetEndPoints();
                    endpointsGeo ??= GeoFac.CreateGeometry(endpoints);
                    return endpointsGeo.Intersects(GeoFac.CreateGeometry(ToList(nonames, balconies)));
                }
                if ((test1() || test2()) && test3())
                {
                    list.Add(fl);
                }
            }
            return list;
        }
        //找出负担地漏下水点的FL
        public static HashSet<Geometry> GetFLsWhereSupportingFloorDrainUnderWaterPoint(List<Geometry> FLs, List<Geometry> kitchens, List<Geometry> floorDrains, List<Geometry> washMachines)
        {
            var f = GeoFac.CreateGeometrySelector(ToList(floorDrains, washMachines));
            var hs = new HashSet<Geometry>();
            {
                var flsf = GeoFac.CreateGeometrySelector(FLs);
                foreach (var kitchen in kitchens)
                {
                    var lst = flsf(kitchen);
                    if (lst.Count > 0)
                    {
                        if (f(kitchen).Count > 0)
                        {
                            hs.AddRange(lst);
                        }
                    }
                }
            }
            return hs;
        }
        static List<Geometry> GetEndPoints(Point start)
        {
            //以图层为W-DRAI-DOME-PIPE或W-DRAI-WAST-PIPE水平水管的管线结构的
            //todo
            throw new NotImplementedException();
        }
        public static List<Geometry> GetBalconyOnlyFLs(List<Geometry> FLs, List<Geometry> kitchens, List<Geometry> nonames, List<Geometry> balconies)
        {
            //6.3.3	只负责阳台的FL
            //-	判断方法
            //找到所有的FL立管，若：
            //1）	以FL圆心为起点，以图层为W-DRAI-DOME-PIPE或W-DRAI-WAST-PIPE水平水管的管线结构的任意一个结束点在没有名称的空间或在阳台空间内。且
            //2）	FL的500范围内没有厨房空间，且
            //3）	以FL圆心为起点，以图层为W-DRAI-DOME-PIPE或W-DRAI-WAST-PIPE水平水管的管线结构的所有结束点不在在厨房空间内
            var kitchensGeo = GeoFac.CreateGeometry(kitchens);
            var list = new List<Geometry>(FLs.Count);
            foreach (var fl in FLs)
            {
                List<Geometry> endpoints = null;
                Geometry endpointsGeo = null;
                List<Geometry> _GetEndPoints()
                {
                    return GetEndPoints(fl.GetCenter().ToNTSPoint());
                }
                bool test1()
                {
                    endpoints ??= _GetEndPoints();
                    endpointsGeo ??= GeoFac.CreateGeometry(endpoints);
                    return endpointsGeo.Intersects(GeoFac.CreateGeometry(ToList(nonames, balconies)));
                }
                bool test2()
                {
                    return !GeoFac.CreateCirclePolygon(fl.GetCenter().ToPoint3d(), 500, 36).Intersects(kitchensGeo);
                }
                bool test3()
                {
                    endpoints ??= _GetEndPoints();
                    endpointsGeo ??= GeoFac.CreateGeometry(endpoints);
                    return !kitchensGeo.Intersects(endpointsGeo);
                }
                if (test1() && test2() && test3())
                {
                    list.Add(fl);
                }
            }
            return list;
        }
        public static List<Geometry> GetKitchenAndBalconyBothFLs(List<Geometry> FLs, List<Geometry> kitchens, List<Geometry> nonames, List<Geometry> balconies)
        {
            //6.3.4	厨房阳台兼用FL
            //-	判断方法
            //1)	FL的500范围内有厨房空间，且
            //2)	以FL圆心为起点，以图层为W-DRAI-DOME-PIPE或W-DRAI-WAST-PIPE水平水管的管线结构的任意一个结束点在没有名称的空间或在阳台空间内。
            var kitchensGeo = GeoFac.CreateGeometry(kitchens);
            var list = new List<Geometry>(FLs.Count);
            foreach (var fl in FLs)
            {
                List<Geometry> endpoints = null;
                Geometry endpointsGeo = null;
                List<Geometry> _GetEndPoints()
                {
                    return GetEndPoints(fl.GetCenter().ToNTSPoint());
                }
                bool test1()
                {
                    return !GeoFac.CreateCirclePolygon(fl.GetCenter().ToPoint3d(), 500, 36).Intersects(kitchensGeo);
                }
                bool test2()
                {
                    endpoints ??= _GetEndPoints();
                    endpointsGeo ??= GeoFac.CreateGeometry(endpoints);
                    return endpointsGeo.Intersects(GeoFac.CreateGeometry(ToList(nonames, balconies)));
                }
                if (test1() && test2())
                {
                    list.Add(fl);
                }
            }
            return list;
        }
    }
    public class DrainageSystemDiagram
    {
        public static readonly Point2d[] LONG_TRANSLATOR_POINTS = new Point2d[] { new Point2d(0, 0), new Point2d(-121, -121), new Point2d(-1379, -121), new Point2d(-1500, -241) };
        public static readonly Point2d[] SHORT_TRANSLATOR_POINTS = new Point2d[] { new Point2d(0, 0), new Point2d(-121, -121) };
        public static double LONG_TRANSLATOR_HEIGHT1 = 780;
        public static double CHECKPOINT_OFFSET_Y = 580;
        public static readonly Point2d[] LEFT_LONG_TRANSLATOR_CLEANING_PORT_POINTS = new Point2d[] { new Point2d(0, 0), new Point2d(-200, 200), new Point2d(-200, 500), new Point2d(-79, 621), new Point2d(1029, 621), new Point2d(1150, 741), new Point2d(1150, 1021) };
        public static void DrawStoreyLine(string label, Point3d basePt, double lineLen)
        {
            {
                var line = DU.DrawLineLazy(basePt.X, basePt.Y, basePt.X + lineLen, basePt.Y);
                var dbt = DU.DrawTextLazy(label, ThWSDStorey.TEXT_HEIGHT, new Point3d(basePt.X + ThWSDStorey.INDEX_TEXT_OFFSET_X, basePt.Y + ThWSDStorey.INDEX_TEXT_OFFSET_Y, 0));
                Dr.SetLabelStylesForWNote(line, dbt);
            }
            if (label == "RF")
            {
                var line = DU.DrawLineLazy(new Point3d(basePt.X + ThWSDStorey.INDEX_TEXT_OFFSET_X, basePt.Y + ThWSDStorey.RF_OFFSET_Y, 0), new Point3d(basePt.X + lineLen, basePt.Y + ThWSDStorey.RF_OFFSET_Y, 0));
                var dbt = DU.DrawTextLazy("建筑完成面", ThWSDStorey.TEXT_HEIGHT, new Point3d(basePt.X + ThWSDStorey.INDEX_TEXT_OFFSET_X, basePt.Y + ThWSDStorey.RF_OFFSET_Y + ThWSDStorey.INDEX_TEXT_OFFSET_Y, 0));
                Dr.SetLabelStylesForWNote(line, dbt);
            }
        }
        public static IEnumerable<Point3d> GetBasePoints(Point3d basePoint, int maxCol, int num, double width, double height)
        {
            int i = 0, j = 0;
            for (int k = 0; k < num; k++)
            {
                yield return new Point3d(basePoint.X + i * width, basePoint.Y - j * height, 0);
                i++;
                if (i >= maxCol)
                {
                    j++;
                    i = 0;
                }
            }
        }
        public static void DrawWashBasin(Point3d basePt, bool leftOrRight)
        {
            if (leftOrRight)
            {
                DU.DrawBlockReference("洗涤盆排水", basePt, br =>
                {
                    br.Layer = "W-DRAI-EQPM";
                    br.ScaleFactors = new Scale3d(2, 2, 2);
                    if (br.IsDynamicBlock)
                    {
                        br.ObjectId.SetDynBlockValue("可见性", "双池S弯");
                    }
                });
            }
            else
            {
                DU.DrawBlockReference("洗涤盆排水", basePt, br =>
                {
                    br.Layer = "W-DRAI-EQPM";
                    br.ScaleFactors = new Scale3d(-2, 2, 2);
                    if (br.IsDynamicBlock)
                    {
                        br.ObjectId.SetDynBlockValue("可见性", "双池S弯");
                    }
                });
            }
        }
        public static void DrawDoubleWashBasins(Point3d basePt, bool leftOrRight)
        {
            basePt = basePt.OffsetY(300);
            if (leftOrRight)
            {
                DU.DrawBlockReference("双格洗涤盆排水", basePt,
                  br =>
                  {
                      br.Layer = "W-DRAI-EQPM";
                      br.ScaleFactors = new Scale3d(2, 2, 2);
                      if (br.IsDynamicBlock)
                      {
                          br.ObjectId.SetDynBlockValue("可见性", "双池S弯");
                      }
                  });
            }
            else
            {
                DU.DrawBlockReference("双格洗涤盆排水", basePt,
                  br =>
                  {
                      br.Layer = "W-DRAI-EQPM";
                      br.ScaleFactors = new Scale3d(-2, 2, 2);
                      if (br.IsDynamicBlock)
                      {
                          br.ObjectId.SetDynBlockValue("可见性", "双池S弯");
                      }
                  });
            }
        }
        public void Draw(Point3d basePoint)
        {
            draw1(basePoint);
        }

        public static void draw1(Point3d basePoint)
        {
            var OFFSET_X = 2500.0;
            var SPAN_X = 5500.0;
            var HEIGHT = 1800.0;
            //var HEIGHT = 5000.0;
            var COUNT = 20;

            var lineLen = OFFSET_X + COUNT * SPAN_X + OFFSET_X;
            var storeys = Enumerable.Range(1, 32).Select(i => i + "F").Concat(new string[] { "RF", "RF+1", "RF+2" }).ToList();
            for (int i = 0; i < storeys.Count; i++)
            {
                var storey = storeys[i];
                var bsPt1 = basePoint.OffsetY(HEIGHT * i);
                DrawStoreyLine(storey, bsPt1, lineLen);
            }
            var outputStartPointOffsets = new Vector2d[COUNT];

            {
                var start = storeys.Count - 1;
                var end = 0;
                for (int j = 0; j < COUNT; j++)
                {
                    var v = default(Vector2d);
                    for (int i = start; i >= end; i--)
                    {
                        var storey = storeys[i];
                        var bsPt1 = basePoint.OffsetY(HEIGHT * i);
                        {
                            var basePt = bsPt1.OffsetX(OFFSET_X + (j + 1) * SPAN_X) + v.ToVector3d();
                            if (i != start)
                            {
                                //long translator left
                                if (storey == "3F")
                                {
                                    var lastPt = NewMethod4(HEIGHT, ref v, basePt);

                                    {
                                        var startPoint = lastPt.TransformBy(basePt.OffsetY(HEIGHT - LONG_TRANSLATOR_HEIGHT1)).OffsetY(-300);
                                        var segs = LEFT_LONG_TRANSLATOR_CLEANING_PORT_POINTS.ToGLineSegments(startPoint.ToPoint3d());
                                        DrawDomePipes(segs);
                                        DrawCleaningPort(segs.Last().EndPoint.ToPoint3d(), true, 2);
                                    }

                                    DrawHorizontalLineOnPipeRun(HEIGHT, basePt);
                                }
                                else if (storey == "12F")
                                {
                                    {
                                        var lastPt = NewMethod4(HEIGHT, ref v, basePt);
                                        var startPoint = lastPt.TransformBy(basePt.OffsetY(HEIGHT - LONG_TRANSLATOR_HEIGHT1)).OffsetY(-300).OffsetY(221 - 90);
                                        var points = new Point2d[] { new Point2d(0, 0), new Point2d(-200, 200), new Point2d(-200, 679), new Point2d(-321, 800), new Point2d(-1110, 800), new Point2d(-2380, 800) };
                                        var p0 = points.Last().TransformBy(startPoint);
                                        var p1 = p0.OffsetX(-180);
                                        var p2 = p1.OffsetX(-1000);
                                        {
                                            DrawFloorDrain(p0.ToPoint3d(), true);
                                            DrawFloorDrain(p2.ToPoint3d(), true);
                                            var p3 = points[4].TransformBy(startPoint).ToPoint3d();
                                            var p4 = p3.OffsetXY(-90, 90);
                                            DrawDomePipes(new GLineSegment(p3, p4));
                                            DrawWashBasin(p4, true);
                                        }

                                        var segs = points.ToGLineSegments(startPoint.ToPoint3d());
                                        DrawDomePipes(segs);
                                        DrawDomePipes(new GLineSegment(p1, p2));
                                    }
                                    DrawHorizontalLineOnPipeRun(HEIGHT, basePt);
                                }
                                else if (storey == "11F")
                                {
                                    {
                                        var lastPt = NewMethod4(HEIGHT, ref v, basePt);
                                        var startPoint = lastPt.TransformBy(basePt.OffsetY(HEIGHT - LONG_TRANSLATOR_HEIGHT1)).OffsetY(-300).OffsetY(221 - 90);
                                        var points = new Point2d[] { new Point2d(0, 0), new Point2d(-200, 200), new Point2d(-200, 679), new Point2d(-321, 800), new Point2d(-1110, 800), new Point2d(-2380, 800) };
                                        var p0 = points.Last().TransformBy(startPoint);
                                        var p1 = p0.OffsetX(-180);
                                        var p2 = p1.OffsetX(-1000);
                                        {
                                            DrawFloorDrain(p0.ToPoint3d(), true);
                                            DrawFloorDrain(p2.ToPoint3d(), true);
                                            var p3 = points[4].TransformBy(startPoint).ToPoint3d();
                                            var p4 = p3.OffsetXY(-90, 90);
                                            DrawDomePipes(new GLineSegment(p3, p4));
                                            DrawDoubleWashBasins(p4, true);
                                        }

                                        var segs = points.ToGLineSegments(startPoint.ToPoint3d());
                                        DrawDomePipes(segs);
                                        DrawDomePipes(new GLineSegment(p1, p2));
                                    }
                                    DrawHorizontalLineOnPipeRun(HEIGHT, basePt);
                                }
                                else if (storey == "10F")
                                {
                                    {
                                        var lastPt = NewMethod4(HEIGHT, ref v, basePt);
                                        var startPoint = lastPt.TransformBy(basePt.OffsetY(HEIGHT - LONG_TRANSLATOR_HEIGHT1)).OffsetY(-300).OffsetY(221 - 90);
                                        var points = new Point2d[] { new Point2d(0, 0), new Point2d(-200, 200), new Point2d(-200, 679), new Point2d(-321, 800), new Point2d(-1110, 800), new Point2d(-2380, 800) };
                                        var p0 = points.Last().TransformBy(startPoint);
                                        var p1 = p0.OffsetX(-180);
                                        var p2 = p1.OffsetX(-1000);
                                        {
                                            DrawFloorDrain(p0.ToPoint3d(), true);
                                            DrawFloorDrain(p2.ToPoint3d(), true);
                                            var p3 = points[4].TransformBy(startPoint).ToPoint3d();
                                            var p4 = p3.OffsetXY(-90, 90);
                                            DrawDomePipes(new GLineSegment(p3, p4));
                                            DrawSWaterStoringCurve(p4, true);
                                        }

                                        var segs = points.ToGLineSegments(startPoint.ToPoint3d());
                                        DrawDomePipes(segs);
                                        DrawDomePipes(new GLineSegment(p1, p2));
                                    }
                                    DrawHorizontalLineOnPipeRun(HEIGHT, basePt);
                                }
                                //short translator left
                                else if (storey == "4F")
                                {
                                    var points = SHORT_TRANSLATOR_POINTS;
                                    NewMethod1(HEIGHT, ref v, basePt, points);
                                    DrawPipeCheckPoint(basePt.OffsetY(HEIGHT / 2), true);
                                    DrawHorizontalLineOnPipeRun(HEIGHT, basePt);
                                }
                                //long and short translator left
                                else if (storey == "5F")
                                {
                                    var height1 = LONG_TRANSLATOR_HEIGHT1;
                                    var points1 = LONG_TRANSLATOR_POINTS;
                                    var points2 = SHORT_TRANSLATOR_POINTS;
                                    var lastPt1 = points1.Last();
                                    NewMethod2(HEIGHT, ref v, basePt, points1, points2, height1);
                                    DrawPipeCheckPoint(basePt.OffsetXY(lastPt1.X, HEIGHT - height1 + lastPt1.Y - CHECKPOINT_OFFSET_Y), true);
                                    DrawHorizontalLineOnPipeRun(HEIGHT, basePt);
                                }
                                //long translator right
                                else if (storey == "7F")
                                {
                                    var lastPt = NewMethod5(HEIGHT, ref v, basePt);

                                    {
                                        var pt = lastPt.TransformBy(basePt.OffsetY(HEIGHT - LONG_TRANSLATOR_HEIGHT1)).OffsetY(-300);
                                        var segs = LEFT_LONG_TRANSLATOR_CLEANING_PORT_POINTS.GetYAxisMirror().ToGLineSegments(pt.ToPoint3d());
                                        DrawDomePipes(segs);
                                        DrawCleaningPort(segs.Last().EndPoint.ToPoint3d(), false, 2);
                                    }
                                    DrawHorizontalLineOnPipeRun(HEIGHT, basePt);
                                }
                                //short translator right
                                else if (storey == "8F")
                                {
                                    var points = SHORT_TRANSLATOR_POINTS.GetYAxisMirror();
                                    NewMethod1(HEIGHT, ref v, basePt, points);
                                    DrawPipeCheckPoint(basePt.OffsetY(HEIGHT / 2), false);
                                    DrawHorizontalLineOnPipeRun(HEIGHT, basePt);
                                }
                                //long and short translator right
                                else if (storey == "9F")
                                {
                                    var points1 = LONG_TRANSLATOR_POINTS.GetYAxisMirror();
                                    var points2 = SHORT_TRANSLATOR_POINTS.GetYAxisMirror();
                                    NewMethod3(HEIGHT, ref v, basePt, points1, points2);
                                    var lastPt1 = points1.Last();
                                    var height1 = LONG_TRANSLATOR_HEIGHT1;
                                    DrawPipeCheckPoint(basePt.OffsetXY(lastPt1.X, HEIGHT - height1 + lastPt1.Y - CHECKPOINT_OFFSET_Y), false);
                                    DrawHorizontalLineOnPipeRun(HEIGHT, basePt);
                                }
                                else
                                {
                                    DrawDomePipes(new GLineSegment(basePt, basePt.OffsetY(HEIGHT)));
                                    DrawPipeCheckPoint(basePt.OffsetY(HEIGHT / 2), true);
                                    DrawHorizontalLineOnPipeRun(HEIGHT, basePt);
                                }
                            }
                        }
                    }
                    outputStartPointOffsets[j] = v;
                }
            }

            for (int j = 0; j < COUNT; j++)
            {
                for (int i = 0; i < storeys.Count; i++)
                {
                    var storey = storeys[i];
                    var bsPt1 = basePoint.OffsetY(HEIGHT * i);
                    {
                        if (storey == "1F")
                        {
                            var basePt = bsPt1.OffsetX(OFFSET_X + (j + 1) * SPAN_X) + outputStartPointOffsets[j].ToVector3d();
                            if (DateTime.Now != DateTime.MinValue)
                            {
                                var vecs = new List<Vector2d> { new Vector2d(0, -479), new Vector2d(-121, -121), new Vector2d(-1579, 0), new Vector2d(-300, 0) };
                                {
                                    var segs = vecs.ToGLineSegments(basePt);
                                    DrawDomePipes(segs);
                                    DrawDirtyWaterWell(segs.Last().EndPoint.OffsetX(-400).ToPoint3d(), "666");
                                    DrawCleaningPort(segs[1].EndPoint.ToPoint3d(), false, 1);
                                    DrawWrappingPipe(segs[2].EndPoint.ToPoint3d());
                                }
                                if (j == 1)
                                {
                                    var bsPt = vecs.GetLastPoint(basePt).OffsetY(-600);
                                    var vecs2 = new List<Vector2d> { new Vector2d(2379, 0), new Vector2d(121, 121), new Vector2d(0, 569) };
                                    {
                                        var segs = vecs2.ToGLineSegments(bsPt);
                                        DrawDomePipes(segs);
                                    }
                                    {
                                        var vecs3 = new List<Vector2d> { new Vector2d(121, 121), new Vector2d(789, 0), new Vector2d(1270, 0), new Vector2d(180, 0), new Vector2d(1090, 0) };
                                        {
                                            var segs = vecs3.ToGLineSegments(vecs2.GetLastPoint(bsPt));
                                            {
                                                var _segs = segs.ToList();
                                                _segs.RemoveAt(3);
                                                DrawDomePipes(_segs);
                                            }
                                            {
                                                var p1 = segs[1].EndPoint;
                                                var p2 = p1.OffsetXY(90, 90);
                                                DrawDomePipes(new GLineSegment(p1, p2));
                                                DrawSWaterStoringCurve(p2.ToPoint3d(), false);
                                                var p3 = segs[2].EndPoint;
                                                var p4 = segs[4].EndPoint;
                                                DrawFloorDrain(p3.ToPoint3d(), false);
                                                DrawFloorDrain(p4.ToPoint3d(), false);
                                            }
                                        }
                                    }
                                }
                                else if (j == 4)
                                {
                                    var bsPt = vecs.GetLastPoint(basePt).OffsetY(-600);
                                    var vecs1 = new List<Vector2d> { new Vector2d(2120, 0), new Vector2d(406, 406), new Vector2d(404, 404), new Vector2d(879, 0), new Vector2d(1180, 0) };
                                    var vecs2 = new List<Vector2d> { new Vector2d(3150, 0), new Vector2d(404, 404), new Vector2d(1270, 0), new Vector2d(180, 0), new Vector2d(1090, 0) };
                                    var segs1 = vecs1.ToGLineSegments(bsPt);
                                    DrawDomePipes(segs1);
                                    var segs2 = vecs2.ToGLineSegments(segs1[1].EndPoint);
                                    {
                                        var _segs2 = segs2.ToList();
                                        _segs2.RemoveAt(3);
                                        DrawDomePipes(_segs2);
                                    }
                                    {
                                        var p1 = segs1[3].EndPoint;
                                        var p2 = p1.OffsetXY(90, 90);
                                        DrawDomePipes(new GLineSegment(p1, p2));
                                        DrawDoubleWashBasins(p2.ToPoint3d(), false);
                                    }

                                    DrawFloorDrain(segs1[4].EndPoint.ToPoint3d(), false);

                                    {
                                        var p1 = segs2[1].EndPoint;
                                        var p2 = p1.OffsetXY(90, 90);
                                        DrawDomePipes(new GLineSegment(p1, p2));
                                        DrawSWaterStoringCurve(p2.ToPoint3d(), false);
                                    }
                                    DrawFloorDrain(segs2[2].EndPoint.ToPoint3d(), false);
                                    DrawFloorDrain(segs2[4].EndPoint.ToPoint3d(), false);
                                }
                                else if (j == 6)
                                {
                                    {
                                        var bsPt = vecs.GetLastPoint(basePt).OffsetY(-600);
                                        var vecs1 = new List<Vector2d> { new Vector2d(300, 0), new Vector2d(4879, 0), new Vector2d(121, 121), new Vector2d(0, 1079) };
                                        var segs = vecs1.ToGLineSegments(bsPt);
                                        DrawDomePipes(segs);
                                        DrawWrappingPipe(segs[0].EndPoint.ToPoint3d());
                                        DrawCleaningPort(segs[1].EndPoint.ToPoint3d(), false, 1);
                                    }
                                    {
                                        var bsPt = vecs.GetLastPoint(basePt).OffsetY(-600 - 600);
                                        var vecs2 = new List<Vector2d> { new Vector2d(300, 0), new Vector2d(5479, 0), new Vector2d(121, 121), new Vector2d(0, 1379) };
                                        var segs = vecs2.ToGLineSegments(bsPt);
                                        DrawDomePipes(segs);
                                        DrawWrappingPipe(segs[0].EndPoint.ToPoint3d());
                                        DrawCleaningPort(segs[1].EndPoint.ToPoint3d(), false, 1);
                                        DrawCleaningPort(segs[3].EndPoint.ToPoint3d(), false, 2);
                                    }
                                }
                            }
                            else
                            {
                                var height = 480;
                                var points = new Point2d[] { new Point2d(0, 0), new Point2d(-121, -121), new Point2d(-2000, -121) };
                                var segs = points.ToGLineSegments(basePt.OffsetY(-height));
                                DrawDomePipes(segs);
                                DrawDomePipes(new GLineSegment(basePt, basePt.OffsetY(-height)));
                                DrawDirtyWaterWell(points.Last().TransformBy(basePt).ToPoint3d().OffsetY(-height), "666");
                            }
                        }
                    }
                }
            }

            if (false)
            {
                for (int j = 0; j < COUNT; j++)
                {
                    var start = storeys.IndexOf("31F");
                    var end = storeys.IndexOf("3F");
                    for (int i = start; i >= end; i--)
                    {
                        var s = storeys[i];
                        var bsPt1 = basePoint.OffsetY(HEIGHT * i);
                        {
                            var basePt = bsPt1.OffsetX(OFFSET_X + (j + 1) * SPAN_X);
                            if (i == start)
                            {
                                var points = new Point2d[] { new Point2d(0, 0), new Point2d(300, -300), new Point2d(300, -600) };
                                var segs = points.ToGLineSegments(basePt.OffsetY(600));
                                DrawDomePipes(segs);
                            }
                            else if (i == end)
                            {
                                var points = new Point2d[] { new Point2d(0, 0), new Point2d(0, -900), new Point2d(-300, -1200) };
                                var segs = points.ToGLineSegments(basePt.OffsetXY(300, HEIGHT));
                                DrawDomePipes(segs);
                            }
                            else
                            {
                                var points = new Point2d[] { new Point2d(0, 0), new Point2d(0, -900), new Point2d(-300, -1200) };
                                var segs = points.ToGLineSegments(basePt.OffsetXY(300, HEIGHT));
                                DrawDomePipes(segs);
                                DrawDomePipes(new GLineSegment(points[1].TransformBy(basePt.OffsetXY(300, HEIGHT)), basePt.OffsetX(300).ToPoint2d()));
                            }
                        }
                    }
                }

            }
            for (int j = 0; j < COUNT; j++)
            {
                var x = basePoint.X + OFFSET_X + (j + 1) * SPAN_X;
                var y1 = basePoint.Y;
                var y2 = y1 + HEIGHT * (storeys.Count - 1);
                //{
                //    var line = DU.DrawLineLazy(x, y1, x, y2);
                //    SetDomePipeLineStyle(line);
                //}
                //{
                //    var line = DU.DrawLineLazy(x + 300, y1, x + 300, y2);
                //    SetVentPipeLineStyle(line);
                //}
            }


            {
                // var bsPt = basePoint.OffsetXY(500, -1000);
                // DU.DrawBlockReference(blkName: "污废合流井编号", basePt: bsPt,
                //scale: 0.5,
                //props: new Dictionary<string, string>() { { "-", "666" } },
                //cb: br =>
                //{
                //    br.Layer = "W-DRAI-EQPM";
                //});
            }
        }

        private static void DrawDomePipes(params GLineSegment[] segs)
        {
            DrawDomePipes((IEnumerable<GLineSegment>)segs);
        }
        private static void DrawDomePipes(IEnumerable<GLineSegment> segs)
        {
            var lines = DU.DrawLineSegmentsLazy(segs);
            lines.ForEach(line => SetDomePipeLineStyle(line));
        }

        private static void DrawWrappingPipe(Point3d basePt)
        {
            DU.DrawBlockReference("套管系统", basePt, br =>
            {
                br.Layer = "W-BUSH";
            });
        }

        private static void DrawHorizontalLineOnPipeRun(double HEIGHT, Point3d basePt)
        {
            var h = HEIGHT * .7;
            if (HEIGHT - h > LONG_TRANSLATOR_HEIGHT1)
            {
                h = HEIGHT - LONG_TRANSLATOR_HEIGHT1 + 150;
            }
            var p1 = basePt.OffsetY(h);
            var p2 = p1.OffsetX(-200);
            var p3 = p1.OffsetX(200);
            var line = DU.DrawLineLazy(p2, p3);
            line.Layer = "W-DRAI-NOTE";
        }

        private static Point2d NewMethod5(double HEIGHT, ref Vector2d v, Point3d basePt)
        {
            var points = LONG_TRANSLATOR_POINTS.GetYAxisMirror();
            NewMethod(HEIGHT, ref v, basePt, points);
            var lastPt = points.Last();
            DrawPipeCheckPoint(basePt.OffsetXY(lastPt.X, HEIGHT - LONG_TRANSLATOR_HEIGHT1 + lastPt.Y - CHECKPOINT_OFFSET_Y), false);
            return lastPt;
        }

        private static Point2d NewMethod4(double HEIGHT, ref Vector2d v, Point3d basePt)
        {
            var points = LONG_TRANSLATOR_POINTS;
            NewMethod(HEIGHT, ref v, basePt, points);
            var lastPt = points.Last();
            DrawPipeCheckPoint(basePt.OffsetXY(lastPt.X, HEIGHT - LONG_TRANSLATOR_HEIGHT1 + lastPt.Y - CHECKPOINT_OFFSET_Y), true);
            return lastPt;
        }
        public static void DrawFloorDrain(Point3d basePt, bool leftOrRight)
        {
            if (leftOrRight)
            {
                DU.DrawBlockReference("地漏系统1", basePt, br =>
                {
                    br.Layer = "W-DRAI-EQPM";
                    br.ScaleFactors = new Scale3d(2, 2, 2);
                    if (br.IsDynamicBlock)
                    {
                        br.ObjectId.SetDynBlockValue("可见性", "普通地漏P弯");
                    }
                });
            }
            else
            {
                DU.DrawBlockReference("地漏系统1", basePt,
               br =>
               {
                   br.Layer = "W-DRAI-EQPM";
                   br.ScaleFactors = new Scale3d(-2, 2, 2);
                   if (br.IsDynamicBlock)
                   {
                       br.ObjectId.SetDynBlockValue("可见性", "普通地漏P弯");
                   }
               });
            }

        }
        public static void DrawSWaterStoringCurve(Point3d basePt, bool leftOrRight)
        {
            if (leftOrRight)
            {
                DU.DrawBlockReference("S型存水弯", basePt, br =>
                {
                    br.Layer = "W-DRAI-EQPM";
                    br.ScaleFactors = new Scale3d(2, 2, 2);
                    if (br.IsDynamicBlock)
                    {
                        br.ObjectId.SetDynBlockValue("可见性", "板上S弯");
                    }
                });
            }
            else
            {
                DU.DrawBlockReference("S型存水弯", basePt,
                   br =>
                   {
                       br.Layer = "W-DRAI-EQPM";
                       br.ScaleFactors = new Scale3d(-2, 2, 2);
                       if (br.IsDynamicBlock)
                       {
                           br.ObjectId.SetDynBlockValue("可见性", "板上S弯");
                       }
                   });
            }
        }
        public static void DrawCleaningPort(Point3d basePt, bool leftOrRight, double scale)
        {
            if (leftOrRight)
            {
                DU.DrawBlockReference("清扫口系统", basePt, scale: scale, cb: br =>
                {
                    br.Layer = "W-DRAI-EQPM";
                    br.Rotation = GeoAlgorithm.AngleFromDegree(90);
                });
            }
            else
            {
                DU.DrawBlockReference("清扫口系统", basePt, scale: scale, cb: br =>
                {
                    br.Layer = "W-DRAI-EQPM";
                    br.Rotation = GeoAlgorithm.AngleFromDegree(90 + 180);
                });
            }
        }
        public static void DrawPipeCheckPoint(Point3d basePt, bool leftOrRight)
        {
            DU.DrawBlockReference(blkName: "立管检查口", basePt: basePt,
cb: br =>
{
    if (leftOrRight)
    {
        br.ScaleFactors = new Scale3d(-1, 1, 1);
    }
    br.Layer = "W-DRAI-EQPM";
});
        }

        private static void NewMethod2(double HEIGHT, ref Vector2d vec, Point3d basePt, Point2d[] points1, Point2d[] points2, double height1)
        {

            var lastPt1 = points1.Last();
            var lastPt2 = points2.Last();
            {
                var segs = points1.ToGLineSegments(basePt.OffsetY(HEIGHT - height1));
                var lines = DU.DrawLineSegmentsLazy(segs);
                lines.ForEach(line => SetDomePipeLineStyle(line));
            }

            {
                var p1 = basePt.OffsetXY(lastPt1.X, -lastPt2.Y);
                var p2 = p1.OffsetY(HEIGHT - height1 + lastPt1.Y + lastPt2.Y);
                var line = DU.DrawLineSegmentLazy(new GLineSegment(p1, p2));
                SetDomePipeLineStyle(line);
            }
            {
                var p1 = basePt.OffsetY(HEIGHT - height1);
                var p2 = basePt.OffsetY(HEIGHT);
                var line = DU.DrawLineSegmentLazy(new GLineSegment(p1, p2));
                SetDomePipeLineStyle(line);
            }
            {
                var v = new Vector2d(lastPt1.X, 0);
                vec += v;
                basePt += v.ToVector3d();
            }
            {
                var height2 = HEIGHT + lastPt2.Y;
                var segs = points2.ToGLineSegments(basePt.OffsetY(HEIGHT - height2));
                DrawDomePipes(segs);
            }
            {
                var v = new Vector2d(lastPt2.X, 0);
                vec += v;
                basePt += v.ToVector3d();
            }
        }
        private static void NewMethod3(double HEIGHT, ref Vector2d vec, Point3d basePt, Point2d[] points1, Point2d[] points2)
        {
            var height1 = LONG_TRANSLATOR_HEIGHT1;
            var lastPt1 = points1.Last();
            var lastPt2 = points2.Last();
            {
                var segs = points1.ToGLineSegments(basePt.OffsetY(HEIGHT - height1));
                var lines = DU.DrawLineSegmentsLazy(segs);
                lines.ForEach(line => SetDomePipeLineStyle(line));
            }

            {
                var p1 = basePt.OffsetXY(lastPt1.X, -lastPt2.Y);
                var p2 = p1.OffsetY(HEIGHT - height1 + lastPt1.Y + lastPt2.Y);
                var line = DU.DrawLineSegmentLazy(new GLineSegment(p1, p2));
                SetDomePipeLineStyle(line);
            }
            {
                var p1 = basePt.OffsetY(HEIGHT - height1);
                var p2 = basePt.OffsetY(HEIGHT);
                var line = DU.DrawLineSegmentLazy(new GLineSegment(p1, p2));
                SetDomePipeLineStyle(line);
            }
            {
                var v = new Vector2d(lastPt1.X, 0);
                vec += v;
                basePt += v.ToVector3d();
            }
            {
                var height2 = HEIGHT + lastPt2.Y;
                var segs = points2.ToGLineSegments(basePt.OffsetY(HEIGHT - height2));
                var lines = DU.DrawLineSegmentsLazy(segs);
                lines.ForEach(line => SetDomePipeLineStyle(line));
            }
            {
                var v = new Vector2d(lastPt2.X, 0);
                vec += v;
                basePt += v.ToVector3d();
            }
        }
        private static void NewMethod1(double HEIGHT, ref Vector2d v, Point3d basePt, Point2d[] points)
        {
            var lastPt = points.Last();
            var height = HEIGHT + lastPt.Y;
            var segs = points.ToGLineSegments(basePt.OffsetY(HEIGHT - height));
            var lines = DU.DrawLineSegmentsLazy(segs);
            lines.ForEach(line => SetDomePipeLineStyle(line));
            {
                var p1 = basePt.OffsetY(HEIGHT - height);
                var p2 = basePt.OffsetY(HEIGHT);
                var line = DU.DrawLineSegmentLazy(new GLineSegment(p1, p2));
                SetDomePipeLineStyle(line);
                v += new Vector2d(lastPt.X, 0);
            }
        }

        private static void NewMethod(double HEIGHT, ref Vector2d v, Point3d basePt, Point2d[] points)
        {
            var lastPt = points.Last();
            var height = LONG_TRANSLATOR_HEIGHT1;
            var segs = points.ToGLineSegments(basePt.OffsetY(HEIGHT - height));
            var lines = DU.DrawLineSegmentsLazy(segs);
            lines.ForEach(line => SetDomePipeLineStyle(line));

            {
                var p1 = basePt.OffsetX(points.Last().X);
                var p2 = p1.OffsetY(HEIGHT - height + lastPt.Y);
                var line = DU.DrawLineSegmentLazy(new GLineSegment(p1, p2));
                SetDomePipeLineStyle(line);
            }
            {
                var p1 = basePt.OffsetY(HEIGHT - height);
                var p2 = basePt.OffsetY(HEIGHT);
                var line = DU.DrawLineSegmentLazy(new GLineSegment(p1, p2));
                SetDomePipeLineStyle(line);
            }
            v += new Vector2d(lastPt.X, 0);
        }

        public static void DrawDirtyWaterWell(Point3d basePt, string value)
        {
            DU.DrawBlockReference(blkName: "污废合流井编号", basePt: basePt.OffsetY(-400),
            props: new Dictionary<string, string>() { { "-", value } },
            cb: br =>
            {
                br.Layer = "W-DRAI-EQPM";
            });
        }
        private static void SetVentPipeLineStyle(Line line)
        {
            line.Layer = "W-DRAI-VENT-PIPE";
            line.ColorIndex = 256;
        }

        private static void SetDomePipeLineStyle(Line line)
        {
            line.Layer = "W-DRAI-DOME-PIPE";
            line.ColorIndex = 256;
        }
    }
    public class ThDrainageService
    {

        public static void ConnectLabelToLabelLine(DrainageGeoData geoData)
        {
            var lines = geoData.LabelLines.Distinct().ToList();
            var bds = geoData.Labels.Select(x => x.Boundary).ToList();
            var lineHs = lines.Where(x => x.IsHorizontal(10)).ToList();
            var lineHGs = lineHs.Select(x => x.ToLineString()).Cast<Geometry>().ToList();
            var f1 = GeoFac.CreateGRectContainsSelector(lineHGs);
            foreach (var bd in bds)
            {
                var g = GRect.Create(bd.Center.OffsetY(-10).OffsetY(-250), 1500, 250);
                {
                    var e = DU.DrawRectLazy(g);
                    e.ColorIndex = 2;
                }
                var _lineHGs = f1(g);
                var f2 = GeoFac.NearestNeighbourGeometryF(_lineHGs);
                var lineH = lineHGs.Select(lineHG => lineHs[lineHGs.IndexOf(lineHG)]).ToList();
                var geo = f2(bd.Center.Expand(.1).ToGRect().ToPolygon());
                if (geo == null) continue;
                {
                    var ents = geo.ToDbObjects().OfType<Entity>().ToList();
                    var line = lineHs[lineHGs.IndexOf(geo)];
                    var dis = line.Center.GetDistanceTo(bd.Center);
                    if (dis.InRange(100, 400) || Math.Abs(line.Center.Y - bd.Center.Y).InRange(.1, 400))
                    {
                        geoData.LabelLines.Add(new GLineSegment(bd.Center, line.Center).Extend(.1));
                    }
                }
            }
        }

        public static void PreFixGeoData(DrainageGeoData geoData)
        {
            for (int i = 0; i < geoData.DLines.Count; i++)
            {
                geoData.DLines[i] = geoData.DLines[i].Extend(5);
            }
            for (int i = 0; i < geoData.VLines.Count; i++)
            {
                geoData.VLines[i] = geoData.VLines[i].Extend(5);
            }
        }
        public static bool HasWL(string label)
        {
            //暂不支持污废分流
            return label.StartsWith("WL");
        }
        public static double GetAiringValue()
        {
            //伸顶通气的值，上人屋面伸顶2000，不上人屋面伸顶500。从面板读取。
            return GetCanPeopleBeOnRoof() ? 2000 : 500;
        }
        public static bool GetCanPeopleBeOnRoof()
        {
            return false;
        }
        public static bool HasAiringHorizontalPipe(string storeyLabel, List<Geometry> PLs, List<Geometry> FLs, List<Geometry> toilets)
        {
            //6.4	通气立管的横管
            //在通气立管出现的每一层都和PL（+DL）或FL进行连通，直至有排水点位的最高层为止。
            //出现通气立管的最高层的判断：
            //在普通楼层（数字+F）找到PL或FL附近300范围内有卫生间的最高楼层即可，这个楼层就是连接通气立管的最高楼层。
            bool test3()
            {
                if (FLs.Count == 0 || toilets.Count == 0) return false;
                var toiletsGeo = GeoFac.CreateGeometry(toilets);
                foreach (var fl in FLs)
                {
                    if (GeoFac.CreateCirclePolygon(fl.GetCenter(), 300, 36).Intersects(toiletsGeo)) return true;
                }
                return false;
            }
            return Regex.IsMatch(storeyLabel, @"^\d+F$") && ((PLs.Count > 0) || test3());
        }
        public static bool IsNotedLabel(string label)
        {
            //接至厨房单排、接至卫生间单排。。。
            return label.Contains("单排") || label.Contains("设置乙字弯");
        }
        public static bool IsWantedLabelText(string label)
        {
            if (label == null) return false;
            return label.StartsWith("FL") || label.StartsWith("PL") || label.StartsWith("TL") || label.StartsWith("DL");
        }

        public class WLGrouper
        {
            public class ToiletGrouper
            {
                public List<Geometry> PLs;//污废合流立管
                public List<Geometry> TLs;//通气立管
                public List<Geometry> DLs;//沉箱立管
                public List<Geometry> FLs;//废水立管
                public WLType WLType;
                public void Init()
                {
                    PLs ??= new List<Geometry>();
                    TLs ??= new List<Geometry>();
                    DLs ??= new List<Geometry>();
                    FLs ??= new List<Geometry>();
                }
                public static List<ToiletGrouper> CollectFLTLs(List<ToiletGrouper> group, List<Geometry> FLs, List<Geometry> TLs)
                {
                    //6.3.5	废水立管（FL）+通气立管（TL）
                    //若FL附近300的范围内存在TL且该THL不属于PL，则将FL与TL设为一组。系统图上和PL+TL的唯一区别是废水管要表达卫生洁具。
                    var tls = new List<Geometry>();
                    foreach (var item in group)
                    {
                        if (item.PLs.Count > 0 && item.TLs.Count > 0)
                        {
                            tls.AddRange(item.TLs);
                        }
                    }
                    var list = new List<ToiletGrouper>();
                    List<Geometry> _tls = null;
                    foreach (var fl in FLs)
                    {
                        _tls ??= TLs.Except(tls).ToList();
                        var f = GeoFac.CreateGeometrySelector(_tls);
                        var range = GeoFac.CreateCirclePolygon(fl.GetCenter().ToPoint3d(), 300, 36);
                        var lst = f(range);
                        if (lst.Count > 0)
                        {
                            tls.AddRange(lst);
                            _tls = null;
                            var item = new ToiletGrouper();
                            list.Add(item);
                            item.Init();
                            item.FLs.Add(fl);
                            item.TLs.AddRange(lst);
                        }
                    }
                    return list;
                }
                public static List<Geometry> GetWaterPipeWellFLs(List<KeyValuePair<string, Geometry>> roomData, List<Geometry> FLs)
                {
                    //若FL在水管井中，则认为该FL在出现的每层都安装了一个地漏。
                    //水管井的判断：
                    //空间名称为“水”、包含“水井”或“水管井”（持续更新）。
                    var rooms = new List<Geometry>();
                    foreach (var kv in roomData)
                    {
                        if (kv.Key == "水" || kv.Key.Contains("水井") || kv.Key.Contains("水管井"))
                        {
                            rooms.Add(kv.Value);
                        }
                    }
                    return GeoFac.CreateGeometrySelector(FLs)(GeoFac.CreateGeometry(rooms));
                }
                public static List<ToiletGrouper> DoGroup(List<Geometry> PLs, List<Geometry> TLs, List<Geometry> DLs)
                {
                    var list = new List<ToiletGrouper>();
                    var hs = new HashSet<Geometry>(PLs.Concat(TLs).Concat(DLs));
                    foreach (var pl in PLs)
                    {
                        hs.Add(pl);
                        var range = GeoFac.CreateCirclePolygon(pl.GetCenter().ToPoint3d(), 300, 12);
                        //在每一根PL的每一层300范围内找TL
                        var tls = GeoFac.CreateGeometrySelector(TLs.Except(hs).ToList())(range);
                        hs.AddRange(tls);
                        //在每一根PL的每一层300范围内找DL
                        var dls = GeoFac.CreateGeometrySelector(DLs.Except(hs).ToList())(range);
                        hs.AddRange(dls);
                        var o = new ToiletGrouper();
                        list.Add(o);
                        o.Init();
                        o.PLs.Add(pl);
                        o.TLs.AddRange(tls);
                        o.DLs.AddRange(dls);
                        if (tls.Count == 0 && dls.Count == 0)
                        {
                            o.WLType = WLType.PL;
                        }
                        else if (tls.Count > 0 && dls.Count > 0)
                        {
                            o.WLType = WLType.PL_TL_DL;
                        }
                        else if (tls.Count > 0 && dls.Count == 0)
                        {
                            o.WLType = WLType.PL_TL;
                        }
                        else if (tls.Count == 0 && dls.Count > 0)
                        {
                            o.WLType = WLType.PL_DL;
                        }
                        else
                        {
                            throw new System.Exception(nameof(ToiletGrouper));
                        }
                    }
                    return list;
                }
            }
            public enum WLType
            {
                PL, PL_TL, PL_DL, PL_TL_DL,
            }
        }


    }
}
#pragma warning disable
namespace ThMEPWSS.DebugNs
{
    using TypeDescriptor = System.ComponentModel.TypeDescriptor;
    using System;
    using System.Linq;
    using System.Text;
    using System.Reflection;
    using System.Collections.Generic;
    using System.Windows.Forms;
    using ThMEPWSS.JsonExtensionsNs;
    using Dbg = ThMEPWSS.DebugNs.ThDebugTool;
    using DU = ThMEPWSS.Assistant.DrawUtils;
    using Autodesk.AutoCAD.EditorInput;
    using AcHelper;
    using Autodesk.AutoCAD.Geometry;
    using Linq2Acad;
    using ThMEPWSS.Pipe.Model;
    using ThMEPWSS.Pipe.Engine;
    using Autodesk.AutoCAD.DatabaseServices;
    using System.Diagnostics;
    using Autodesk.AutoCAD.ApplicationServices;
    using Dreambuild.AutoCAD;
    using DotNetARX;
    using Autodesk.AutoCAD.Internal;
    using static ThMEPWSS.DebugNs.ThPublicMethods;
    using ThMEPWSS.CADExtensionsNs;
    using ThMEPWSS.Uitl;
    using ThMEPWSS.Uitl.DebugNs;
    using ThMEPWSS.Uitl.ExtensionsNs;
    using ThMEPWSS.Assistant;
    using ThMEPWSS.Pipe.Service;
    using NFox.Cad;
    using ThCADCore.NTS;
    using Autodesk.AutoCAD.Colors;
    using System.Runtime.Remoting;
    using PolylineTools = Pipe.Service.PolylineTools;
    using CircleTools = Pipe.Service.CircleTools;
    using System.IO;
    using Autodesk.AutoCAD.Runtime;
    using static ThMEPWSS.DebugNs.StaticMethods;
    using ThMEPWSS.Pipe;
    using Newtonsoft.Json;
    using System.Text.RegularExpressions;
    using ThCADExtension;
    using System.Collections;
    using ThCADCore.NTS.IO;
    using Newtonsoft.Json.Linq;
    using ThMEPEngineCore.Engine;
    using NetTopologySuite.Geometries;
    using NetTopologySuite.Operation.Linemerge;
    using Microsoft.CSharp;
    using System.CodeDom.Compiler;
    using System.Linq.Expressions;
    using ThMEPEngineCore.Algorithm;
    public class RainSystemService
    {
        public RainSystemCadData CadDataMain;
        public List<RainSystemCadData> CadDatas;
        public List<ThStoreysData> Storeys;
        public RainSystemGeoData GeoData;
        public ThWRainSystemDiagram RainSystemDiagram;
        public List<RainSystemDrawingData> DrawingDatas;

        public void BuildRainSystemDiagram<T>(string label, T sys, VerticalPipeType sysType) where T : ThWRainPipeSystem
        {
            for (int i = 0; i < RainSystemDiagram.WSDStoreys.Count; i++)
            {
                var run = new ThWRainPipeRun()
                {
                    MainRainPipe = new ThWSDPipe()
                    {
                        Label = label,
                        DN = "DN100",
                    },
                    Storey = RainSystemDiagram.WSDStoreys[i],
                    TranslatorPipe = new ThWSDTranslatorPipe(),
                };
                var bd = run.Storey.Boundary;
                var storey = Storeys.FirstOrDefault(s => s.Boundary.Equals(bd));
                if (storey != null)
                {
                    var storeyI = Storeys.IndexOf(storey);
                    List<string> labels = sysType switch
                    {
                        VerticalPipeType.RoofVerticalPipe => DrawingDatas[storeyI].RoofLabels,
                        VerticalPipeType.BalconyVerticalPipe => DrawingDatas[storeyI].BalconyLabels,
                        VerticalPipeType.CondenseVerticalPipe => DrawingDatas[storeyI].CondenseLabels,
                        _ => throw new NotSupportedException(),
                    };
                    AddPipeRuns(label, sys, run, storeyI, labels);
                }
            }
        }
        private void AddPipeRuns<T>(string label, T sys, ThWRainPipeRun run, int storeyI, List<string> labels) where T : ThWRainPipeSystem
        {
            var drData = DrawingDatas[storeyI];
            if (labels.Contains(label))
            {
                if (drData.ShortTranslatorLabels.Contains(label))
                {
                    run.TranslatorPipe.TranslatorType = TranslatorTypeEnum.Short;
                }
                else if (drData.LongTranslatorLabels.Contains(label))
                {
                    run.TranslatorPipe.TranslatorType = TranslatorTypeEnum.Long;
                }
                foreach (var kv in drData.PipeLabelToWaterWellLabels)
                {
                    if (kv.Key == label)
                    {
                        sys.OutputType.Label = kv.Value;
                        break;
                    }
                }
                foreach (var kv in drData.OutputTypes)
                {
                    if (kv.Key == label)
                    {
                        sys.OutputType.OutputType = kv.Value;
                        if (kv.Value == RainOutputTypeEnum.WaterWell)
                        {
                            foreach (var _kv in drData.WaterWellWrappingPipes)
                            {
                                if (_kv.Key == label && _kv.Value > 0)
                                {
                                    sys.OutputType.HasDrivePipe = true;
                                    break;
                                }
                            }
                        }
                        else if (kv.Value == RainOutputTypeEnum.RainPort)
                        {
                            foreach (var _kv in drData.RainPortWrappingPipes)
                            {
                                if (_kv.Key == label && _kv.Value > 0)
                                {
                                    sys.OutputType.HasDrivePipe = true;
                                    break;
                                }
                            }
                        }
                        break;
                    }
                }
                {
                    foreach (var kv in drData.CondensePipes)
                    {
                        if (kv.Key == label)
                        {
                            for (int i = 0; i < kv.Value.Key; i++)
                            {
                                var cp = new ThWSDCondensePipe();
                                run.CondensePipes.Add(cp);
                                run.HasBrokenCondensePipe = kv.Value.Value;
                            }
                        }
                    }
                    foreach (var kv in drData.FloorDrains)
                    {
                        if (kv.Key == label)
                        {
                            for (int i = 0; i < kv.Value; i++)
                            {
                                run.FloorDrains.Add(new ThWSDFloorDrain());
                            }
                        }
                    }
                    foreach (var kv in drData.FloorDrainsWrappingPipes)
                    {
                        if (kv.Key == label)
                        {
                            for (int i = 0; i < kv.Value; i++)
                            {
                                if (i < run.FloorDrains.Count)
                                {
                                    run.FloorDrains[i].HasDrivePipe = true;
                                }
                            }
                        }
                    }
                }
                sys.PipeRuns.Add(run);
                AddPipeRunsForRF(label, sys);
                sys.PipeRuns = sys.PipeRuns.OrderBy(run => RainSystemDiagram.WSDStoreys.IndexOf(run.Storey)).ToList();
                ThWRainSystemDiagram.SetCheckPoints(sys);
                ThWRainSystemDiagram.SetCheckPoints(sys.PipeRuns);
            }

        }
        static void SortStoreys(List<ThWSDStorey> wsdStoreys)
        {
            static int getScore(string label)
            {
                if (label == null) return 0;
                switch (label)
                {
                    case "RF": return ushort.MaxValue;
                    case "RF+1": return ushort.MaxValue + 1;
                    case "RF+2": return ushort.MaxValue + 2;
                    default:
                        {
                            int.TryParse(label.Replace("F", ""), out int ret);
                            return ret;
                        }
                }
            }
            wsdStoreys.Sort((x, y) => getScore(x.Label) - getScore(y.Label));
        }
        public void CreateRainSystemDiagram()
        {
            var wsdStoreys = new List<ThWSDStorey>();
            CollectStoreys(wsdStoreys);
            SortStoreys(wsdStoreys);
            var dg = new ThWRainSystemDiagram();
            this.RainSystemDiagram = dg;
            dg.WSDStoreys.AddRange(wsdStoreys);

            foreach (var label in DrawingDatas.SelectMany(drData => drData.RoofLabels).Distinct())
            {
                var sys = new ThWRoofRainPipeSystem()
                {
                    VerticalPipeId = label,
                    OutputType = new ThWSDOutputType(),
                };
                dg.RoofVerticalRainPipes.Add(sys);
                BuildRainSystemDiagram(label, sys, VerticalPipeType.RoofVerticalPipe);
            }
            foreach (var label in DrawingDatas.SelectMany(drData => drData.BalconyLabels).Distinct())
            {
                var sys = new ThWBalconyRainPipeSystem()
                {
                    VerticalPipeId = label,
                    OutputType = new ThWSDOutputType(),
                };
                dg.BalconyVerticalRainPipes.Add(sys);
                BuildRainSystemDiagram(label, sys, VerticalPipeType.BalconyVerticalPipe);
            }
            foreach (var label in DrawingDatas.SelectMany(drData => drData.CondenseLabels).Distinct())
            {
                var sys = new ThWCondensePipeSystem()
                {
                    VerticalPipeId = label,
                    OutputType = new ThWSDOutputType(),
                };
                dg.CondenseVerticalRainPipes.Add(sys);
                BuildRainSystemDiagram(label, sys, VerticalPipeType.CondenseVerticalPipe);
            }
            fixDiagramData(RainSystemDiagram);
            FixWaterBucketDN();
        }

        static void fixDiagramData(ThWRainSystemDiagram dg)
        {
            //根据实际业务修正

            fixOutput(dg.RoofVerticalRainPipes);
            fixOutput(dg.BalconyVerticalRainPipes);
            fixOutput(dg.CondenseVerticalRainPipes);
        }

        private static void fixOutput<T>(IList<T> systems) where T : ThWRainPipeSystem
        {
            foreach (var sys in systems)
            {
                //没有1楼的一律散排
                var r = sys.PipeRuns.FirstOrDefault(r => r.Storey?.Label == "1F");
                if (r == null)
                {
                    sys.OutputType.OutputType = RainOutputTypeEnum.None;
                }
            }
        }

        public void CollectStoreys(List<ThWSDStorey> wsdStoreys)
        {
            if (false)
            {
                var lst = Storeys.Where(s => s.StoreyType == ThMEPEngineCore.Model.Common.StoreyType.StandardStorey || s.StoreyType == ThMEPEngineCore.Model.Common.StoreyType.NonStandardStorey)
              .SelectMany(s => s.Storeys).ToList();
                var min = lst.Min();
                var max = lst.Max();
            }
            List<string> GetVerticalPipeNotes(ThStoreysData storey)
            {
                var storeyI = Storeys.IndexOf(storey);
                if (storeyI < 0) return new List<string>();
                return DrawingDatas[storeyI].GetAllLabels();
            }
            {
                var largeRoofVPTexts = new List<string>();
                foreach (var storey in Storeys)
                {
                    var bd = storey.Boundary;
                    switch (storey.StoreyType)
                    {
                        case ThMEPEngineCore.Model.Common.StoreyType.LargeRoof:
                            {
                                largeRoofVPTexts = GetVerticalPipeNotes(storey);

                                var vps1 = new List<ThWSDPipe>();
                                largeRoofVPTexts.ForEach(pt =>
                                {
                                    vps1.Add(new ThWSDPipe() { Label = pt, });
                                });

                                wsdStoreys.Add(new ThWSDStorey() { Label = $"RF", Boundary = bd, VerticalPipes = vps1 });
                                break;
                            }
                        case ThMEPEngineCore.Model.Common.StoreyType.SmallRoof:
                            break;
                        case ThMEPEngineCore.Model.Common.StoreyType.StandardStorey:
                        case ThMEPEngineCore.Model.Common.StoreyType.NonStandardStorey:
                            storey.Storeys.ForEach(i => wsdStoreys.Add(new ThWSDStorey() { Label = $"{i}F", Boundary = bd, }));
                            break;
                        case ThMEPEngineCore.Model.Common.StoreyType.Unknown:
                        default:
                            break;
                    }
                }
                {
                    var storeys = Storeys.Where(s => s.StoreyType == ThMEPEngineCore.Model.Common.StoreyType.SmallRoof).ToList();
                    if (storeys.Count == 1)
                    {
                        var storey = storeys[0];
                        var bd = storey.Boundary;
                        var smallRoofVPTexts = GetVerticalPipeNotes(storey);
                        var rf1Storey = new ThWSDStorey() { Label = $"RF+1", Boundary = bd };
                        wsdStoreys.Add(rf1Storey);

                        if (largeRoofVPTexts.Count > 0)
                        {
                            var rf2VerticalPipeText = smallRoofVPTexts.Except(largeRoofVPTexts);

                            if (rf2VerticalPipeText.Count() == 0)
                            {
                                //just has rf + 1, do nothing
                                var vps1 = new List<ThWSDPipe>();
                                smallRoofVPTexts.ForEach(pt =>
                                {
                                    vps1.Add(new ThWSDPipe() { Label = pt, });
                                });
                                rf1Storey.VerticalPipes = vps1;
                            }
                            else
                            {
                                //has rf + 1, rf + 2
                                var rf1VerticalPipeObjects = new List<ThWSDPipe>();
                                var rf1VerticalPipeTexts = smallRoofVPTexts.Except(rf2VerticalPipeText);
                                rf1VerticalPipeTexts.ForEach(pt =>
                                {
                                    rf1VerticalPipeObjects.Add(new ThWSDPipe() { Label = pt });
                                });
                                rf1Storey.VerticalPipes = rf1VerticalPipeObjects;

                                var rf2VerticalPipeObjects = new List<ThWSDPipe>();
                                rf2VerticalPipeText.ForEach(pt =>
                                {
                                    rf2VerticalPipeObjects.Add(new ThWSDPipe() { Label = pt });
                                });

                                wsdStoreys.Add(new ThWSDStorey() { Label = $"RF+2", Boundary = bd, VerticalPipes = rf2VerticalPipeObjects });
                            }
                        }
                    }
                    else if (storeys.Count == 2)
                    {
                        var s1 = storeys[0];
                        var s2 = storeys[1];
                        var bd1 = s1.Boundary;
                        var bd2 = s2.Boundary;
                        SwapBy2DSpace(ref s1, ref s2, bd1, bd2);
                        var vpts1 = GetVerticalPipeNotes(s1);
                        var vpts2 = GetVerticalPipeNotes(s2);
                        var vps1 = vpts1.Select(vpt => new ThWSDPipe() { Label = vpt }).ToList();
                        var vps2 = vpts2.Select(vpt => new ThWSDPipe() { Label = vpt }).ToList();
                        wsdStoreys.Add(new ThWSDStorey() { Label = $"RF+1", Boundary = bd1, VerticalPipes = vps1 });
                        wsdStoreys.Add(new ThWSDStorey() { Label = $"RF+2", Boundary = bd2, VerticalPipes = vps2 });
                    }
                }
            }
        }
        static void Swap<T>(ref T v1, ref T v2)
        {
            var tmp = v1;
            v1 = v2;
            v2 = tmp;
        }
        static void SwapBy2DSpace<T>(ref T v1, ref T v2, GRect bd1, GRect bd2)
        {
            var deltaX = Math.Abs(bd1.MinX - bd2.MinX);
            var deltaY = Math.Abs(bd1.MaxY - bd2.MaxY);
            if (deltaY > bd1.Height)
            {
                if (bd2.MaxY > bd1.MaxY)
                {
                    Swap(ref v1, ref v2);
                }
            }
            else if (deltaX > bd1.Width)
            {
                if (bd2.MinX < bd1.MinX)
                {
                    Swap(ref v1, ref v2);
                }
            }

        }
        static bool AllNotEmpty(params List<Geometry>[] plss)
        {
            foreach (var pls in plss)
            {
                if (pls.Count == 0) return false;
            }
            return true;
        }
        public static List<Geometry> ToList(params List<Geometry>[] plss)
        {
            return plss.SelectMany(pls => pls).ToList();
        }
        public static bool IsWantedText(string text)
        {
            return ThRainSystemService.IsWantedLabelText(text) || ThRainSystemService.HasGravityLabelConnected(text)
                || text.StartsWith("YL") || IsWaterPortLabel(text) || IsGravityWaterBucketDNText(text);
        }

        public static bool IsGravityWaterBucketDNText(string text)
        {
            return re.IsMatch(text);
        }

        static readonly Regex re = new Regex(@"^重力型雨水斗(DN\d+)$");
        public static bool IsWaterPortLabel(string text)
        {
            return text.Contains("接至") && text.Contains("雨水口");
        }
        public void CreateDrawingDatas()
        {
            var cadDataMain = CadDataMain;
            var geoData = GeoData;
            var cadDatas = CadDatas;
            var storeys = Storeys;

            var drawingDatas = new List<RainSystemDrawingData>();
            this.DrawingDatas = drawingDatas;

            var sb = new StringBuilder(8192);
            for (int storeyI = 0; storeyI < cadDatas.Count; storeyI++)
            {
                var drData = new RainSystemDrawingData();
                drawingDatas.Add(drData);
                {
                    var s = storeys[storeyI];
                    sb.AppendLine("楼层");
                    sb.AppendLine(s.Storeys.ToJson());
                    sb.AppendLine(s.StoreyType.ToString());
                }
                {
                    var s = geoData.Storeys[storeyI];
                    var e = DU.DrawRectLazy(s);
                    e.ColorIndex = 1;
                }
                var item = cadDatas[storeyI];
                {
                    var wantedLabels = new List<string>();
                    foreach (var pl in item.Labels)
                    {
                        var j = cadDataMain.Labels.IndexOf(pl);
                        var m = geoData.Labels[j];
                        if (IsWantedText(m.Text))
                        {
                            wantedLabels.Add(m.Text);
                        }
                    }
                    sb.AppendLine("立管");
                    sb.AppendLine(ThRainSystemService.GetRoofLabels(wantedLabels).ToJson());
                    sb.AppendLine(ThRainSystemService.GetBalconyLabels(wantedLabels).ToJson());
                    sb.AppendLine(ThRainSystemService.GetCondenseLabels(wantedLabels).ToJson());
                    drData.RoofLabels.AddRange(ThRainSystemService.GetRoofLabels(wantedLabels));
                    drData.BalconyLabels.AddRange(ThRainSystemService.GetBalconyLabels(wantedLabels));
                    drData.CondenseLabels.AddRange(ThRainSystemService.GetCondenseLabels(wantedLabels));
                    drData.CommentLabels.AddRange(wantedLabels.Where(x => ThRainSystemService.HasGravityLabelConnected(x)));
                }
                var waterBucketFixingList = new List<KeyValuePair<string, Geometry>>();
                var lbDict = new Dictionary<Geometry, string>();
                {
                    //凭空生成一个雨水口
                    if (false)
                    {
                        var f = GeoFac.CreateGeometrySelector(item.LabelLines);
                        foreach (var lb in item.Labels)
                        {
                            var j = cadDataMain.Labels.IndexOf(lb);
                            var m = geoData.Labels[j];
                            var label = m.Text;
                            if (IsWaterPortLabel(label))
                            {
                                var ok_ents = new HashSet<Geometry>();
                                var lines = f(lb);
                                if (lines.Count == 1)
                                {
                                    var line1 = lines[0];
                                    ok_ents.Add(line1);
                                    lines = f(line1).Except(ok_ents).ToList();
                                    if (lines.Count == 1)
                                    {
                                        var line2 = lines[0];
                                        ok_ents.Add(line2);
                                        lines = f(line2).Except(ok_ents).ToList();
                                        if (lines.Count == 1)
                                        {
                                            var line3 = lines[0];
                                            ok_ents.Add(line3);
                                            var seg2 = geoData.LabelLines[cadDataMain.LabelLines.IndexOf(line2)];
                                            var seg3 = geoData.LabelLines[cadDataMain.LabelLines.IndexOf(line3)];
                                            var pt = ThRainSystemService.GetTargetPoint(seg2, seg3);
                                            var r = GRect.Create(pt, 50);
                                            geoData.WaterPortSymbols.Add(r);
                                            var pl = r.ToPolygon();
                                            cadDataMain.WaterPortSymbols.Add(pl);
                                            item.WaterPortSymbols.Add(pl);
                                        }
                                    }
                                }
                            }
                        }
                    }


                    {
                        var gs = GeoFac.GroupGeometries(item.LabelLines).Where(g => g.Count >= 3).ToList();

                        //凭空生成一个雨水口
                        {
                            var labels = new List<Geometry>();
                            foreach (var lb in item.Labels)
                            {
                                var j = cadDataMain.Labels.IndexOf(lb);
                                var m = geoData.Labels[j];
                                var label = m.Text;
                                if (IsWaterPortLabel(label))
                                {
                                    labels.Add(lb);
                                }
                            }
                            var f = GeoFac.CreateGeometrySelector(item.LabelLines);
                            foreach (var lb in labels)
                            {
                                var _lines = f(GRect.Create(lb.GetCenter(), 100).ToPolygon());
                                if (_lines.Count == 1)
                                {
                                    var firstLine = _lines[0];
                                    var g = gs.FirstOrDefault(g => g.Contains(firstLine));
                                    if (g != null)
                                    {
                                        var segs = g.Select(cadDataMain.LabelLines).ToList().Select(geoData.LabelLines).ToList();
                                        var h = GeoFac.LineGrouppingHelper.Create(segs);
                                        h.InitPointGeos(radius: 10);
                                        h.DoGroupingByPoint();
                                        h.CalcAlonePoints();
                                        var pointGeos = h.AlonePoints;
                                        {
                                            var hLabelLines = item.LabelLines.Select(cadDataMain.LabelLines).ToList().Select(geoData.LabelLines).Where(seg => seg.IsHorizontal(5)).ToList();
                                            var lst = item.Labels.Distinct().ToList();
                                            lst.Remove(lb);
                                            lst.AddRange(hLabelLines.Select(seg => seg.Buffer(10)));
                                            lst.Add(firstLine);
                                            lst.AddRange(item.VerticalPipes);
                                            lst.AddRange(item.WaterPort13s);
                                            lst.AddRange(item.WaterWells);
                                            lst.AddRange(item.WaterPortSymbols);
                                            var _geo = GeoFac.CreateGeometry(lst.Distinct().ToArray());
                                            pointGeos = pointGeos.Except(GeoFac.CreateGeometrySelector(pointGeos)(_geo)).Distinct().ToList();
                                        }
                                        foreach (var geo in pointGeos)
                                        {
                                            var pt = geo.GetCenter();
                                            var r = GRect.Create(pt, 50);
                                            geoData.WaterPortSymbols.Add(r);
                                            var pl = r.ToPolygon();
                                            cadDataMain.WaterPortSymbols.Add(pl);
                                            item.WaterPortSymbols.Add(pl);
                                        }
                                    }
                                }
                            }
                        }

                        if (false)
                        {
                            //修复屋面雨水斗，后面继续补上相关信息
                            var f1 = GeoFac.CreateGeometrySelector(item.VerticalPipes);
                            var lbLinesGroups = GeoFac.GroupGeometries(item.LabelLines).Select(g => GeoFac.CreateGeometry(g.ToArray())).ToList();
                            var f2 = GeoFac.CreateGeometrySelector(lbLinesGroups);
                            var f3 = GeoFac.CreateGeometrySelector(item.WLines);
                            foreach (var lb in item.Labels)
                            {
                                var ct = geoData.Labels[cadDataMain.Labels.IndexOf(lb)];
                                var m = ThRainSystemService.TestGravityLabelConnected(ct.Text);
                                if (m.Success)
                                {
                                    var targetFloor = m.Groups[1].Value;
                                    var lst = f2(lb);
                                    if (lst.Count == 1)
                                    {
                                        var lblineGroupGeo = lst[0];
                                        var pipes = f1(lblineGroupGeo);
                                        if (pipes.Count == 1)
                                        {
                                            var pipe = pipes[0];
                                            lst = f3(pipe);
                                            lst.Remove(pipe);
                                            if (lst.Count == 1)
                                            {
                                                waterBucketFixingList.Add(new KeyValuePair<string, Geometry>(targetFloor, lst[0]));
                                            }
                                        }
                                    }
                                }
                            }
                        }


                    }


                }
                foreach (var o in item.LabelLines)
                {
                    var j = cadDataMain.LabelLines.IndexOf(o);
                    var m = geoData.LabelLines[j];
                    var e = DU.DrawLineSegmentLazy(m);
                    e.ColorIndex = 1;
                }

                var labelLinesGroup = GeoFac.GroupGeometries(item.LabelLines);
                var lbsGeosFilter = GeoFac.CreateGeometrySelector(labelLinesGroup.Select(lbs => GeoFac.CreateGeometry(lbs)).ToList());
                foreach (var pl in item.Labels)
                {
                    var j = cadDataMain.Labels.IndexOf(pl);
                    var m = geoData.Labels[j];
                    var e = DU.DrawTextLazy(m.Text, m.Boundary.LeftButtom.ToPoint3d());
                    e.ColorIndex = 2;
                    var _pl = DU.DrawRectLazy(m.Boundary);
                    _pl.ColorIndex = 2;
                }
                foreach (var o in item.VerticalPipes)
                {
                    var j = cadDataMain.VerticalPipes.IndexOf(o);
                    var m = geoData.VerticalPipes[j];
                    var e = DU.DrawRectLazy(m);
                    e.ColorIndex = 3;
                }
                foreach (var o in item.FloorDrains)
                {
                    var j = cadDataMain.FloorDrains.IndexOf(o);
                    var m = geoData.FloorDrains[j];
                    var e = DU.DrawRectLazy(m);
                    e.ColorIndex = 6;
                }
                foreach (var o in item.CondensePipes)
                {
                    var j = cadDataMain.CondensePipes.IndexOf(o);
                    var m = geoData.CondensePipes[j];
                    var e = DU.DrawRectLazy(m);
                    e.ColorIndex = 2;
                }
                foreach (var o in item.WaterWells)
                {
                    var j = cadDataMain.WaterWells.IndexOf(o);
                    var m = geoData.WaterWells[j];
                    var e = DU.DrawRectLazy(m);
                    e.ColorIndex = 7;
                }
                foreach (var o in item.SideWaterBuckets)
                {
                    var j = cadDataMain.SideWaterBuckets.IndexOf(o);
                    var m = geoData.SideWaterBuckets[j];
                    var e = DU.DrawRectLazy(m);
                    e.ColorIndex = 7;
                    Dbg.ShowXLabel(m.Center, 100);
                }


                {
                    var cl = Color.FromRgb(0xff, 0x9f, 0x7f);
                    foreach (var o in item.WaterPortSymbols)
                    {
                        var j = cadDataMain.WaterPortSymbols.IndexOf(o);
                        var m = geoData.WaterPortSymbols[j];
                        var e = DU.DrawRectLazy(m);
                        e.Color = cl;
                    }
                    foreach (var o in item.WaterPort13s)
                    {
                        var j = cadDataMain.WaterPort13s.IndexOf(o);
                        var m = geoData.WaterPort13s[j];
                        var e = DU.DrawRectLazy(m);
                        e.Color = cl;
                    }
                }
                {
                    var cl = Color.FromRgb(0x91, 0xc7, 0xae);
                    foreach (var o in item.WrappingPipes)
                    {
                        var j = cadDataMain.WrappingPipes.IndexOf(o);
                        var m = geoData.WrappingPipes[j];
                        var e = DU.DrawRectLazy(m);
                        e.Color = cl;
                    }
                }
                var shortTranslatorLabels = new HashSet<string>();

                {
                    var gbkf = GeoFac.CreateGeometrySelector(item.GravityWaterBuckets);
                    foreach (var lb in item.Labels)
                    {
                        var j = cadDataMain.Labels.IndexOf(lb);
                        var m = geoData.Labels[j];
                        var label = m.Text;
                        if (IsGravityWaterBucketDNText(label))
                        {
                            var dn = re.Match(label).Groups[1].Value;
                            var lst = lbsGeosFilter(m.Boundary.ToPolygon());
                            if (lst.Count == 1)
                            {
                                lst = gbkf(lst[0]);
                                if (lst.Count == 1)
                                {
                                    drData.GravityWaterBuckets.Add(new KeyValuePair<string, GRect>(dn, geoData.GravityWaterBuckets[cadDataMain.GravityWaterBuckets.IndexOf(lst[0])]));
                                }
                            }
                        }
                    }

                }
                {
                    var ok_ents = new HashSet<Geometry>();
                    {
                        var labels = item.Labels.Except(ok_ents).ToList();
                        var pipes = item.VerticalPipes.Except(ok_ents).ToList();
                        foreach (var labelLines in labelLinesGroup)
                        {
                            var lst = ToList(labelLines, labels, pipes);
                            var gs = GeoFac.GroupGeometries(lst);
                            foreach (var g in gs)
                            {
                                var _pipes = g.Where(pl => pipes.Contains(pl)).ToList();
                                var _labels = g.Where(pl => labels.Contains(pl)).ToList();
                                var _labelLines = g.Where(pl => labelLines.Contains(pl)).ToList();
                                if (!AllNotEmpty(_labels, _pipes, _labelLines)) continue;
                                {
                                    //过滤掉不正确的text
                                    var tmp = new List<Geometry>();
                                    foreach (var lb in _labels)
                                    {
                                        var label = geoData.Labels[cadDataMain.Labels.IndexOf(lb)].Text;
                                        if (ThRainSystemService.IsWantedLabelText(label) || label.StartsWith("YL"))
                                        {
                                            tmp.Add(lb);
                                        }
                                    }
                                    _labels = tmp;
                                }
                                if (_labels.Count == 1 && _pipes.Count == 1)
                                {
                                    var pp = _pipes[0];
                                    if (lbDict.ContainsKey(pp)) continue;
                                    var lb = _labels[0];
                                    var j = cadDataMain.Labels.IndexOf(lb);
                                    var m = geoData.Labels[j];
                                    var label = m.Text;
                                    lbDict[pp] = label;
                                    //OK，识别成功
                                    ok_ents.Add(pp);
                                    ok_ents.Add(lb);
                                    break;
                                }
                            }
                        }
                    }
                    {
                        var labels = item.Labels.Except(ok_ents).ToList();
                        var pipes = item.VerticalPipes.Except(ok_ents).ToList();
                        foreach (var labelLines in labelLinesGroup)
                        {
                            var lst = ToList(labelLines, labels, pipes);
                            var gs = GeoFac.GroupGeometries(lst);
                            foreach (var g in gs)
                            {
                                var _pipes = g.Where(pl => pipes.Contains(pl)).ToList();
                                var _labels = g.Where(pl => labels.Contains(pl)).ToList();
                                var _labelLines = g.Where(pl => labelLines.Contains(pl)).ToList();
                                if (!AllNotEmpty(_labels, _pipes, _labelLines)) continue;
                                if (_labels.Count == 1 && _pipes.Count == 2)
                                {
                                    var pp1 = _pipes[0];
                                    var pp2 = _pipes[1];
                                    {
                                        if (!lbDict.ContainsKey(pp1))
                                        {
                                            var lb = _labels[0];
                                            var j = cadDataMain.Labels.IndexOf(lb);
                                            var m = geoData.Labels[j];
                                            var label = m.Text;
                                            lbDict[pp1] = label;
                                            ok_ents.Add(pp1);
                                        }
                                        if (!lbDict.ContainsKey(pp2))
                                        {
                                            var lb = _labels[0];
                                            var j = cadDataMain.Labels.IndexOf(lb);
                                            var m = geoData.Labels[j];
                                            var label = m.Text;
                                            lbDict[pp2] = label;
                                            ok_ents.Add(pp2);
                                        }
                                        shortTranslatorLabels.Add(lbDict[pp1]);
                                    }
                                }
                            }
                        }
                    }
                    //上面的提取一遍，然后再提取一遍
                    {
                        var labels = item.Labels.Except(ok_ents).ToList();
                        var pipes = item.VerticalPipes.Except(ok_ents).ToList();
                        foreach (var labelLines in labelLinesGroup)
                        {
                            var gs = GeoFac.GroupGeometries(ToList(labelLines, labels, pipes));
                            foreach (var g in gs)
                            {
                                //{
                                //    var r = GeoAlgorithm.GetEntitiesBoundaryRect(g);
                                //    r.Expand(3);
                                //    var pl = DU.DrawRectLazy(r);
                                //    pl.ColorIndex = 3;
                                //}
                                var _pipes = g.Where(pl => pipes.Contains(pl)).ToList();
                                var _labels = g.Where(pl => labels.Contains(pl)).ToList();
                                var _labelLines = g.Where(pl => labelLines.Contains(pl)).ToList();
                                if (!AllNotEmpty(_labels, _pipes, _labelLines)) continue;
                                if (_pipes.Count == _labels.Count)
                                {
                                    //foreach (var pp in pps)
                                    //{
                                    //    DU.DrawTextLazy("xx", pp.Bounds.ToGRect().LeftTop.ToPoint3d());
                                    //}
                                    _pipes = ThRainSystemService.SortGeometrysBy2DSpacePosition(_pipes).ToList();
                                    _labels = ThRainSystemService.SortGeometrysBy2DSpacePosition(_labels).ToList();
                                    for (int k = 0; k < _pipes.Count; k++)
                                    {
                                        var pp = _pipes[k];
                                        var lb = _labels[k];
                                        var j = cadDataMain.Labels.IndexOf(lb);
                                        var m = geoData.Labels[j];
                                        var label = m.Text;
                                        lbDict[pp] = label;
                                        //DU.DrawTextLazy(label, pp.Bounds.ToGRect().LeftTop.ToPoint3d());
                                    }
                                    //OK，识别成功
                                    ok_ents.AddRange(_pipes);
                                    ok_ents.AddRange(_labels);
                                }
                                //这是原先识别短转管的代码，碰到某种case，会出问题，先注释掉
                                //else if (lbs.Count == 1)
                                //{
                                //    var lb = lbs[0];
                                //    var j = cadDataMain.Labels.IndexOf(lb);
                                //    var m = geoData.Labels[j];
                                //    var label = m.Text;
                                //    foreach (var pp in pps)
                                //    {
                                //        lbDict[pp] = label;
                                //        shortTranslatorLabels.Add(label);
                                //    }
                                //}
                            }
                        }
                    }

                    //再提取一遍
                    {
                        var labels = item.Labels.Except(ok_ents).ToList();
                        var pipes = item.VerticalPipes.Except(ok_ents).ToList();
                        foreach (var labelLines in labelLinesGroup)
                        {
                            var gs = GeoFac.GroupGeometries(ToList(labelLines, labels, pipes));
                            foreach (var g in gs)
                            {
                                if (!g.Any(pl => labelLines.Contains(pl))) continue;
                                var _labels = g.Where(pl => labels.Contains(pl)).ToList();
                                var _pipes = g.Where(pl => pipes.Contains(pl)).ToList();
                                var _labelLines = g.Where(pl => labelLines.Contains(pl)).ToList();
                                if (!AllNotEmpty(_labels, _pipes, _labelLines)) continue;
                                if (_pipes.Count == _labels.Count)
                                {
                                    //foreach (var pp in pps)
                                    //{
                                    //    DU.DrawTextLazy("xx", pp.Bounds.ToGRect().LeftTop.ToPoint3d());
                                    //}
                                    _pipes = ThRainSystemService.SortGeometrysBy2DSpacePosition(_pipes).ToList();
                                    _labels = ThRainSystemService.SortGeometrysBy2DSpacePosition(_labels).ToList();
                                    for (int k = 0; k < _pipes.Count; k++)
                                    {
                                        var pp = _pipes[k];
                                        var lb = _labels[k];
                                        var j = cadDataMain.Labels.IndexOf(lb);
                                        var m = geoData.Labels[j];
                                        var label = m.Text;
                                        lbDict[pp] = label;
                                        //DU.DrawTextLazy(label, pp.Bounds.ToGRect().LeftTop.ToPoint3d());
                                    }
                                    //OK，识别成功
                                    ok_ents.AddRange(_pipes);
                                    ok_ents.AddRange(_labels);
                                }
                            }
                        }
                    }
                }
                List<List<Geometry>> wLinesGroups;
                {
                    var gs = GeoFac.GroupGeometries(item.WLines);
                    wLinesGroups = gs;

                    //var wlines = item.WLines;
                    //var gwlines = wlines.Select(wl => cadDataMain.WLines.IndexOf(wl)).Select(i => geoData.WLines[i]).ToList();
                    //var h = GeometryFac.LineGrouppingHelper.Create(gwlines);
                    //h.InitPointGeos(20);
                    //h.DoGroupingByPoint();
                    //{
                    //    var gs = h.GeoGroupsByPoint;
                    //    var pts = h.DoublePoints;
                    //    var gs2 = new List<List<Geometry>>();
                    //    foreach (var g in gs)
                    //    {
                    //        var _g = new List<Geometry>();
                    //        foreach (var pt in g)
                    //        {
                    //            var i = pts.IndexOf(pt);
                    //            _g.Add(wlines[i]);
                    //        }
                    //        gs2.Add(_g);
                    //    }
                    //    wLinesGroups = gs2;
                    //}

                    //foreach (var g in gs)
                    //{
                    //    var r = GeoAlgorithm.GetEntitiesBoundaryRect(g);
                    //    r.Expand(3);
                    //    var pl = DU.DrawRectLazy(r);
                    //    pl.ColorIndex = 3;
                    //}

                }
                foreach (var o in item.WLines)
                {
                    var j = cadDataMain.WLines.IndexOf(o);
                    var m = geoData.WLines[j];
                    var e = DU.DrawLineSegmentLazy(m);
                    e.ColorIndex = 4;
                    //if (m.IsVertical(5))
                    //{
                    //    var ee = DU.DrawGeometryLazy(m);
                    //    ee.ColorIndex = 4;
                    //    ee.ConstantWidth = 100;
                    //}
                    //{
                    //    //DU.DrawTextLazy(m.AngleDegree.ToString(),100, m.StartPoint.ToPoint3d());
                    //}
                }

                var longTranslatorLabels = new HashSet<string>();
                {
                    foreach (var wlines in wLinesGroups)
                    {
                        var gs = GeoFac.GroupGeometries(ToList(wlines, item.VerticalPipes));
                        foreach (var g in gs)
                        {
                            var _pipes = g.Where(pl => item.VerticalPipes.Contains(pl)).ToList();
                            var _wlines = g.Where(pl => wlines.Contains(pl)).ToList();
                            if (!AllNotEmpty(_pipes, _wlines)) continue;

                            var pps1 = _pipes.Where(x => lbDict.ContainsKey(x)).ToList();
                            var pps2 = _pipes.Where(x => !lbDict.ContainsKey(x)).ToList();
                            if (pps1.Count == 1 && pps2.Count == 1)
                            {
                                var pp1 = pps1[0];
                                var pp2 = pps2[0];
                                //两根立管都要与wline相连才行
                                bool test(Geometry pipe)
                                {
                                    var lst = ToList(_wlines);
                                    lst.Add(pipe);
                                    var _gs = GeoFac.GroupGeometries(lst);
                                    foreach (var _g in _gs)
                                    {
                                        if (!_g.Contains(pipe)) continue;
                                        var __wlines = _g.Where(pl => _wlines.Contains(pl)).ToList();
                                        if (!AllNotEmpty(__wlines)) continue;
                                        return true;
                                    }
                                    return false;
                                }
                                if (test(pp1) && test(pp2))
                                {
                                    var label = lbDict[pp1];
                                    lbDict[pp2] = label;
                                    //连线的长度小于等于300。而且连线只有一条直线的情况是短转管
                                    var isShort = false;
                                    {
                                        var lst = ToList(_wlines);
                                        lst.Add(pp1);
                                        lst.Add(pp2);
                                        var _gs = GeoFac.GroupGeometries(lst).Where(_g => _g.Count == 3 && _g.Contains(pp1) && _g.Contains(pp2)).ToList();
                                        foreach (var _g in _gs)
                                        {
                                            var __wlines = _g.Where(pl => _wlines.Contains(pl)).ToList();
                                            if (__wlines.Count == 1)
                                            {
                                                var gWLine = geoData.WLines[cadDataMain.WLines.IndexOf(__wlines[0])];
                                                if (gWLine.Length <= 300)
                                                {
                                                    isShort = true;
                                                    shortTranslatorLabels.Add(label);
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                    //然后才是长转管
                                    if (!isShort) longTranslatorLabels.Add(label);
                                }
                            }
                        }
                    }
                }
                {
                    //临时修复wline中间被其他wline横插一脚的情况
                    foreach (var wline in item.WLines)
                    {
                        var lst = ToList(item.VerticalPipes);
                        lst.Add(wline);
                        var gs = GeoFac.GroupGeometries(lst);
                        foreach (var g in gs)
                        {
                            if (!g.Contains(wline)) continue;
                            var _pipes = g.Where(pl => item.VerticalPipes.Contains(pl)).ToList();
                            if (!AllNotEmpty(_pipes)) continue;

                            var pps1 = _pipes.Where(x => lbDict.ContainsKey(x)).ToList();
                            var pps2 = _pipes.Where(x => !lbDict.ContainsKey(x)).ToList();
                            if (pps1.Count == 1 && pps2.Count == 1)
                            {
                                var pp1 = pps1[0];
                                var pp2 = pps2[0];
                                var gf = ThCADCoreNTSService.Instance.PreparedGeometryFactory.Create(wline);
                                //两根立管都要与wline相连才行
                                if (gf.Intersects(pp1) && gf.Intersects(pp2))
                                {
                                    var label = lbDict[pp1];
                                    lbDict[pp2] = label;
                                    //连线的长度小于等于300。而且连线只有一条直线的情况是短转管
                                    var isShort = false;
                                    if (geoData.WLines[cadDataMain.WLines.IndexOf(wline)].Length <= 300)
                                    {
                                        isShort = true;
                                        shortTranslatorLabels.Add(label);
                                    }
                                    //然后才是长转管
                                    if (!isShort) longTranslatorLabels.Add(label);
                                }
                            }
                        }
                    }
                }


                {
                    //var pps = new List<GRect>();
                    //foreach (var o in item.VerticalPipes)
                    //{
                    //    var j = cadData.VerticalPipes.IndexOf(o);
                    //    var m = geoData.VerticalPipes[j];
                    //    pps.Add(m);
                    //}
                    GRect getRect(Geometry o)
                    {
                        return geoData.VerticalPipes[cadDataMain.VerticalPipes.IndexOf(o)];
                    }
                    ThRainSystemService.Triangle(item.VerticalPipes, (_pp1, _pp2) =>
                    {
                        Geometry pp1, pp2;
                        if (lbDict.ContainsKey(_pp1) && !lbDict.ContainsKey(_pp2))
                        {
                            pp1 = _pp1; pp2 = _pp2;
                        }
                        else if (!lbDict.ContainsKey(_pp1) && lbDict.ContainsKey(_pp2))
                        {
                            pp1 = _pp2; pp2 = _pp1;
                        }
                        else
                        {
                            return;
                        }
                        var r1 = getRect(pp1);
                        var r2 = getRect(pp2);
                        if (r1.Center.GetDistanceTo(r2.Center) < r1.OuterRadius + r2.OuterRadius + 5)
                        {
                            var label = lbDict[pp1];
                            lbDict[pp2] = label;
                            shortTranslatorLabels.Add(label);
                        }
                    });
                }
                {
                    var pipes = item.VerticalPipes.Where(p => lbDict.ContainsKey(p)).ToList();
                    ThRainSystemService.Triangle(pipes, (p1, p2) =>
                    {
                        if (lbDict[p1] != lbDict[p2]) return;
                        var label = lbDict[p1];
                        if (p1.GetCenter().GetDistanceTo(p2.GetCenter()) <= 300)
                        {
                            longTranslatorLabels.Remove(label);
                            shortTranslatorLabels.Add(label);
                        }
                    });
                }

                var _longTranslatorLabels = longTranslatorLabels.Distinct().ToList();
                _longTranslatorLabels.Sort();
                sb.AppendLine("长转管:" + _longTranslatorLabels.JoinWith(","));
                drData.LongTranslatorLabels.AddRange(_longTranslatorLabels);

                var _shortTranslatorLabels = shortTranslatorLabels.ToList();
                _shortTranslatorLabels.Sort();
                sb.AppendLine("短转管:" + _shortTranslatorLabels.JoinWith(","));
                drData.ShortTranslatorLabels.AddRange(_shortTranslatorLabels);

                #region 地漏
                //var floorDrainsLabelAndCount = new CountDict<string>();
                var floorDrainsLabelAndEnts = new ListDict<string, Geometry>();
                var floorDrainsWrappingPipesLabelAndEnts = new ListDict<string, Geometry>();
                {
                    //foreach (var group in wLinesGroups)
                    {
                        //var gs =GeometryFac.GroupGeometries(ToList(group, item.VerticalPipes, item.FloorDrains));
                        var gs = GeoFac.GroupGeometries(ToList(item.WLines, item.VerticalPipes, item.FloorDrains));
                        foreach (var g in gs)
                        {
                            var pps = g.Where(pl => item.VerticalPipes.Contains(pl)).ToList();
                            var fds = g.Where(pl => item.FloorDrains.Contains(pl)).ToList();
                            var wlines = g.Where(pl => item.WLines.Contains(pl)).ToList();
                            //var wrappingPipes = g.Where(pl => item.WrappingPipes.Contains(pl)).ToList();
                            var wrappingPipes = new List<Geometry>();
                            {
                                var f = GeoFac.CreateGeometrySelector(item.WrappingPipes);
                                foreach (var wline in wlines)
                                {
                                    wrappingPipes.AddRange(f(wline));
                                }
                                wrappingPipes = wrappingPipes.Distinct().ToList();
                            }

                            if (!AllNotEmpty(pps, fds, wlines)) continue;

                            //{
                            //    var r = GeoAlgorithm.GetEntitiesBoundaryRect(g);
                            //    r.Expand(10);
                            //    var pl = DU.DrawRectLazy(r);
                            //    pl.ColorIndex = 1;
                            //}

                            foreach (var pp in pps)
                            {
                                //新的逻辑
                                {
                                    lbDict.TryGetValue(pp, out string label);
                                    if (label == null) continue;

                                    {
                                        var lst = ToList(wlines, fds);
                                        lst.Add(pp);
                                        var _gs = GeoFac.GroupGeometries(lst);
                                        foreach (var _g in _gs)
                                        {
                                            if (!_g.Contains(pp)) continue;
                                            var _fds = g.Where(pl => fds.Contains(pl)).ToList();
                                            var _wlines = g.Where(pl => wlines.Contains(pl)).ToList();
                                            if (!AllNotEmpty(_fds, _wlines)) continue;
                                            {
                                                //pipe和wline不相交的情况，跳过
                                                var f = GeoFac.CreateGeometrySelector(_wlines);
                                                if (f(pp).Count == 0) continue;
                                            }
                                            foreach (var fd in _fds)
                                            {
                                                floorDrainsLabelAndEnts.Add(label, fd);
                                            }
                                            {
                                                //套管还要在wline上才行
                                                var _wrappingPipes = new List<Geometry>();
                                                var f = GeoFac.CreateGeometrySelector(wrappingPipes);
                                                foreach (var wline in _wlines)
                                                {
                                                    _wrappingPipes.AddRange(f(wline));
                                                }
                                                _wrappingPipes = wrappingPipes.Distinct().ToList();
                                                foreach (var wp in _wrappingPipes)
                                                {
                                                    floorDrainsWrappingPipesLabelAndEnts.Add(label, wp);
                                                }
                                            }
                                        }
                                    }
                                    if (false)
                                    {
                                        var lst = ToList(wlines, fds, wrappingPipes);
                                        lst.Add(pp);
                                        var _gs = GeoFac.GroupGeometries(lst);
                                        foreach (var _g in _gs)
                                        {
                                            var _fds = g.Where(pl => fds.Contains(pl)).ToList();
                                            var _wlines = g.Where(pl => wlines.Contains(pl)).ToList();
                                            var _wrappingPipes = g.Where(pl => wrappingPipes.Contains(pl)).ToList();
                                            var _pps = g.Where(pl => pl == pp).ToList();
                                            if (!AllNotEmpty(_fds, _wlines, _pps)) continue;
                                            {
                                                //pipe和wline不相交的情况，跳过
                                                var f = GeoFac.CreateGeometrySelector(_wlines);
                                                if (f(pp).Count == 0) continue;
                                            }
                                            foreach (var fd in _fds)
                                            {
                                                floorDrainsLabelAndEnts.Add(label, fd);
                                            }
                                            {
                                                //套管还要在wline上才行
                                                var __gs = GeoFac.GroupGeometries(ToList(_wrappingPipes, _wlines));
                                                foreach (var __g in __gs)
                                                {
                                                    var __wlines = __g.Where(pl => _wlines.Contains(pl)).ToList();
                                                    var wps = __g.Where(pl => _wrappingPipes.Contains(pl)).ToList();
                                                    if (!AllNotEmpty(wps, __wlines)) continue;
                                                    foreach (var wp in wps)
                                                    {
                                                        floorDrainsWrappingPipesLabelAndEnts.Add(label, wp);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                ////原先的逻辑
                                //if (false)
                                //{
                                //    lbDict.TryGetValue(pp, out string label);
                                //    if (label != null)
                                //    {
                                //        //floorDrainsLabelAndCount[label] += fds.Count;
                                //        foreach (var fd in fds)
                                //        {
                                //            floorDrainsLabelAndEnts.Add(label, fd);
                                //        }
                                //        //foreach (var wp in wrappingPipes)
                                //        //{
                                //        //    floorDrainsWrappingPipesLabelAndEnts.Add(label, wp);
                                //        //}
                                //        //套管还要在wline上才行
                                //        var _gs = GeometryFac.GroupGeometries(ToList(wrappingPipes, item.WLines));
                                //        foreach (var _g in _gs)
                                //        {
                                //            var _wlines = _g.Where(pl => item.WLines.Contains(pl)).ToList();
                                //            var wps = _g.Where(pl => item.WrappingPipes.Contains(pl)).ToList();
                                //            if (!AllNotEmpty(wps, _wlines)) continue;
                                //            foreach (var wp in wps)
                                //            {
                                //                floorDrainsWrappingPipesLabelAndEnts.Add(label, wp);
                                //            }
                                //        }
                                //    }
                                //}
                            }
                        }
                    }
                }


                {


                }
                {
                    //😗佳兆业滨江新城--NL1-4和NL2-4莫名多了地漏
                    foreach (var kv in floorDrainsLabelAndEnts.Where(x => x.Value.Distinct().Count() == 2).ToList())
                    {
                        var fds = kv.Value.Distinct().ToList();
                        var label = kv.Key;
                        var pipes = item.VerticalPipes.Where(pipe =>
                        {
                            lbDict.TryGetValue(pipe, out string _label);
                            return _label == label;
                        }).ToList();
                        if (pipes.Count == 2)
                        {
                            var c1 = pipes[0].GetCenter();
                            var c2 = pipes[1].GetCenter();
                            var dis1 = c1.GetDistanceTo(c2);
                            var c3 = fds[0].GetCenter();
                            var c4 = fds[1].GetCenter();
                            var dis2 = c3.GetDistanceTo(c4);

                            if (Math.Abs(dis1 - dis2) < 1 && (c1 - c2).IsParallelTo(c3 - c4, new Tolerance(1, 1)))
                            {
                                {
                                    var lst = floorDrainsLabelAndEnts[label];
                                    lst.Clear();
                                    lst.Add(fds[0]);
                                }
                                {
                                    floorDrainsWrappingPipesLabelAndEnts.TryGetValue(label, out List<Geometry> lst);
                                    if (lst != null)
                                    {
                                        if (lst.Count > 1)
                                        {
                                            var x = lst[0];
                                            lst.Clear();
                                            lst.Add(x);
                                        }
                                    }
                                }
                            }
                        }

                    }
                }

                {
                    var ok_labels = new HashSet<string>(floorDrainsLabelAndEnts.Where(kv => kv.Value.Count > 0).Select(kv => kv.Key));
                    //当地漏和立管非常靠近的情况下，有的设计师会不画横管，导致程序识别时只找到了立管。
                    //处理方式：
                    //若地漏没有连接任何横管，则在地漏圆心500的范围内找没有连接任何地漏的最近的NL或Y2L，认为两者相连。
                    var gs = GeoFac.GroupGeometries(ToList(item.FloorDrains, item.WLines));
                    var f = GeoFac.CreateGeometrySelector(item.VerticalPipes);
                    foreach (var g in gs)
                    {
                        if (g.Count == 1)
                        {
                            var _x = g[0];
                            if (item.FloorDrains.Contains(_x))
                            {
                                var fd = _x;
                                var center = fd.GetCenter().ToPoint3d();
                                var range = GeoFac.CreateCirclePolygon(center, 500, 6);
                                var pipes = f(range).Where(pipe =>
                                {
                                    lbDict.TryGetValue(pipe, out string label);
                                    if (label != null && label.StartsWith("Y2L") && label.StartsWith("NL"))
                                    {
                                        if (!ok_labels.Contains(label))
                                        {
                                            return true;
                                        }
                                    }
                                    return false;
                                }).ToList();
                                var pipe = GeoFac.NearestNeighbourPoint3dF(pipes)(center);
                                if (pipe != null)
                                {
                                    lbDict.TryGetValue(pipe, out string label);
                                    if (label != null)
                                    {
                                        floorDrainsLabelAndEnts.Add(label, fd);
                                        ok_labels.Add(label);
                                    }
                                }
                            }
                        }
                    }
                }
                //sb.AppendLine("地漏:" + floorDrainsLabelAndCount.Select(kv => $"{kv.Key}({kv.Value})").JoinWith(","));
                sb.AppendLine("地漏:" + floorDrainsLabelAndEnts
                    .Where(kv => kv.Value.Count > 0).Select(kv => $"{kv.Key}({kv.Value.Distinct().Count()})").JoinWith(","));
                sb.AppendLine("地漏套管:" + floorDrainsWrappingPipesLabelAndEnts
        .Where(kv => kv.Value.Count > 0).Select(kv => $"{kv.Key}({kv.Value.Distinct().Count()})").JoinWith(","));
                foreach (var kv in floorDrainsLabelAndEnts)
                {
                    drData.FloorDrains.Add(new KeyValuePair<string, int>(kv.Key, kv.Value.Distinct().Count()));
                }
                foreach (var kv in floorDrainsWrappingPipesLabelAndEnts)
                {
                    drData.FloorDrainsWrappingPipes.Add(new KeyValuePair<string, int>(kv.Key, kv.Value.Distinct().Count()));
                }
                #endregion

                #region 冷凝管
                var condensePipesLabelAndEnts = new ListDict<string, Geometry>();
                {
                    //foreach (var group in wLinesGroups)
                    {
                        //var gs =GeometryFac.GroupGeometries(ToList(group, item.VerticalPipes, item.CondensePipes));
                        var gs = GeoFac.GroupGeometries(ToList(item.WLines, item.VerticalPipes, item.CondensePipes));
                        foreach (var g in gs)
                        {
                            var pps = g.Where(pl => item.VerticalPipes.Contains(pl)).ToList();
                            var cps = g.Where(pl => item.CondensePipes.Contains(pl)).ToList();
                            var wlines = g.Where(pl => item.WLines.Contains(pl)).ToList();
                            if (!AllNotEmpty(pps, cps, wlines)) continue;

                            if (pps.Count != 1) continue;
                            //{
                            //    var r = GeoAlgorithm.GetEntitiesBoundaryRect(g);
                            //    r.Expand(10);
                            //    var pl = DU.DrawRectLazy(r);
                            //    pl.ColorIndex = 5;
                            //}
                            var pp = pps[0];
                            lbDict.TryGetValue(pp, out string label);
                            if (label != null)
                            {
                                //floorDrainsLabelAndCount[label] += fds.Count;
                                foreach (var cp in cps)
                                {
                                    condensePipesLabelAndEnts.Add(label, cp);
                                }
                            }
                        }
                    }
                }
                //sb.AppendLine("冷凝管:" + condensePipesLabelAndEnts
                //   .Where(kv => kv.Value.Count > 0).Select(kv => $"{kv.Key}({kv.Value.Distinct().Count()})").JoinWith(","));

                //生成辅助线
                var brokenCondensePipeLines = new List<GLineSegment>();
                {
                    var wlines = item.WLines.Select(o => geoData.WLines[cadDataMain.WLines.IndexOf(o)]).ToList();
                    var li = wlines.SelectInts(wl => wl.IsHorizontal(5)).ToList();
                    ThRainSystemService.Triangle(li, (i, j) =>
                    {
                        var kvs = GeoAlgorithm.YieldPoints(wlines[i], wlines[j]).ToList();
                        var pts = kvs.Flattern().ToList();
                        var tol = 5;
                        var _y = pts[0].Y;
                        if (pts.All(pt => GeoAlgorithm.InRange(pt.Y, _y, tol)))
                        {
                            var dis = kvs.Select(kv => kv.Key.GetDistanceTo(kv.Value)).Min();
                            if (dis > 100 && dis < 300)
                            {
                                var x1 = pts.Select(pt => pt.X).Min();
                                var x2 = pts.Select(pt => pt.X).Max();
                                var newSeg = new GLineSegment(x1, _y, x2, _y);
                                if (newSeg.Length > 0) brokenCondensePipeLines.Add(newSeg);
                                //var pl=DU.DrawGeometryLazy(newSeg);
                                //pl.ConstantWidth = 20;
                            }
                        }
                    });
                }
                //收集断开的冷凝管
                var brokenCondensePipes = new List<List<Geometry>>();
                {
                    var bkCondensePipeLines = brokenCondensePipeLines.Select(seg => seg.Buffer(10)).ToList();
                    var gs = GeoFac.GroupGeometries(ToList(item.CondensePipes, bkCondensePipeLines));
                    foreach (var g in gs)
                    {
                        var cps = g.Where(pl => item.CondensePipes.Contains(pl)).ToList();
                        var wlines = g.Where(pl => bkCondensePipeLines.Contains(pl)).ToList();
                        if (!AllNotEmpty(cps, wlines)) continue;
                        if (cps.Count < 2) continue;
                        brokenCondensePipes.Add(cps);
                    }
                }
                {
                    IEnumerable<KeyValuePair<string, KeyValuePair<int, bool>>> GetCondensePipesData()
                    {
                        foreach (var kv in condensePipesLabelAndEnts)
                        {
                            List<Geometry> f()
                            {
                                var lst = kv.Value.ToList();
                                foreach (var cp1 in lst)
                                {
                                    foreach (var lst2 in brokenCondensePipes)
                                    {
                                        foreach (var cp2 in lst2)
                                        {
                                            if (cp1 == cp2)
                                            {
                                                return lst2;
                                            }
                                        }
                                    }
                                }
                                return null;
                            }
                            var ret = f();
                            if (ret == null)
                            {
                                //yield return $"{kv.Key}({kv.Value.Count},非断开)";
                                yield return new KeyValuePair<string, KeyValuePair<int, bool>>(kv.Key, new KeyValuePair<int, bool>(kv.Value.Count, false));
                            }
                            else
                            {
                                //yield return $"{kv.Key}({ret.Count},断开)";
                                yield return new KeyValuePair<string, KeyValuePair<int, bool>>(kv.Key, new KeyValuePair<int, bool>(ret.Count, true));
                            }
                        }
                    }
                    var lst = GetCondensePipesData().ToList();
                    sb.AppendLine("冷凝管:" + lst.Select(kv => kv.Value.Value ? $"{kv.Key}({kv.Value.Value},断开)" : $"{kv.Key}({kv.Value.Value},非断开)").JoinWith(","));
                    drData.CondensePipes.AddRange(lst);
                }

                #endregion

                var waterWellsWrappingPipesLabelAndEnts = new ListDict<string, Geometry>();
                var rainPortsWrappingPipesLabelAndEnts = new ListDict<string, Geometry>();
                var outputDict = new Dictionary<string, RainOutputTypeEnum>();

                bool ok = false;
                void CollectOutputs(List<Geometry> item_WLines, List<Geometry> item_WaterWells,
                    List<Geometry> item_WrappingPipes, List<Geometry> item_WaterPortSymbols, List<Geometry> item_VerticalPipes)
                {
                    if (ok)
                    {
                        foreach (var geo in item_WLines)
                        {
                            DU.DrawGeometryLazy(geo);
                        }
                    }
                    var wlinesGeo = new List<Geometry>();
                    {
                        var gs = GeoFac.GroupGeometries(item_WLines);
                        foreach (var g in gs)
                        {
                            wlinesGeo.Add(GeoFac.CreateGeometry(g.ToArray()));
                        }
                    }
                    var filtWLines = GeoFac.CreateGeometrySelector(wlinesGeo);
                    {
                        var f1 = GeoFac.CreateGeometrySelector(item_WrappingPipes);
                        var f2 = GeoFac.CreateGeometrySelector(item_WaterPortSymbols);
                        var gs = GeoFac.GroupGeometries(ToList(item_WLines, item_VerticalPipes, item_WaterPortSymbols));
                        foreach (var g in gs)
                        {
                            var pps = g.Where(pl => item_VerticalPipes.Contains(pl)).ToList();
                            var wlines = g.Where(pl => item_WLines.Contains(pl)).ToList();
                            var symbols = g.Where(pl => item_WaterPortSymbols.Contains(pl)).ToList();
                            if (!AllNotEmpty(pps, wlines, symbols)) continue;
                            foreach (var pp in pps)
                            {
                                lbDict.TryGetValue(pp, out string label);
                                if (label != null)
                                {
                                    if (outputDict.ContainsKey(label)) continue;
                                    outputDict[label] = RainOutputTypeEnum.RainPort;
                                    var lst = new List<Geometry>();
                                    lst.Add(pp);
                                    lst.AddRange(wlines);
                                    var geo = GeoFac.CreateGeometry(lst.ToArray());
                                    var wp = f1(geo).FirstOrDefault();
                                    if (wp != null)
                                    {
                                        rainPortsWrappingPipesLabelAndEnts.Add(label, wp);
                                    }
                                }
                            }
                        }
                        foreach (var pipe in item_VerticalPipes)
                        {
                            void f()
                            {
                                lbDict.TryGetValue(pipe, out string label);
                                if (label != null)
                                {
                                    if (!outputDict.ContainsKey(label))
                                    {
                                        var wps = f1(pipe);
                                        if (wps.Count == 1)
                                        {
                                            var wp = wps[0];
                                            var wlines = filtWLines(wp);
                                            foreach (var wline in wlines)
                                            {
                                                if (f2(wline).Count > 0)
                                                {
                                                    outputDict[label] = RainOutputTypeEnum.RainPort;
                                                    rainPortsWrappingPipesLabelAndEnts.Add(label, wp);
                                                    return;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            f();
                        }
                    }
                    {
                        var pipeLabelToWaterWellLabels = new List<KeyValuePair<string, string>>();
                        var ok_wells = new HashSet<Geometry>();
                        {
                            var gs = GeoFac.GroupGeometries(ToList(item_WLines, item_VerticalPipes, item_WaterWells));
                            foreach (var g in gs)
                            {
                                var pps = g.Where(pl => item_VerticalPipes.Contains(pl)).ToList();
                                var wlines = g.Where(pl => item_WLines.Contains(pl)).ToList();
                                var wells = g.Where(pl => item_WaterWells.Contains(pl)).ToList();
                                //var wrappingPipes = g.Where(pl => item_WrappingPipes.Contains(pl)).ToList();
                                if (!AllNotEmpty(pps, wlines, wells)) continue;
                                foreach (var pp in pps)
                                {
                                    lbDict.TryGetValue(pp, out string label);
                                    if (label != null)
                                    {
                                        if (outputDict.ContainsKey(label)) continue;
                                        outputDict[label] = RainOutputTypeEnum.WaterWell;
                                        foreach (var w in wells)
                                        {
                                            var wellLabel = GeoData.WaterWellLabels[CadDataMain.WaterWells.IndexOf(w)];
                                            pipeLabelToWaterWellLabels.Add(new KeyValuePair<string, string>(label, wellLabel));
                                        }
                                        ok_wells.AddRange(wells);

                                        ////套管还要在wline上才行
                                        //var _lst = ToList(wrappingPipes, wlines);
                                        //_lst.Add(pp);
                                        //var _gs = GeometryFac.GroupGeometries(_lst);
                                        //foreach (var _g in _gs)
                                        //{
                                        //    if (!_g.Contains(pp)) continue;
                                        //    //var _wells = _g.Where(pl => item_WaterWells.Contains(pl)).ToList();
                                        //    var _wlines = _g.Where(pl => item_WLines.Contains(pl)).ToList();
                                        //    var wps = _g.Where(pl => item_WrappingPipes.Contains(pl)).ToList();
                                        //    if (!AllNotEmpty(wps, _wlines)) continue;
                                        //    foreach (var wp in wps)
                                        //    {
                                        //        waterWellsWrappingPipesLabelAndEnts.Add(label, wp);
                                        //    }
                                        //}
                                        {
                                            //检查是否有套管
                                            void f()
                                            {
                                                foreach (var wp in item_WrappingPipes)
                                                {
                                                    var lst = ToList(wlines);
                                                    lst.Add(wp);
                                                    lst.Add(pp);
                                                    var _gs = GeoFac.GroupGeometries(lst);
                                                    foreach (var _g in _gs)
                                                    {
                                                        if (!_g.Contains(wp)) continue;
                                                        if (!_g.Contains(pp)) continue;
                                                        if (!g.Any(pl => wlines.Contains(pl))) continue;
                                                        //OK,有套管
                                                        waterWellsWrappingPipesLabelAndEnts.Add(label, wp);
                                                        return;
                                                    }
                                                }
                                            }
                                            f();
                                        }


                                    }
                                }
                            }
                        }
                        {
                            var _wells = item_WaterWells.Except(ok_wells).ToList();
                            var gwells = _wells.Select(o => geoData.WaterWells[cadDataMain.WaterWells.IndexOf(o)]).ToList();
                            for (int k = 0; k < gwells.Count; k++)
                            {
                                gwells[k] = GRect.Create(gwells[k].Center, 1500);
                            }
                            var shadowWells = gwells.Select(r => r.ToLinearRing()).Cast<Geometry>().ToList();
                            var gs = GeoFac.GroupGeometries(ToList(item_WLines, item_VerticalPipes, shadowWells, item.WaterPort13s));
                            foreach (var g in gs)
                            {
                                var pps = g.Where(pl => item_VerticalPipes.Contains(pl)).ToList();
                                var wlines = g.Where(pl => item_WLines.Contains(pl)).ToList();
                                var wells = g.Where(pl =>
                                {
                                    var k = shadowWells.IndexOf(pl);
                                    if (k < 0) return false;
                                    return item_WaterWells.Contains(_wells[k]);
                                }).ToList();
                                //var wrappingPipes = g.Where(pl => item_WrappingPipes.Contains(pl)).ToList();

                                if (!AllNotEmpty(pps, wlines, wells)) continue;
                                foreach (var pp in pps)
                                {
                                    lbDict.TryGetValue(pp, out string label);
                                    if (label != null)
                                    {
                                        if (outputDict.ContainsKey(label)) continue;
                                        outputDict[label] = RainOutputTypeEnum.WaterWell;
                                        foreach (var w in wells)
                                        {
                                            var wellLabel = GeoData.WaterWellLabels[CadDataMain.WaterWells.IndexOf(_wells[shadowWells.IndexOf(w)])];
                                            pipeLabelToWaterWellLabels.Add(new KeyValuePair<string, string>(label, wellLabel));
                                        }
                                        ok_wells.AddRange(wells);

                                        ////检查是否有套管
                                        //if (wrappingPipes.Count > 0)
                                        //{
                                        //    //套管还要在wline上才行
                                        //    var _lst = ToList(wrappingPipes, wlines);
                                        //    _lst.Add(pp);
                                        //    var _gs = GeometryFac.GroupGeometries(_lst);
                                        //    foreach (var _g in _gs)
                                        //    {
                                        //        if (!_g.Contains(pp)) continue;
                                        //        //var _wells = _g.Where(pl => item_WaterWells.Contains(pl)).ToList();
                                        //        var _wlines = _g.Where(pl => item_WLines.Contains(pl)).ToList();
                                        //        var wps = _g.Where(pl => item_WrappingPipes.Contains(pl)).ToList();
                                        //        if (!AllNotEmpty(wps, _wlines)) continue;
                                        //        foreach (var wp in wps)
                                        //        {
                                        //            waterWellsWrappingPipesLabelAndEnts.Add(label, wp);
                                        //        }
                                        //    }
                                        //}

                                        {
                                            //检查是否有套管
                                            void f()
                                            {
                                                foreach (var wp in item_WrappingPipes)
                                                {
                                                    var lst = ToList(wlines);
                                                    lst.Add(wp);
                                                    lst.Add(pp);
                                                    var _gs = GeoFac.GroupGeometries(lst);
                                                    foreach (var _g in _gs)
                                                    {
                                                        if (!_g.Contains(wp)) continue;
                                                        if (!_g.Contains(pp)) continue;
                                                        if (!g.Any(pl => wlines.Contains(pl))) continue;
                                                        //OK,有套管
                                                        waterWellsWrappingPipesLabelAndEnts.Add(label, wp);
                                                        return;
                                                    }
                                                }
                                            }
                                            f();
                                        }
                                    }
                                }
                            }
                        }

                        sb.AppendLine("pipeLabelToWaterWellLabels：" + pipeLabelToWaterWellLabels.ToCadJson());
                        drData.PipeLabelToWaterWellLabels.AddRange(pipeLabelToWaterWellLabels);
                    }
                    {
                        var f1 = GeoFac.CreateGeometrySelector(item_WrappingPipes);
                        var f3 = GeoFac.CreateGeometrySelector(item_WaterPortSymbols);
                        foreach (var pipe in item_VerticalPipes)
                        {
                            lbDict.TryGetValue(pipe, out string label);
                            if (label != null)
                            {
                                if (!outputDict.ContainsKey(label))
                                {
                                    var wrappingPipes = f1(pipe);
                                    if (wrappingPipes.Count == 1)
                                    {
                                        var wp = wrappingPipes[0];
                                        var _wlines = filtWLines(wp);
                                        if (_wlines.Count == 1)
                                        {
                                            var wline = _wlines[0];
                                            var symbols = f3(wline);
                                            if (symbols.Count == 1)
                                            {
                                                var symbol = symbols[0];
                                                outputDict[label] = RainOutputTypeEnum.RainPort;
                                                rainPortsWrappingPipesLabelAndEnts.Add(label, wp);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        var f2 = GeoFac.CreateGeometrySelector(item.WaterPort13s);
                        foreach (var pipe in item_VerticalPipes)
                        {
                            void f()
                            {
                                lbDict.TryGetValue(pipe, out string label);
                                if (label != null)
                                {
                                    if (!outputDict.ContainsKey(label))
                                    {
                                        foreach (var wlineG in filtWLines(pipe))
                                        {
                                            if (f2(wlineG).Any())
                                            {
                                                outputDict[label] = RainOutputTypeEnum.RainPort;
                                                foreach (var wp in f1(pipe).Concat(f1(wlineG).Distinct()))
                                                {
                                                    rainPortsWrappingPipesLabelAndEnts.Add(label, wp);
                                                }
                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                            f();
                        }
                    }
                }

                CollectOutputs(item.WLines, item.WaterWells, item.WrappingPipes, item.WaterPortSymbols, item.VerticalPipes);
                {
                    var wlines1 = item.WLines.Select(pl => geoData.WLines[cadDataMain.WLines.IndexOf(pl)]).ToList();
                    var rs = item.WaterWells.Select(pl => geoData.WaterWells[cadDataMain.WaterWells.IndexOf(pl)])
                        .Concat(item.WrappingPipes.Select(pl => geoData.WrappingPipes[cadDataMain.WrappingPipes.IndexOf(pl)]))
                        .Concat(item.WaterPortSymbols.Select(pl => geoData.WaterPortSymbols[cadDataMain.WaterPortSymbols.IndexOf(pl)]))
                        .Concat(item.VerticalPipes.Select(pl => geoData.VerticalPipes[cadDataMain.VerticalPipes.IndexOf(pl)]))
                        .ToList();

                    void f5(List<GLineSegment> segs)
                    {
                        var _wlines1 = wlines1.Select(seg => seg.Buffer(10)).ToList();
                        var f1 = GeoFac.CreateGeometrySelector(_wlines1);
                        var _wlines2 = segs.Select(seg => seg.Extend(-20).Buffer(10)).ToList();
                        var _wlines3 = segs.Select(seg => seg.Extend(.1).Buffer(10)).ToList();
                        var f2 = GeoFac.CreateGeometrySelector(_wlines2);
                        var _rs = item.WaterWells.Concat(item.WrappingPipes).Concat(item.WaterPortSymbols).Concat(item.VerticalPipes).Distinct().ToArray();
                        var __wlines2 = _wlines2.Except(f2(GeoFac.CreateGeometry(_rs))).Distinct().ToList();
                        var __wlines3 = __wlines2.Select(_wlines2).ToList().Select(_wlines3).ToList();
                        var wlines = _wlines1.Except(f1(GeoFac.CreateGeometry(__wlines2.ToArray()))).Concat(__wlines3).Distinct().ToList();

                        //FengDbgTesting.AddLazyAction("看看自动连接OK不", adb =>
                        //{
                        //    Dbg.BuildAndSetCurrentLayer(adb.Database);
                        //    foreach (var wline in wlines)
                        //    {
                        //        DU.DrawEntitiesLazy(wline.ToDbObjects().OfType<Entity>().ToList());
                        //    }
                        //    DU.Draw();
                        //});
                        {
                            foreach (var wline in __wlines3)
                            {
                                DU.DrawEntitiesLazy(wline.ToDbObjects().OfType<Entity>().ToList());
                            }
                        }
                        CollectOutputs(wlines, item.WaterWells, item.WrappingPipes, item.WaterPortSymbols, item.VerticalPipes);
                    }
                    {
                        var segs = FengDbgTesting.GetSegsToConnect(wlines1, rs, 8000, radius: 15).Distinct().ToList();
                        //FengDbgTesting.AddLazyAction("看看自动连接OK不", adb =>
                        //{
                        //    Dbg.BuildAndSetCurrentLayer(adb.Database);
                        //    foreach (var seg in segs)
                        //    {
                        //        DU.DrawLineSegmentLazy(seg);
                        //    }
                        //    DU.Draw();
                        //});
                        f5(segs);
                    }
                    {
                        var segs = item.WLinesAddition.Select(cadDataMain.WLinesAddition).ToList().Select(geoData.WLinesAddition).ToList();
                        ok = true;
                        f5(segs);
                    }
                }


                sb.AppendLine("排出方式：" + outputDict.ToCadJson());
                sb.AppendLine("雨水井套管:" + waterWellsWrappingPipesLabelAndEnts
        .Where(kv => kv.Value.Count > 0).Select(kv => $"{kv.Key}({kv.Value.Distinct().Count()})").JoinWith(","));
                sb.AppendLine("雨水口套管:" + rainPortsWrappingPipesLabelAndEnts
        .Where(kv => kv.Value.Count > 0).Select(kv => $"{kv.Key}({kv.Value.Distinct().Count()})").JoinWith(","));
                foreach (var kv in outputDict)
                {
                    drData.OutputTypes.Add(kv);
                }
                foreach (var kv in waterWellsWrappingPipesLabelAndEnts)
                {
                    drData.WaterWellWrappingPipes.Add(new KeyValuePair<string, int>(kv.Key, kv.Value.Distinct().Count()));
                }
                foreach (var kv in rainPortsWrappingPipesLabelAndEnts)
                {
                    drData.RainPortWrappingPipes.Add(new KeyValuePair<string, int>(kv.Key, kv.Value.Distinct().Count()));
                }

                var gravityWaterBucketTranslatorLabels = new List<string>();
                {
                    var gs = GeoFac.GroupGeometries(ToList(item.WLines, item.VerticalPipes, item.GravityWaterBuckets));
                    foreach (var g in gs)
                    {
                        var pps = g.Where(pl => item.VerticalPipes.Contains(pl)).ToList();
                        var wlines = g.Where(pl => item.WLines.Contains(pl)).ToList();
                        var gbks = g.Where(pl => item.GravityWaterBuckets.Contains(pl)).ToList();
                        if (!AllNotEmpty(pps, wlines, gbks)) continue;

                        foreach (var pp in pps)
                        {
                            lbDict.TryGetValue(pp, out string label);
                            if (label != null)
                            {
                                gravityWaterBucketTranslatorLabels.Add(label);
                            }
                        }
                    }
                }
                gravityWaterBucketTranslatorLabels = gravityWaterBucketTranslatorLabels.Distinct().ToList();
                gravityWaterBucketTranslatorLabels.Sort();
                sb.AppendLine("重力雨水斗转管:" + gravityWaterBucketTranslatorLabels.JoinWith(","));
                drData.GravityWaterBucketTranslatorLabels.AddRange(gravityWaterBucketTranslatorLabels);


                {
                    //补丁：把部分重力雨水斗变成87雨水斗
                    var _87bks = new List<Geometry>();
                    var labels = item.Labels.Where(lb => geoData.Labels[cadDataMain.Labels.IndexOf(lb)].Text == "87型雨水斗").ToList();
                    foreach (var label in labels)
                    {
                        var lst = item.LabelLines.ToList();
                        lst.Add(label);
                        lst.AddRange(item.GravityWaterBuckets);
                        var gs = GeoFac.GroupGeometries(lst);
                        foreach (var g in gs)
                        {
                            if (!g.Contains(label)) continue;
                            var labelLines = g.Where(pl => item.LabelLines.Contains(pl)).ToList();
                            var bks = g.Where(pl => item.GravityWaterBuckets.Contains(pl)).ToList();
                            if (!AllNotEmpty(labelLines, bks)) continue;
                            _87bks.AddRange(bks);
                        }
                    }
                    _87bks = _87bks.Distinct().ToList();
                    item._87WaterBuckets.AddRange(_87bks);
                    cadDataMain._87WaterBuckets.AddRange(_87bks);
                    geoData._87WaterBuckets.AddRange(_87bks.Select(pl => geoData.GravityWaterBuckets[cadDataMain.GravityWaterBuckets.IndexOf(pl)]));
                    foreach (var bk in _87bks)
                    {
                        item.GravityWaterBuckets.Remove(bk);
                        var i = cadDataMain.GravityWaterBuckets.IndexOf(bk);
                        cadDataMain.GravityWaterBuckets.RemoveAt(i);
                        geoData.GravityWaterBuckets.RemoveAt(i);
                    }
                    foreach (var o in item.GravityWaterBuckets)
                    {
                        var j = cadDataMain.GravityWaterBuckets.IndexOf(o);
                        var m = geoData.GravityWaterBuckets[j];
                        var e = DU.DrawRectLazy(m);
                        e.ColorIndex = 7;
                        Dbg.ShowXLabel(m.Center, 200);
                    }
                    foreach (var o in item._87WaterBuckets)
                    {
                        var j = cadDataMain._87WaterBuckets.IndexOf(o);
                        var m = geoData._87WaterBuckets[j];
                        var e = DU.DrawRectLazy(m);
                        e.ColorIndex = 7;
                        Dbg.ShowXLabel(m.Center, 500);
                    }
                }




                {
                    foreach (var kv in waterBucketFixingList)
                    {
                        break;
                        lbDict.TryGetValue(kv.Value, out string label);
                        if (label != null)
                        {
                            break;
                        }
                    }

                }
                {
                    var f1 = GeoFac.CreateGeometrySelector(item.WLines);
                    var f2 = GeoFac.CreateGeometrySelector(lbDict.Keys.ToList());
                    foreach (var kv in lbDict)
                    {
                        var m = ThRainSystemService.TestGravityLabelConnected(kv.Value);
                        if (m.Success)
                        {
                            var pipe = kv.Key;
                            var targetFloor = m.Groups[1].Value;
                            var lst = f1(pipe);
                            if (lst.Count == 1)
                            {
                                lst = f2(lst[0]);
                                lst.Remove(pipe);
                                if (lst.Count == 1)
                                {
                                    lbDict.TryGetValue(lst[0], out string label);
                                    if (label != null)
                                    {
                                        var tp = new Tuple<int, string, string>(storeyI, label, targetFloor);
                                        drData.GravityWaterBucketTranslatorLabels.Add(label);
                                    }
                                }
                            }


                        }
                    }
                }

                {
                    //标出所有的立管编号（看看识别成功了没）
                    foreach (var pp in item.VerticalPipes)
                    {
                        lbDict.TryGetValue(pp, out string label);
                        if (label != null)
                        {
                            DU.DrawTextLazy(label, pp.ToGRect().LeftTop.ToPoint3d());
                        }
                    }

                    foreach (var pp in item.VerticalPipes)
                    {
                        lbDict.TryGetValue(pp, out string label);
                        if (label != null)
                        {
                            var r = GeoData.VerticalPipes[CadDataMain.VerticalPipes.IndexOf(pp)];
                            drData.VerticalPipes.Add(new KeyValuePair<string, GRect>(label, r));
                        }
                    }
                }
            }

            if (ShouldPrintReadableDrawingData) Dbg.PrintText(sb.ToString());
        }
        static bool ShouldPrintReadableDrawingData = true;
        //static bool ShouldPrintReadableDrawingData = false;


        public void FixWaterBucketDN()
        {
            static bool f(ThWSDWaterBucket x) => x.Storey != null && x.Boundary.IsValid;
            var list = new List<ThWSDWaterBucket>();
            list.AddRange(RainSystemDiagram.RoofVerticalRainPipes.Select(x => x.WaterBucket).Where(f));
            list.AddRange(RainSystemDiagram.BalconyVerticalRainPipes.Select(x => x.WaterBucket).Where(f));
            list.AddRange(RainSystemDiagram.CondenseVerticalRainPipes.Select(x => x.WaterBucket).Where(f));
            foreach (var drData in DrawingDatas)
            {
                list.Join(drData.GravityWaterBuckets, x => x.Boundary, x => x.Value, (x, y) =>
                {
                    x.DN = y.Key;
                    return 0;
                }).Count();
            }
        }
        public void AddPipeRunsForRF<T>(string roofPipeNote, T sys) where T : ThWRainPipeSystem
        {
            var runs = sys.PipeRuns;
            var WSDStoreys = RainSystemDiagram.WSDStoreys;
            bool HasGravityLabelConnected(GRect bd, string pipeId)
            {
                var storey = Storeys.FirstOrDefault(s => s.Boundary.Equals(bd));
                if (storey != null)
                {
                    var i = Storeys.IndexOf(storey);
                    var drData = DrawingDatas[i];
                    return drData.GravityWaterBucketTranslatorLabels.Contains(pipeId);
                }
                return false;
            }
            List<Extents3d> GetRelatedGravityWaterBucket(GRect bd)
            {
                var storey = Storeys.FirstOrDefault(s => s.Boundary.Equals(bd));
                if (storey != null)
                {
                    var i = Storeys.IndexOf(storey);
                    return CadDatas[i].GravityWaterBuckets.Select(o => GeoData.GravityWaterBuckets[CadDataMain.GravityWaterBuckets.IndexOf(o)]).Select(x => x.ToExtents2d().ToExtents3d()).ToList();
                }
                return new List<Extents3d>();
            }
            List<Extents3d> GetRelated87WaterBucket(GRect bd)
            {
                var storey = Storeys.FirstOrDefault(s => s.Boundary.Equals(bd));
                if (storey != null)
                {
                    var i = Storeys.IndexOf(storey);
                    return CadDatas[i]._87WaterBuckets.Select(o => GeoData._87WaterBuckets[CadDataMain._87WaterBuckets.IndexOf(o)]).Select(x => x.ToExtents2d().ToExtents3d()).ToList();
                }
                return new List<Extents3d>();
            }
            List<Extents3d> GetSideWaterBucketsInRange(GRect bd)
            {
                var storey = Storeys.FirstOrDefault(s => s.Boundary.Equals(bd));
                if (storey != null)
                {
                    var i = Storeys.IndexOf(storey);
                    return CadDatas[i].SideWaterBuckets.Select(o => GeoData.SideWaterBuckets[CadDataMain.SideWaterBuckets.IndexOf(o)]).Select(x => x.ToExtents2d().ToExtents3d()).ToList();
                }
                return new List<Extents3d>();
            }
            WaterBucketEnum GetRelatedSideWaterBucket(Point3d center)
            {
                var p = center.ToPoint2d();
                foreach (var bd in GeoData.SideWaterBuckets)
                {
                    if (bd.ContainsPoint(p)) return WaterBucketEnum.Side;
                }
                return WaterBucketEnum.None;
            }
            IEnumerable<Point3d> GetCenterOfVerticalPipe(GRect bd, string label)
            {
                var storey = Storeys.FirstOrDefault(s => s.Boundary.Equals(bd));
                if (storey != null)
                {
                    var i = Storeys.IndexOf(storey);
                    var lst = CadDatas[i].VerticalPipes.Select(o => GeoData.VerticalPipes[CadDataMain.VerticalPipes.IndexOf(o)]);
                    var drData = DrawingDatas[i];
                    foreach (var kv in drData.VerticalPipes)
                    {
                        if (kv.Key == label)
                        {
                            var center = kv.Value.Center.ToPoint3d();
                            yield return center;
                        }
                    }
                }
            }
            TranslatorTypeEnum GetTranslatorType(GRect bd, string label)
            {
                var storey = Storeys.FirstOrDefault(s => s.Boundary.Equals(bd));
                if (storey != null)
                {
                    var i = Storeys.IndexOf(storey);
                    var lst = CadDatas[i].VerticalPipes.Select(o => GeoData.VerticalPipes[CadDataMain.VerticalPipes.IndexOf(o)]);
                    var drData = DrawingDatas[i];
                    if (drData.ShortTranslatorLabels.Contains(label)) return TranslatorTypeEnum.Short;
                    if (drData.LongTranslatorLabels.Contains(label)) return TranslatorTypeEnum.Long;
                }
                return TranslatorTypeEnum.None;
            }
            foreach (var s in WSDStoreys)
            {
                if (s.Label == "RF+2" || s.Label == "RF+1")
                {
                    var matchedPipe = s.VerticalPipes.FirstOrDefault(vp => vp.Label == roofPipeNote);
                    if (matchedPipe != null) runs.Add(new ThWRainPipeRun() { Storey = s, MainRainPipe = matchedPipe });
                }
                else
                {
                }


                {
                    //for gravity bucket, still need to check label
                    //尝试通过label得到重力雨水斗
                    var hasWaterBucket = HasGravityLabelConnected(s.Boundary, roofPipeNote);
                    if (hasWaterBucket)
                    {
                        sys.WaterBucket = new ThWSDWaterBucket() { Type = WaterBucketEnum.Gravity, Storey = RainSystemDiagram.GetHigerStorey(s) };

                        //???加这个piperun干啥？
                        //runs.Add(new ThWRainPipeRun()
                        //{
                        //    Storey = RainSystemDiagram.GetHigerStorey(s),
                        //});
                        return;
                    }
                }

                //var storey = Storeys.FirstOrDefault(s => s.Boundary.Equals(s.Boundary));
                //if (storey == null) continue;
                if (sys.WaterBucket.Storey == null)
                {
                    var lowerStorey = RainSystemDiagram.GetLowerStorey(s);
                    if (lowerStorey != null)
                    {
                        void f()
                        {
                            var q = GetCenterOfVerticalPipe(lowerStorey.Boundary, roofPipeNote);
                            foreach (var roofPipeCenter in q)
                            {
                                var waterBucketType = GetRelatedSideWaterBucket(roofPipeCenter);

                                //side
                                if (!waterBucketType.Equals(WaterBucketEnum.None))
                                {
                                    if (s.VerticalPipes.Select(p => p.Label).Contains(roofPipeNote))
                                    {
                                        //Dbg.ShowWhere(roofPipeCenter);
                                        sys.WaterBucket = new ThWSDWaterBucket() { Type = waterBucketType, Storey = s, };
                                        return;
                                    }
                                }
                            }
                        }
                        f();
                    }
                    //尝试通过对位得到侧入雨水斗
                    var allSideWaterBucketsInThisRange = GetSideWaterBucketsInRange(s.Boundary);

                    if (sys.WaterBucket.Storey == null && allSideWaterBucketsInThisRange.Count > 0)
                    {
                        if (lowerStorey != null)
                        {
                            void f()
                            {
                                var q = GetCenterOfVerticalPipe(lowerStorey.Boundary, roofPipeNote);
                                foreach (var roofPipeCenterInLowerStorey in q)
                                {
                                    var lowerBasePt = lowerStorey.Boundary.LeftTop.ToPoint3d();
                                    var pipeCenterInLowerUcs = new Point3d(roofPipeCenterInLowerStorey.X - lowerBasePt.X, roofPipeCenterInLowerStorey.Y - lowerBasePt.Y, 0);

                                    //compute ucs
                                    foreach (var wbe in allSideWaterBucketsInThisRange)
                                    {
                                        var minPt = wbe.MinPoint;
                                        var maxPt = wbe.MaxPoint;

                                        var basePt = s.Boundary.LeftTop.ToPoint3d();

                                        var minPtInUcs = new Point3d(minPt.X - basePt.X, minPt.Y - basePt.Y, 0);
                                        var maxPtInUcs = new Point3d(maxPt.X - basePt.X, maxPt.Y - basePt.Y, 0);

                                        var extentInUcs = new Extents3d(minPtInUcs, maxPtInUcs);

                                        if (extentInUcs.IsPointIn(pipeCenterInLowerUcs))
                                        {
                                            sys.WaterBucket = new ThWSDWaterBucket() { Type = WaterBucketEnum.Side, Storey = s, Boundary = wbe.ToGRect() };
                                            return;
                                        }
                                    }
                                }
                            }
                            f();
                        }
                    }

                    //gravity
                    if (sys.WaterBucket.Storey == null)
                    {
                        //尝试通过对位得到重力雨水斗
                        var allWaterBucketsInThisRange = GetRelatedGravityWaterBucket(s.Boundary);
                        if (allWaterBucketsInThisRange.Count > 0)
                        {
                            if (lowerStorey != null)
                            {
                                void f()
                                {
                                    var q = GetCenterOfVerticalPipe(lowerStorey.Boundary, roofPipeNote);
                                    foreach (var roofPipeCenterInLowerStorey in q)
                                    {
                                        var lowerBasePt = lowerStorey.Boundary.LeftTop.ToPoint3d();
                                        var pipeCenterInLowerUcs = new Point3d(roofPipeCenterInLowerStorey.X - lowerBasePt.X, roofPipeCenterInLowerStorey.Y - lowerBasePt.Y, 0);

                                        //compute ucs
                                        foreach (var wbe in allWaterBucketsInThisRange)
                                        {
                                            var minPt = wbe.MinPoint;
                                            var maxPt = wbe.MaxPoint;

                                            var basePt = s.Boundary.LeftTop.ToPoint3d();

                                            var minPtInUcs = new Point3d(minPt.X - basePt.X, minPt.Y - basePt.Y, 0);
                                            var maxPtInUcs = new Point3d(maxPt.X - basePt.X, maxPt.Y - basePt.Y, 0);

                                            var extentInUcs = new Extents3d(minPtInUcs, maxPtInUcs);

                                            if (extentInUcs.IsPointIn(pipeCenterInLowerUcs))
                                            {
                                                sys.WaterBucket = new ThWSDWaterBucket() { Type = WaterBucketEnum.Gravity, Storey = s, Boundary = wbe.ToGRect(), };
                                                return;
                                            }
                                        }
                                    }


                                }
                                f();
                            }
                        }
                    }

                    //87waterbucket
                    if (sys.WaterBucket.Storey == null)
                    {
                        //尝试通过对位得到87waterbucket
                        var allWaterBucketsInThisRange = GetRelated87WaterBucket(s.Boundary);
                        if (allWaterBucketsInThisRange.Count > 0)
                        {
                            if (lowerStorey != null)
                            {

                                void f()
                                {
                                    var q = GetCenterOfVerticalPipe(lowerStorey.Boundary, roofPipeNote);
                                    foreach (var roofPipeCenterInLowerStorey in q)
                                    {
                                        var lowerBasePt = lowerStorey.Boundary.LeftTop.ToPoint3d();
                                        var pipeCenterInLowerUcs = new Point3d(roofPipeCenterInLowerStorey.X - lowerBasePt.X, roofPipeCenterInLowerStorey.Y - lowerBasePt.Y, 0);

                                        //compute ucs
                                        foreach (var wbe in allWaterBucketsInThisRange)
                                        {
                                            var minPt = wbe.MinPoint;
                                            var maxPt = wbe.MaxPoint;

                                            var basePt = s.Boundary.LeftTop.ToPoint3d();

                                            var minPtInUcs = new Point3d(minPt.X - basePt.X, minPt.Y - basePt.Y, 0);
                                            var maxPtInUcs = new Point3d(maxPt.X - basePt.X, maxPt.Y - basePt.Y, 0);

                                            var extentInUcs = new Extents3d(minPtInUcs, maxPtInUcs);

                                            if (extentInUcs.IsPointIn(pipeCenterInLowerUcs))
                                            {
                                                sys.WaterBucket = new ThWSDWaterBucket() { Type = WaterBucketEnum._87, Storey = s, Boundary = wbe.ToGRect() };
                                                return;
                                            }
                                        }
                                    }

                                }
                                f();
                            }
                        }
                    }


                }
            }
        }
    }
}
