using DotNetARX;
using System.Linq;
using AcHelper;
using Linq2Acad;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Service.Hvac;
using ThCADExtension;

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
                    name = dataModel.Substring(3);                 
                    foreach (var block in BlockReferences)
                    {
                        var category = BlockTools.GetDynBlockValue(block.Id, "楼层类型");
                        if (category.Contains("小屋面") || category.Contains("大屋面"))
                        {
                            continue;
                        }
                        var number = BlockTools.GetAttributeInBlockReference(block.Id, "楼层编号");
                        if (name == number)
                        {
                            blockReferences.Add(block);
                        }
                    }
                }                                           
                if (blockReferences.Any())
                {
                    Active.Editor.ZoomToObjects(blockReferences.ToArray(), 2.0);                               
                }
            }
        }

        public static void PickFirstModels(string blockName)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var objIds = acadDatabase.ModelSpace
                    .OfType<BlockReference>()
                    .Where(o => o.GetBlockName().Contains(blockName))
                    .Select(o => o.ObjectId);
                Active.Editor.PickFirstObjects(objIds.ToArray());
            }
        }
    }
}
