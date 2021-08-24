using Linq2Acad;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Service
{
    public class ThDbLayerManager
    {
        public static List<string> Layers(Database database)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                return acadDatabase.Layers
                    .Where(o => IsVisibleLayer(o))
                    .Select(o => o.Name)
                    .ToList();
            }
        }

        protected static bool IsVisibleLayer(LayerTableRecord layerTableRecord)
        {
            return !(layerTableRecord.IsOff || layerTableRecord.IsFrozen);
        }
    }
}
