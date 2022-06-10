using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using ThCADExtension;
using ThMEPWSS.Uitl;
using System.Linq;
using NetTopologySuite.Geometries;
using ThMEPWSS.ReleaseNs;
using ThCADCore.NTS;
using DotNetARX;
using ThMEPWSS.Assistant;
using NetTopologySuite.Operation.Buffer;

namespace ThMEPWSS.JsonExtensionsNs
{
    public static class LinqExtensions
    {
        public static IEnumerable<KeyValuePair<T, T>> Select<T>(this IList<KeyValuePair<int, int>> lis, IList<T> source)
        {
            return lis.Select(kv => new KeyValuePair<T, T>(source[kv.Key], source[kv.Value]));
        }
        public static IEnumerable<T> Select<T>(this IList<int> lis, IList<T> source)
        {
            return lis.Select(li => source[li]);
        }
        public static T TryGet<T>(this IList<T> source, int i, T dft = default(T))
        {
            if (0 <= i && i < source.Count) return source[i];
            return dft;
        }
        public static (bool, T) TryGetValue<T>(this IList<T> source, int i)
        {
            if (0 <= i && i < source.Count) return (true, source[i]);
            return (false, default);
        }
        public static IEnumerable<T> Flattern<T>(this IEnumerable<KeyValuePair<T, T>> source)
        {
            foreach (var kv in source)
            {
                yield return kv.Key;
                yield return kv.Value;
            }
        }
        public static List<T> ToList<T>(this IEnumerable<int> lis, IList<T> source, bool reverse = false)
        {
            if (reverse)
            {
                IList<int> lst = (lis as IList<int>) ?? lis.ToList();
                return Enumerable.Range(0, source.Count).Where(i => !lst.Contains(i)).Select(i => source[i]).ToList();
            }
            return lis.Select(li => source[li]).ToList();
        }
        public static List<T> ToList<T>(this bool[] flags, IList<T> std, bool inverse)
        {
            var ret = new List<T>(std.Count);
            if (inverse)
            {
                for (int i = 0; i < std.Count; i++)
                {
                    if (!flags[i])
                    {
                        ret.Add(std[i]);
                    }
                }
            }
            else
            {
                for (int i = 0; i < std.Count; i++)
                {
                    if (flags[i])
                    {
                        ret.Add(std[i]);
                    }
                }
            }
            return ret;
        }
        public static IEnumerable<int> Select<T>(this IEnumerable<T> source, List<T> std, bool fast = false)
        {
            IList<T> lst = (source as IList<T>) ?? source.ToList();
            if (fast)
            {
                for (int i = 0; i < lst.Count; i++)
                {
                    yield return std.BinarySearch(lst[i]);
                }
            }
            else
            {
                for (int i = 0; i < lst.Count; i++)
                {
                    yield return std.IndexOf(lst[i]);
                }
            }
        }
        public static T Last<T>(this IList<T> source, int index)
        {
            return source[source.Count - index];
        }
        public static Dictionary<T, int> ToCountDict<T>(this IEnumerable<T> source)
        {
            var d = new Dictionary<T, int>();
            foreach (var item in source)
            {
                d.TryGetValue(item, out int value);
                d[item] = value + 1;
            }
            return d;
        }
        public static bool SeqEqual<T>(this IList<T> source, IList<T> target)
        {
            return source == target || (source != null && target != null && source.Count == target.Count && source.SequenceEqual(target));
        }
        public static bool SeqEqual<T>(this IEnumerable<T> source, IEnumerable<T> target)
        {
            return source == target || (source != null && target != null && source.SequenceEqual(target));
        }
        public static IEnumerable<int> SelectInts<T>(this IList<T> source, Func<T, bool> f)
        {
            for (int i = 0; i < source.Count; i++)
            {
                if (f(source[i])) yield return i;
            }
        }
        public static List<T> ToList<T>(this IEnumerable<T> source, int cap)
        {
            var lst = new List<T>(cap);
            foreach (var item in source) lst.Add(item);
            return lst;
        }
        public static List<V> SelectList<K, V>(this IList<K> source, Func<K, V> f)
        {
            return source.Select(f).ToList(source.Count);
        }
        public static string JoinN(this IEnumerable<string> strs)
        {
            return strs.JoinWith("\n");
        }
        public static string JoinWith(this IEnumerable<string> strs, string s)
        {
            return string.Join(s, strs);
        }
        public static void AddRange<T>(this HashSet<T> source, IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                source.Add(item);
            }
        }
        public static KeyValuePair<K, V> ToKV<K, V>(this Tuple<K, V> t)
        {
            return new KeyValuePair<K, V>(t.Item1, t.Item2);
        }
        public static GRect ToGRect(this Extents2d extents2D)
        {
            return new GRect(extents2D.MinPoint, extents2D.MaxPoint);
        }
        public static GRect ToGRect(this Extents3d extents3D)
        {
            return new GRect(extents3D.MinPoint, extents3D.MaxPoint);
        }
        public static Envelope ToEnvelope(this Extents3d extents3D)
        {
            return new Envelope(extents3D.MinPoint.X, extents3D.MaxPoint.X, extents3D.MinPoint.Y, extents3D.MaxPoint.Y);
        }
        public static Envelope ToEnvolope(this GRect r)
        {
            return new Envelope(r.MinX, r.MaxX, r.MinY, r.MaxY);
        }
        public static GRect ToGRect(this Extents3d? extents3D, double radius)
        {
            if (extents3D is Extents3d ext)
            {
                var center = GeoAlgorithm.MidPoint(ext.MinPoint, ext.MaxPoint);
                return GRect.Create(center, radius);
            }
            return default;
        }
        public static GRect ToGRect(this Extents3d? extents3D)
        {
            if (extents3D.HasValue) return new GRect(extents3D.Value.MinPoint, extents3D.Value.MaxPoint);
            return default;
        }
        public static IEnumerable<Point2d> YieldPoints(this IEnumerable<GLineSegment> segs)
        {
            foreach (var seg in segs)
            {
                yield return seg.StartPoint;
                yield return seg.EndPoint;
            }
        }
        public static int MaxOrZero(this IEnumerable<int> source)
        {
            var max = 0;
            foreach (var i in source)
            {
                if (i > max)
                {
                    max = i;
                }
            }
            return max;
        }
    }
    public static class JsonExtensions
    {
        public static string ToJson(this GRect rect)
        {
            return $"[{rect.MinX},{rect.MinY},{rect.MaxX},{rect.MaxY}]";
        }
        public static string ToJson(this Point3d p) => $"{{x:{p.X},y:{p.Y},z:{p.Z}}}";
        public static Point3d JsonToPoint3d(this string json)
        {
            var jo = JsonConvert.DeserializeObject<JObject>(json);
            return new Point3d(jo["x"].ToObject<double>(), jo["y"].ToObject<double>(), jo["z"].ToObject<double>());
        }
        public static string ToJson(this Point2d p) => $"{{x:{p.X},y:{p.Y}}}";
        public static Point2d JsonToPoint2d(this string json)
        {
            var jo = JsonConvert.DeserializeObject<JObject>(json);
            return new Point2d(jo["x"].ToObject<double>(), jo["y"].ToObject<double>());
        }
        public static GRect JsonToGRect(this string json)
        {
            var ja = JsonConvert.DeserializeObject<JArray>(json);
            return new GRect(ja[0].ToObject<double>(), ja[1].ToObject<double>(), ja[2].ToObject<double>(), ja[3].ToObject<double>());
        }
        public static string ToJson(this object obj)
        {
            return JsonConvert.SerializeObject(obj);
        }
        public static T FromJson<T>(this string json)
        {
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}
namespace ThMEPWSS.CADExtensionsNs
{
    public static class NTSExtensions
    {
        public static Point3d ToPoint3d(this Coordinate c) => new(c.X, c.Y, 0);
        public static Point3d ToPoint3d(this Point p) => new(p.X, p.Y, 0);
        public static Point2d ToPoint2d(this Coordinate c) => new(c.X, c.Y);
        public static Point2d ToPoint2d(this Point p) => new(p.X, p.Y);
        public static Point ToNTSPoint(this Point3d p) => new(p.X, p.Y);
        public static Point ToNTSPoint(this Point3d p, object tag) => new(p.X, p.Y) { UserData = tag };
        public static Point ToNTSPoint(this Point2d p) => new(p.X, p.Y);
        public static Point ToNTSPoint(this Point2d p, object tag) => new(p.X, p.Y) { UserData = tag };
        public static Coordinate ToNTSCoordinate(this Point3d p) => new(p.X, p.Y);
        public static Coordinate ToNTSCoordinate(this Point2d p) => new(p.X, p.Y);
    }
    public static class CADExtensions
    {
        public static IEnumerable<GLineSegment> ToGLineSegments(this LineString lineString)
        {
            for (int i = 1; i < lineString.NumPoints; i++)
            {
                var seg = new GLineSegment(lineString.Coordinates[i - 1].ToPoint2d(), lineString.Coordinates[i].ToPoint2d());
                if (seg.IsValid) yield return seg;
            }
        }
        public static Point2d GetCenter(this Geometry geo) => geo.ToGRect().Center;
        public static GArc ToGArc(this Arc arc) => new(arc.Center.X, arc.Center.Y, arc.Radius, arc.StartAngle, arc.EndAngle, arc.Normal.DotProduct(Vector3d.ZAxis) < 0);
        public static GRect ToGRect(this Geometry geo)
        {
            var env = geo.EnvelopeInternal;
            return new GRect(env.MinX, env.MinY, env.MaxX, env.MaxY);
        }
        public static LinearRing ToLinearRing(this GRect r)
        {
            return new LinearRing(GeoNTSConvertion.ConvertToCoordinateArray(r));
        }
        public static LinearRing ToLinearRing2(this GRect r, Matrix3d matrix)
        {
            if (r.IsNull) return null;
            if (!r.IsValid) return null;
            if (matrix == Matrix3d.Identity) return r.ToLinearRing();
            var p1 = r.LeftTop.ToPoint3d().TransformBy(matrix).ToNTSCoordinate();
            var p2 = r.LeftButtom.ToPoint3d().TransformBy(matrix).ToNTSCoordinate();
            var p3 = r.RightButtom.ToPoint3d().TransformBy(matrix).ToNTSCoordinate();
            var p4 = r.RightTop.ToPoint3d().TransformBy(matrix).ToNTSCoordinate();
            return new LinearRing(new Coordinate[] { p1, p2, p3, p4, p1 });
        }
        public static Polygon ToPolygon(this GRect r,Matrix3d matrix)
        {
            var lr = r.ToLinearRing2(matrix);
            if (lr is null) return null;
            return new Polygon(lr);
        }
        public static LineString ToLineString(this IList<Point3d> pts)
        {
            var points = pts.Cast<Point3d>().Select(pt => pt.ToNTSCoordinate()).ToArray();
            return ThCADCoreNTSService.Instance.GeometryFactory.CreateLineString(points);
        }
        public static LinearRing ToLinearRing(this IList<Point3d> pts)
        {
            var points = pts.Cast<Point3d>().Select(pt => pt.ToNTSCoordinate()).ToArray();
            return ThCADCoreNTSService.Instance.GeometryFactory.CreateLinearRing(points);
        }
        public static GCircle ToGCircle(this GRect r, bool larger)
        {
            return larger ? new GCircle(r.Center, r.OuterRadius) : new GCircle(r.Center, r.InnerRadius);
        }
        public static Point2d TransformBy(this Point2d pt, Point2d basePt)
        {
            return pt + basePt.ToVector2d();
        }
        public static Point2d TransformBy(this Point2d pt, Point3d basePt)
        {
            return pt + basePt.ToPoint2d().ToVector2d();
        }
        public static GRect ToGRect(this Point2d pt, double ext)
        {
            return GRect.Create(pt, ext);
        }
        public static GRect ToGRect(this Point3d pt, double ext)
        {
            return GRect.Create(pt, ext);
        }
        public static GCircle ToGCircle(this Point2d pt, double radius)
        {
            return new GCircle(pt, radius);
        }
        public static Polygon ToPolygon(this GRect r)
        {
            return new Polygon(r.ToLinearRing());
        }
        public static List<Geometry> ToGeometryList(this IEnumerable<Geometry> source) => source.ToList();
        public static Polygon ToCirclePolygon(this GCircle circle, int numPoints, bool larger = true)
        {
            return GeoFac.CreateCirclePolygon(center: circle.Center, radius: circle.Radius, numPoints: numPoints, larger: larger);
        }
        public static LineString ToLineString(this GLineSegment seg)
        {
            var points = new Coordinate[]
            {
                seg.StartPoint.ToNTSCoordinate(),
                seg.EndPoint.ToNTSCoordinate(),
            };
            return ThCADCoreNTSService.Instance.GeometryFactory.CreateLineString(points);
        }
        public static NetTopologySuite.Geometries.Prepared.IPreparedGeometry ToIPreparedGeometry(this Geometry geo)
        {
            return ThCADCoreNTSService.Instance.PreparedGeometryFactory.Create(geo);
        }
        public static Geometry Buffer(this GLineSegment seg, double distance, EndCapStyle endCapStyle = EndCapStyle.Flat)
        {
            return seg.ToLineString().Buffer(distance, endCapStyle);
        }
        public static GRect ToGRect(this Envelope env)
        {
            return new GRect(env.MinX, env.MinY, env.MaxX, env.MaxY);
        }
        public static Polyline ToCadPolyline(this GRect r)
        {
            var pline = new Polyline() { Closed = true };
            var pts = new Point2dCollection() { new Point2d(r.MinX, r.MaxY), new Point2d(r.MaxX, r.MaxY), new Point2d(r.MaxX, r.MinY), new Point2d(r.MinX, r.MinY), };
            for (int i = 0; i < pts.Count; i++)
            {
                pline.AddVertexAt(i, pts[i], 0, 0, 0);
            }
            return pline;
        }
        public static Polyline CreatePolygon(this GRect r, int num)
        {
            return Pipe.Service.PolylineTools.CreatePolygon(r.Center, num, r.Radius);
        }
        public static Point3dCollection ToPt3dCollection(this GRect r)
        {
            return new Point3dCollection() { new Point3d(r.MinX, r.MinY, 0), new Point3d(r.MinX, r.MaxY, 0), new Point3d(r.MaxX, r.MaxY, 0), new Point3d(r.MaxX, r.MinY, 0) };
        }

        public static Line ToCadLine(this GLineSegment lineSegment)
        {
            return new Line() { StartPoint = lineSegment.StartPoint.ToPoint3d(), EndPoint = lineSegment.EndPoint.ToPoint3d() };
        }
        public static Dictionary<K, V> ToDict<T, K, V>(this IEnumerable<T> source, Func<T, V> getValue, Func<T, K> getKey)
        {
            var d = new Dictionary<K, V>();
            foreach (var item in source)
            {
                d[getKey(item)] = getValue(item);
            }
            return d;
        }
        public static Point3dCollection ToPoint3dCollection(this Tuple<Point3d, Point3d> input)
        {
            var points = new Point3dCollection();
            points.Add(input.Item1);
            points.Add(new Point3d(input.Item1.X, input.Item2.Y, 0));
            points.Add(input.Item2);
            points.Add(new Point3d(input.Item2.X, input.Item1.Y, 0));
            return points;
        }
        public static DBObject[] ToArray(this DBObjectCollection colle)
        {
            var arr = new DBObject[colle.Count];
            System.Collections.IList list = colle;
            for (int i = 0; i < list.Count; i++)
            {
                var @object = (DBObject)list[i];
                arr[i] = @object;
            }
            return arr;
        }
        public static string GetCustomPropertyStrValue(this BlockReference e, string key)
        {
            if (!e.ObjectId.IsValid) return null;
            var d = ToDict(e.ObjectId.GetDynProperties());
            d.TryGetValue(key, out object o);
            return o?.ToString();
        }
        public static string GetAttributesStrValue(this BlockReference br, string key)
        {
            if (!br.ObjectId.IsValid) return null;
            var d = br.ObjectId.GetAttributesInBlockReference();
            d.TryGetValue(key, out string ret);
            return ret;
        }
        public static ThBlockReferenceData ToDataItem(this Entity ent)
        {
            var d = new ThBlockReferenceData(ent.ObjectId);
            return d;
        }

        public static List<DynamicBlockReferenceProperty> ToList(this DynamicBlockReferencePropertyCollection colle)
        {
            var ret = new List<DynamicBlockReferenceProperty>();
            foreach (DynamicBlockReferenceProperty item in colle)
            {
                ret.Add(item);
            }
            return ret;
        }
        public static DBObjectCollection ExplodeToDBObjectCollection(this Entity ent)
        {
            var entitySet = new DBObjectCollection();
            ent.Explode(entitySet);
            return entitySet;
        }
        public static List<DBObject> ToDBObjectList(this DBObjectCollection colle)
        {
            var list = new List<DBObject>();
            foreach (DBObject obj in colle)
            {
                list.Add(obj);
            }
            return list;
        }
        public static Dictionary<string, object> ToDict(this DynamicBlockReferencePropertyCollection colle)
        {
            var ret = new Dictionary<string, object>();
            foreach (var p in colle.ToList())
            {
                ret[p.PropertyName] = p.Value;
            }
            return ret;
        }
        public static double DistanceToPoint(this Line thisLine, Point3d otherPoint)
        {
            double distance = 0.0;

            Point3d closestPoint = thisLine.GetClosestPointTo(otherPoint, false);
            distance = otherPoint.DistanceTo(closestPoint);

            return distance;
        }
        public static double PolyLineLength(this Polyline thisLine, double flag = 2000.0)
        {
            double length = 0.0;
            length = thisLine.Length + (thisLine.NumberOfVertices - 2) * flag;

            return length;
        }

        public static bool PointOnLine(this Line thisLine, Point3d pt, bool extent, double tolerance)
        {
            var ptNearest = thisLine.GetClosestPointTo(pt, extent);

            return (pt.DistanceTo(ptNearest) < tolerance);

        }
    }
}