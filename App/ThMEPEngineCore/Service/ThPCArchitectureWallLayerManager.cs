﻿using Linq2Acad;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Service
{
    public class ThPCArchitectureWallLayerManager: ThDbLayerManager
    {
        public static List<string> CurveXrefLayers(Database database)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                return acadDatabase.Layers
                    .Where(o => IsVisibleLayer(o))
                    .Where(o => IsArchitectureWallCurveLayer(o.Name))
                    .Select(o => o.Name)
                    .ToList();
            }
        }

        private static bool IsArchitectureWallCurveLayer(string name)
        {
            string[] patterns = ThStructureUtils.OriginalFromXref(name).ToUpper().Split('$').Reverse().ToArray();
            return (patterns[0] == "PC_YZ_WALL") || (patterns[0] == "PC_NQ_GZ_HACH" || (patterns[0] == "PC_YZ"));
        }
    }
}
