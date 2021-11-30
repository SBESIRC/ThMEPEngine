using Linq2Acad;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Service;

namespace ThMEPStructure.GirderConnect.ConnectMainBeam.Utils
{
    internal class ThMainBuildingLayerManager : ThDbLayerManager
    {
        public static List<string> HatchXrefLayers(Database database)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                return acadDatabase.Layers
                    .Where(o => IsVisibleLayer(o))
                    .Where(o => IsMainBuildingHatchLayer(o.Name))
                    .Select(o => o.Name)
                    .ToList();
            }
        }

        private static bool IsMainBuildingHatchLayer(string name)
        {
            string layer = ThStructureUtils.OriginalFromXref(name);
            //return layer.EndsWith("主楼填充") || layer.ToUpper().EndsWith("S_WALL_DETL");
            return  layer.EndsWith("S_WALL_DETL") || layer.EndsWith("主楼填充");
        }
    }
}
