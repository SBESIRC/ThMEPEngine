using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPWSS.Uitl;
using ThMEPWSS.CADExtensionsNs;

namespace ThMEPWSS.Pipe.Service
{
    public static class ThDataItemExtensions
    {
        public static GRect ToRect(this Point3dCollection colle)
        {
            if (colle.Count == 0) return default;
            var arr = colle.Cast<Point3d>().ToArray();
            var x1 = arr.Select(p => p.X).Min();
            var x2 = arr.Select(p => p.X).Max();
            var y1 = arr.Select(p => p.Y).Max();
            var y2 = arr.Select(p => p.Y).Min();
            return new GRect(x1, y1, x2, y2);
        }
        public static string GetBlockEffectiveName(this BlockReference br)
        {
            if (!br.ObjectId.IsValid) return null;
            return DotNetARX.BlockTools.GetBlockName(br.ObjectId.GetObject(OpenMode.ForRead) as BlockReference);
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
        public static Dictionary<string, object> ToDict(this DynamicBlockReferencePropertyCollection colle)
        {
            var ret = new Dictionary<string, object>();
            foreach (var p in colle.ToList())
            {
                ret[p.PropertyName] = p.Value;
            }
            return ret;
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
    }
}
