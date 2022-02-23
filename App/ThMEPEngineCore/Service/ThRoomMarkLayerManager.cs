using System;
using System.Linq;
using Linq2Acad;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Service
{
    public class ThRoomMarkLayerManager
    {
        public static List<string> TextXrefLayers(Database database)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                return acadDatabase.Layers
                    .Where(o => IsVisibleLayer(o))
                    .Where(o => IsSpaceNameLayer(o.Name))
                    .Select(o => o.Name)
                    .ToList();
            }
        }
        public static List<string> TextModelSpaceLayers(Database database)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                return acadDatabase.Layers
                    .Where(o => IsVisibleLayer(o))
                    .Where(o => IsModelSpaceLayer(o.Name))
                    .Select(o => o.Name)
                    .ToList();
            }
        }
        public static List<string> AIRoomMarkXRefLayers(Database database)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                return acadDatabase.Layers
                    .Where(o => IsVisibleLayer(o))
                    .Where(o => IsAIRoomMarkXRefLayer(o.Name))
                    .Select(o => o.Name)
                    .ToList();
            }
        }
        private static bool IsSpaceNameLayer(string name)
        {
            var layerName = ThStructureUtils.OriginalFromXref(name).ToUpper();
            if (!layerName.Contains("AD-FLOOR-AREA") && !layerName.Contains("AD-NAME-ROOM"))
            {
                return false;
            }
            string[] patterns = layerName.Split('-').Reverse().ToArray();
            if (patterns.Count() < 3)
            {
                return false;
            }
            return (patterns[0] == "AREA" && patterns[1] == "FLOOR"&& patterns[2] == "AD")||
               (patterns[0] == "ROOM" && patterns[1] == "NAME" && patterns[2] == "AD");
        }
        private static bool IsVisibleLayer(LayerTableRecord layerTableRecord)
        {
            return !(layerTableRecord.IsOff || layerTableRecord.IsFrozen);
        }
        private static bool IsModelSpaceLayer(string name)
        {
            string[] patterns = name.ToUpper().Split('-').ToArray();
            if (patterns.Count() < 2)
            {
                return false;
            }
            return patterns[0] == "AI" && patterns[1] == "房间名称";
        }
        private static bool IsAIRoomMarkXRefLayer(string name)
        {
            string[] patterns = ThStructureUtils.OriginalFromXref(name).ToUpper().Split('-').ToArray();
            if (patterns.Count() < 2)
            {
                return false;
            }
            return patterns[0] == "AI" && patterns[1] == "房间名称";
        }
    }
}
