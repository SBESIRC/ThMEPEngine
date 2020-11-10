using System;
using System.Linq;
using System.Text;
using Linq2Acad;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Service
{
    public class ThSpaceNameLayerManager
    {
        public static List<string> TextXrefLayers(Database database)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                return acadDatabase.Layers
                    .Where(o => IsSpaceNameLayer(o.Name))
                    .Select(o => o.Name)
                    .ToList();
            }
        }
        public  static bool IsSpaceNameLayer(string name)
        {
            var layerName = ThStructureUtils.OriginalFromXref(name).ToUpper();
            // 图层名未包含S_BEAM
            if (!layerName.Contains("AD-NAME-ROOM"))
            {
                return false;
            }
            string[] patterns = layerName.Split('-').Reverse().ToArray();
            if (patterns.Count() < 3)
            {
                return false;
            }
            return (patterns[0] == "ROOM") && (patterns[1] == "NAME") && (patterns[2] == "AD");
        }
    }
}
