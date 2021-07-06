using DotNetARX;
using Linq2Acad;
using System.Collections.Generic;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Service
{
    public class ThDclPrintService
    {
        private Database Db { get; set; }
        private string LayerName { get; set; }
        private const string BelowFloorBlkName = "E-BGND33"; // 从本层引下去
        private const string FromUpFloorBlkName = "E-BGND34"; // 从上面引至本层
        public ThDclPrintService(Database database, string layerName)
        {
            Db = database;
            LayerName = layerName;
        }
        public void Print(List<DclInfo> infos)
        {
            using (var currentDb = AcadDatabase.Use(Db))
            {
                ImportBlock();
                var layerId = Db.CreateAILayer(LayerName, 3);
                infos.ForEach(o =>
                {
                    ObjectId objId = ObjectId.Null;
                    if (o.Class.Equals("A"))
                    {
                        //A类表示从上面引至本层
                        objId = currentDb.ModelSpace.ObjectId.InsertBlockReference(
                            LayerName, FromUpFloorBlkName, o.Position, new Scale3d(10), 0.0);
                    }
                    else if (o.Class.Equals("B"))
                    {
                        //B类表示从本层引下去
                        objId = currentDb.ModelSpace.ObjectId.InsertBlockReference(
                           LayerName, BelowFloorBlkName, o.Position, new Scale3d(10), 0.0);
                    }
                    if(objId!= ObjectId.Null)
                    {
                        var br = currentDb.Element<BlockReference>(objId);
                        br.UpgradeOpen();
                        br.LayerId = layerId;
                        br.DowngradeOpen();
                    }
                });
            }
        }
        private void ImportBlock()
        {
            using (var currentDb = AcadDatabase.Use(Db))
            using (var blockDb = AcadDatabase.Open(ThCADCommon.AutoFireAlarmSystemDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                currentDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(BelowFloorBlkName), true);
                currentDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(FromUpFloorBlkName), true);                
            }
        }
    }    
}
