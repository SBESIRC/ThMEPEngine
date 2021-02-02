using Linq2Acad;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Service
{
    public class ThCondensePipeLayerManager
    {
        public static List<string> XrefLayers(Database database)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                return acadDatabase.Layers
                    .Where(o => IsCondensePipeLayerName(o.Name))
                    .Select(o => o.Name)
                    .ToList();
            }
        }
        private static bool IsCondensePipeLayerName(string name)
        {
            return true;
            //string[] patterns = ThStructureUtils.OriginalFromXref(name).ToUpper().Split('-').Reverse().ToArray();
            //if (patterns.Count() < 3)
            //{
            //    return false;
            //}
            //return (patterns[0] == "TOLT") && (patterns[1] == "EQPM") && (patterns[2] == "AE");
        }
        public static bool IsCondensePipeBlockName(string name)
        {
            string[] patterns = ThStructureUtils.OriginalFromXref(name).ToUpper().Split('-').Reverse().ToArray();
            if (patterns.Count() < 3)
            {
                return false;
            }
            return ((patterns[0] == "S") && (patterns[1] == "P") && (patterns[2] == "H")|| (patterns[0] == "3") && (patterns[1] == "PIPE") && (patterns[2] == "W"));
        }
    }
}
