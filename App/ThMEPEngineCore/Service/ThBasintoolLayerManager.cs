using System;
using Linq2Acad;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;


namespace ThMEPEngineCore.Service
{
    public class ThBasintoolLayerManager
    {
        public static List<string> XrefLayers(Database database)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                return acadDatabase.Layers
                    .Where(o => IsBasintoolLayerName(o.Name))
                    .Select(o => o.Name)
                    .ToList();
            }
        }
        private static bool IsBasintoolLayerName(string name)
        {
            return true;
        }
        public static bool IsBasintoolBlockName(string name)
        {
            string[] patterns = ThStructureUtils.OriginalFromXref(name).ToUpper().Split('-').Reverse().ToArray();
            if (patterns.Count() < 3)
            {
                return false;
            }
            return (patterns[0] == "4") && (patterns[1] == "KITCHEN") && (patterns[2] == "A");
        }
    }
}
