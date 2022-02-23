using Linq2Acad;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Service
{
    public class ThFireCompartmentLayerManager
    {
        public static List<string> CurveModelSpaceLayers(Database database)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                return acadDatabase.Layers
                    .Where(o => IsVisibleLayer(o))
                    .Where(o => IsModelSpaceFireCompartmentLayer(o.Name))
                    .Select(o => o.Name)
                    .ToList();
            }
        }
        private static bool IsVisibleLayer(LayerTableRecord layerTableRecord)
        {
            return !(layerTableRecord.IsOff || layerTableRecord.IsFrozen);
        }
        private static bool IsModelSpaceFireCompartmentLayer(string name)
        {
            string[] patterns = ThStructureUtils.OriginalFromXref(name).
                ToUpper().Split('-').Reverse().ToArray();
            if (patterns.Count() < 2)
            {
                return false;
            }
            return patterns[1] == "AI" && patterns[0] == "防火分区";
        }
    }
}
