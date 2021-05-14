using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using ThCADExtension;
using ThMEPWSS.Command;
using ThMEPWSS.Pipe.Engine;
using Dbg = ThMEPWSS.DebugNs.ThDebugTool;
using Linq2Acad;
using ThMEPWSS.Assistant;
using ThMEPWSS.Pipe.Service;
using ThCADCore.NTS;
using DotNetARX;
using System;
using System.ComponentModel;
using System.Linq;
using Autodesk.AutoCAD.EditorInput;
using System.IO;
using AcHelper;
using ThMEPWSS.Pipe.Model;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model.Common;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPWSS
{
    public partial class ThSystemDiagramCmds
    {
        /// <summary>
        /// Tian Hua Create water suply system diagram
        /// </summary>
        [CommandMethod("TIANHUACAD", "THCSSD", CommandFlags.Modal)]
        public void ThCreateWaterSuplySystemDiagram()
        {
            using (var cmd = new ThWaterSuplySystemDiagramCmd())
            {
                cmd.Execute();
            }
        }

        //public void ThCreateWaterSuplySystemDiagram()
        //{

        //    using (var db = Linq2Acad.AcadDatabase.Active())

        //    {
        //        var storey = new Storey();
        //        for (int i = 0; i < 32; i++)
        //        {

        //            storey.Draw(i);
        //        }
        //    }

        /// <summary>
        /// Test command to get clean tools
        /// </summary>
        [CommandMethod("TIANHUACAD", "TestExractCleanTools", CommandFlags.Modal)]
        public void TestExractCleanTools()
        {
            using (var db = AcadDatabase.Active())
            using (var engine = new ThWCleanToolsRecongnitionEngine())//创建卫生洁具识别引擎
            {
                var per = Active.Editor.GetEntity("\n选择一个框");//交互界面
                if (per.Status == PromptStatus.OK)//框选择成功
                {
                    var storeysRecEngine = new ThStoreysRecognitionEngine();//创建楼板识别引擎
                    //楼板分隔线识别
                    storeysRecEngine.Recognize(db.Database, db.Element<Polyline>(per.ObjectId).Vertices());
                    //var rsts = storeysRecEngine.Elements;
                    var rectList = new List<Point3dCollection>();
                    foreach (var obj in storeysRecEngine.Elements)
                    {
                        if (obj is ThStoreys)
                        {
                            var sobj = obj as ThStoreys;
                            var br = db.Element<BlockReference>(sobj.ObjectId);
                            if (!br.IsDynamicBlock) continue;

                            var spt = sobj.ObjectId.GetBlockPosition();//获取楼层分割线的起始点
                            var Line1X = spt.X + Convert.ToDouble(sobj.ObjectId.GetDynBlockValue("分割1 X"));
                            var Line2X = spt.X + Convert.ToDouble(sobj.ObjectId.GetDynBlockValue("分割2 X"));
                            var Line3X = spt.X + Convert.ToDouble(sobj.ObjectId.GetDynBlockValue("分割3 X"));
                            var eptX = spt.X + Convert.ToDouble(sobj.ObjectId.GetDynBlockValue("宽度"));
                            var eptY = spt.Y - Convert.ToDouble(sobj.ObjectId.GetDynBlockValue("高度"));

                            var floorZone = new FloorZone(spt, new Point3d(eptX, eptY, 0), Line1X, Line2X, Line3X);

                            rectList = floorZone.CreateRectList();
                        }
                    }

                    //var frame = db.Element<Polyline>(per.ObjectId);
                    var CleanToolList = new List<CleaningToolsSystem>();

                    //engine.Recognize(db.Database, frame.Vertices());
                    for (int i = 0; i < rectList.Count; i++)
                    {
                        engine.Recognize(db.Database, rectList[i]);
                        var allBlockNames = engine.Datas.Select(ct => ct.Data as string);
                        var cleanTools = new int[8] { 0, 0, 0, 0, 0, 0, 0, 0 };
                        int FloorNumber = 0;
                        if (allBlockNames.ToList().Count > 0)
                        {
                            foreach (var tool in allBlockNames.ToList())
                            {
                                cleanTools[ThCleanToolsManager.CleanToolIndex(tool)] += 1;
                            }
                            var CleanTool = new CleaningToolsSystem(FloorNumber, i, cleanTools);
                            CleanToolList.Add(CleanTool);
                        }
                    }
                }
            }
        }
    }
}
