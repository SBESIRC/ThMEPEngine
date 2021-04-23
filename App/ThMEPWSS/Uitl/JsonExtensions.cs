using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using ThCADExtension;
using ThMEPWSS.Uitl;

namespace ThMEPWSS.JsonExtensionsNs
{
    public static class LinqExtensions
    {
        public static void AddRange<T>(this HashSet<T> source,IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                source.Add(item);
            }
        }
        public static KeyValuePair<K,V> ToKV<K,V>(this Tuple<K,V> t)
        {
            return new KeyValuePair<K, V>(t.Item1, t.Item2);
        }

    }
    public static class JsonExtensions
    {
        public static string ToJson(this ThWGRect rect)
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
        public static ThWGRect JsonToThWGRect(this string json)
        {
            var ja = JsonConvert.DeserializeObject<JArray>(json);
            return new ThWGRect(ja[0].ToObject<double>(), ja[1].ToObject<double>(), ja[2].ToObject<double>(), ja[3].ToObject<double>());
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
        public static Dictionary<K, V> ToDict<T, K, V>(this IEnumerable<T> source, Func<T, V> getValue, Func<T, K> getKey)
        {
            var d = new Dictionary<K, V>();
            foreach (var item in source)
            {
                d[getKey(item)] = getValue(item);
            }
            return d;
        }
        public static Point3dCollection ToPoint3dCollection(this ThWGRect rect)
        {
            return (new Tuple<Point3d, Point3d>(rect.LeftTop.ToPoint3d(), rect.RightButtom.ToPoint3d())).ToPoint3dCollection();
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