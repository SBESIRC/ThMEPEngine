using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Command;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.Diagnostics;

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
        public override void SubExecute()
        {
            try
            {
                using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
                using (var database = AcadDatabase.Active())
                {
                    ThFanConnectUtils.ImportBlockFile();
                    //选择起点
                    var startPt = ThFanConnectUtils.SelectPoint();
                    if (startPt.IsEqualTo(new Point3d()))
                    {
                        return;
                    }

                    //读取冷媒管径配置表
                    var gasDNList = new List<Tuple<double, double>>();
                    var liquidDNList = new List<Tuple<double, double>>();
                    if (ConfigInfo.WaterSystemConfigInfo.SystemType == 1 && ConfigInfo.WaterSystemConfigInfo.IsACPipeDim)
                    {
                        var file = ConfigInfo.WaterSystemConfigInfo.ACPipeDimConfigFile.FullPath;
                        var bRead = ThFanConnectDataUtil.ReadCoolACPipeDNConfig(file, ref gasDNList, ref liquidDNList);
                        if (bRead == false)
                        {
                            Active.Editor.WriteMessage("冷媒管管径配置有误，请检查修改\n");
                            return;
                        }
                    }

                    //提取水管路由
                    var pipes = ThEquipElementExtractService.GetFanPipes(startPt, ConfigInfo.WaterSystemConfigInfo.IsCodeAndHotPipe, ConfigInfo.WaterSystemConfigInfo.IsCWPipe);
                    if (pipes.Count == 0)
                    {
                        return;
                    }
                    //提取风机
                    var fcus = ThEquipElementExtractService.GetFCUModels(ConfigInfo.WaterSystemConfigInfo.SystemType);
                    //获取标记
                    var markes = ThEquipElementExtractService.GetPipeMarkes("H-PIPE-DIMS");
                    var mt = Matrix3d.Displacement(startPt.GetVectorTo(Point3d.Origin));
                    //mt = Matrix3d.Displacement(Point3d.Origin.GetVectorTo(Point3d.Origin));

                    var handlePipeService = new ThHandleFanPipeService()
                    {
                        StartPoint = startPt,
                        AllFan = fcus,
                        AllLine = pipes
                    };
                    var tempLine = handlePipeService.CleanPipe(mt);
                    var tmpTree = handlePipeService.HandleFanPipe(mt, tempLine);
                    if (tmpTree == null)
                    {
                        return;
                    }
                    var badLines = handlePipeService.GetBadLine(tmpTree, mt);//已经处理的坏线
                    var rightLines = handlePipeService.GetRightLine(tmpTree, mt);//已经处理的好线

                    double space = 300.0;
                    if (ConfigInfo.WaterSystemConfigInfo.SystemType == 1)//冷媒系统
                    {
                        space = 150.0;
                    }
                    //构建Tree
                    ThFanTreeModel treeModel = new ThFanTreeModel(startPt, rightLines, space);
                    if (treeModel.RootNode == null)
                    {
                        return;
                    }
                    //标记4通结点
                    ThFanConnectUtils.FindFourWay(treeModel.RootNode);
                    //
                    foreach (var fcu in fcus)
                    {
                        ThFanConnectUtils.FindFcuNode(treeModel.RootNode, fcu);
                    }
                    //检查末端断管图块方向
                    ThCheckUpdateEndBlkDir.UpdateEndBlkDir(treeModel.RootNode);

                    if (ConfigInfo.WaterSystemConfigInfo.IsCodeAndHotPipe)
                    {
                        double fanWidth = 600;
                        //if (ConfigInfo.WaterSystemConfigInfo.PipeSystemType == 1)
                        //{
                        //    fanWidth = 1200.0;
                        //}
                        foreach (var fcu in fcus)
                        {
                            if (fcu.IsConnected)
                                ThFanConnectUtils.UpdateFan(fcu, fanWidth);
                        }
                    }
                    //提取结点标记
                    var pipeDims = ThEquipElementExtractService.GetPipeDims("H-PIPE-APPE");
                    if (ConfigInfo.WaterSystemConfigInfo.IsCodeAndHotPipe)
                    {
                        var csPipes = ThEquipElementExtractService.GetWaterSpm("H-PIPE-CS");
                        var crPipes = ThEquipElementExtractService.GetWaterSpm("H-PIPE-CR");
                        var hsPipes = ThEquipElementExtractService.GetWaterSpm("H-PIPE-HS");
                        var hrPipes = ThEquipElementExtractService.GetWaterSpm("H-PIPE-HR");
                        var chsPipes = ThEquipElementExtractService.GetWaterSpm("H-PIPE-CHS");
                        var chrPipes = ThEquipElementExtractService.GetWaterSpm("H-PIPE-CHR");
                        var rPipes = ThEquipElementExtractService.GetWaterSpm("H-PIPE-R");
                        RemoveSPMLine(treeModel.RootNode, ref pipeDims, ref csPipes);
                        RemoveSPMLine(treeModel.RootNode, ref pipeDims, ref crPipes);
                        RemoveSPMLine(treeModel.RootNode, ref pipeDims, ref hsPipes);
                        RemoveSPMLine(treeModel.RootNode, ref pipeDims, ref hrPipes);
                        RemoveSPMLine(treeModel.RootNode, ref pipeDims, ref chsPipes);
                        RemoveSPMLine(treeModel.RootNode, ref pipeDims, ref chrPipes);
                        RemoveSPMLine(treeModel.RootNode, ref pipeDims, ref rPipes);

                        RemoveSPMLine(badLines, ref pipeDims, ref csPipes);
                        RemoveSPMLine(badLines, ref pipeDims, ref crPipes);
                        RemoveSPMLine(badLines, ref pipeDims, ref hsPipes);
                        RemoveSPMLine(badLines, ref pipeDims, ref hrPipes);
                        RemoveSPMLine(badLines, ref pipeDims, ref chsPipes);
                        RemoveSPMLine(badLines, ref pipeDims, ref chrPipes);
                        RemoveSPMLine(badLines, ref pipeDims, ref rPipes);
                    }
                    if (ConfigInfo.WaterSystemConfigInfo.IsCWPipe)
                    {
                        //提取各种线
                        //var cPipes = ThEquipElementExtractService.GetWaterSpm("H-PIPE-C");
                        //RemoveSPMLine(treeModel.RootNode, ref pipeDims, ref cPipes);
                        //RemoveSPMLine(badLines, ref pipeDims, ref cPipes);

                        var cPipes = ThEquipElementExtractService.GetWaterSpmNew("H-PIPE-C");
                        RemoveSPMLineNew(treeModel.RootNode, ref pipeDims, ref cPipes);
                        RemoveSPMLineNew(badLines, ref pipeDims, ref cPipes);


                    }

                    //扩展管路
                    ThWaterPipeExtendService pipeExtendServiece = new ThWaterPipeExtendService();
                    pipeExtendServiece.ConfigInfo = ConfigInfo;
                    pipeExtendServiece.PipeExtend(treeModel);

                    ////计算流量
                    //ThPointTreeModel pointTreeModel = new ThPointTreeModel(treeModel.RootNode, fcus);
                    //if (pointTreeModel.RootNode == null)
                    //{
                    //    return;
                    //}
                    //pointTreeModel.RemEndNode(pointTreeModel.RootNode, PIPELEVEL.LEVEL2, ConfigInfo.WaterSystemConfigInfo.IsCodeAndHotPipe, ConfigInfo.WaterSystemConfigInfo.IsCWPipe, ref markes);


                    ////标记流量
                    //ThWaterPipeMarkService pipeMarkServiece = new ThWaterPipeMarkService();
                    //pipeMarkServiece.ConfigInfo = ConfigInfo;
                    //pipeMarkServiece.UpdateMark(pointTreeModel, markes);

                    //计算流量
                    DrawUtils.ShowGeometry(rightLines, "l0rightline");
                    var pipeTreeNodes = treeModel.RootNode.GetAllTreeNode();
                    var lines = pipeTreeNodes.Select(x => x.Item.PLine).ToList();
                    DrawUtils.ShowGeometry(lines, "l0pline");
                    var breakLine = ThPointTreeModelService.BreakLine(lines, mt);
                    DrawUtils.ShowGeometry(breakLine, "l0breakline");
                    var flowCalTree = ThPointTreeModelService.BuildTree(breakLine, startPt);
                    if (flowCalTree != null)
                    {
                        ThPointTreeModelService.CalNodeLevel(flowCalTree);
                        ThPointTreeModelService.CheckMarkForLevel(flowCalTree);
                        ThPointTreeModelService.CalNodeFlowValue(flowCalTree, fcus);
                        ThPointTreeModelService.CalNodeDimValue(flowCalTree, ConfigInfo.WaterSystemConfigInfo.FrictionCoeff, gasDNList, liquidDNList);
                        ThPointTreeModelService.CheckDimChange(flowCalTree);

                        ThPointTreeModelService.PrintTree(flowCalTree, "l0node");

                        //标记流量
                        ThWaterPipeMarkService pipeMarkServiece = new ThWaterPipeMarkService();
                        pipeMarkServiece.ConfigInfo = ConfigInfo;
                        //pipeMarkServiece.PipeMark(pointTreeModel);
                        pipeMarkServiece.UpdateMark(flowCalTree, pipeTreeNodes, markes);
                    }
                }
            }
            catch (Exception ex)
            {
                Active.Editor.WriteMessage(ex.Message);
            }
        }
        public void RemoveSPMLine(List<Line> baseLines, ref List<Entity> dims, ref List<Line> lines)
        {
            foreach (var l in baseLines)
            {
                RemoveSPMLine(l, ref dims, ref lines);
            }
        }
        private void RemoveSPMLine(Line baseLine, ref List<Entity> dims, ref List<Line> lines)
        {
            var box = baseLine.Buffer(440);
            var spatialIndex = new ThCADCoreNTSSpatialIndex(lines.ToCollection())
            {
                AllowDuplicate = true,
            };
            var dbObjs = spatialIndex.SelectCrossingPolygon(box);
            var remLines = new List<Line>();
            foreach (var obj in dbObjs)
            {
                if (obj is Line)
                {
                    var line = obj as Line;
                    if (ThFanConnectUtils.IsParallelLine(baseLine, line))
                    {
                        remLines.Add(line);
                        RemoveDims(line, ref dims);
                    }
                }
            }
            lines = lines.Except(remLines).ToList();
        }
        public void RemoveSPMLine(ThFanTreeNode<ThFanPipeModel> node, ref List<Entity> dims, ref List<Line> lines)
        {
            foreach (var child in node.Children)
            {
                RemoveSPMLine(child, ref dims, ref lines);
            }
            RemoveSPMLine(node.Item.PLine, ref dims, ref lines);
        }


        public void RemoveSPMLineNew(ThFanTreeNode<ThFanPipeModel> node, ref List<Entity> dims, ref Dictionary<Line, Entity> lines)
        {
            foreach (var child in node.Children)
            {
                RemoveSPMLineNew(child, ref dims, ref lines);
            }
            RemoveSPMLineNew(node.Item.PLine, ref dims, ref lines);
        }

        public void RemoveSPMLineNew(List<Line> baseLines, ref List<Entity> dims, ref Dictionary<Line, Entity> lines)
        {
            foreach (var l in baseLines)
            {
                RemoveSPMLineNew(l, ref dims, ref lines);
            }
        }
        private void RemoveSPMLineNew(Line baseLine, ref List<Entity> dims, ref Dictionary<Line, Entity> lines)
        {
            var box = baseLine.Buffer(440);
            var spatialIndex = new ThCADCoreNTSSpatialIndex(lines.Select(x => x.Key).ToCollection())
            {
                AllowDuplicate = true,
            };
            var dbObjs = spatialIndex.SelectCrossingPolygon(box);
            var remLines = new List<Line>();
            foreach (var obj in dbObjs)
            {
                if (obj is Line)
                {
                    var line = obj as Line;
                    if (ThFanConnectUtils.IsParallelLine(baseLine, line))
                    {
                        remLines.Add(line);
                        RemoveDimsNew(line, ref dims, ref lines);
                    }
                }
            }
            foreach (var l in remLines)
            {
                lines.Remove(l);
            }
        }

        private void RemoveDimsNew(Line line, ref List<Entity> dims, ref Dictionary<Line, Entity> lines)
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
                else if (e is BlockReference)
                {
                    var blk = e as BlockReference;
                    if (blk.GetEffectiveName().Contains("AI-分歧管"))
                    {
                        if (box.Contains(blk.Position))
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

            lines.TryGetValue(line, out var item);
            item.UpgradeOpen();
            item.Erase();
            item.DowngradeOpen();

        }


        private void RemoveDims(Line line, ref List<Entity> dims)
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
                else if (e is BlockReference)
                {
                    var blk = e as BlockReference;
                    if (blk.GetEffectiveName().Contains("AI-分歧管"))
                    {
                        if (box.Contains(blk.Position))
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
