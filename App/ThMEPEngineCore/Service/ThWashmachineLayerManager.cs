using System;
using Linq2Acad;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Service
{
   public class ThWashMachineLayerManager
    {
        public static List<string> XrefLayers(Database database)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                return acadDatabase.Layers
                    .Where(o => IsWashmachineLayerName(o.Name))
                    .Select(o => o.Name)
                    .ToList();
            }
        }
        private static bool IsWashmachineLayerName(string name)
        {
                return true;
        }
        public static bool IsWashmachineBlockName(string name)
        {
             string[] patterns = ThStructureUtils.OriginalFromXref(name).ToUpper().Split('-').Reverse().ToArray();
             if (patterns.Count() < 3)
             {
                    return false;
             }
             return (patterns[0] == "9") && (patterns[1] == "TOILET") && (patterns[2] == "A");
        }
        
    }
}
