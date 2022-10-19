using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.ApplicationServices;
using System.IO;
using ThMEPArchitecture.ParkingStallArrangement.PreProcess;
using ThMEPArchitecture.MultiProcess;

namespace ThMEPArchitecture.ParkingStallArrangement.Extractor
{
    public static class InputData
    {
        public static BlockReference SelectBlock(AcadDatabase acadDatabase)
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
                ThMPArrangementCmd.DisplayLogger.Information("选择的地库对象不是一个块！");
                Active.Editor.WriteMessage("选择的地库对象不是一个块！");
                return null;
            }
        }
        public static DBObjectCollection SelectObstacles(AcadDatabase acadDatabase,out string layerKeyWord)
        {
            //layerKeyWord = "AI描边";
            layerKeyWord = "WALL";
            //var msg = Active.Editor.GetString("\n 请输入目标图层关键字:");
            //if (msg.Status == PromptStatus.OK )
            //{ 
            //    if (msg.StringResult != "") layerKeyWord = msg.StringResult.ToUpper(); 
            //}
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
        public static List<BlockReference> SelectBlocks(AcadDatabase acadDatabase)
        {
            var entOpt = new PromptSelectionOptions { MessageForAdding = "\n请选择地库:" };
            var result = Active.Editor.GetSelection(entOpt);
            if (result.Status != PromptStatus.OK)
            {
                return null;
            }
            var objs = new List<BlockReference>();
            foreach (var id in result.Value.GetObjectIds())
            {

                var obj = acadDatabase.Element<Entity>(id);
                if (obj is BlockReference blk)
                {
                    objs.Add(blk);
                }
            }
            if(objs.Count == 0) ThMPArrangementCmd.DisplayLogger.Information("选择的元素中不包含块");
            return objs;
        }
        public static bool GetOuterBrder(AcadDatabase acadDatabase, out OuterBrder outerBrder, Serilog.Core.Logger Logger = null,bool CheckSeglines = true)
        {
            outerBrder = new OuterBrder();
            var block = SelectBlock(acadDatabase);//提取地库对象
            if (block is null)
            {
                return false;
            }
            Logger?.Information("块名：" + block.GetEffectiveName());
            Document doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            string drawingName = Path.GetFileName(doc.Name);
            Logger?.Information("文件名：" + drawingName);
            var extractRst = outerBrder.Extract(block);//提取多段线
#if DEBUG
            using (AcadDatabase currentDb = AcadDatabase.Active())
            {
                var pline = outerBrder.WallLine;
                currentDb.CurrentSpace.Add(pline);
            }
#endif
            if (!extractRst)
            {
                return false;
            }
            if (!(Logger == null) && outerBrder.SegLines.Count != 0&& CheckSeglines)
            {
                bool Isvaild = outerBrder.SegLineVaild(Logger);
                outerBrder.SegLines.ShowInitSegLine();
                //outerBrder.RemoveInnerSegLine();
                //check seg lines
                if (!Isvaild) return false;
            }

            return true;
        }

    }
}
