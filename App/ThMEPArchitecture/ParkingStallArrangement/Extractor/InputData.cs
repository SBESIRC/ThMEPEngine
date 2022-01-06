using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            if (dbObj is BlockReference)
            {
                return (BlockReference)dbObj;
            }
            else
            {
                Active.Editor.WriteMessage("选择的地库对象不是一个块！");
                return null;
            }
            
        }

        public static bool GetOuterBrder(AcadDatabase acadDatabase, out OuterBrder outerBrder)
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
            return true;
        }
    }
}
