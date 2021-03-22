using DotNetARX;
using System.Linq;
using AcHelper;
using Linq2Acad;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Service.Hvac;

namespace ThMEPWSS.Pipe.Engine
{
    public static  class ThBlockSelectionEngine
    {
        public static void ZoomToModels(string dataModel)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var BlockReferences = BlockTools.GetAllDynBlockReferences(acadDatabase.Database, "楼层框定");            
                string name = "";
                var blockReferences = new List<BlockReference>();
                if (dataModel.Length==3)
                {
                    name = dataModel.Substring(0,1);
                    foreach (var block in BlockReferences)
                    {
                        if (BlockTools.GetDynBlockValue(block.Id, "楼层类型").Contains(name))
                        {
                            blockReferences.Add(block);
                        }
                    }
                }
                else
                {
                    name = dataModel.Substring(3, dataModel.Length - 3);
                    foreach (var block in BlockReferences)
                    {
                        string strings = BlockTools.GetAttributeInBlockReference(block.Id, "楼层编号");
                        if (strings!=null&& strings.Equals(name))
                        {
                            blockReferences.Add(block);
                        }
                    }
                }                                           
                if (blockReferences.Any())
                {
                    Active.Editor.ZoomToModels(blockReferences.ToArray(), 2.0);                               
                    var BlockReferencesSelected = GetBlockReferences(acadDatabase.Database, dataModel);
                    Active.Editor.PickFirstModels(BlockReferencesSelected.Select(o => o.ObjectId).ToArray());
                }
            }
        }
        private static List<BlockReference> GetBlockReferences(Database db, string blockName)
        {
            List<BlockReference> blocks = new List<BlockReference>();
            var trans = db.TransactionManager;
            BlockTable bt = (BlockTable)trans.GetObject(db.BlockTableId, OpenMode.ForRead);
            blocks = (from b in db.GetEntsInDatabase<BlockReference>()
                      where (b.GetBlockName().Contains(blockName))
                      select b).ToList();
            return blocks;
        }

    }
}
