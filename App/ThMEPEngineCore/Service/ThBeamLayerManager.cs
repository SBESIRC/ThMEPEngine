using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPEngineCore.Service
{
    public class ThBeamLayerManager
    {
        public static List<string> GeometryLayers(Database database)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                var layers = new List<string>();
                acadDatabase.Layers.Where(o =>
                {
                    // 图层名未包含S_BEAM
                    if (!o.Name.ToUpper().Contains("S_BEAM"))
                    {
                        return false;
                    }

                    // 若图层名包含S_BEAM，
                    // 则继续判断是否包含TEXT
                    if (o.Name.ToUpper().Contains("TEXT"))
                    {
                        return false;
                    }

                    // 继续判断是否包含REIN
                    if (o.Name.ToUpper().Contains("REIN"))
                    {
                        return false;
                    }

                    // 返回指定的图层
                    return true;
                }).ForEachDbObject(o => layers.Add(o.Name));
                return layers;
            }
        }

        public static List<string> AnnotationLayers(Database database)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                var layers = new List<string>();
                acadDatabase.Layers.Where(o =>
                {
                    if (o.Name.ToUpper().Contains("S_BEAM_TEXT"))
                    {
                        return true;
                    }

                    if (o.Name.ToUpper().Contains("S_BEAM_SECD_TEXT"))
                    {
                        return true;
                    }

                    if (o.Name.ToUpper().Contains("S_BEAM_XL_TEXT"))
                    {
                        return true;
                    }

                    if (o.Name.ToUpper().Contains("S_BEAM_WALL_TEXT"))
                    {
                        return true;
                    }
                    return false;
                }).ForEachDbObject(o => layers.Add(o.Name));
                return layers;
            }
        }
    }
}
