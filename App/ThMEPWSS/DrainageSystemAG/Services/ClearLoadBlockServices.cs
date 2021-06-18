using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;

namespace ThMEPWSS.DrainageSystemAG.Services
{
    static class ClearLoadBlockServices
    {
        static List<string> lampBlockNames = new List<string>()
        {
                ThWSSCommon.FloorDrainBlockName,
        };
        public static void ClearHisFloorBlock(Polyline outPolyline) 
        {
            
        }
        public static void LoadBlockToDocument(this Database database)
        {
            using (AcadDatabase currentDb = AcadDatabase.Use(database))
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.WSSDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                foreach (var item in lampBlockNames)
                {
                    if (item == null)
                        continue;
                    var block = blockDb.Blocks.ElementOrDefault(item);
                    if (null == block)
                        continue;
                    currentDb.Blocks.Import(block, false);
                }
            }
        }
    }
}
