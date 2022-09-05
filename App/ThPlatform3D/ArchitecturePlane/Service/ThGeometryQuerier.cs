using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.Model;

namespace ThPlatform3D.ArchitecturePlane.Service
{
    internal static class ThGeometryQuerier
    {
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
    }
}
