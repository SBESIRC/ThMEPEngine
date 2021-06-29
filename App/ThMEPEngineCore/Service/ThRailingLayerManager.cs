using Linq2Acad;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Service
{
    public class ThRailingLayerManager: ThDbLayerManager
    {
        public static List<string> CurveXrefLayers(Database database)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                return acadDatabase.Layers
                    .Where(o => IsVisibleLayer(o))
                    .Where(o => IsRailingLayer(o.Name))
                    .Select(o => o.Name)
                    .ToList();
            }
        }

        private static bool IsRailingLayer(string name)
        {
            string[] patterns = ThStructureUtils.OriginalFromXref(name).ToUpper().Split('-').Reverse().ToArray();
            if (patterns.Count() < 2)
            {
                return false;
            }
            return patterns[0] == "HDWR" && patterns[1] == "AE";
        }
    }
}
