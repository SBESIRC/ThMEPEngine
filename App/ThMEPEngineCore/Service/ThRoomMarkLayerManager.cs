using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Linq2Acad;
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
            return IsNameRoomLayer(layerName) || IsFloorAreaLayer(layerName);
        }
        private static bool IsNameRoomLayer(string mark)
        {
            //*-NAME-ROOM都可以
            //整个字符被"-"分成N段（N>=3)，最后两段是NAME和ROOM即可，前面有N段都无所谓
            string newMark = mark.Trim().ToUpper();
            string pattern = @"^\S[\s\S]*[-]{1}\s{0,}(NAME)\s{0,}[-]{1}\s{0,}(ROOM)$";
            return Regex.IsMatch(newMark,pattern);
        }
        private static bool IsFloorAreaLayer(string mark)
        {
            //*-FLOOR-AREA都可以
            //整个字符被"-"分成N段（N>=3)，最后两段是FLOOR和AREA即可，前面有N段都无所谓
            string newMark = mark.Trim().ToUpper();
            string pattern = @"^\S[\s\S]*[-]{1}\s{0,}(FLOOR)\s{0,}[-]{1}\s{0,}(AREA)$";
            return Regex.IsMatch(newMark, pattern);
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
