using Linq2Acad;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Service
{
    public class ThRoomLayerManager
    {
        public static List<string> CurveXrefLayers(Database database)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                return acadDatabase.Layers
                    .Where(o => IsVisibleLayer(o))
                    .Where(o => IsRoomLayer(o.Name))
                    .Select(o => o.Name)
                    .ToList();
            }
        }
        public static List<string> CurveModelSpaceLayers(Database database)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                return acadDatabase.Layers
                    .Where(o => IsVisibleLayer(o))
                    .Where(o => IsModelSpaceRoomLayer(o.Name))
                    .Select(o => o.Name)
                    .ToList();
            }
        }

        private static bool IsVisibleLayer(LayerTableRecord layerTableRecord)
        {
            return !(layerTableRecord.IsOff || layerTableRecord.IsFrozen);
        }

        private static bool IsRoomLayer(string name)
        {
            string[] patterns = ThStructureUtils.OriginalFromXref(name).ToUpper().Split('_').Reverse().ToArray();
            if (patterns.Count() < 1)
            {
                return false;
            }
            return patterns[0] == "ROOM";
        }

        private static bool IsModelSpaceRoomLayer(string name)
        {
            string[] patterns = ThStructureUtils.OriginalFromXref(name).
                ToUpper().Split('-').Reverse().ToArray();
            if (patterns.Count() < 2)
            {
                return false;
            }
            return patterns[1] == "AI" && patterns[0] == "房间框线";
        }
    }
}
