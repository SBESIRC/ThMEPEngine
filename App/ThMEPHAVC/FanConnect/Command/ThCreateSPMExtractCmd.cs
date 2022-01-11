using System;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.Command;
using Autodesk.AutoCAD.Geometry;
using ThMEPHVAC.FanConnect.Model;
using ThMEPHVAC.FanConnect.Service;
using ThMEPHVAC.FanConnect.ViewModel;
using DotNetARX;
using ThMEPEngineCore.LaneLine;
using ThMEPEngineCore.Service;
using NFox.Cad;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPHVAC.FanConnect.Command
{
    public class ThCreateSPMExtractCmd : ThMEPBaseCommand, IDisposable
    {
        public ThWaterPipeConfigInfo ConfigInfo { set; get; }//界面输入信息
        public void Dispose()
        {
            //
        }
        public ThCreateSPMExtractCmd()
        {
            CommandName = "THSPM";
            ActionName = "生成水管路由平面";
        }

        public void ImportBlockFile()
        {
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.HvacPipeDwgPath(), DwgOpenMode.ReadOnly, false))//引用模块的位置
            using (var acadDb = Linq2Acad.AcadDatabase.Active())
            {
                if (blockDb.Blocks.Contains("AI-水管多排标注(4排)"))
                {
                    acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault("AI-水管多排标注(4排)"));
                }
                if (blockDb.Blocks.Contains("AI-水管多排标注(2排)"))
                {
                    acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault("AI-水管多排标注(2排)"));
                }
                if (blockDb.Blocks.Contains("AI-分歧管"))
                {
                    acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault("AI-分歧管"));
                }
                if (blockDb.Blocks.Contains("AI-水阀"))
                {
                    acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault("AI-水阀"));
                }
                if (blockDb.Layers.Contains("H-PIPE-DIMS"))
                {
                    acadDb.Layers.Import(blockDb.Layers.ElementOrDefault("H-PIPE-DIMS"), false);
                }
                if (blockDb.Layers.Contains("H-PIPE-CS"))
                {
                    acadDb.Layers.Import(blockDb.Layers.ElementOrDefault("H-PIPE-CS"), false);
                }
                if (blockDb.Layers.Contains("H-PIPE-CR"))
                {
                    acadDb.Layers.Import(blockDb.Layers.ElementOrDefault("H-PIPE-CR"), false);
                }
                if (blockDb.Layers.Contains("H-PIPE-HS"))
                {
                    acadDb.Layers.Import(blockDb.Layers.ElementOrDefault("H-PIPE-HS"), false);
                }
                if (blockDb.Layers.Contains("H-PIPE-HR"))
                {
                    acadDb.Layers.Import(blockDb.Layers.ElementOrDefault("H-PIPE-HR"), false);
                }
                if (blockDb.Layers.Contains("H-PIPE-C"))
                {
                    acadDb.Layers.Import(blockDb.Layers.ElementOrDefault("H-PIPE-C"), false);
                }
                if (blockDb.Layers.Contains("H-PIPE-CHS"))
                {
                    acadDb.Layers.Import(blockDb.Layers.ElementOrDefault("H-PIPE-CHS"), false);
                }
                if (blockDb.Layers.Contains("H-PIPE-CHR"))
                {
                    acadDb.Layers.Import(blockDb.Layers.ElementOrDefault("H-PIPE-CHR"), false);
                }
                if (blockDb.Layers.Contains("H-PIPE-R"))
                {
                    acadDb.Layers.Import(blockDb.Layers.ElementOrDefault("H-PIPE-R"), false);
                }
                if (blockDb.Layers.Contains("H-PIPE-APPE"))
                {
                    acadDb.Layers.Import(blockDb.Layers.ElementOrDefault("H-PIPE-APPE"), false);
                }
                if (blockDb.Layers.Contains("H-PAPP-VALV"))
                {
                    acadDb.Layers.Import(blockDb.Layers.ElementOrDefault("H-PAPP-VALV"), false);
                }
                
            }
            using (var acadDb = Linq2Acad.AcadDatabase.Active())
            {
                ThFanConnectUtils.EnsureLayerOn(acadDb, "0");
                ThFanConnectUtils.EnsureLayerOn(acadDb,"H-PIPE-DIMS");
                ThFanConnectUtils.EnsureLayerOn(acadDb,"H-PIPE-CS");
                ThFanConnectUtils.EnsureLayerOn(acadDb,"H-PIPE-CR");
                ThFanConnectUtils.EnsureLayerOn(acadDb,"H-PIPE-HS");
                ThFanConnectUtils.EnsureLayerOn(acadDb,"H-PIPE-HR");
                ThFanConnectUtils.EnsureLayerOn(acadDb,"H-PIPE-C");
                ThFanConnectUtils.EnsureLayerOn(acadDb,"H-PIPE-CHS");
                ThFanConnectUtils.EnsureLayerOn(acadDb,"H-PIPE-CHR");
                ThFanConnectUtils.EnsureLayerOn(acadDb,"H-PIPE-R");
                ThFanConnectUtils.EnsureLayerOn(acadDb, "H-PIPE-APPE");
                ThFanConnectUtils.EnsureLayerOn(acadDb, "H-PAPP-VALV");
            }
        }
        public override void SubExecute()
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var database = AcadDatabase.Active())
            {
                ImportBlockFile();
                //选择起点
                var startPt = ThFanConnectUtils.SelectPoint();
                if(startPt.IsEqualTo(new Point3d()))
                {
                    return;
                }
                //提取水管路由
                var mt = Matrix3d.Displacement(startPt.GetVectorTo(Point3d.Origin));
                var pipes = ThEquipElementExtractService.GetFanPipes(startPt);
                foreach(var p in pipes)
                {
                    p.TransformBy(mt);
                }
                //提取水管连接点
                var fcus = ThEquipElementExtractService.GetFCUModels();
                if(fcus.Count == 0)
                {
                    return;
                }
                //处理pipes 1.清除重复线段 ；2.将同线的线段连接起来；
                ThLaneLineCleanService cleanServiec = new ThLaneLineCleanService();
                var lineColl = cleanServiec.CleanNoding(pipes.ToCollection());
                var tmpLines = new List<Line>();
                foreach (var l in lineColl)
                {
                    tmpLines.Add(l as Line);
                }

                var lines = ThFanConnectUtils.CleanLaneLines(tmpLines);
                foreach(var l in lines)
                {
                    l.TransformBy(mt.Inverse());
                }

                double space = 300.0;
                if(ConfigInfo.WaterSystemConfigInfo.SystemType == 1)//冷媒系统
                {
                    space = 150.0;
                }
                //构建Tree
                ThFanTreeModel treeModel = new ThFanTreeModel(startPt, lines, space);
                if (treeModel.RootNode == null)
                {
                    return;
                }
                //标记4通结点
                ThFanConnectUtils.FindFourWay(treeModel.RootNode);
                //
                foreach (var fcu in fcus)
                {
                    ThFanConnectUtils.FindFcuNode(treeModel.RootNode, fcu.FanPoint);
                }
                //扩展管路
                ThWaterPipeExtendService pipeExtendServiece = new ThWaterPipeExtendService();
                pipeExtendServiece.ConfigInfo = ConfigInfo;
                pipeExtendServiece.PipeExtend(treeModel);
                //计算流量
                ThPointTreeModel pointTreeModel = new ThPointTreeModel(treeModel.RootNode, fcus);
                if(pointTreeModel.RootNode == null)
                {
                    return;
                }

                //标记流量
                ThWaterPipeMarkService pipeMarkServiece = new ThWaterPipeMarkService();
                pipeMarkServiece.ConfigInfo = ConfigInfo;
                pipeMarkServiece.PipeMark(pointTreeModel);
                if(ConfigInfo.WaterSystemConfigInfo.IsGenerValve && ConfigInfo.WaterSystemConfigInfo.IsCodeAndHotPipe)
                {
                    //插入阀门
                    ThAddValveService addValveServiece = new ThAddValveService();
                    addValveServiece.ConfigInfo = ConfigInfo;
                    addValveServiece.AddValve(treeModel);
                }
            }
        }
    }
}
