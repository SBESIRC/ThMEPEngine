using Linq2Acad;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Service
{
    public class ThRoofRainPipeLayerManager
    {
        public static List<string> XrefLayers(Database database)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                return acadDatabase.Layers
                    .Where(o => IsRoofPipeLayerName(o.Name))
                    .Select(o => o.Name)
                    .ToList();
            }
        }
        private static bool IsRoofPipeLayerName(string name)
        {
            return true;
            //string[] patterns = ThStructureUtils.OriginalFromXref(name).ToUpper().Split('-').Reverse().ToArray();
            //if (patterns.Count() < 3)
            //{
            //    return false;
            //}
            //return (patterns[0] == "TOLT") && (patterns[1] == "EQPM") && (patterns[2] == "AE");
        }
        public static bool IsRoofPipeBlockName(string name)
        {
            string[] patterns = ThStructureUtils.OriginalFromXref(name).ToUpper().Split('-').Reverse().ToArray();
            if (patterns.Count() < 3)
            {
                return false;
            }
            return (patterns[0] == "1") && (patterns[1] == "PIPE") && (patterns[2] == "W");
        }
    }
}