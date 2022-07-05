using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;
using ThMEPEngineCore.Engine;

namespace ThMEPElectrical.BlockConvert
{
    public class ThBConvertCompareService
    {
        public void Compare(Database database, List<ThBlockReferenceData> targetBlocks, List<ObjectId> objectIds)
        {
            using (var currentDb = AcadDatabase.Use(database))
            {
                var sourceEntites = targetBlocks.Select(o => currentDb.Element<Entity>(o.ObjId, true)).ToList();
                var targetEntites = objectIds.Select(o => currentDb.Element<Entity>(o, true)).ToList();
            }
        }
    }
}
