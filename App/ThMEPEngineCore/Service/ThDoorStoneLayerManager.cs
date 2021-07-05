using Linq2Acad;
using System.Linq;
using System.Collections.Generic;
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
                    .Where(o => IsVisibleLayer(o))
                    .Where(o => IsDoorStoneLayer(o.Name))
                    .Select(o => o.Name)
                    .ToList();
            }
        }

        private static bool IsDoorStoneLayer(string name)
        {
            string endChars = "DEFPOINTS-1";
            string newName = ThStructureUtils.OriginalFromXref(name).ToUpper();
            int index = newName.LastIndexOf(endChars);
            if (index >= 0 && (index+endChars.Length)==newName.Length)
            {
                return true;
            }
            return false;
        }
    }
}
