using System;
using System.Linq;
using Linq2Acad;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Service
{
    public class ThGravityWaterBucketLayerManager
    {
        public static List<string> XrefLayers(Database database)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                return acadDatabase.Layers
                    .Where(o => IsVisibleLayer(o))
                    .Where(o => IsGravityWaterBucketLayerName(o.Name))
                    .Select(o => o.Name)
                    .ToList();
            }
        }
        private static bool IsVisibleLayer(LayerTableRecord layerTableRecord)
        {
            return !(layerTableRecord.IsOff || layerTableRecord.IsFrozen);
        }
        private static bool IsGravityWaterBucketLayerName(string name)
        {
            return true;
        }
        public static bool IsGravityWaterBucketBlockName(string name)
        {
            string[] patterns = ThStructureUtils.OriginalFromXref(name).ToUpper().Split('-').Reverse().ToArray();
            if (patterns.Count() < 3)
            {
                return false;
            }
            var isGravityPattern = ((patterns[0] == "1")) && (patterns[1] == "DRAIN") && (patterns[2] == "W");
            var is87Pattern = name.Contains("87");
            return isGravityPattern || is87Pattern;//重力型雨水斗
        }
    }
}
