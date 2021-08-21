using Linq2Acad;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Service
{
    public class ThStructureShearWallLayerManager: ThDbLayerManager
    {
        public static List<string> HatchXrefLayers(Database database)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                return acadDatabase.Layers
                    .Where(o => IsVisibleLayer(o))
                    .Where(o => IsShearWallHatchLayer(o.Name))
                    .Select(o => o.Name)
                    .ToList();
            }
        }

        private static bool IsShearWallHatchLayer(string name)
        {
            string[] patterns = ThStructureUtils.OriginalFromXref(name).ToUpper().Split('_').Reverse().ToArray();
            if (patterns.Count() < 3)
            {
                return false;
            }
            return (patterns[0] == "HACH") && (patterns[1] == "WALL") && (patterns[2] == "S");
        }
    }
}
