namespace ThMEPWSS.Pipe.Service
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using ThMEPWSS.JsonExtensionsNs;
    using Autodesk.AutoCAD.Geometry;
    using ThMEPWSS.Pipe.Model;
    using Autodesk.AutoCAD.DatabaseServices;
    using Dreambuild.AutoCAD;
    using ThMEPWSS.CADExtensionsNs;
    using ThMEPWSS.Uitl;
    using ThCADCore.NTS;
    using System.Text.RegularExpressions;
    using ThCADExtension;
    using NetTopologySuite.Geometries;
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
        public static List<Point2d> GetLabelLineEndPoints(List<GLineSegment> lines, Geometry killer,double radius=5)
        {
            var points = GetAlivePoints(lines, radius);
            var pts = points.Select(x => new GCircle(x, radius).ToCirclePolygon(6, false)).ToGeometryList();
            var list = lines.Where(x => x.IsHorizontal(5)).Select(x => x.ToLineString()).ToGeometryList();
            list.Add(killer);
            return points.Except(GeoFac.CreateIntersectsSelector(pts)(GeoFac.CreateGeometry(list)).Select(pts).ToList(points)).ToList();
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
        public static List<List<Geometry>> GroupGeometriesEx(List<Geometry> linesGeos, List<Geometry> items)
        {
            if (linesGeos.Count == 0 || items.Count == 0) return new List<List<Geometry>>();
            IEnumerable<KeyValuePair<Geometry, Geometry>> f()
            {
                linesGeos = linesGeos.Distinct().ToList();
                items = items.Distinct().ToList();
                var engine = new NetTopologySuite.Index.Strtree.STRtree<Geometry>();
                foreach (var geo in items) engine.Insert(geo.EnvelopeInternal, geo);
                foreach (var linesGeo in linesGeos)
                {
                    var gf = ThCADCoreNTSService.Instance.PreparedGeometryFactory.Create(linesGeo);
                    foreach (var polygon in engine.Query(linesGeo.EnvelopeInternal).Where(item => gf.Intersects(item)))
                    {
                        yield return new KeyValuePair<Geometry, Geometry>(linesGeo, polygon);
                    }
                }
            }
            var geosGroup = new List<List<Geometry>>();
            var dict = new ListDict<Geometry>();
            var pairs = f().ToArray();
            var h = new BFSHelper2<Geometry>()
            {
                Pairs = pairs,
                Items = linesGeos.Concat(items).ToArray(),
                Callback = (bfs, item) => dict.Add(bfs.root, item),
            };
            h.BFS();
            dict.ForEach((_i, l) =>
            {
                geosGroup.Add(l);
            });
            return geosGroup;
        }
        public static List<List<Geometry>> GroupGeometries(List<Geometry> geos)
        {
            static void GroupGeometries(List<Geometry> geos, List<List<Geometry>> geosGroup)
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

            var geosGroup = new List<List<Geometry>>();
            GroupGeometries(geos, geosGroup);
            return geosGroup;
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
        public static Func<Geometry, List<Geometry>> CreateContainsSelector(List<Geometry> geos)
        {
            if (geos.Count == 0) return r => new List<Geometry>();
            var engine = new NetTopologySuite.Index.Strtree.STRtree<Geometry>();
            foreach (var geo in geos) engine.Insert(geo.EnvelopeInternal, geo);
            return geo =>
            {
                var gf = ThCADCoreNTSService.Instance.PreparedGeometryFactory.Create(geo);
                return engine.Query(geo.EnvelopeInternal).Where(g => gf.Contains(g)).ToList();
            };
        }
        public static Func<Geometry, List<Geometry>> CreateIntersectsSelector<T>(List<T> geos)where T:Geometry
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
                var ptsf = CreateIntersectsSelector(_pts);
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
        public List<Geometry> WrappingPipes;
        public List<Geometry> FloorDrains;
        public List<Geometry> WaterPorts;
        public List<Geometry> WashingMachines;
        public List<Geometry> CleaningPorts;
        public List<Geometry> SideFloorDrains;
        public void Init()
        {
            Storeys ??= new List<Geometry>();
            Labels ??= new List<Geometry>();
            LabelLines ??= new List<Geometry>();
            DLines ??= new List<Geometry>();
            VLines ??= new List<Geometry>();
            VerticalPipes ??= new List<Geometry>();
            WrappingPipes ??= new List<Geometry>();
            FloorDrains ??= new List<Geometry>();
            WaterPorts ??= new List<Geometry>();
            WashingMachines ??= new List<Geometry>();
            CleaningPorts ??= new List<Geometry>();
            SideFloorDrains ??= new List<Geometry>();
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
            o.WrappingPipes.AddRange(data.WrappingPipes.Select(ConvertWrappingPipesF()));
            if (false) o.VerticalPipes.AddRange(data.VerticalPipes.Select(ConvertVerticalPipesPreciseF()));
            else o.VerticalPipes.AddRange(data.VerticalPipes.Select(ConvertVerticalPipesF()));
            o.FloorDrains.AddRange(data.FloorDrains.Select(ConvertFloorDrainsF()));
            o.WaterPorts.AddRange(data.WaterPorts.Select(ConvertWaterPortsF()));
            o.WashingMachines.AddRange(data.WashingMachines.Select(ConvertWashingMachinesF()));
            o.CleaningPorts.AddRange(data.CleaningPorts.Select(ConvertCleaningPortsF()));
            o.SideFloorDrains.AddRange(data.SideFloorDrains.Select(ConvertSideFloorDrains()));
            return o;
        }

        private static Func<GRect, Polygon> ConvertWrappingPipesF()
        {
            return x => x.ToPolygon();
        }

        private static Func<GLineSegment, Geometry> NewMethod(int bfSize)
        {
            return x => x.Buffer(bfSize);
        }
        public static Func<Point2d, Point> ConvertSideFloorDrains()
        {
            return x => x.ToNTSPoint();
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
            ret.AddRange(WrappingPipes);
            ret.AddRange(FloorDrains);
            ret.AddRange(WaterPorts);
            ret.AddRange(WashingMachines);
            ret.AddRange(CleaningPorts);
            ret.AddRange(SideFloorDrains);
            return ret;
        }
        public List<DrainageCadData> SplitByStorey()
        {
            var lst = new List<DrainageCadData>(this.Storeys.Count);
            if (this.Storeys.Count == 0) return lst;
            var f = GeoFac.CreateIntersectsSelector(GetAllEntities());
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
                o.WrappingPipes.AddRange(objs.Where(x => this.WrappingPipes.Contains(x)));
                o.FloorDrains.AddRange(objs.Where(x => this.FloorDrains.Contains(x)));
                o.WaterPorts.AddRange(objs.Where(x => this.WaterPorts.Contains(x)));
                o.WashingMachines.AddRange(objs.Where(x => this.WashingMachines.Contains(x)));
                o.CleaningPorts.AddRange(objs.Where(x => this.CleaningPorts.Contains(x)));
                o.SideFloorDrains.AddRange(objs.Where(x => this.SideFloorDrains.Contains(x)));
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
            var f = GeoFac.CreateIntersectsSelector(GetAllEntities());
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
        public List<GRect> WrappingPipes;
        public List<GRect> FloorDrains;
        public List<GRect> WaterPorts;
        public List<string> WaterPortLabels;
        public List<GRect> WashingMachines;
        public List<Point2d> CleaningPorts;
        public List<Point2d> SideFloorDrains;
        public void Init()
        {
            Storeys ??= new List<GRect>();
            Labels ??= new List<CText>();
            LabelLines ??= new List<GLineSegment>();
            DLines ??= new List<GLineSegment>();
            VLines ??= new List<GLineSegment>();
            VerticalPipes ??= new List<GRect>();
            WrappingPipes ??= new List<GRect>();
            FloorDrains ??= new List<GRect>();
            WaterPorts ??= new List<GRect>();
            WaterPortLabels ??= new List<string>();
            WashingMachines ??= new List<GRect>();
            CleaningPorts ??= new List<Point2d>();
            SideFloorDrains ??= new List<Point2d>();
        }
        public void FixData()
        {
            Init();
            Storeys = Storeys.Where(x => x.IsValid).Distinct().ToList();
            Labels = Labels.Where(x => x.Boundary.IsValid).Distinct().ToList();
            LabelLines = LabelLines.Where(x => x.Length > 0).Distinct().ToList();
            DLines = DLines.Where(x => x.Length > 0).Distinct().ToList();
            VLines = VLines.Where(x => x.Length > 0).Distinct().ToList();
            VerticalPipes = VerticalPipes.Where(x => x.IsValid).Distinct().ToList();
            WrappingPipes = WrappingPipes.Where(x => x.IsValid).Distinct().ToList();
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

}