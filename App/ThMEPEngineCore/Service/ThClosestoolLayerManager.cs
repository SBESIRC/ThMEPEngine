using Linq2Acad;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Service
{
    public class ThClosestoolLayerManager
    {
        public static List<string> XrefLayers(Database database)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                return acadDatabase.Layers
                    .Where(o => IsVisibleLayer(o))
                    .Where(o => IsClosetoolLayerName(o.Name))
                    .Select(o => o.Name)
                    .ToList();
            }
        }
        private static bool IsVisibleLayer(LayerTableRecord layerTableRecord)
        {
            return !(layerTableRecord.IsOff || layerTableRecord.IsFrozen);
        }
        private static bool IsClosetoolLayerName(string name)
        {
            return true;
            //string[] patterns = ThStructureUtils.OriginalFromXref(name).ToUpper().Split('-').Reverse().ToArray();
            //if (patterns.Count() < 3)
            //{
            //    return false;
            //}
            //return (patterns[0] == "TOLT") && (patterns[1] == "EQPM") && (patterns[2] == "AE");
        }
        public static bool IsClosetoolBlockName(string name)
        {
            string[] patterns = ThStructureUtils.OriginalFromXref(name).ToUpper().Split('-').Reverse().ToArray();
            if (patterns.Count() < 3)
            {
                return false;
            }
            return (patterns[0] == "5") && (patterns[1] == "TOILET") && (patterns[2] == "A");
        }
    }
}
