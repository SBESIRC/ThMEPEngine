﻿using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Command;
using ThMEPHVAC.FanConnect.Model;
using ThMEPHVAC.FanConnect.Service;
using ThMEPHVAC.FanConnect.ViewModel;

namespace ThMEPHVAC.FanConnect.Command
{
    public class ThUpdateSPMExtractCmd : ThMEPBaseCommand, IDisposable
    {
        public ThWaterPipeConfigInfo ConfigInfo { set; get; }//界面输入信息
        public void Dispose()
        {
        }
        public ThUpdateSPMExtractCmd()
        {
            CommandName = "THSPM";
            ActionName = "更新水管平面";
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
                    acadDb.Layers.Import(blockDb.Layers.ElementOrDefault("H-PIPE-DIMS"), true);
                }
                if (blockDb.Layers.Contains("H-PIPE-CS"))
                {
                    acadDb.Layers.Import(blockDb.Layers.ElementOrDefault("H-PIPE-CS"), true);
                }
                if (blockDb.Layers.Contains("H-PIPE-CR"))
                {
                    acadDb.Layers.Import(blockDb.Layers.ElementOrDefault("H-PIPE-CR"), true);
                }
                if (blockDb.Layers.Contains("H-PIPE-HS"))
                {
                    acadDb.Layers.Import(blockDb.Layers.ElementOrDefault("H-PIPE-HS"), true);
                }
                if (blockDb.Layers.Contains("H-PIPE-HR"))
                {
                    acadDb.Layers.Import(blockDb.Layers.ElementOrDefault("H-PIPE-HR"), true);
                }
                if (blockDb.Layers.Contains("H-PIPE-C"))
                {
                    acadDb.Layers.Import(blockDb.Layers.ElementOrDefault("H-PIPE-C"), true);
                }
                if (blockDb.Layers.Contains("H-PIPE-CHS"))
                {
                    acadDb.Layers.Import(blockDb.Layers.ElementOrDefault("H-PIPE-CHS"), true);
                }
                if (blockDb.Layers.Contains("H-PIPE-CHR"))
                {
                    acadDb.Layers.Import(blockDb.Layers.ElementOrDefault("H-PIPE-CHR"), true);
                }
                if (blockDb.Layers.Contains("H-PIPE-R"))
                {
                    acadDb.Layers.Import(blockDb.Layers.ElementOrDefault("H-PIPE-R"), true);
                }
                if (blockDb.Layers.Contains("H-PIPE-APPE"))
                {
                    acadDb.Layers.Import(blockDb.Layers.ElementOrDefault("H-PIPE-APPE"), true);
                }
                if (blockDb.Layers.Contains("H-PAPP-VALV"))
                {
                    acadDb.Layers.Import(blockDb.Layers.ElementOrDefault("H-PAPP-VALV"), true);
                }

            }
            using (var acadDb = Linq2Acad.AcadDatabase.Active())
            {
                ThFanConnectUtils.EnsureLayerOn(acadDb, "H-PIPE-DIMS");
                ThFanConnectUtils.EnsureLayerOn(acadDb, "H-PIPE-CS");
                ThFanConnectUtils.EnsureLayerOn(acadDb, "H-PIPE-CR");
                ThFanConnectUtils.EnsureLayerOn(acadDb, "H-PIPE-HS");
                ThFanConnectUtils.EnsureLayerOn(acadDb, "H-PIPE-HR");
                ThFanConnectUtils.EnsureLayerOn(acadDb, "H-PIPE-C");
                ThFanConnectUtils.EnsureLayerOn(acadDb, "H-PIPE-CHS");
                ThFanConnectUtils.EnsureLayerOn(acadDb, "H-PIPE-CHR");
                ThFanConnectUtils.EnsureLayerOn(acadDb, "H-PIPE-R");
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
                //提取水管路由
                var pipes = ThEquipElementExtractServiece.GetFanPipes(startPt);
                //提取风机
                var fcus = ThEquipElementExtractServiece.GetFCUModels();
                //处理pipes 1.清除重复线段 ；2.将同线的线段连接起来；
                var lines = ThFanConnectUtils.CleanLaneLines(pipes);
                double space = 300.0;
                if (ConfigInfo.WaterSystemConfigInfo.SystemType == 1)//冷媒系统
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
                //提取各种线
                var csPipes = ThEquipElementExtractServiece.GetWaterSpm("H-PIPE-CS");
                var crPipes = ThEquipElementExtractServiece.GetWaterSpm("H-PIPE-CR");
                var hsPipes = ThEquipElementExtractServiece.GetWaterSpm("H-PIPE-HS");
                var hrPipes = ThEquipElementExtractServiece.GetWaterSpm("H-PIPE-HR");
                var cPipes = ThEquipElementExtractServiece.GetWaterSpm("H-PIPE-C");
                var chsPipes = ThEquipElementExtractServiece.GetWaterSpm("H-PIPE-CHS");
                var chrPipes = ThEquipElementExtractServiece.GetWaterSpm("H-PIPE-CHR");
                var rPipes = ThEquipElementExtractServiece.GetWaterSpm("H-PIPE-R");
                //提取结点标记
                var dims = ThEquipElementExtractServiece.GetPipeDims("H-PIPE-APPE");
                RemoveSPMLine(treeModel.RootNode, ref dims, ref csPipes);
                RemoveSPMLine(treeModel.RootNode, ref dims, ref crPipes);
                RemoveSPMLine(treeModel.RootNode, ref dims, ref hsPipes);
                RemoveSPMLine(treeModel.RootNode, ref dims, ref hrPipes);
                RemoveSPMLine(treeModel.RootNode, ref dims, ref chsPipes);
                RemoveSPMLine(treeModel.RootNode, ref dims, ref chrPipes);
                RemoveSPMLine(treeModel.RootNode, ref dims, ref rPipes);
                RemoveSPMLine(treeModel.RootNode, ref dims, ref cPipes);

                //扩展管路
                ThWaterPipeExtendServiece pipeExtendServiece = new ThWaterPipeExtendServiece();
                pipeExtendServiece.ConfigInfo = ConfigInfo;
                pipeExtendServiece.PipeExtend(treeModel);

                //计算流量
                ThPointTreeModel pointTreeModel = new ThPointTreeModel(treeModel.RootNode, fcus);
                if (pointTreeModel.RootNode == null)
                {
                    return;
                }
                var markes = ThEquipElementExtractServiece.GetPipeMarkes("H-PIPE-DIMS");
                //标记流量
                ThWaterPipeMarkServiece pipeMarkServiece = new ThWaterPipeMarkServiece();
                pipeMarkServiece.ConfigInfo = ConfigInfo;
                pipeMarkServiece.UpdateMark(pointTreeModel, markes);
            }
        }
        public void RemoveSPMLine(ThFanTreeNode<ThFanPipeModel> node,ref List<Entity> dims, ref List<Line> lines)
        {
            foreach(var child in node.Children)
            {
                RemoveSPMLine(child, ref dims, ref lines);
            }
            var box = node.Item.PLine.ExtendLine(440).Buffer(440);
            var spatialIndex = new ThCADCoreNTSSpatialIndex(lines.ToCollection())
            {
                AllowDuplicate = true,
            };
            var dbObjs = spatialIndex.SelectWindowPolygon(box);

            var remLines = new List<Line>();
            foreach (var obj in dbObjs)
            {
                if(obj is Line)
                {
                    var line = obj as Line;
                    remLines.Add(line);
                    RemoveDims(line,ref dims);
                }
            }
            lines = lines.Except(remLines).ToList();
        }
        public void RemoveDims(Line line ,ref List<Entity> dims)
        {
            var box = line.ExtendLine(10).Buffer(10);
            var remEntity = new List<Entity>();
            foreach (var e in dims)
            {
                if (e is Circle)
                {
                    var circle = e as Circle;
                    if (box.Contains(circle.Center))
                    {
                        circle.UpgradeOpen();
                        circle.Erase();
                        circle.DowngradeOpen();
                        remEntity.Add(e);
                    }
                }
                else if(e is BlockReference)
                {
                    var blk = e as BlockReference;
                    if(blk.GetEffectiveName().Contains("AI-分歧管"))
                    {
                        if(box.Contains(blk.Position))
                        {
                            blk.UpgradeOpen();
                            blk.Erase();
                            blk.DowngradeOpen();
                            remEntity.Add(e);
                        }
                    }
                }
            }
            dims = dims.Except(remEntity).ToList();
            line.UpgradeOpen();
            line.Erase();
            line.DowngradeOpen();

        }
    }
}
