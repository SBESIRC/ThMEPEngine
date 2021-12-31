using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using DotNetARX;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPWSS.CADExtensionsNs;
using ThMEPWSS.Uitl;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Internal;
using ThMEPWSS.Uitl.ExtensionsNs;
using System.Windows.Forms;
using NetTopologySuite.Geometries;
using AcHelper;
using ThMEPWSS.Pipe.Service;
using ThMEPWSS.JsonExtensionsNs;
using Autodesk.AutoCAD.EditorInput;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using ThMEPEngineCore.Engine;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using NFox.Cad;
using NetTopologySuite.Operation.OverlayNG;
using NetTopologySuite.Operation.Overlay;
using NetTopologySuite.Algorithm;
using ThCADCoreNTSService = ThCADCore.NTS.ThCADCoreNTSService;
using NetTopologySuite.Geometries.Prepared;
using NetTopologySuite;

namespace ThMEPWSS.Assistant
{
    public static class ObjFac
    {
        public static T CloneByJson<T>(T o) => JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(o));
        public static object CloneObjByJson(object o)
        {
            return JsonConvert.DeserializeObject(JsonConvert.SerializeObject(o), o.GetType());
        }
        public static void CopyProperties(object src, object dst)
        {
            src.GetType().GetProperties().Join(dst.GetType().GetProperties(), x => new KeyValuePair<string, Type>(x.Name, x.PropertyType), x => new KeyValuePair<string, Type>(x.Name, x.PropertyType), (x, y) =>
            {
                y.SetValue(dst, x.GetValue(src));
                return 666;
            }).Count();
        }
    }
    public static class GeoEntensions
    {
        public static Coordinate Offset(this Coordinate c, Vector2d v) => c.Offset(v.X, v.Y);
        public static Point Offset(this Point pt, Vector2d v) => pt.Offset(v.X, v.Y);
        public static LineString Offset(this LineString ls, Vector2d v) => ls.Offset(v.X, v.Y);
        public static LinearRing Offset(this LinearRing lr, Vector2d v) => lr.Offset(v.X, v.Y);
        public static Polygon Offset(this Polygon pl, Vector2d v) => pl.Offset(v.X, v.Y);
        public static GeometryCollection Offset(this GeometryCollection colle, Vector2d v) => colle.Offset(v.X, v.Y);
        public static Geometry Offset(this Geometry geo, Vector2d v) => geo.Offset(v.X, v.Y);
        public static Coordinate Offset(this Coordinate c, double dx, double dy) => new(c.X + dx, c.Y + dy);
        public static Point Offset(this Point pt, double dx, double dy) => new(pt.X + dx, pt.Y + dy);
        public static LineString Offset(this LineString ls, double dx, double dy) => new(ls.Coordinates.Select(x => x.Offset(dx, dy)).ToArray());
        public static LinearRing Offset(this LinearRing lr, double dx, double dy) => new(lr.Coordinates.Select(x => x.Offset(dx, dy)).ToArray());
        public static Polygon Offset(this Polygon pl, double dx, double dy) => new(pl.Shell.Offset(dx, dy));
        public static GeometryCollection Offset(this GeometryCollection colle, double dx, double dy) => new(colle.Geometries.Select(geo => geo.Offset(dx, dy)).ToArray());
        public static Geometry Offset(this Geometry geo, double dx, double dy)
        {
            if (geo is null) throw new ArgumentNullException();
            if (geo is Point point) return point.Offset(dx, dy);
            if (geo is LinearRing lr) return lr.Offset(dx, dy);
            if (geo is LineString ls) return ls.Offset(dx, dy);
            if (geo is Polygon pl) return pl.Offset(dx, dy);
            if (geo is GeometryCollection colle) return colle.Offset(dx, dy);
            throw new NotSupportedException();
        }

        public static Coordinate TransformBy(this Coordinate c, Matrix3d m) => c.ToPoint3d().TransformBy(m).ToNTSCoordinate();
        public static Point TransformBy(this Point pt, Matrix3d m) => pt.ToPoint3d().TransformBy(m).ToNTSPoint();
        public static LineString TransformBy(this LineString ls, Matrix3d m) => new(ls.Coordinates.Select(c => c.TransformBy(m)).ToArray());
        public static LinearRing TransformBy(this LinearRing lr, Matrix3d m) => new(lr.Coordinates.Select(c => c.TransformBy(m)).ToArray());
        public static Polygon TransformBy(this Polygon pl, Matrix3d m) => new(pl.Shell.TransformBy(m));
        public static GeometryCollection TransformBy(this GeometryCollection colle, Matrix3d m) => new(colle.Geometries.Select(x => x.TransformBy(m)).ToArray());
        public static Geometry TransformBy(this Geometry geo, Matrix3d m)
        {
            if (geo is null) throw new ArgumentNullException();
            if (geo is Point point) return point.TransformBy(m);
            if (geo is LinearRing lr) return lr.TransformBy(m);
            if (geo is LineString ls) return ls.TransformBy(m);
            if (geo is Polygon pl) return pl.TransformBy(m);
            if (geo is GeometryCollection colle) return colle.TransformBy(m);
            throw new NotSupportedException();
        }
    }
    public static class GeoNTSConvertion
    {
        public static Coordinate[] ConvertToCoordinateArray(GRect r)
        {
            var pt = r.LeftTop.ToNTSCoordinate();
            return new Coordinate[] { pt, r.RightTop.ToNTSCoordinate(), r.RightButtom.ToNTSCoordinate(), r.LeftButtom.ToNTSCoordinate(), pt, };
        }
    }
    public class Extents2dCalculator
    {
        public double MinX = double.MaxValue;
        public double MinY = double.MaxValue;
        public double MaxX = double.MinValue;
        public double MaxY = double.MinValue;
        public bool IsValid => MinX != double.MaxValue && MinY != double.MaxValue && MaxX != double.MinValue && MaxY != double.MinValue;
        public static Extents2d Calc(IEnumerable<Point2d> points)
        {
            var o = new Extents2dCalculator();
            foreach (var pt in points)
            {
                o.Update(pt);
            }
            return o.ToExtents2d();
        }
        public static GLineSegment GetCenterLine(IEnumerable<GLineSegment> segs)
        {
            var o = new Extents2dCalculator();
            var c = 0;
            var s = .0;
            foreach (var seg in segs)
            {
                var angle = seg.ToVector2d().Angle.AngleToDegree();
                if (angle >= 180.0) angle -= 180.0;
                s += angle;
                ++c;
                o.Update(seg);
            }
            if (c == 0) throw new ArgumentException();
            var avg = s / c;
            var r = o.ToGRect();
            var center = r.Center;
            if (avg >= 90.0) avg -= 90.0;

            {
                var angle = avg.AngleFromDegree();
                Vector2d vec;
                if (0 <= avg && avg <= 45.0)
                {
                    vec = new Vector2d(r.Width / 2, Math.Tan(angle) * r.Width / 2);
                }
                else if (45.0 < avg && avg <= 90.0)
                {
                    vec = new Vector2d(r.Height / 2 / Math.Tan(angle), r.Height / 2);
                }
                else
                {
                    throw new Exception(avg.ToString());
                }

                return new GLineSegment(center + vec, center - vec);
            }
        }
        public static Extents2d Calc(IEnumerable<GLineSegment> segs)
        {
            var o = new Extents2dCalculator();
            o.Update(segs);
            return o.ToExtents2d();
        }
        public void Update(IEnumerable<GLineSegment> segs)
        {
            foreach (var seg in segs)
            {
                Update(seg);
            }
        }
        public void Update(GRect r)
        {
            Update(r.LeftTop);
            Update(r.RightButtom);
        }
        public void Update(GLineSegment seg)
        {
            Update(seg.StartPoint);
            Update(seg.EndPoint);
        }
        public void Update(Point2d pt)
        {
            if (MinX > pt.X) MinX = pt.X;
            if (MinY > pt.Y) MinY = pt.Y;
            if (MaxX < pt.X) MaxX = pt.X;
            if (MaxY < pt.Y) MaxY = pt.Y;
        }
        public void Update(Point3d pt)
        {
            if (MinX > pt.X) MinX = pt.X;
            if (MinY > pt.Y) MinY = pt.Y;
            if (MaxX < pt.X) MaxX = pt.X;
            if (MaxY < pt.Y) MaxY = pt.Y;
        }
        public void Update(Extents2d ext)
        {
            var pt = ext.MinPoint;
            if (MinX > pt.X) MinX = pt.X;
            if (MinY > pt.Y) MinY = pt.Y;
            pt = ext.MaxPoint;
            if (MaxX < pt.X) MaxX = pt.X;
            if (MaxY < pt.Y) MaxY = pt.Y;
        }
        public Extents2d ToExtents2d()
        {
            return new Extents2d(MinX, MinY, MaxX, MaxY);
        }
        public Extents3d ToExtents3d()
        {
            return new Extents3d(new Point3d(MinX, MinY, 0), new Point3d(MaxX, MaxY, 0));
        }
        public GRect ToGRect()
        {
            return new GRect(MinX, MinY, MaxX, MaxY);
        }
    }
    public static class GeoFac
    {
        public class AngleDegreeParallelComparer : IEqualityComparer<double>
        {
            double tol;

            public AngleDegreeParallelComparer(double tol)
            {
                this.tol = tol;
            }

            public static void FixAngleDegree(ref double v)
            {
                while (v < 0) v += 360;
                while (v >= 360) v -= 360;
            }
            public bool Equals(double x, double y)
            {
                var v = x - y;
                FixAngleDegree(ref v);
                return v.EqualsTo(0, tol) || v.EqualsTo(360, tol);
            }

            public int GetHashCode(double obj)
            {
                return 0;
            }
        }
        public static List<T> ToGeoList<T>(JArray ja) where T : Geometry
        {
            return ja.Select(o => (T)ToGeometry(o)).ToList();
        }
        public static JArray ToJArray(IEnumerable<Geometry> geos) => new JArray(geos.Select(x => GeoFac.ToJToken(x)).ToArray());
        public static JToken ToJToken(Geometry geo)
        {
            if (geo is null) return new JValue((object)null);
            if (geo is Point pt) return ToJToken(pt);
            if (geo is LinearRing lr) return ToJToken(lr);
            if (geo is LineString ls) return ToJToken(ls);
            if (geo is Polygon pl) return ToJToken(pl);
            if (geo is MultiPoint mpt) return ToJToken(mpt);
            if (geo is MultiLineString mls) return ToJToken(mls);
            if (geo is MultiPolygon mpl) return ToJToken(mpl);
            if (geo is GeometryCollection gc) return ToJToken(gc);
            throw new NotSupportedException();
        }
        public static Point2d ToPoint2d(JToken jo)
        {
            var ja = (JArray)jo["values"];
            return new Point2d(ja[0].ToObject<double>(), ja[1].ToObject<double>());
        }
        public static Coordinate ToCoordinate(JToken jo)
        {
            var ja = (JArray)jo["values"];
            return new Coordinate(ja[0].ToObject<double>(), ja[1].ToObject<double>());
        }
        public static LineString ToLineString(JToken jo)
        {
            var ja = (JArray)jo["points"];
            return new LineString(ja.Select(jtk => ToCoordinate((JObject)jtk)).ToArray());
        }
        public static LinearRing ToLinearRing(JToken jo)
        {
            var ja = (JArray)jo["points"];
            return new LinearRing(ja.Select(jtk => ToCoordinate((JObject)jtk)).ToArray());
        }
        public static Polygon ToPolygon(JToken jo)
        {
            return new Polygon(ToLinearRing(jo["shell"]));
        }
        public static Point ToPoint(JToken jo)
        {
            var ja = (JArray)jo["values"];
            return new Point(ja[0].ToObject<double>(), ja[1].ToObject<double>());
        }
        public static MultiPoint ToMultiPoint(JToken jo)
        {
            var ja = (JArray)jo["values"];
            return new MultiPoint(ja.Select(o => ToPoint(o)).ToArray());
        }
        public static MultiLineString ToMultiLineString(JToken jo)
        {
            var ja = (JArray)jo["values"];
            return new MultiLineString(ja.Select(o => ToLineString(o)).ToArray());
        }
        public static MultiPolygon ToMultiPolygon(JToken jo)
        {
            var ja = (JArray)jo["values"];
            return new MultiPolygon(ja.Select(o => ToPolygon(o)).ToArray());
        }
        public static GeometryCollection ToGeometryCollection(JToken jo)
        {
            var ja = (JArray)jo["values"];
            return new GeometryCollection(ja.Select(o => ToGeometry(o)).ToArray());
        }
        public static Geometry ToGeometry(JToken jo)
        {
            switch ((string)jo["type"])
            {
                case "Point2d":
                case "Point3d":
                case "Point":
                case "Pt":
                    return ToPoint(jo);
                case "LineString":
                    return ToLineString(jo);
                case "LinearRing":
                    return ToLinearRing(jo);
                case "Polygon":
                    return ToPolygon(jo);
                case "MultiPoint":
                    return ToMultiPoint(jo);
                case "MultiLineString":
                    return ToMultiLineString(jo);
                case "MultiPolygon":
                    return ToMultiPolygon(jo);
                case "GeometryCollection":
                    return ToGeometryCollection(jo);
                default:
                    throw new NotSupportedException();
            }
        }
        public static JToken ToJToken(Point3d o)
        {
            return new JObject() { ["type"] = "Point3d", ["values"] = new JArray() { o.X, o.Y, o.Z } };
        }
        public static JToken ToJToken(Point2d o)
        {
            return new JObject() { ["type"] = "Point2d", ["values"] = new JArray() { o.X, o.Y, } };
        }
        public static JToken ToJToken(Coordinate o)
        {
            return new JObject() { ["type"] = "Pt", ["values"] = new JArray() { o.X, o.Y, } };
        }
        public static JToken ToJToken(Point o)
        {
            return new JObject() { ["type"] = "Point", ["values"] = new JArray() { o.X, o.Y, } };
        }
        public static JToken ToJToken(LinearRing o)
        {
            return new JObject() { ["type"] = "LinearRing", ["points"] = new JArray(o.Coordinates.Select(x => ToJToken(x)).ToArray()) };
        }
        public static JToken ToJToken(LineString o)
        {
            if (o is LinearRing r) return ToJToken(r);
            return new JObject() { ["type"] = "LineString", ["points"] = new JArray(o.Coordinates.Select(x => ToJToken(x)).ToArray()) };
        }
        public static JToken ToJToken(Polygon o)
        {
            return new JObject() { ["type"] = "Polygon", ["shell"] = ToJToken(o.Shell) };
        }
        public static JToken ToJToken(MultiPoint l)
        {
            return new JObject() { ["type"] = "MultiPoint", ["values"] = new JArray(l.Geometries.Select(x => ToJToken(x)).ToArray()) };
        }
        public static JToken ToJToken(MultiLineString l)
        {
            return new JObject() { ["type"] = "MultiLineString", ["values"] = new JArray(l.Geometries.Select(x => ToJToken(x)).ToArray()) };
        }
        public static JToken ToJToken(MultiPolygon l)
        {
            return new JObject() { ["type"] = "MultiPolygon", ["values"] = new JArray(l.Geometries.Select(x => ToJToken(x)).ToArray()) };
        }
        public static JToken ToJToken(GeometryCollection l)
        {
            return new JObject() { ["type"] = "GeometryCollection", ["values"] = new JArray(l.Geometries.Select(x => ToJToken(x)).ToArray()) };
        }
        public static List<List<GLineSegment>> GroupParallelLines(List<GLineSegment> lines, double extend_distance, double collinear_gap_distance, double angle_tollerence = 1)
        {
            lines = lines.Where(x => x.IsValid).Distinct().ToList();
            var lineGeos = lines.Select(x => x.ToLineString()).ToList();
            var spatialIndex = GeoFac.CreateIntersectsSelector(lineGeos);
            foreach (var line in lines)
            {
                var buffer = line.Extend(extend_distance).Buffer(collinear_gap_distance);
                var objs = spatialIndex(buffer).Select(lineGeos).ToList(lines);
                if (objs.Count > 1)
                {
                    var parallelLines = objs.Where(l => l.IsParallelTo(line, angle_tollerence)).ToList();
                    if (parallelLines.Count > 1)
                    {
                        var angle = line.AngleDegree;
                        var parallelLineGeos = parallelLines.Select(lines).ToList(lineGeos);
                        var tag = parallelLineGeos.Select(l => l.UserData).FirstOrDefault(x => x != null) ?? new object();
                        foreach (var l in parallelLineGeos)
                        {
                            if (l.UserData == null)
                            {
                                l.UserData = tag;
                            }
                            else if (l.UserData != tag)
                            {
                                var _tag = l.UserData;
                                foreach (var _l in lineGeos.Where(x => x.UserData == _tag))
                                {
                                    _l.UserData = tag;
                                }
                            }
                        }
                    }
                }
            }
            var results = new List<List<GLineSegment>>();
            foreach (var group in lineGeos.GroupBy(o => o.UserData))
            {
                if (group.Key == null)
                {
                    foreach (var o in group)
                    {
                        results.Add(new List<GLineSegment>() { lines[lineGeos.IndexOf(o)] });
                    }
                }
                else
                {
                    results.Add(group.Select(lineGeos).ToList(lines));
                }
            }
            return results;
        }
        public static readonly GeometryFactory DefaultGeometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory();

        public static readonly NetTopologySuite.Index.Strtree.GeometryItemDistance DefaultGeometryItemDistance = new NetTopologySuite.Index.Strtree.GeometryItemDistance();
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
        public static List<Point2d> GetLabelLineEndPoints(List<GLineSegment> lines, Geometry killer, double radius = 5)
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
        public static Func<Geometry, T> NearestNeighbourGeometryF<T>(List<T> geos) where T : Geometry
        {
            if (geos.Count == 0) return geometry => null;
            else if (geos.Count == 1) return geometry => geos[0];
            var engine = new NetTopologySuite.Index.Strtree.STRtree<Geometry>();
            foreach (var geo in geos) engine.Insert(geo.EnvelopeInternal, geo);
            return geometry => (T)engine.NearestNeighbour(geometry.EnvelopeInternal, geometry, DefaultGeometryItemDistance);
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
                var neighbours = engine.NearestNeighbour(geometry.EnvelopeInternal, geometry, DefaultGeometryItemDistance, num)
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
                var gf = PreparedGeometryFactory.Create(geo);
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
                    var geo = DefaultGeometryFactory.BuildGeometry(geos);
                    var gf = PreparedGeometryFactory.Create(geo);
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
                    var geo = DefaultGeometryFactory.BuildGeometry(new Geometry[] { point1, point2 });
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
        public static readonly NetTopologySuite.Utilities.GeometricShapeFactory GeometricShapeFactory = new NetTopologySuite.Utilities.GeometricShapeFactory(DefaultGeometryFactory);
        public static List<GLineSegment> ToNodedLineSegments(IEnumerable<GLineSegment> lineSegments)
        {
            var arr = lineSegments.Where(x => x.IsValid).Distinct().Select(x => x.ToLineString()).ToArray();
            if (arr.Length == 0) return new List<GLineSegment>();
            var geo = OverlayNGRobust.Overlay(new MultiLineString(arr), null, SpatialFunction.Union);
            static IEnumerable<GLineSegment> f(LineString ls)
            {
                var arr = ls.Coordinates.Select(x => x.ToPoint2d()).ToArray();
                for (int i = 1; i < arr.Length; i++)
                {
                    var seg = new GLineSegment(arr[i - 1], arr[i]);
                    if (seg.IsValid)
                    {
                        yield return seg;
                    }
                }
            }
            if (geo is LineString ls)
            {
                return f(ls).Distinct().ToList();
            }
            else if (geo is MultiLineString mls)
            {
                return mls.Geometries.OfType<LineString>().SelectMany(f).Distinct().ToList();
            }
            else
            {
                throw new NotSupportedException();
            }
        }
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
                var geo = DefaultGeometryFactory.BuildGeometry(new Geometry[] { ring1, ring2 });
                ret.Add(geo);
            }
            return ret;
        }

        public static IEnumerable<KeyValuePair<int, int>> GroupGLineSegmentsToKVIndex(IList<GLineSegment> lineSegments, double radius, int numPoints = 6)
        {
            var geos = CreateGeometries(lineSegments, radius, numPoints);
            return _GroupGeometriesToKVIndex(geos.Distinct().ToList());
        }
        public static GLineSegment GetCenterLine(List<GLineSegment> segs)
        {
            // GetMinimumRectangle()对于非常远的坐标（WCS下，>10E10)处理得不好
            // Workaround就是将位于非常远的图元临时移动到WCS原点附近，参与运算
            // 运算结束后将运算结果再按相同的偏移从WCS原点附近移动到其原始位置
            if (segs.Count == 0) throw new ArgumentException();
            if (segs.Count == 1) return segs[0];
            var geo = MinimumDiameter.GetMinimumRectangle(new MultiLineString(segs.Select(x => x.ToLineString()).ToArray()));
            var ls = (geo as Polygon)?.Shell ?? (geo as LineString);
            var pts = ls.Coordinates.Select(x => x.ToPoint2d()).ToArray();
            if (pts.Length == 2)
            {
                return new GLineSegment(pts[0], pts[1]);
            }
            return new GLineSegment(pts[0] + .5 * (pts[1] - pts[0]), pts[2] + .5 * (pts[3] - pts[2]));
        }
        public static GLineSegment GetCenterLine(List<GLineSegment> segs, double work_around)
        {
            if (work_around > 0)
            {
                var ext = Extents2dCalculator.Calc(segs);
                var center = ext.GetCenter();
                if (center.GetDistanceTo(Point2d.Origin) > work_around)
                {
                    var v = -center.ToVector2d();
                    //if (ThMEPWSS.DebugNs3.ThDebugTool._)
                    //{
                    //    var m1 = Matrix2d.Displacement(v);
                    //    var m2 = Matrix2d.Displacement(-v);
                    //    return GetCenterLine(segs.Select(seg => seg.TransformBy(ref m1)).ToList()).TransformBy(ref m2);
                    //}
                    //else
                    //{
                    return GetCenterLine(segs.Select(seg => seg.Offset(v)).ToList()).Offset(-v);
                    //}
                }
            }
            return GetCenterLine(segs);
        }
        public static IEnumerable<LineString> GetNodedLineStrings(IEnumerable<Geometry> geos, bool distinct = true)
        {
            return ToNodedLineSegments(GetManyLines(geos, distinct)).Select(x => x.ToLineString());
        }
        public static IEnumerable<LineString> GetManyLineStrings(IEnumerable<Geometry> geos, bool distinct = true)
        {
            return GetManyLines(geos, distinct).Select(x => x.ToLineString());
        }
        public static IEnumerable<GLineSegment> GetManyLines(IEnumerable<Geometry> geos, bool distinct = true)
        {
            var q = geos.SelectMany(geo => GetLines(geo, false));
            if (distinct)
            {
                return q.Distinct();
            }
            else
            {
                return q;
            }
        }
        public static IEnumerable<GLineSegment> GetLines(Geometry geo, bool distinct = true)
        {
            IEnumerable<GLineSegment> f()
            {
                if (geo is LineString ls)
                {
                    var arr = ls.Coordinates;
                    for (int i = 0; i < arr.Length - 1; i++)
                    {
                        var seg = new GLineSegment(arr[i].ToPoint2d(), arr[i + 1].ToPoint2d());
                        if (seg.IsValid)
                        {
                            yield return seg;
                        }
                    }
                }
                else if (geo is Polygon pl)
                {
                    foreach (var r in GetLines(pl.Shell))
                    {
                        yield return r;
                    }
                }
                else if (geo is GeometryCollection mls)
                {
                    foreach (var _g in mls.Geometries)
                    {
                        foreach (var r in GetLines(_g))
                        {
                            yield return r;
                        }
                    }
                }
            }
            if (distinct)
            {
                return f().Distinct();
            }
            else
            {
                return f();
            }
        }
        public static IEnumerable<Point2d> GetPoints(Geometry geo)
        {
            if (geo is Point pt)
            {
                yield return pt.ToPoint2d();
            }
            else if (geo is GeometryCollection mls)
            {
                foreach (var _g in mls.Geometries)
                {
                    foreach (var r in GetPoints(_g))
                    {
                        yield return r;
                    }
                }
            }
        }
        public static IEnumerable<Geometry> GroupLinesByConnPoints<T>(List<T> geos, double radius) where T : Geometry
        {
            var lines = geos.SelectMany(o => GetLines(o)).Distinct().ToList();
            var _lines = lines.Select(x => x.ToLineString()).ToList();
            var _linesf = CreateIntersectsSelector(_lines);
            var _geos = lines.Select(line => CreateGeometryEx(new Geometry[] { CreateCirclePolygon(line.StartPoint, radius, 6), CreateCirclePolygon(line.EndPoint, radius, 6) })).ToList();
            for (int i1 = 0; i1 < _geos.Count; i1++)
            {
                var _geo = _geos[i1];
                foreach (var i in _linesf(_geo).Select(_lines))
                {
                    if (i == i1) continue;
                    var line = lines[i];
                    var _line = _lines[i];
                    var r1 = CreateCirclePolygon(line.StartPoint, radius, 6);
                    if (r1.Intersects(_line))
                    {
                        _geos[i1] = _geos[i1].Union(r1);
                    }
                    var r2 = CreateCirclePolygon(line.EndPoint, radius, 6);
                    if (r2.Intersects(_line))
                    {
                        _geos[i1] = _geos[i1].Union(r2);
                    }
                }
            }
            foreach (var list in GroupGeometries(_geos))
            {
                yield return CreateGeometry(list.Select(_geos).ToList(lines).Select(x => x.ToLineString()));
            }
        }
        public static Geometry CreateGeometry(IEnumerable<Geometry> geomList)
        {
            return DefaultGeometryFactory.BuildGeometry(geomList);
        }
        public static Geometry CreateGeometryEx<T>(ICollection<T> geomList) where T : Geometry => CreateGeometryEx(geomList.Cast<Geometry>().ToList());
        public static Geometry CreateGeometryEx<T>(T[] geomList) where T : Geometry => CreateGeometryEx(geomList.Cast<Geometry>().ToList());
        public static Geometry CreateGeometryEx(List<Geometry> geomList) => CreateGeometry(GeoFac.GroupGeometries(geomList).Select(x => (x.Count > 1 ? (x.Aggregate((x, y) => x.Union(y))) : x[0])).Distinct().ToList());
        public static Geometry CreateGeometry(object tag, params Geometry[] geos)
        {
            var geo = CreateGeometry(geos);
            geo.UserData = tag;
            return geo;
        }
        public static Geometry CreateGeometry(params Geometry[] geos)
        {
            return DefaultGeometryFactory.BuildGeometry(geos);
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
                    var gf = PreparedGeometryFactory.Create(linesGeo);
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
        public static List<List<T>> GroupGeometries<T>(List<T> geos) where T : Geometry
        {
            static void GroupGeometries(List<T> geos, List<List<T>> geosGroup)
            {
                if (geos.Count == 0) return;
                geos = geos.Distinct().ToList();
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

            var geosGroup = new List<List<T>>();
            GroupGeometries(geos, geosGroup);
            return geosGroup;
        }

        public static Geometry ToNTSGeometry(GLineSegment seg, double radius)
        {
            var points1 = GeoNTSConvertion.ConvertToCoordinateArray(GRect.Create(seg.StartPoint, radius));
            var points2 = GeoNTSConvertion.ConvertToCoordinateArray(GRect.Create(seg.EndPoint, radius));
            var ring1 = new LinearRing(points1);
            var ring2 = new LinearRing(points2);
            var geo = DefaultGeometryFactory.BuildGeometry(new Geometry[] { ring1, ring2 });
            return geo;
        }
        public static Polygon CreateCirclePolygon(Point3d center, double radius, int numPoints, bool larger = true)
        {
            radius = larger ? radius / Math.Cos(Math.PI / numPoints) : radius;
            var shapeFactory = new NetTopologySuite.Utilities.GeometricShapeFactory(DefaultGeometryFactory)
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
            var shapeFactory = new NetTopologySuite.Utilities.GeometricShapeFactory(DefaultGeometryFactory)
            {
                NumPoints = numPoints,
                Size = 2 * radius,
                Centre = center.ToNTSCoordinate(),
            };
            return shapeFactory.CreateCircle();
        }
        public static readonly PreparedGeometryFactory PreparedGeometryFactory = new PreparedGeometryFactory();
        public static Func<Geometry, bool> CreateIntersectsTester<T>(ICollection<T> geos) where T : Geometry
        {
            if (geos.Count == 0) return r => false;
            var engine = new NetTopologySuite.Index.Strtree.STRtree<T>(geos.Count > 10 ? geos.Count : 10);
            foreach (var geo in geos) engine.Insert(geo.EnvelopeInternal, geo);
            return geo =>
            {
                if (geo == null) throw new ArgumentNullException();
                var gf = PreparedGeometryFactory.Create(geo);
                return engine.Query(geo.EnvelopeInternal).Any(g => gf.Intersects(g));
            };
        }
        public static (Func<Geometry, bool>, Action<T>) CreateIntersectsTesterEngine<T>(IEnumerable<T> geos = null) where T : Geometry
        {
            var hasBuild = false;
            var engine = new NetTopologySuite.Index.Strtree.STRtree<T>();
            var hs = new HashSet<T>(16);
            if (geos != null)
            {
                foreach (var geo in geos)
                {
                    engine.Insert(geo.EnvelopeInternal, geo);
                    hs.Add(geo);
                }
            }
            return (geo =>
            {
                if (geo == null) throw new ArgumentNullException();
                var gf = PreparedGeometryFactory.Create(geo);
                hasBuild = true;
                return engine.Query(geo.EnvelopeInternal).Where(g => gf.Intersects(g)).Any();
            }, geo =>
            {
                if (geo == null) throw new ArgumentNullException();
                if (hs.Contains(geo)) return;
                if (hasBuild)
                {
                    engine = new NetTopologySuite.Index.Strtree.STRtree<T>(hs.Count + 1 > 10 ? hs.Count + 1 : 10);
                    foreach (var _geo in hs)
                    {
                        engine.Insert(_geo.EnvelopeInternal, _geo);
                    }
                    hasBuild = false;
                }
                engine.Insert(geo.EnvelopeInternal, geo);
                hs.Add(geo);
            }
            );
        }
        public static (Func<Geometry, List<T>>, Action<T>) CreateIntersectsSelectorEngine<T>(IEnumerable<T> geos = null) where T : Geometry
        {
            var hasBuild = false;
            var engine = new NetTopologySuite.Index.Strtree.STRtree<T>();
            var hs = new HashSet<T>(16);
            if (geos != null)
            {
                foreach (var geo in geos)
                {
                    engine.Insert(geo.EnvelopeInternal, geo);
                    hs.Add(geo);
                }
            }
            return (geo =>
            {
                if (geo == null) throw new ArgumentNullException();
                var gf = PreparedGeometryFactory.Create(geo);
                hasBuild = true;
                return engine.Query(geo.EnvelopeInternal).Where(g => gf.Intersects(g)).ToList();
            }, geo =>
            {
                if (geo == null) throw new ArgumentNullException();
                if (hs.Contains(geo)) return;
                if (hasBuild)
                {
                    engine = new NetTopologySuite.Index.Strtree.STRtree<T>(hs.Count + 1 > 10 ? hs.Count + 1 : 10);
                    foreach (var _geo in hs)
                    {
                        engine.Insert(_geo.EnvelopeInternal, _geo);
                    }
                    hasBuild = false;
                }
                engine.Insert(geo.EnvelopeInternal, geo);
                hs.Add(geo);
            }
            );
        }
        public static Func<Geometry, List<T>> CreateContainsSelector<T>(ICollection<T> geos) where T : Geometry
        {
            if (geos.Count == 0) return r => new List<T>();
            var engine = new NetTopologySuite.Index.Strtree.STRtree<T>(geos.Count > 10 ? geos.Count : 10);
            foreach (var geo in geos) engine.Insert(geo.EnvelopeInternal, geo);
            return geo =>
            {
                var gf = PreparedGeometryFactory.Create(geo);
                return engine.Query(geo.EnvelopeInternal).Where(g => gf.Contains(g)).ToList();
            };
        }
        public static Func<Geometry, List<T>> CreateIntersectsSelector<T>(ICollection<T> geos) where T : Geometry
        {
            if (geos.Count == 0) return r => new List<T>();
            var engine = new NetTopologySuite.Index.Strtree.STRtree<T>(geos.Count > 10 ? geos.Count : 10);
            foreach (var geo in geos) engine.Insert(geo.EnvelopeInternal, geo);
            return geo =>
            {
                if (geo == null) throw new ArgumentNullException();
                var gf = PreparedGeometryFactory.Create(geo);
                return engine.Query(geo.EnvelopeInternal).Where(g => gf.Intersects(g)).ToList();
            };
        }
        public static Func<Geometry, List<T>> CreateCoveredBySelectorr<T>(ICollection<T> geos) where T : Geometry
        {
            if (geos.Count == 0) return r => new List<T>();
            var engine = new NetTopologySuite.Index.Strtree.STRtree<T>(geos.Count > 10 ? geos.Count : 10);
            foreach (var geo in geos) engine.Insert(geo.EnvelopeInternal, geo);
            return geo =>
            {
                var gf = PreparedGeometryFactory.Create(geo);
                return engine.Query(geo.EnvelopeInternal).Where(g => gf.CoveredBy(g)).ToList();
            };
        }
        public static Func<Geometry, List<T>> CreateDisjointSelectorr<T>(ICollection<T> geos) where T : Geometry
        {
            if (geos.Count == 0) return r => new List<T>();
            var engine = new NetTopologySuite.Index.Strtree.STRtree<T>(geos.Count > 10 ? geos.Count : 10);
            foreach (var geo in geos) engine.Insert(geo.EnvelopeInternal, geo);
            return geo =>
            {
                var gf = PreparedGeometryFactory.Create(geo);
                return engine.Query(geo.EnvelopeInternal).Where(g => gf.Disjoint(g)).ToList();
            };
        }
        public static Func<Geometry, List<T>> CreateEnvelopeSelector<T>(ICollection<T> geos) where T : Geometry
        {
            if (geos.Count == 0) return r => new List<T>();
            var engine = new NetTopologySuite.Index.Strtree.STRtree<T>(geos.Count > 10 ? geos.Count : 10);
            foreach (var geo in geos) engine.Insert(geo.EnvelopeInternal, geo);
            return geo =>
            {
                if (geo == null) throw new ArgumentNullException();
                return engine.Query(geo.EnvelopeInternal).ToList();
            };
        }
        public static Func<T, List<T>> CreateSTRTreeSelector<T>(ICollection<T> list, Func<T, Envelope> getEnvelope, Func<T, bool> test)
        {
            if (list.Count == 0) return r => new List<T>();
            var engine = new NetTopologySuite.Index.Strtree.STRtree<T>(list.Count > 10 ? list.Count : 10);
            foreach (var item in list) engine.Insert(getEnvelope(item), item);
            return item =>
            {
                if (item == null) throw new ArgumentNullException();
                return engine.Query(getEnvelope(item)).Where(g => test(g)).ToList();
            };
        }
        public static IEnumerable<KeyValuePair<int, int>> _GroupGeometriesToKVIndex<T>(List<T> geos) where T : Geometry
        {
            if (geos.Count == 0) yield break;
            var engine = new NetTopologySuite.Index.Strtree.STRtree<T>(geos.Count > 10 ? geos.Count : 10);
            foreach (var geo in geos) engine.Insert(geo.EnvelopeInternal, geo);
            for (int i = 0; i < geos.Count; i++)
            {
                var geo = geos[i];
                var gf = PreparedGeometryFactory.Create(geo);
                foreach (var j in engine.Query(geo.EnvelopeInternal).Where(g => gf.Intersects(g)).Select(g => geos.IndexOf(g)).Where(j => i < j))
                {
                    yield return new KeyValuePair<int, int>(i, j);
                }
            }
        }
        public static IEnumerable<GLineSegment> AutoConn(List<GLineSegment> lines, int maxDis, int angleTolleranceDegree = 1)
        {
            var pts = GetAlivePoints(lines, radius: 10);
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
                                    var seg = new GLineSegment(gvs[i].StartPoint, gvs[j].StartPoint);
                                    if (seg.IsValid)
                                    {
                                        yield return seg;
                                        flags[i] = true;
                                        flags[j] = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        public static IEnumerable<GLineSegment> AutoConn(List<GLineSegment> lines, Geometry killer, int maxDis, int angleTolleranceDegree = 1)
        {
            var pts = GetAlivePoints(lines, radius: 10);
            var _pts = pts.Select(pt => pt.ToNTSPoint()).Cast<Geometry>().ToList();
            var ptsf = CreateIntersectsSelector(_pts);
            pts = ptsf(killer).Select(_pts).ToList(pts, reverse: true);
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
                                    var seg = new GLineSegment(gvs[i].StartPoint, gvs[j].StartPoint);
                                    if (seg.IsValid)
                                    {
                                        if (!seg.ToLineString().Intersects(killer))
                                        {
                                            yield return seg;
                                            flags[i] = true;
                                            flags[j] = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        public static double FixAngle(double angle)
        {
            if (angle < -Math.PI * 2) angle += Math.PI * 2 * Math.Ceiling(angle);
            if (angle > Math.PI * 2) angle -= Math.PI * 2 * Math.Floor(angle);
            while (angle < 0) angle += Math.PI * 2;
            while (angle >= Math.PI * 2) angle -= Math.PI * 2;
            return angle;
        }
        public static IEnumerable<GLineSegment> YieldGLineSegments(IEnumerable<Point2d> pts)
        {
            if (pts is null) yield break;
            int c = 0;
            Point2d last = default;
            foreach (var pt in pts)
            {
                if (c != 0)
                {
                    var seg = new GLineSegment(pt, last);
                    if (seg.IsValid) yield return seg;
                }
                last = pt;
                ++c;
            }
        }
        public static IEnumerable<GLineSegment> YieldGLineSegments(IEnumerable<Point3d> pts)
        {
            if (pts is null) yield break;
            int c = 0;
            Point3d last = default;
            foreach (var pt in pts)
            {
                if (c != 0)
                {
                    var seg = new GLineSegment(pt, last);
                    if (seg.IsValid) yield return seg;
                }
                last = pt;
                ++c;
            }
        }
        public static T Tag<T>(this T geo, Action cb) where T : Geometry
        {
            geo.UserData = cb;
            return geo;
        }
        public static T Tag<T>(this T geo, object tag) where T : Geometry
        {
            geo.UserData = tag;
            return geo;
        }
    }
    public static class CadJsonExtension
    {
        public class JsonConverter5 : JsonConverter
        {
            static readonly HashSet<Type> types;
            static JsonConverter5()
            {
                types = new HashSet<Type>()
            {
                typeof(Geometry),
                typeof(Coordinate),
                typeof(Point),
                typeof(LineString),
                typeof(LinearRing),
                typeof(Polygon),
                typeof(MultiPoint),
                typeof(MultiLineString),
                typeof(MultiPolygon),
                typeof(GeometryCollection),
            };
            }
            public override bool CanConvert(Type objectType)
            {
                return types.Contains(objectType);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                if (objectType == typeof(Geometry)) return GeoFac.ToGeometry(serializer.Deserialize<JObject>(reader));
                if (objectType == typeof(Coordinate)) return GeoFac.ToCoordinate(serializer.Deserialize<JObject>(reader));
                if (objectType == typeof(Point)) return GeoFac.ToPoint(serializer.Deserialize<JObject>(reader));
                if (objectType == typeof(LineString)) return GeoFac.ToLineString(serializer.Deserialize<JObject>(reader));
                if (objectType == typeof(LinearRing)) return GeoFac.ToLinearRing(serializer.Deserialize<JObject>(reader));
                if (objectType == typeof(Polygon)) return GeoFac.ToPolygon(serializer.Deserialize<JObject>(reader));
                if (objectType == typeof(MultiLineString)) return GeoFac.ToMultiLineString(serializer.Deserialize<JObject>(reader));
                if (objectType == typeof(MultiPolygon)) return GeoFac.ToMultiPolygon(serializer.Deserialize<JObject>(reader));
                if (objectType == typeof(GeometryCollection)) return GeoFac.ToGeometryCollection(serializer.Deserialize<JObject>(reader));
                throw new NotSupportedException();
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                if (value is Geometry geo)
                {
                    serializer.Serialize(writer, GeoFac.ToJToken(geo));
                    return;
                }
                throw new NotSupportedException();
            }
        }
        public class JsonConverter4 : JsonConverter
        {
            public override bool CanRead => true;
            public override bool CanWrite => true;
            static readonly HashSet<Type> types = new HashSet<Type>();
            static JsonConverter4()
            {
                types.Add(typeof(GRect));
                types.Add(typeof(GLineSegment));
                types.Add(typeof(GVector));
                types.Add(typeof(GArc));
                types.Add(typeof(Point2d));
                types.Add(typeof(Point3d));
                types.Add(typeof(Vector2d));
                types.Add(typeof(Vector3d));
                types.Add(typeof(System.Drawing.Color));
                types.Add(typeof(System.Windows.Point));
                types.Add(typeof(System.Windows.Vector));
                types.Add(typeof(System.Windows.Rect));
                types.Add(typeof(System.Windows.Media.LineGeometry));
                types.Add(typeof(Action));
            }
            public override bool CanConvert(Type objectType)
            {
                return types.Contains(objectType);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                if (typeof(GRect) == objectType)
                {
                    var jo = serializer.Deserialize<JObject>(reader);
                    var ja = (JArray)jo["values"];
                    return new GRect(ja[0].ToObject<double>(), ja[1].ToObject<double>(), ja[2].ToObject<double>(), ja[3].ToObject<double>());
                }
                if (typeof(GLineSegment) == objectType)
                {
                    var jo = serializer.Deserialize<JObject>(reader);
                    var ja = (JArray)jo["values"];
                    return new GLineSegment(new Point2d(ja[0].ToObject<double>(), ja[1].ToObject<double>()),
                        new Point2d(ja[2].ToObject<double>(), ja[3].ToObject<double>()));
                }
                if (typeof(GVector) == objectType)
                {
                    var jo = serializer.Deserialize<JObject>(reader);
                    var ja = (JArray)jo["values"];
                    return new GVector(new Point2d(ja[0].ToObject<double>(), ja[1].ToObject<double>()), new Vector2d(ja[2].ToObject<double>(), ja[3].ToObject<double>()));
                }
                if (typeof(GArc) == objectType)
                {
                    var jo = serializer.Deserialize<JObject>(reader);
                    var ja = (JArray)jo["values"];
                    return new GArc(ja[0].ToObject<double>(), ja[1].ToObject<double>(), ja[2].ToObject<double>(), ja[3].ToObject<double>(), ja[4].ToObject<double>(), ja[5].ToObject<bool>());
                }
                if (typeof(Point2d) == objectType)
                {
                    var jo = serializer.Deserialize<JObject>(reader);
                    var ja = (JArray)jo["values"];
                    return new Point2d(ja[0].ToObject<double>(), ja[1].ToObject<double>());
                }
                if (typeof(Point3d) == objectType)
                {
                    var jo = serializer.Deserialize<JObject>(reader);
                    var ja = (JArray)jo["values"];
                    return new Point3d(ja[0].ToObject<double>(), ja[1].ToObject<double>(), ja[2].ToObject<double>());
                }
                if (typeof(Vector2d) == objectType)
                {
                    var jo = serializer.Deserialize<JObject>(reader);
                    var ja = (JArray)jo["values"];
                    return new Vector2d(ja[0].ToObject<double>(), ja[1].ToObject<double>());
                }
                if (typeof(Vector3d) == objectType)
                {
                    var jo = serializer.Deserialize<JObject>(reader);
                    var ja = (JArray)jo["values"];
                    return new Vector3d(ja[0].ToObject<double>(), ja[1].ToObject<double>(), ja[2].ToObject<double>());
                }
                if (typeof(System.Drawing.Color) == objectType)
                {
                    var jo = serializer.Deserialize<JObject>(reader);
                    var ja = (JArray)jo["values"];
                    return System.Drawing.Color.FromArgb(ja[0].ToObject<int>(), ja[1].ToObject<int>(), ja[2].ToObject<int>(), ja[3].ToObject<int>());
                }
                if (typeof(System.Windows.Point) == objectType)
                {
                    var jo = serializer.Deserialize<JObject>(reader);
                    var ja = (JArray)jo["values"];
                    return new System.Windows.Point(ja[0].ToObject<double>(), ja[1].ToObject<double>());
                }
                if (typeof(System.Windows.Vector) == objectType)
                {
                    var jo = serializer.Deserialize<JObject>(reader);
                    var ja = (JArray)jo["values"];
                    return new System.Windows.Vector(ja[0].ToObject<double>(), ja[1].ToObject<double>());
                }
                if (typeof(System.Windows.Rect) == objectType)
                {
                    var jo = serializer.Deserialize<JObject>(reader);
                    var ja = (JArray)jo["values"];
                    return new System.Windows.Rect(ja[0].ToObject<double>(), ja[1].ToObject<double>(), ja[2].ToObject<double>(), ja[3].ToObject<double>());
                }
                if (typeof(System.Windows.Media.LineGeometry) == objectType)
                {
                    var jo = serializer.Deserialize<JObject>(reader);
                    var ja = (JArray)jo["values"];
                    return new System.Windows.Media.LineGeometry(new System.Windows.Point(ja[0].ToObject<double>(), ja[1].ToObject<double>()),
                        new System.Windows.Point(ja[2].ToObject<double>(), ja[3].ToObject<double>()));
                }
                if (typeof(Action) == objectType)
                {
                    var jo = serializer.Deserialize<JObject>(reader);
                    return null;
                }
                throw new NotSupportedException();
            }
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                {
                    if (value is GRect r)
                    {
                        var json = (new Dictionary<string, object>() { { "type", nameof(GRect) }, { "values", new double[] { r.MinX, r.MinY, r.MaxX, r.MaxY } } }).ToJson();
                        writer.WriteRawValue(json);
                        return;
                    }
                }
                {
                    if (value is GLineSegment seg)
                    {
                        var json = (new Dictionary<string, object>() { { "type", nameof(GLineSegment) }, { "values", new double[] { seg.StartPoint.X, seg.StartPoint.Y, seg.EndPoint.X, seg.EndPoint.Y } } }).ToJson();
                        writer.WriteRawValue(json);
                        return;
                    }
                }
                {
                    if (value is GArc arc)
                    {
                        var json = (new Dictionary<string, object>() { { "type", nameof(GArc) }, { "values", new double[] { arc.X, arc.Y, arc.Radius, arc.StartAngle, arc.EndAngle } } }).ToJson();
                        writer.WriteRawValue(json);
                        return;
                    }
                }
                {
                    if (value is GVector vec)
                    {
                        var json = (new Dictionary<string, object>() { { "type", nameof(GVector) }, { "values", new double[] { vec.StartPoint.X, vec.StartPoint.Y, vec.Vector.X, vec.Vector.Y } } }).ToJson();
                        writer.WriteRawValue(json);
                        return;
                    }
                }
                {
                    if (value is Point2d pt)
                    {
                        var json = (new Dictionary<string, object>() { { "type", nameof(Point2d) }, { "values", new double[] { pt.X, pt.Y } } }).ToJson();
                        writer.WriteRawValue(json);
                        return;
                    }
                }
                {
                    if (value is Point3d pt)
                    {
                        var json = (new Dictionary<string, object>() { { "type", nameof(Point3d) }, { "values", new double[] { pt.X, pt.Y, pt.Z } } }).ToJson();
                        writer.WriteRawValue(json);
                        return;
                    }
                }
                {
                    if (value is Vector2d vec)
                    {
                        var json = (new Dictionary<string, object>() { { "type", nameof(Vector2d) }, { "values", new double[] { vec.X, vec.Y } } }).ToJson();
                        writer.WriteRawValue(json);
                        return;
                    }
                }
                {
                    if (value is Vector3d vec)
                    {
                        var json = (new Dictionary<string, object>() { { "type", nameof(Vector3d) }, { "values", new double[] { vec.X, vec.Y, vec.Z } } }).ToJson();
                        writer.WriteRawValue(json);
                        return;
                    }
                }
                {
                    if (value is System.Drawing.Color cl)
                    {
                        var json = (new Dictionary<string, object>() { { "type", nameof(Color) }, { "values", new double[] { cl.A, cl.R, cl.G, cl.B } } }).ToJson();
                        writer.WriteRawValue(json);
                        return;
                    }
                }
                {
                    if (value is System.Windows.Point pt)
                    {
                        var json = (new Dictionary<string, object>() { { "type", nameof(System.Windows.Point) }, { "values", new double[] { pt.X, pt.Y } } }).ToJson();
                        writer.WriteRawValue(json);
                        return;
                    }
                }
                {
                    if (value is System.Windows.Vector vec)
                    {
                        var json = (new Dictionary<string, object>() { { "type", nameof(System.Windows.Vector) }, { "values", new double[] { vec.X, vec.Y } } }).ToJson();
                        writer.WriteRawValue(json);
                        return;
                    }
                }
                {
                    if (value is System.Windows.Rect r)
                    {
                        var json = (new Dictionary<string, object>() { { "type", nameof(System.Windows.Rect) }, { "values", new double[] { r.X, r.Y, r.Width, r.Height } } }).ToJson();
                        writer.WriteRawValue(json);
                        return;
                    }
                }
                {
                    if (value is System.Windows.Media.LineGeometry seg)
                    {
                        var json = (new Dictionary<string, object>() { { "type", nameof(System.Windows.Media.LineGeometry) }, { "values", new double[] { seg.StartPoint.X, seg.StartPoint.Y, seg.EndPoint.X, seg.EndPoint.Y } } }).ToJson();
                        writer.WriteRawValue(json);
                        return;
                    }
                }
                {
                    if (value is Action)
                    {
                        writer.WriteRawValue("null");
                        return;
                    }
                }
                throw new NotSupportedException();
            }
        }
        public static readonly JsonConverter4 cvt4 = new JsonConverter4();
        public static readonly JsonConverter5 cvt5 = new JsonConverter5();
        public class JsonConverter3 : JsonConverter
        {
            public override bool CanRead => false;
            public override bool CanWrite => true;
            public override bool CanConvert(Type objectType)
            {
                return objectType.IsEnum;
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                throw new NotSupportedException();
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                writer.WriteValue(value.ToString());
            }
        }
        static readonly JsonConverter3 cvt3 = new JsonConverter3();

        public static string ToCadJson(this object obj)
        {
            return ToJson(obj);
        }
        public static T FromCadJson<T>(this string json)
        {
            return FromJson<T>(json);
        }

        public static string ToJson(object obj)
        {
            return JsonConvert.SerializeObject(obj, cvt3, cvt4, cvt5);
        }
        public static T FromJson<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, cvt4, cvt5);
        }

    }
    public class Cache<K, V>
    {
        Dictionary<K, V> d = new Dictionary<K, V>();
        Func<K, V> f;
        public Cache(Func<K, V> f)
        {
            this.f = f;
        }
        public V this[K k]
        {
            get
            {
                if (!d.TryGetValue(k, out V v))
                {
                    v = f(k);
                    d[k] = v;
                }
                return v;
            }
        }
    }
    public class _DrawingTransaction : IDisposable
    {
        public DBText dBText;
        public readonly Dictionary<string, ObjectId> TextStyleIdDict = new Dictionary<string, ObjectId>();
        public bool NoDraw;
        public bool? AbleToDraw = null;
        public static _DrawingTransaction Current { get; private set; }
        public AcadDatabase adb { get; private set; }
        public _DrawingTransaction(AcadDatabase adb) : this()
        {
            this.adb = adb;
        }
        public _DrawingTransaction(AcadDatabase adb, bool noDraw) : this()
        {
            this.adb = adb;
            this.NoDraw = noDraw;
        }
        public _DrawingTransaction()
        {
            DrawUtils.DrawingQueue.Clear();
            Current = this;
        }
        public void Dispose()
        {
            try
            {
                dBText?.Dispose();
                if (!NoDraw)
                {
                    if (AbleToDraw != false)
                    {
                        if (adb != null)
                        {
                            DrawUtils.FlushDQ(adb);
                        }
                        else
                        {
                            DrawUtils.FlushDQ();
                        }
                    }
                }
            }
            finally
            {
                Current = null;
                DrawUtils.Dispose();
            }
        }

        HashSet<string> visibleLayers = new HashSet<string>();
        HashSet<string> invisibleLayers = new HashSet<string>();
        public bool IsLayerVisible(string layer)
        {
            if (visibleLayers.Contains(layer)) return true;
            if (invisibleLayers.Contains(layer)) return false;
            var ly = adb.Layers.ElementOrDefault(layer);
            if (ly != null && !ly.IsFrozen && !ly.IsOff && !ly.IsHidden)
            {
                visibleLayers.Add(layer);
                return true;
            }
            else
            {
                invisibleLayers.Add(layer);
                return false;
            }
        }

    }

    public class BlockReferenceVisitor : ThDistributionElementExtractionVisitor
    {
        public override void DoExtract(List<ThRawIfcDistributionElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            if (dbObj is BlockReference blkref)
            {
                _HandleBlockReference(elements, blkref, matrix);
            }
        }

        public override void DoXClip(List<ThRawIfcDistributionElementData> elements, BlockReference blockReference, Matrix3d matrix)
        {
            var xclip = blockReference.XClipInfo();
            if (xclip.IsValid)
            {
                xclip.TransformBy(matrix);
                elements.RemoveAll(o => !IsContain(xclip, o.Geometry));
            }
        }

        public override bool CheckLayerValid(Entity curve)
        {
            return true;
        }
        public Func<BlockReference, bool> IsTargetBlockReferenceCb;
        public override bool IsDistributionElement(Entity entity)
        {
            if (entity is BlockReference blkref)
            {
                return IsTargetBlockReferenceCb(blkref);
                var name = blkref.GetEffectiveName();
                return (ThMEPEngineCore.Service.ThGravityWaterBucketLayerManager.IsGravityWaterBucketBlockName(name));
            }
            return false;
        }
        public Action<BlockReference, Matrix3d> HandleBlockReferenceCb;
        public bool SupportDynamicBlock;
        public override bool IsBuildElementBlock(BlockTableRecord blockTableRecord)
        {
            //// 暂时不支持动态块，外部参照，覆盖
            //if (blockTableRecord.IsDynamicBlock)
            //{
            //    return false;
            //}

            if (!SupportDynamicBlock)
            {
                if (blockTableRecord.IsDynamicBlock)
                {
                    return false;
                }
            }

            // 忽略图纸空间和匿名块
            if (blockTableRecord.IsLayout || blockTableRecord.IsAnonymous)
            {
                return false;
            }

            // 忽略不可“炸开”的块
            if (!blockTableRecord.Explodable)
            {
                return false;
            }

            return true;
        }
        private void _HandleBlockReference(List<ThRawIfcDistributionElementData> elements, BlockReference blkref, Matrix3d matrix)
        {
            if (!blkref.ObjectId.IsValid) return;
            HandleBlockReferenceCb(blkref, matrix);
        }

        private bool IsContain(ThMEPEngineCore.Algorithm.ThMEPXClipInfo xclip, Entity ent)
        {
            if (ent is BlockReference br)
            {
                return xclip.Contains(br.GeometricExtents.ToRectangle());
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }

    public static class HighlightHelper
    {

        public static Point2d GetCurrentViewSize()
        {
            double h = (double)Autodesk.AutoCAD.ApplicationServices.Application.GetSystemVariable("VIEWSIZE");
            Point2d screen = (Point2d)Autodesk.AutoCAD.ApplicationServices.Application.GetSystemVariable("SCREENSIZE");
            double w = h * (screen.X / screen.Y);
            return new Point2d(w, h);
        }
        public static Extents2d GetCurrentViewBound(double shrinkScale = 1.0)
        {
            Point2d vSize = GetCurrentViewSize();
            Point3d center = ((Point3d)Autodesk.AutoCAD.ApplicationServices.Application.GetSystemVariable("VIEWCTR")).
                    TransformBy(Active.Editor.CurrentUserCoordinateSystem);
            double w = vSize.X * shrinkScale;
            double h = vSize.Y * shrinkScale;
            Point2d minPoint = new Point2d(center.X - w / 2.0, center.Y - h / 2.0);
            Point2d maxPoint = new Point2d(center.X + w / 2.0, center.Y + h / 2.0);
            return new Extents2d(minPoint, maxPoint);
        }
        public static void HighLight(IEnumerable<Entity> ents)
        {
            //var extents = ThAuxiliaryUtils.GetCurrentViewBound();
            var extents = GetCurrentViewBound();
            foreach (var e in ents)
            {
                if (!e.IsErased && !e.IsDisposed && e.Bounds is Extents3d ext)
                {
                    if (IsInActiveView(ext.MinPoint,
                        extents.MinPoint.X, extents.MaxPoint.X,
                        extents.MinPoint.Y, extents.MaxPoint.Y) ||
                    IsInActiveView(ext.MaxPoint,
                    extents.MinPoint.X, extents.MaxPoint.X,
                    extents.MinPoint.Y, extents.MaxPoint.Y))
                    {
                        e.Highlight();
                    }
                }
            }
        }
        public static void UnHighLight(IEnumerable<Entity> ents)
        {
            foreach (var e in ents)
            {
                if (!e.IsErased && !e.IsDisposed)
                {
                    e.Unhighlight();
                }
            }
        }
        private static bool IsInActiveView(Point3d pt, double minX, double maxX, double minY, double maxY)
        {
            return pt.X >= minX && pt.X <= maxX && pt.Y >= minY && pt.Y <= maxY;
        }
    }
    public static class DrawUtils
    {
        public static (double, double) GetDBTextSize(string text, double height, double widthFactor, string style)
        {
            if (string.IsNullOrEmpty(text) || height <= 0 || widthFactor <= 0) return (0, 0);
            var dbt = _DrawingTransaction.Current?.dBText ?? new DBText();
            if (_DrawingTransaction.Current != null && _DrawingTransaction.Current.dBText == null)
            {
                _DrawingTransaction.Current.dBText = dbt;
            }
            dbt.TextString = text;
            dbt.Height = height;
            dbt.WidthFactor = widthFactor;
            var id = GetTextStyleId(style);
            if (id.IsValid) dbt.TextStyleId = id;
            var r = dbt.Bounds.ToGRect();
            return (r.Width, r.Height);
        }
        public static DocumentLock DocLock => Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument();

        public static _DrawingTransaction DrawingTransaction => new _DrawingTransaction();
        public static Queue<Action<AcadDatabase>> DrawingQueue { get; } = new Queue<Action<AcadDatabase>>(4096);
        public static void Dispose()
        {
            DrawingQueue.Clear();
        }
        public static List<Action<AcadDatabase>> TakeAllDrawingActions()
        {
            var lst = DrawingQueue.ToList();
            DrawingQueue.Clear();
            return lst;
        }
        public static void Draw(IEnumerable<Action<AcadDatabase>> fs, AcadDatabase adb, bool notifyOnException = true)
        {
            foreach (var f in fs)
            {
                try
                {
                    f(adb);
                }
                catch (System.Exception ex)
                {
                    if (notifyOnException)
                    {
                        MessageBox.Show((ex.InnerException ?? ex).Message);
                    }
                    break;
                }
            }
        }
        public static ObjectId GetTextStyleId(string textStyleName)
        {
            var d = _DrawingTransaction.Current?.TextStyleIdDict;
            if (d != null)
            {
                if (!d.TryGetValue(textStyleName, out ObjectId id))
                {
                    id = DbHelper.GetTextStyleId(textStyleName);
                    d[textStyleName] = id;
                }
                return id;
            }
            return DbHelper.GetTextStyleId(textStyleName);
        }
        public static void SetTextStyle(DBText t, string textStyleName)
        {
            if (!t.ObjectId.IsValid) return;
            var textStyleId = GetTextStyleId(textStyleName);
            if (!textStyleId.IsValid) return;
            t.TextStyleId = textStyleId;
        }
        public static void SetTextStyleLazy(DBText t, string textStyleName)
        {
            DrawingQueue.Enqueue(adb =>
            {
                SetTextStyle(t, textStyleName);
            });
        }
        public static bool IsLayerVisible(Entity e)
        {
            return IsLayerVisible(e.Layer);
        }
        public static bool IsLayerVisible(string layer)
        {
            return _DrawingTransaction.Current.IsLayerVisible(layer);
        }
        public static void LayerThreeAxes(IEnumerable<string> layers)
        {
            static void EnsureLayerOn(string layerName)
            {
                var id = DbHelper.GetLayerId(layerName);
                id.QOpenForWrite<LayerTableRecord>(layer =>
                {
                    layer.IsLocked = false;
                    layer.IsFrozen = false;
                    layer.IsHidden = false;
                    layer.IsOff = false;
                });
            }
            foreach (var layer in layers)
            {
                try
                {
                    //Dreambuild.AutoCAD.DbHelper.EnsureLayerOn(layer);
                    EnsureLayerOn(layer);
                }
                catch { }
            }
        }

        static bool IsVisibleBlockRef(AcadDatabase adb, BlockReference br)
        {
            var layer = br?.Layer;
            if (layer == null) return false;
            return IsVisibleLayer(adb.Layers.Element(layer));
        }
        static bool IsVisibleLayer(LayerTableRecord layerTableRecord)
        {
            //return !(layerTableRecord.IsOff || layerTableRecord.IsFrozen);
            //return !layerTableRecord.IsOff;

            //一个个试出来的，我也不懂。。。
            return !layerTableRecord.IsFrozen;

            //return !layerTableRecord.IsHidden;
            //return layerTableRecord.IsUsed;
        }

        public static T LoadFromJsonFile<T>(string file)
        {
            return File.ReadAllText(file).FromCadJson<T>();
        }
        public static void DoExtract(AcadDatabase adb, Func<BlockReference, Matrix3d, bool> doExtract,
                  bool supportDynamicBlock = false,
                  Action<BlockReference, Matrix3d> doXClip = null,
                  Action<Entity, Matrix3d> entCb = null)
        {
            foreach (var br in adb.ModelSpace.OfType<BlockReference>())
            {
                if (br.BlockTableRecord.IsNull) continue;
                var record = adb.Blocks.Element(br.BlockTableRecord);
                var ignoreInvisible = record.XrefStatus == XrefStatus.NotAnXref;
                var m = br.BlockTransform;
                DoExtract(br, m, doExtract, supportDynamicBlock: supportDynamicBlock, doXClip: doXClip, rootInclude: false, entCb: entCb, ignoreInvisible: ignoreInvisible);
            }
        }

        public static void DoExtract(BlockReference blockReference, Matrix3d matrix,
                                     Func<BlockReference, Matrix3d, bool> doExtract,
                                     bool supportDynamicBlock = false,
                                     Action<BlockReference, Matrix3d> doXClip = null,
                                     bool rootInclude = false,
                                     Action<Entity, Matrix3d> entCb = null,
                                     bool ignoreInvisible = true)
        {
            bool isMaybeWantedBlockReference(AcadDatabase adb, BlockReference br)
            {
                if (!ignoreInvisible) return true;
                return IsVisibleBlockRef(adb, br);
            }
            bool IsBuildElementBlock(BlockTableRecord blockTableRecord)
            {
                if (!supportDynamicBlock)
                {
                    // 暂时不支持动态块，外部参照，覆盖
                    if (blockTableRecord.IsDynamicBlock)
                    {
                        return false;
                    }
                }

                // 忽略图纸空间和匿名块
                if (blockTableRecord.IsLayout || blockTableRecord.IsAnonymous)
                {
                    return false;
                }

                // 忽略不可“炸开”的块
                if (!blockTableRecord.Explodable)
                {
                    return false;
                }

                return true;
            }
            using AcadDatabase adb = AcadDatabase.Use(blockReference.Database);
            if (blockReference.BlockTableRecord.IsValid)
            {
                if (rootInclude && blockReference.ObjectId.IsValid && (isMaybeWantedBlockReference(adb, blockReference)))
                {
                    if (doExtract(blockReference, matrix)) return;
                }
                var blockTableRecord = adb.Blocks.Element(blockReference.BlockTableRecord);
                if (IsBuildElementBlock(blockTableRecord))
                {
                    // 提取图元信息                        
                    foreach (var objId in blockTableRecord)
                    {
                        var dbObj = adb.Element<Entity>(objId);
                        if (dbObj is BlockReference blockObj)
                        {
                            if (!isMaybeWantedBlockReference(adb, blockObj)) continue;
                            if (blockObj.BlockTableRecord.IsValid)
                            {
                                if (doExtract(blockObj, matrix)) continue;
                                var mcs2wcs = blockObj.BlockTransform.PreMultiplyBy(matrix);
                                DoExtract(blockObj, mcs2wcs, doExtract, supportDynamicBlock: supportDynamicBlock, doXClip: doXClip, rootInclude: false, entCb: entCb, ignoreInvisible: ignoreInvisible);
                            }
                        }
                        else
                        {
                            entCb?.Invoke(dbObj, matrix);
                        }
                    }

                    // 过滤XClip外的图元信息
                    doXClip?.Invoke(blockReference, matrix);
                    {
                        //var xclip = blockReference.XClipInfo();
                        //var poly = xclip.Polygon;
                        //if (poly != null)
                        //{
                        //    poly.TransformBy(m);
                        //    var gf = poly.ToNTSGeometry().ToIPreparedGeometry();
                        //    geos.RemoveAll(o => !gf.Contains(o));
                        //}
                    }
                }
            }
        }


        public static void FlushDQ()
        {
            if (DrawingQueue.Count == 0) return;
            using var adb = AcadDatabase.Active();
            FlushDQ(adb);
        }
        public static void FlushDQ(AcadDatabase adb)
        {
            try
            {
                while (DrawingQueue.Count > 0)
                {
                    DrawingQueue.Dequeue()(adb);
                }
            }
            finally
            {
                if (DrawingQueue.Count > 0) DrawingQueue.Clear();
            }
        }

        public static bool TrySelectPoint(out Point3d pt, string prompt = "\n选择图纸基点")
        {
            var basePtOptions = new PromptPointOptions(prompt);
            var rst = Active.Editor.GetPoint(basePtOptions);
            if (rst.Status != PromptStatus.OK)
            {
                pt = default;
                return false;
            }
            pt = rst.Value;
            return true;
        }

        public class EqualityComparer<T> : IEqualityComparer<T>
        {
            Func<T, T, bool> f;

            public EqualityComparer(Func<T, T, bool> f)
            {
                this.f = f;
            }

            public bool Equals(T x, T y)
            {
                return f(x, y);
            }

            public int GetHashCode(T obj)
            {
                return 0;
            }
        }
        public static IEqualityComparer<T> CreateEqualityComparer<T>(Func<T, T, bool> f)
        {
            return new EqualityComparer<T>(f);
        }
        public static Point3dCollection SelectRange()
        {
            return SelectGRect().ToPoint3dCollection();
        }
        public static Point3dCollection TrySelectRange()
        {
            return TrySelectRect()?.ToPoint3dCollection();
        }
        public static Point3dCollection TrySelectRangeEx()
        {
            var range = ThMEPWSS.Common.Utils.SelectAreas();
            if (range.Count == 0) return null;
            return range;
        }
        public static Tuple<Point3d, Point3d> TrySelectRect()
        {
            var ptLeftRes = Active.Editor.GetPoint("\n请您框选范围，先选择左上角点");
            if (ptLeftRes.Status != PromptStatus.OK) return null;
            Point3d leftDownPt = ptLeftRes.Value;
            var ptRightRes = Active.Editor.GetCorner("\n再选择右下角点", leftDownPt);
            if (ptRightRes.Status != PromptStatus.OK) return null;
            return new Tuple<Point3d, Point3d>(leftDownPt, ptRightRes.Value);
        }
        public static Point3d SelectPoint()
        {
            var basePtOptions = new PromptPointOptions("\n选择图纸基点");
            var rst = Active.Editor.GetPoint(basePtOptions);
            if (rst.Status != PromptStatus.OK) return default;
            var basePt = rst.Value;
            return basePt;
        }
        public static GRect SelectGRect()
        {
            var t = SelectRect();
            return new GRect(t.Item1.ToPoint2d(), t.Item2.ToPoint2d());
        }
        public static Tuple<Point3d, Point3d> SelectRect()
        {
            var ptLeftRes = Active.Editor.GetPoint("\n请您框选范围，先选择左上角点");
            Point3d leftDownPt = Point3d.Origin;
            if (ptLeftRes.Status == PromptStatus.OK)
            {
                leftDownPt = ptLeftRes.Value;
            }
            else
            {
                return Tuple.Create(leftDownPt, leftDownPt);
            }

            var ptRightRes = Active.Editor.GetCorner("\n再选择右下角点", leftDownPt);
            if (ptRightRes.Status == PromptStatus.OK)
            {
                return Tuple.Create(leftDownPt, ptRightRes.Value);
            }
            else
            {
                return Tuple.Create(leftDownPt, leftDownPt);
            }
        }
        static ObjectId GetEntity()
        {
            var ed = Active.Editor;
            var opt = new PromptEntityOptions("请选择");
            var ret = ed.GetEntity(opt);
            if (ret.Status != PromptStatus.OK) return ObjectId.Null;
            return ret.ObjectId;
        }
        public static DBObjectCollection SelectEntities(AcadDatabase adb)
        {
            IEnumerable<ObjectId> f()
            {
                var ed = Active.Editor;
                var opt = new PromptEntityOptions("请选择");
                while (true)
                {
                    var ret = ed.GetEntity(opt);
                    if (ret.Status == PromptStatus.OK) yield return ret.ObjectId;
                    else yield break;
                }
            }
            return f().Select(id => adb.Element<DBObject>(id)).ToCollection();
        }
        public static List<Entity> SelectEntitiesEx(AcadDatabase adb)
        {
            {
                var options = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    MessageForAdding = "请选择",
                };
                var result = Active.Editor.GetSelection(options);
                if (result.Status == PromptStatus.OK)
                {
                    var selectedIds = result.Value.GetObjectIds();
                    return selectedIds.Select(id => adb.Element<Entity>(id)).ToList();
                }
                return null;
            }
            //if (Dbg._)
            //{
            //    var options = new PromptSelectionOptions()
            //    {
            //        AllowDuplicates = false,
            //        MessageForAdding = "请选择楼层框线",
            //        //RejectObjectsOnLockedLayers = true,
            //    };
            //    var dxfNames = new string[]
            //    {
            //            RXClass.GetClass(typeof(BlockReference)).DxfName,
            //    };
            //    var filter = ThSelectionFilterTool.Build(dxfNames);
            //    var result = Active.Editor.GetSelection(options, filter);
            //}
        }

        public static T SelectEntity<T>(AcadDatabase adb, bool openForWrite = false) where T : DBObject
        {
            var id = GetEntity();
            var ent = adb.Element<T>(id, openForWrite);
            return ent;
        }
        public static T TrySelectEntity<T>(AcadDatabase adb) where T : DBObject
        {
            var ed = Active.Editor;
            var opt = new PromptEntityOptions("请选择");
            var ret = ed.GetEntity(opt);
            if (ret.Status != PromptStatus.OK) return null;
            return adb.Element<T>(ret.ObjectId);
        }

        const double DEFAULT_DELTA = 10000;

        public static void FocusMainWindow()
        {
            ThMEPWSS.Common.Utils.FocusMainWindow();
        }
        static bool IsContrary(double v1, double v2)
        {
            return v1 < 0 && v2 > 0 || v1 > 0 && v2 < 0;
        }
        public static bool IsTianZhengRainPort(Entity e)
        {
            if ((e.Layer == "W-RAIN-EQPM" || e.Layer == "W-RAIN-NOTE" || e.Layer == "W-RAIN-DIMS") && ThRainSystemService.IsTianZhengElement(e))
            {
                var lst = e.ExplodeToDBObjectCollection();
                if (lst.Count == 1)
                {
                    if (lst[0] is BlockReference br)
                    {
                        var lst2 = br.ExplodeToDBObjectCollection();
                        if (lst2.Count == 1)
                        {
                            if (lst2[0] is Polyline pl)
                            {
                                if (pl.HasBulges && pl.NumberOfVertices == 3)
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }
        public static void CollectTianzhengVerticalPipes(List<GLineSegment> labelLines, List<CText> cts, List<Entity> entities)
        {
            foreach (var ent in entities.Where(e => ThRainSystemService.IsTianZhengElement(e)).ToList())
            {
                void f()
                {
                    var lst = ent.ExplodeToDBObjectCollection().OfType<Entity>().ToList();
                    if (lst.OfType<Line>().Any())
                    {
                        foreach (var et in lst)
                        {
                            if (ThRainSystemService.IsTianZhengElement(et))
                            {
                                var l = et.ExplodeToDBObjectCollection().OfType<DBText>().ToList();
                                if (l.Count == 1)
                                {
                                    var e = l[0];
                                    var t = e.TextString;
                                    if (!ThRainSystemService.IsWantedLabelText(t)) return;
                                    var bd = e.Bounds.ToGRect();
                                    var ct = new CText() { Text = t, Boundary = bd };
                                    cts.Add(ct);
                                    if (!ct.Boundary.IsValid)
                                    {
                                        var p = e.Position.ToPoint2d();
                                        var h = e.Height;
                                        var w = h * e.WidthFactor * e.WidthFactor * e.TextString.Length;
                                        var r = new GRect(p, p.OffsetXY(w, h));
                                        ct.Boundary = r;
                                    }
                                    labelLines.AddRange(lst.OfType<Line>().Where(e => e.Length > 0).Select(e => e.ToGLineSegment()));
                                    return;
                                }
                            }
                        }
                    }
                }
                f();
            }
        }
        public static void SetLayerAndByLayer(string layer, params Entity[] ents)
        {
            foreach (var ent in ents)
            {
                ent.Layer = layer;
                ByLayer(ent);
            }
        }
        public static Circle DrawGeometryLazy(GCircle circle)
        {
            var c = new Circle() { Center = circle.Center.ToPoint3d(), Radius = circle.Radius };
            DrawEntityLazy(c);
            return c;
        }
        public static void DrawGeometryLazy(Geometry geo, Action<List<Entity>> cb = null)
        {
            var ents = ThCADCore.NTS.ThCADCoreNTSDbExtension.ToDbObjects(geo).OfType<Entity>().ToList();
            cb?.Invoke(ents);
            DrawEntitiesLazy(ents);
        }
        public static void DrawEntityLazy(Entity ent)
        {
            DrawingQueue.Enqueue(adb => adb.ModelSpace.Add(ent));
        }

        public static void DrawEntitiesLazy<T>(IList<T> ents) where T : Entity
        {
            DrawingQueue.Enqueue(adb => ents.ForEach(ent => adb.ModelSpace.Add(ent)));
        }

        public static void DrawBlockReference(string blkName, Point3d basePt, Action<BlockReference> cb = null, Dictionary<string, string> props = null, string layer = null, double scale = 1, double rotateDegree = 0)
        {
            static ObjectId InsertBlockReference(ObjectId spaceId, string layer, string blockName, Point3d position, Scale3d scale, double rotateAngle, Dictionary<string, string> attNameValues)
            {
                var db = spaceId.Database;
                var bt = (BlockTable)db.BlockTableId.GetObject(OpenMode.ForRead);
                if (!bt.Has(blockName)) return ObjectId.Null;
                var space = (BlockTableRecord)spaceId.GetObject(OpenMode.ForWrite);
                var btrId = bt[blockName];
                var record = (BlockTableRecord)btrId.GetObject(OpenMode.ForRead);
                var br = new BlockReference(position, bt[blockName]) { ScaleFactors = scale };
                if (layer != null) br.Layer = layer;
                br.Rotation = rotateAngle;
                space.AppendEntity(br);
                if (attNameValues != null && record.HasAttributeDefinitions)
                {
                    foreach (ObjectId id in record)
                    {
                        if (id.GetObject(OpenMode.ForRead) is AttributeDefinition attDef)
                        {
                            var attribute = new AttributeReference();
                            attribute.SetAttributeFromBlock(attDef, br.BlockTransform);
                            attribute.Position = attDef.Position.TransformBy(br.BlockTransform);
                            attribute.Rotation = attDef.Rotation;
                            attribute.AdjustAlignment(db);
                            if (attNameValues.ContainsKey(attDef.Tag.ToUpper()))
                            {
                                attribute.TextString = attNameValues[attDef.Tag.ToUpper()].ToString();
                            }
                            br.AttributeCollection.AppendAttribute(attribute);
                            db.TransactionManager.AddNewlyCreatedDBObject(attribute, true);
                        }
                    }
                }
                db.TransactionManager.AddNewlyCreatedDBObject(br, true);
                return br.ObjectId;
            }
            DrawingQueue.Enqueue(adb =>
            {
                var id = InsertBlockReference(adb.ModelSpace.ObjectId, layer, blkName, basePt, new Scale3d(scale), GeoAlgorithm.AngleFromDegree(rotateDegree), props);
                if (!id.IsValid) return;
                if (cb != null)
                {
                    var br = adb.Element<BlockReference>(id);
                    cb(br);
                }
            });
        }
        public static void DrawBlockReference(string blkName, Point3d basePt, Action<BlockReference> cb)
        {
            static ObjectId InsertBlockReference(ObjectId spaceId, string layer, string blockName, Point3d position, Scale3d scale, double rotateAngle)
            {
                var db = spaceId.Database;
                var bt = (BlockTable)db.BlockTableId.GetObject(OpenMode.ForRead);
                if (!bt.Has(blockName)) return ObjectId.Null;
                var space = (BlockTableRecord)spaceId.GetObject(OpenMode.ForWrite);
                var br = new BlockReference(position, bt[blockName]) { ScaleFactors = scale };
                if (layer != null) br.Layer = layer;
                br.Rotation = rotateAngle;
                var btrId = bt[blockName];
                var record = (BlockTableRecord)btrId.GetObject(OpenMode.ForRead);
                if (record.Annotative == AnnotativeStates.True)
                {
                    ObjectContextCollection contextCollection = db.ObjectContextManager.GetContextCollection("ACDB_ANNOTATIONSCALES");
                    ObjectContexts.AddContext(br, contextCollection.GetContext("1:1"));
                }
                var blockRefId = space.AppendEntity(br);
                db.TransactionManager.AddNewlyCreatedDBObject(br, true);
                space.DowngradeOpen();
                return blockRefId;
            }
            DrawingQueue.Enqueue(adb =>
            {
                var id = InsertBlockReference(adb.ModelSpace.ObjectId, null, blkName, basePt, new Scale3d(1), 0);
                if (!id.IsValid) return;
                if (cb != null)
                {
                    var br = adb.Element<BlockReference>(id);
                    cb(br);
                }
            });
        }
        public static Polyline DrawBoundaryLazy(Entity[] ents, double thickness)
        {
            if (ents.Length == 0) return null;
            var lst = ents.Select(e => GeoAlgorithm.GetBoundaryRect(e)).ToList();
            var minx = lst.Select(r => r.MinX).Min();
            var miny = lst.Select(r => r.MinY).Min();
            var maxx = lst.Select(r => r.MaxX).Max();
            var maxy = lst.Select(r => r.MaxY).Max();
            var pl = DrawRectLazy(new GRect(minx, miny, maxx, maxy));
            pl.ConstantWidth = thickness;
            return pl;
        }
        public static Polyline DrawRectLazyFromLeftButtom(Point3d leftButtom, double width, double height)
        {
            return DrawRectLazy(leftButtom, new Point3d(leftButtom.X + width, leftButtom.Y + height, leftButtom.Z));
        }
        public static Polyline DrawRectLazyFromLeftTop(Point3d leftButtom, double width, double height)
        {
            return DrawRectLazy(leftButtom, new Point3d(leftButtom.X + width, leftButtom.Y - height, leftButtom.Z));
        }
        public static void DrawGVectorLazy(GVector gv, Action<Entity> cb = null)
        {
            DrawingQueue.Enqueue(adb =>
            {
                if (gv.Vector.Length > 0)
                {
                    //var e = new Leader();
                    //e.HasArrowHead = true;
                    //var v = gv.Vector.Length / 2;
                    //if (v > 200) v = 200;
                    //e.Dimasz = v;
                    //e.AppendVertex((gv.EndPoint).ToPoint3d());
                    //e.AppendVertex(gv.StartPoint.ToPoint3d());
                    //cb?.Invoke(e);
                    //DrawEntityLazy(e);

                    DrawLineLazy(gv.StartPoint, gv.EndPoint);
                    var v = gv.Vector.Length / 4;
                    if (v > 200) v = 200;
                    DrawCircleLazy(gv.EndPoint.ToPoint3d(), v);
                }
            });
        }
        public static Line DrawLineSegmentLazy(GLineSegment seg, string layer)
        {
            var line = DrawLineSegmentLazy(seg);
            line.ColorIndex = 256;
            line.Layer = layer;
            return line;
        }
        public static void ByLayer(Entity line)
        {
            line.ColorIndex = 256;
            line.LineWeight = LineWeight.ByLayer;
            line.Linetype = "ByLayer";
        }
        public static Line DrawLineSegmentLazy(GLineSegment seg)
        {
            return DrawLineLazy(seg.StartPoint, seg.EndPoint);
        }
        public static List<Line> DrawLineSegmentsLazy(IEnumerable<GLineSegment> segs)
        {
            var lines = segs.Select(seg => new Line() { StartPoint = seg.StartPoint.ToPoint3d(), EndPoint = seg.EndPoint.ToPoint3d() }).ToList();
            DrawingQueue.Enqueue(adb =>
            {
                foreach (var line in lines)
                {
                    adb.ModelSpace.Add(line);
                }
            });
            return lines;
        }
        public static Polyline DrawLineSegmentLazy(GLineSegment seg, double width)
        {
            var pl = DrawPolyLineLazy(new Point2d[] { seg.StartPoint, seg.EndPoint });
            pl.ConstantWidth = width;
            return pl;
        }
        public static void DrawLineSegmentBufferLazy(IEnumerable<GLineSegment> segs, double bufSize)
        {
            foreach (var seg in segs)
            {
                DrawLineSegmentBufferLazy(seg, bufSize);
            }
        }
        public static Polyline DrawLineSegmentBufferLazy(GLineSegment seg, double bufSize)
        {
            int i = 0;
            var pline = new Polyline();
            foreach (var pt in (seg.Buffer(bufSize) as Polygon).Shell.Coordinates.Select(x => x.ToPoint2d()))
            {
                pline.AddVertexAt(i++, pt, 0, 0, 0);
            }
            DrawEntityLazy(pline);
            return pline;
        }
        public static Polyline DrawPolyLineLazy(Coordinate[] coordinates)
        {
            return DrawPolyLineLazy(coordinates.Select(c => c.ToPoint3d()).ToArray());
        }
        public static Polyline DrawPolyLineLazy(GLineSegment seg)
        {
            var c = new Point2dCollection() { seg.StartPoint, seg.EndPoint };
            var pl = new Polyline();
            DotNetARX.PolylineTools.CreatePolyline(pl, c);
            DrawingQueue.Enqueue(adb => adb.ModelSpace.Add(pl));
            return pl;
        }
        public static Polyline DrawRectLazy(GRect rect)
        {
            return DrawRectLazyFromLeftTop(new Point2d(rect.MinX, rect.MaxY).ToPoint3d(), rect.Width, rect.Height);
        }
        public static Polyline DrawRectLazy(GRect rect, double thickness)
        {
            var pl = DrawRectLazyFromLeftTop(new Point2d(rect.MinX, rect.MaxY).ToPoint3d(), rect.Width, rect.Height);
            pl.ConstantWidth = thickness;
            return pl;
        }
        public static Polyline DrawRectLazy(Point3d pt1, Point3d pt2)
        {
            static Polyline CreatePolyline(Point2dCollection pts)
            {
                var pline = new Polyline();
                for (int i = 0; i < pts.Count; i++)
                {
                    pline.AddVertexAt(i, pts[i], 0, 0, 0);
                }
                return pline;
            }
            static Polyline CreateRectangle(Point2d pt1, Point2d pt2)
            {
                var minX = Math.Min(pt1.X, pt2.X);
                var maxX = Math.Max(pt1.X, pt2.X);
                var minY = Math.Min(pt1.Y, pt2.Y);
                var maxY = Math.Max(pt1.Y, pt2.Y);
                var pts = new Point2dCollection
                {
                    new Point2d(minX, minY),
                    new Point2d(minX, maxY),
                    new Point2d(maxX, maxY),
                    new Point2d(maxX, minY)
                };
                var pline = CreatePolyline(pts);
                pline.Closed = true;
                return pline;
            }
            var polyline = CreateRectangle(pt1.ToPoint2D(), pt2.ToPoint2D());
            DrawingQueue.Enqueue(adb =>
            {
                adb.ModelSpace.Add(polyline);
            });
            return polyline;
        }
        public static Point3d GetMidPoint(Point3d first, Point3d second)
        {
            var x = (first.X + second.X) / 2;
            var y = (first.Y + second.Y) / 2;
            return new Point3d(x, y, 0);
        }
        public static Point2d GetMidPoint(Point2d first, Point2d second)
        {
            var x = (first.X + second.X) / 2;
            var y = (first.Y + second.Y) / 2;
            return new Point2d(x, y);
        }
        public static Circle DrawCircleLazy(GRect rect)
        {
            var p1 = new Point3d(rect.MinX, rect.MinY, 0);
            var p2 = new Point3d(rect.MaxX, rect.MaxY, 0);
            var center = GetMidPoint(p1, p2);
            var radius = GeoAlgorithm.Distance(p1, p2) / 2;
            return DrawCircleLazy(center, radius);
        }
        public static Circle DrawCircleLazy(Point2d center, double radius)
        {
            return DrawCircleLazy(center.ToPoint3d(), radius);
        }
        public static Circle DrawCircleLazy(Point3d center, double radius)
        {
            if (radius <= 0) radius = 1;
            var circle = new Circle() { Center = center, Radius = radius };
            DrawingQueue.Enqueue(adb =>
            {
                adb.ModelSpace.Add(circle);
            });
            return circle;
        }
        public static Polyline DrawPolyLineLazy(params Point2d[] pts)
        {
            var c = new Point2dCollection();
            foreach (var pt in pts)
            {
                c.Add(pt);
            }
            var pl = new Polyline();
            DotNetARX.PolylineTools.CreatePolyline(pl, c);
            DrawingQueue.Enqueue(adb => adb.ModelSpace.Add(pl));
            return pl;
        }
        public static Polyline DrawPolyLineLazy(params Point3d[] pts)
        {
            var c = new Point2dCollection();
            foreach (var pt in pts)
            {
                c.Add(pt.ToPoint2d());
            }
            var pl = new Polyline();
            DotNetARX.PolylineTools.CreatePolyline(pl, c);
            DrawingQueue.Enqueue(adb => adb.ModelSpace.Add(pl));
            return pl;
        }
        public static List<Line> DrawLinesLazy(params Point2d[] pts)
        {
            return DrawLinesLazy((IList<Point2d>)pts);
        }
        public static List<Line> DrawLinesLazy(params Point3d[] pts)
        {
            return DrawLinesLazy((IList<Point3d>)pts);
        }
        public static List<Line> DrawLinesLazy(IList<Point3d> pts)
        {
            var ret = new List<Line>();
            for (int i = 0; i < pts.Count - 1; i++)
            {
                var line = DrawLineLazy(pts[i], pts[i + 1]);
                ret.Add(line);
            }
            return ret;
        }
        public static List<Line> DrawLinesLazy(IList<Point2d> pts)
        {
            var ret = new List<Line>();
            for (int i = 0; i < pts.Count - 1; i++)
            {
                var line = DrawLineLazy(pts[i], pts[i + 1]);
                ret.Add(line);
            }
            return ret;
        }
        public static Line DrawLineLazy(double x1, double y1, double x2, double y2)
        {
            return DrawLineLazy(new Point3d(x1, y1, 0), new Point3d(x2, y2, 0));
        }
        public static Line DrawLineLazy(Point2d start, Point2d end)
        {
            return DrawLineLazy(start.ToPoint3d(), end.ToPoint3d());
        }
        public static Line DrawLineLazy(Point3d start, Point3d end)
        {
            var line = new Line() { StartPoint = start, EndPoint = end };
            DrawingQueue.Enqueue(adb =>
            {
                adb.ModelSpace.Add(line);
            });
            return line;
        }
        public static Line DrawTextAndLinesLazy(DBText t1, DBText t2, double extH, double extV)
        {
            var r1 = GeoAlgorithm.GetBoundaryRect(t1);
            var r2 = GeoAlgorithm.GetBoundaryRect(t2);
            var pts = new Point3d[]
            {
                r1.LeftButtom.OffsetXY(-extH, -extV).ToPoint3d(), r1.RightButtom.OffsetXY(extH, -extV).ToPoint3d(),
                r2.LeftButtom.OffsetXY(-extH, -extV).ToPoint3d(), r2.RightButtom.OffsetXY(extH, -extV).ToPoint3d(),
            };
            var r = GeoAlgorithm.ToGRect(pts);
            var pt1 = GeoAlgorithm.MidPoint(r.LeftTop, r.LeftButtom).ToPoint3d();
            var pt2 = GeoAlgorithm.MidPoint(r.RightTop, r.RightButtom).ToPoint3d();
            return DrawLineLazy(pt1, pt2);
        }

        public static Line DrawTextUnderlineLazy(DBText t, double extH, double extV)
        {
            var r = GeoAlgorithm.GetBoundaryRect(t);
            return DrawLineLazy(r.LeftButtom.OffsetXY(-extH, -extV).ToPoint3d(), r.RightButtom.OffsetXY(extH, -extV).ToPoint3d());
        }
        public static DBText DrawTextLazy(string text, double height, Point2d position) => DrawTextLazy(text, height, position.ToPoint3d());
        public static DBText DrawTextLazy(string text, Point2d position)
        {
            return DrawTextLazy(text, position.ToPoint3d());
        }
        public static DBText DrawTextLazy(string text, Point3d position)
        {
            return DrawTextLazy(text, 100, position);
        }
        public static DBText DrawTextLazy(string text, double height, Point3d position, Action<DBText> cb = null)
        {
            var dbText = new DBText
            {
                TextString = text,
                Position = position,
                Height = height,
            };
            DrawingQueue.Enqueue(adb =>
            {
                adb.ModelSpace.Add(dbText);
                cb?.Invoke(dbText);
            });
            return dbText;
        }
        public static List<ObjectId> DrawProfile(List<Curve> curves, string LayerName, Color color = null)
        {
            var objectIds = new List<ObjectId>();
            if (curves == null || curves.Count == 0)
                return objectIds;

            using (var db = AcadDatabase.Active())
            {
                if (color == null)
                    CreateLayer(LayerName, Color.FromRgb(255, 0, 0));
                else
                    CreateLayer(LayerName, color);

                foreach (var curve in curves)
                {
                    var clone = curve.Clone() as Curve;
                    clone.Layer = LayerName;
                    objectIds.Add(db.ModelSpace.Add(clone));
                }
            }

            return objectIds;
        }
        public static Polyline DrawLineString(LineString lineString)
        {
            var points = new Point3d[lineString.NumPoints];
            for (int i = 0; i < lineString.NumPoints; i++)
            {
                var pt = lineString.GetPointN(i);
                var p = new Point3d(pt.X, pt.Y, pt.Z);
                points[i] = p;
            }
            return DrawPolyLineLazy(points);
        }
        public static Polyline DrawLinearRing(LinearRing ring)
        {
            var points = new Point3d[ring.NumPoints];
            for (int i = 0; i < ring.NumPoints; i++)
            {
                var pt = ring.GetPointN(i);
                var p = new Point3d(pt.X, pt.Y, pt.Z);
                points[i] = p;
            }
            return DrawPolyLineLazy(points);
        }

        /// <summary>
        /// 创建新的图层
        /// </summary>
        /// <param name="allLayers"></param>
        /// <param name="aimLayer"></param>
        public static void CreateLayer(string aimLayer, Color color)
        {
            LayerTableRecord layerRecord = null;
            using (var db = AcadDatabase.Active())
            {
                foreach (var layer in db.Layers)
                {
                    if (layer.Name.Equals(aimLayer))
                    {
                        layerRecord = db.Layers.Element(aimLayer);
                        break;
                    }
                }

                // 创建新的图层
                if (layerRecord == null)
                {
                    layerRecord = db.Layers.Create(aimLayer);
                    layerRecord.Color = color;
                    layerRecord.IsPlottable = false;
                }
            }
        }
    }
    public static class ThBlock
    {
        public static bool IsSupportedBlock(BlockTableRecord blockTableRecord)
        {
            // 暂时不支持动态块，外部参照，覆盖
            if (blockTableRecord.IsDynamicBlock) return false;
            // 忽略图纸空间和匿名块
            if (blockTableRecord.IsLayout || blockTableRecord.IsAnonymous) return false;
            // 忽略不可“炸开”的块
            if (!blockTableRecord.Explodable) return false;
            return true;
        }
    }
}
