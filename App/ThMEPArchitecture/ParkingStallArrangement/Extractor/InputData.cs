using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;

namespace ThMEPArchitecture.ParkingStallArrangement.Extractor
{
    public static class InputData
    {
        private static BlockReference SelectBlock(AcadDatabase acadDatabase)
        {
            var entOpt = new PromptEntityOptions("\n请选择地库:");
            var entityResult = Active.Editor.GetEntity(entOpt);
            var entId = entityResult.ObjectId;
            var dbObj = acadDatabase.Element<Entity>(entId);
            if (dbObj is BlockReference blk)
            {
                return blk;
            }
            else
            {
                Active.Editor.WriteMessage("选择的地库对象不是一个块！");
                return null;
            }
        }
        public static DBObjectCollection SelectObstacles(AcadDatabase acadDatabase)
        {
            var entOpt = new PromptSelectionOptions { MessageForAdding = "\n请选择包含障碍物的块:" };
            var result = Active.Editor.GetSelection(entOpt);
            if (result.Status != PromptStatus.OK)
            {
                return null;
            }
            var objs = new DBObjectCollection();
            foreach (var id in result.Value.GetObjectIds())
            {

                var obj = acadDatabase.Element<Entity>(id);
                if(obj is BlockReference blk)
                {
                    objs.Add(blk);
                }
            }
            return objs;
        }
        public static bool GetOuterBrder(AcadDatabase acadDatabase, out OuterBrder outerBrder, Serilog.Core.Logger Logger = null)
        {
            outerBrder = new OuterBrder();
            var block = SelectBlock(acadDatabase);//提取地库对象
            if (block is null)
            {
                return false;
            }
            var extractRst = outerBrder.Extract(block);//提取多段线
            if (!extractRst)
            {
                return false;
            }
            if (!(Logger == null) && outerBrder.SegLines.Count != 0)
            {
                //check seg lines
                if(!outerBrder.SegLineVaild(Logger)) return false;
            }

            return true;
        }

    }
}
