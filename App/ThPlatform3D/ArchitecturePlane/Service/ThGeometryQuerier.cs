using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.Model;

namespace ThPlatform3D.ArchitecturePlane.Service
{
    internal static class ThGeometryQuerier
    {
        public const string HiddenKanxianLTypeKeyWord1 = "stroke-dasharray";

        public static DBObjectCollection GetWalls(this List<ThGeometry> geos)
        {
            var results = new DBObjectCollection();
            geos.ForEach(o =>
            {
                string category = o.Properties.GetCategory();
                if (category == ThIfcCategoryManager.WallCategory)
                {
                    results.Add(o.Boundary);
                }
            });
            return results;
        }
        public static DBObjectCollection GetShearWalls(this List<ThGeometry> geos)
        {
            // 这个后期要调整
            var results = new DBObjectCollection();
            geos.ForEach(o =>
            {
                string category = o.Properties.GetCategory();
                if (category.Contains("混凝土"))
                {
                    results.Add(o.Boundary);
                }
            });
            return results;
        }

        public static List<ThGeometry> GetKanXians(this List<ThGeometry> geos)
        {
            return geos.Where(o => o.Properties.IsKanXian()).ToList();
        }

        public static List<ThGeometry> GetDuanMians(this List<ThGeometry> geos)
        {
            return geos.Where(o => o.Properties.IsDuanMian()).ToList();
        }

        public static List<ThGeometry> GetHiddenKanXians(this List<ThGeometry> geos)
        {
            return GetKanXians(geos).Where(o => o.Properties.IsHiddenKanxian()).ToList();
        }

        public static bool IsHiddenKanxian(this Dictionary<string,object> properties)
        {
            return properties.ContainsKey(HiddenKanxianLTypeKeyWord1);
        }
    }
}
