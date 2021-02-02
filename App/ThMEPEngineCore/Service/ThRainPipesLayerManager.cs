﻿using Linq2Acad;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Service
{
  public  class ThRainPipesLayerManager
    {
        public static List<string> XrefLayers(Database database)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                return acadDatabase.Layers
                    .Where(o => IsVisibleLayer(o))
                    .Where(o => IsDimensionTagsLayerName(o.Name))
                    .Select(o => o.Name)
                    .ToList();
            }
        }
        private static bool IsVisibleLayer(LayerTableRecord layerTableRecord)
        {
            return !(layerTableRecord.IsOff || layerTableRecord.IsFrozen);
        }
        private static bool IsDimensionTagsLayerName(string name)
        {
            string[] patterns = ThStructureUtils.OriginalFromXref(name).ToUpper().Split('-').ToArray();
            if (patterns.Count() < 3)
            {
                return false;
            }
            else if (patterns.Count() == 4)
            {
                return ((patterns[0] == "W") && (patterns[1] == "FRPT") && (patterns[2] == "HYDT") && (patterns[3] == "PIPE"))||
                    ((patterns[0] == "W") && (patterns[1] == "DRAI") && (patterns[2] == "DOME") && (patterns[3] == "PIPE"))||
                    ((patterns[0] == "W") && (patterns[1] == "DRAI") && (patterns[2] == "WAST") && (patterns[3] == "PIPE"))||
                    ((patterns[0] == "W") && (patterns[1] == "WSUP") && (patterns[2] == "COOL") && (patterns[3] == "PIPE"));

            }
            else
            {
                return ((patterns[0] == "W") && (patterns[1] == "RAIN") && (patterns[2] == "PIPE")) ;
            }
        }
    }
}
