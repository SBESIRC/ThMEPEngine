using Linq2Acad;
using System.Linq;
using ThMEPEngineCore.Service;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.WaterWellPumpLayout.Service
{
    public class ThWWallLayerManager: ThDbLayerManager
    {
        public static List<string> XrefLayers(Database database)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                return acadDatabase.Layers
                    .Where(o => IsVisibleLayer(o))
                    .Where(o => IsWallLayerName(o.Name))
                    .Select(o => o.Name)
                    .ToList();
            }
        }
        private static bool IsWallLayerName(string name)
        {
            string layerName = ThStructureUtils.OriginalFromXref(name).ToUpper();
            // 
            if(name.Contains("AE-WALL"))
            {
                return true;
            }
            if (name.Contains("S_WALL") && !name.Contains("DETL"))
            {
                return true;
            }
            if (name.Contains("S_COLU") && !(name.Contains("TEXT") || name.Contains("DIMS")))
            {
                return true;
            }
            return false;
        }
    }

}
