using Linq2Acad;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Service
{
    public class ThColumnLayerManager
    {
        public static List<string> GeometryLayers(Database database)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                var layers = new List<string>();
                acadDatabase.Layers.Where(o =>
                {
                    // 图层名未包含S_BEAM
                    if (!o.Name.ToUpper().Contains("S_COLU"))
                    {
                        return false;
                    }

                    // 若图层名包含S_BEAM，
                    // 则继续判断是否包含HACH
                    if (o.Name.ToUpper().Contains("HACH"))
                    {
                        return false;
                    }

                    // 继续判断是否包含CAP
                    if (o.Name.ToUpper().Contains("CAP"))
                    {
                        return false;
                    }

                    // 继续判断是否包含TEXT
                    if (o.Name.ToUpper().Contains("TEXT"))
                    {
                        return false;
                    }

                    // 继续判断是否包含DIMS
                    if (o.Name.ToUpper().Contains("DIMS"))
                    {
                        return false;
                    }

                    // 返回指定的图层
                    return true;
                }).ForEachDbObject(o => layers.Add(o.Name));
                return layers;
            }
        }
        public static List<string> GeometryXrefLayers(Database database)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                var layers = new List<string>();
                acadDatabase.Layers
                    .Where(o =>
                    {
                        var layerName = ThStructureUtils.OriginalFromXref(o.Name).ToUpper();

                        // 图层名未包含S_BEAM
                        if (!layerName.Contains("S_COLU"))
                        {
                            return false;
                        }

                        // 若图层名包含S_BEAM，
                        // 则继续判断是否包含HACH
                        if (layerName.Contains("HACH"))
                        {
                            return false;
                        }

                        // 继续判断是否包含CAP
                        if (layerName.Contains("CAP"))
                        {
                            return false;
                        }

                        // 继续判断是否包含TEXT
                        if (layerName.Contains("TEXT"))
                        {
                            return false;
                        }

                        // 继续判断是否包含DIMS
                        if (layerName.Contains("DIMS"))
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
