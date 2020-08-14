using Linq2Acad;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Service
{
    public class ThShearWallLayerManager
    {
        /// <summary>
        /// 获取Xref下的Beam图层
        /// </summary>
        /// <param name="database"></param>
        /// <returns></returns>
        public static List<string> GeometryXrefLayers(Database database)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                var layers = new List<string>();
                acadDatabase.Layers
                    .Where(o =>
                    {
                        var layerName = ThStructureUtils.OriginalFromXref(o.Name).ToUpper();
                        if (!layerName.Contains("S_WALL"))
                        {
                            return false;
                        }
                        // 若图层名包含S_WALL，
                        // 则继续判断是否包含TEXT
                        if (layerName.Contains("HATCH"))
                        {
                            return false;
                        }
                        // 返回指定的图层
                        return true;
                    }).ForEachDbObject(o => layers.Add(o.Name));
                return layers;
            }
        }
    }
}
