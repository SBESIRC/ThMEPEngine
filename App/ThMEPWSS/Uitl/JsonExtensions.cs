﻿using Autodesk.AutoCAD.DatabaseServices;
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
using ThMEPWSS.DebugNs;
using ThCADCore.NTS;

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
        public static IEnumerable<T> Flattern<T>(this IEnumerable<KeyValuePair<T, T>> source)
        {
            foreach (var kv in source)
            {
                yield return kv.Key;
                yield return kv.Value;
            }
        }
        public static List<T> ToList<T>(this IList<T> source, IList<int> lis)
        {
            return lis.Select(li => source[li]).ToList();
        }
        public static IEnumerable<int> SelectInts<T>(this IList<T> source, Func<T, bool> f)
        {
            for (int i = 0; i < source.Count; i++)
            {
                if (f(source[i])) yield return i;
            }
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
        public static GRect ToGRect(this Extents3d extents3D)
        {
            return new GRect(extents3D.MinPoint, extents3D.MaxPoint);
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
            if (extents3D is Extents3d _ext) return new GRect(_ext.MinPoint, _ext.MaxPoint);
            return default;
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
    public static class CADExtensions
    {
        public static GRect ToGRect(this Geometry geo)
        {
            var env=geo.EnvelopeInternal;
            return new GRect(env.MinX, env.MinY, env.MaxX, env.MaxY);
        }
        public static LinearRing ToLinearRing(this GRect r)
        {
            return new LinearRing(GeoNTSConvertion.ConvertToCoordinateArray(r));
        }
        public static LineString ToLineString(this GLineSegment seg)
        {
            var points = new Coordinate[]
            {
                seg.StartPoint.ToNTSCoordinate(),
                seg.EndPoint.ToNTSCoordinate(),
            };
            return ThCADCoreNTSService.Instance.GeometryFactory.CreateLineString(points.ToArray());
        }
        public static NetTopologySuite.Geometries.Prepared.IPreparedGeometry ToIPreparedGeometry(this Geometry geo)
        {
            return ThCADCoreNTSService.Instance.PreparedGeometryFactory.Create(geo);
        }
        public static Geometry Buffer(this GLineSegment seg,double distance)
        {
            return seg.ToLineString().Buffer(distance,NetTopologySuite.Operation.Buffer.EndCapStyle.Flat);
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
        public static Point3dCollection ToPoint3dCollection(this GRect r)
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
        public static string GetCustomPropertyStrValue(this Entity e, string key)
        {
            var d = ToDict(e.ToDataItem().CustomProperties);
            d.TryGetValue(key, out object o);
            return o?.ToString();
        }
        public static string GetAttributesStrValue(this Entity e, string key)
        {
            if (!(e is BlockReference)) return null;
            var d = e.ToDataItem().Attributes;
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
        public static Polyline ClonePolyline(this Polyline pl)
        {
            var r = new Polyline();
            for (int i = 0; i < pl.NumberOfVertices; i++) r.AddVertexAt(i, pl.GetPoint2dAt(i), 0, 0, 0);
            r.Closed = pl.Closed;
            return r;
        }
        public static Point3dCollection ToPoint3dCollection(this Polyline pl)
        {
            var r = new Point3dCollection();
            for (int i = 0; i < pl.NumberOfVertices; i++) r.Add(pl.GetPoint3dAt(i));
            return r;
        }
        public static DBObjectCollection ExplodeToDBObjectCollection(this Entity ent)
        {
            var entitySet = new DBObjectCollection();
            ent.Explode(entitySet);
            return entitySet;
        }
        public static DBObjectCollection ExplodeBlockRef(this BlockReference blk)
        {
            var entitySet = new DBObjectCollection();
            void explode(Entity ent)
            {
                var obl = new DBObjectCollection();
                ent.Explode(obl);
                foreach (Entity e in obl)
                {
                    if (e is BlockReference br)
                    {
                        explode(br);
                    }
                    else
                    {
                        entitySet.Add(e);
                    }
                }
            }
            explode(blk);
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
    }
}