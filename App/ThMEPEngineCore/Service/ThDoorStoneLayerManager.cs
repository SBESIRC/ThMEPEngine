using Linq2Acad;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Service
{
    public class ThDoorStoneLayerManager: ThDbLayerManager
    {
        public static List<string> XrefLayers(Database database)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                return acadDatabase.Layers
                    .Where(o => IsDoorStoneLayer(o.Name))
                    .Select(o => o.Name)
                    .ToList();
            }
        }

        private static bool IsDoorStoneLayer(string name)
        {
            string pattern = @"(DEFPOINTS-)\d+";
            string newName = ThStructureUtils.OriginalFromXref(name).ToUpper();
            return Regex.IsMatch(newName.ToUpper(), pattern);
        }
    }
}
