using System;
using System.Linq;
using System.Collections.Generic;

using AcHelper;
using NFox.Cad;
using Linq2Acad;
using Dreambuild.AutoCAD;
using GeometryExtensions;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using ThCADCore.NTS;
using ThMEPWSS.Pipe;
using ThCADExtension;
using ThMEPTCH.Model;
using ThMEPWSS.ViewModel;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Command;
using ThMEPTCH.TCHDrawServices;
using ThMEPWSS.CADExtensionsNs;
using ThMEPWSS.HydrantConnectPipe.Model;
using ThMEPWSS.HydrantConnectPipe.Service;
using ThMEPWSS.UndergroundSpraySystem.General;
using ThMEPWSS.UndergroundFireHydrantSystem.Extract;

namespace ThMEPWSS.HydrantConnectPipe.Command
{
    public class ThHydrantConnectPipeCmd : ThMEPBaseCommand, IDisposable
    {
        private ThHydrantConnectPipeConfigInfo ConfigInfo;

        public ThHydrantConnectPipeCmd(ThHydrantConnectPipeConfigInfo configInfo)
        {
            ConfigInfo = configInfo;
        }

        public string BlockFilePath
        {
            get
            {
                var path = ThCADCommon.WSSDwgPath();
                return path;
            }
        }

        public void ImportBlockFile()
        {
            using (AcadDatabase blockDb = AcadDatabase.Open(BlockFilePath, DwgOpenMode.ReadOnly, false))//引用模块的位置
            using (var acadDb = Linq2Acad.AcadDatabase.Active())
            {
                if (blockDb.Blocks.Contains("蝶阀"))
                {
                    acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault("蝶阀"), true);
                }
                if (blockDb.Blocks.Contains("消火栓管线管径"))
                {
                    acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault("消火栓管线管径"), true);
                }
                if (blockDb.Blocks.Contains("消火栓管径150"))
                {
                    acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault("消火栓管径150"), true);
                }
                if (blockDb.Layers.Contains("W-FRPT-HYDT-PIPE-AI"))
                {
                    acadDb.Layers.Import(blockDb.Layers.ElementOrDefault("W-FRPT-HYDT-PIPE-AI"), true);
                }
                if (blockDb.Layers.Contains("W-FRPT-HYDT-EQPM"))
                {
                    acadDb.Layers.Import(blockDb.Layers.ElementOrDefault("W-FRPT-HYDT-EQPM"), true);
                }
                if (blockDb.Layers.Contains("W-FRPT-HYDT-DIMS"))
                {
                    acadDb.Layers.Import(blockDb.Layers.ElementOrDefault("W-FRPT-HYDT-DIMS"), true);
                }
            }
            using (var acadDb = Linq2Acad.AcadDatabase.Active())
            {
                DbHelper.EnsureLayerOn("W-FRPT-HYDT-PIPE-AI");
                DbHelper.EnsureLayerOn("W-FRPT-HYDT-EQPM");
                DbHelper.EnsureLayerOn("W-FRPT-HYDT-DIMS");
            }
        }

        public override void SubExecute()
        {
            try
            {
                ThMEPWSS.Common.Utils.FocusMainWindow();
                using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
                using (var database = AcadDatabase.Active())
                {
                    ImportBlockFile();
                    //选择起点
                    var startPt = ThWGeUtils.SelectPoint();
                    if (startPt.IsEqualTo(new Point3d()))
                    {
                        return;
                    }
                    var input = ThWGeUtils.SelectPoints();//获取范围
                    if (input.Item1.IsEqualTo(input.Item2))
                    {
                        Active.Editor.WriteMessage("框选范围为空！\n");
                        return;
                    }
                    Active.Editor.WriteMessage(System.DateTime.Now.ToString("yyyy-MM-dd HH：mm：ss：ffff"));
                    Active.Editor.WriteMessage("\n");
                    var range = new Point3dCollection();
                    range.Add(input.Item1.Point3dZ0().TransformBy(Active.Editor.UCS2WCS()));
                    range.Add(new Point3d(input.Item1.X, input.Item2.Y, 0).TransformBy(Active.Editor.UCS2WCS()));
                    range.Add(input.Item2.Point3dZ0().TransformBy(Active.Editor.UCS2WCS()));
                    range.Add(new Point3d(input.Item2.X, input.Item1.Y, 0).TransformBy(Active.Editor.UCS2WCS()));

                    var electricWells = ThHydrantDataManager.GetElectricWells(range);//获取电井
                    var shearWalls = ThHydrantDataManager.GetShearWalls(range);//获取剪力墙
                    var stairsRooms = ThHydrantDataManager.GetStairsRooms(range);//获取楼梯间
                    var structureCols = ThHydrantDataManager.GetStructuralCols(range);//获取结构柱
                    var windWells = ThHydrantDataManager.GetWindWells(range);//获取风井
                    var hydrants = ThHydrantDataManager.GetFireHydrants(range);//获取消火栓
                    var hydrantPipes = ThHydrantDataManager.GetFireHydrantPipes(range);//获取立管

                    if (hydrantPipes.Count == 0 || hydrants.Count == 0)
                    {
                        Active.Editor.WriteMessage("找不到立管或者消火栓！\n");
                        return;
                    }
                    var buildRooms = ThHydrantDataManager.GetBuildRoom(range);//获取建筑房间
                    var otherPileLines = ThHydrantDataManager.GetOtherPipeLineList(range);//获取其他管线
                    var hydrantValve = ThHydrantDataManager.GetHydrantValve(range);//获取蝶阀
                    var pipeMark = ThHydrantDataManager.GetHydrantPipeMark(range);//获取管径标记
                    var loopLines = new List<Line>();
                    var branchLines = new List<Line>();
                    ThHydrantDataManager.GetHydrantLoopAndBranchLines(ref loopLines, ref branchLines, startPt, range);//获取环管和支路
                    var pathService = new ThCreateHydrantPathService();
                    foreach (var shearWall in shearWalls)
                    {
                        pathService.SetObstacle(shearWall.Outline);
                    }
                    foreach (var structureCol in structureCols)
                    {
                        pathService.SetObstacle(structureCol.Outline);
                    }
                    foreach (var electricWell in electricWells)
                    {
                        pathService.SetObstacle(electricWell.Outline);
                    }
                    foreach (var windWell in windWells)
                    {
                        pathService.SetObstacle(windWell.Outline);
                    }
                    foreach (var stairsRoom in stairsRooms)
                    {
                        pathService.SetStairsRoom(stairsRoom.Outline);
                    }
                    foreach (var buildRoom in buildRooms)
                    {
                        pathService.SetBuildRoom(buildRoom.Outline);
                    }
                    foreach (var pipe in hydrantPipes)
                    {
                        pathService.SetHydrantPipe(pipe.Obb);
                    }
                    //添加约束终止线
                    pathService.SetTermination(loopLines);
                    pathService.InitData();

                    if (ConfigInfo.isCoveredGraph)
                    {
                        //branchLines 包含所有的支路（需要删除和不需要删除的数据）
                        var toDeleteLine = new List<Line>();
                        foreach (var hydrant in hydrants)
                        {
                            if (ThHydrantConnectPipeUtils.HydrantIsContainPipe1(hydrant, hydrantPipes))
                            {
                                foreach (var l in branchLines)
                                {
                                    double dist = l.DistanceToPoint(hydrant.FireHydrantPipe.PipePosition);
                                    if (dist < 100.0)
                                    {
                                        toDeleteLine.AddRange(FindListLine(l, branchLines));
                                    }
                                }
                            }
                        }
                        //挑选出需要删除的数据
                        //
                        ThHydrantDataManager.RemoveBranchLines(toDeleteLine, loopLines, hydrantValve, pipeMark, range);
                    }
                    else
                    {
                        List<ThHydrantPipe> tmpPipes = new List<ThHydrantPipe>();
                        while (hydrantPipes.Count != 0)
                        {
                            var pipe = hydrantPipes.Last();
                            hydrantPipes.Remove(pipe);
                            if (!ThHydrantConnectPipeUtils.PipeIsContainBranchLine(pipe, branchLines))
                            {
                                tmpPipes.Add(pipe);
                            }
                        }
                        hydrantPipes = tmpPipes;
                    }

                    var brLines = new List<ThHydrantBranchLine>();
                    foreach (var hydrant in hydrants)
                    {
                        if (ThHydrantConnectPipeUtils.HydrantIsContainPipe(hydrant, hydrantPipes))
                        {
                            bool isOnLine = false;
                            foreach (var l in loopLines)
                            {
                                if (l.PointOnLine(hydrant.FireHydrantPipe.PipePosition, false, 50))
                                {
                                    isOnLine = true;
                                    break;
                                }
                            }
                            if (isOnLine)
                            {
                                continue;
                            }

                            //创建路径
                            pathService.SetStartPoint(hydrant.FireHydrantPipe.PipePosition);//设置立管点为起始点
                            var path = pathService.CreateHydrantPath();
                            if (path != null)
                            {
                                var brLine = ThHydrantBranchLine.Create(path);
                                brLines.Add(brLine);

                                var objcets = (path.BufferFlatPL(400)[0] as Polyline).Buffer(-200);
                                if (objcets.Count <= 0)
                                {
                                    objcets = (path.BufferFlatPL(400)[0] as Polyline).Buffer(-50);
                                }
                                if (objcets.Count <= 0)
                                {
                                    objcets = path.BufferFlatPL(400);
                                }
                                var obb = objcets[0] as Polyline;
                                pathService.AddObstacle(obb);
                                path.Dispose();
                            }
                        }
                    }

                    var isTchPipe = false;
                    var system = "消防";
                    switch (ConfigInfo.isTchPipe)
                    {
                        case OutputType.CAD:
                            break;
                        case OutputType.TCH:
                            isTchPipe = true;
                            break;
                        case OutputType.ByMainRing:
                            isTchPipe = ThExtractHYDTPipeService.TCHPipeInfo.HasTCHPipe;
                            system = ThExtractHYDTPipeService.TCHPipeInfo.System;
                            break;
                    }
                    if (isTchPipe)
                    {
                        var service = new TCHDrawTwtPipeService();
                        foreach (var brLine in brLines)
                        {
                            var docScale = 100;
                            switch (ConfigInfo.strMapScale)
                            {
                                case "1:100":
                                    docScale = 100;
                                    break;
                                case "1:150":
                                    docScale = 150;
                                    break;
                                default:
                                    break;
                            }
                            foreach (var line in brLine.DrawLineList)
                            {
                                service.Pipes.Add(CreateThTCHTwtPipe(line, docScale, ConfigInfo.isMarkSpecif, system));
                            }
                            if (ConfigInfo.isSetupValve)
                            {
                                var lines = brLine.BranchPolyline.ToLines();
                                if (lines.Count > 0)
                                {
                                    var location = brLine.InsertValve(database, lines, otherPileLines, ConfigInfo.strMapScale, true);
                                    service.Valves.Add(CreateThTCHTwtPipeValve(location, docScale, system));
                                }
                            }
                        }
                        service.DrawExecute(true, false);
                    }
                    else
                    {
                        foreach (var brLine in brLines)
                        {
                            if (ConfigInfo.isSetupValve)
                            {
                                var lines = brLine.BranchPolyline.ToLines();
                                if (lines.Count > 0)
                                {
                                    brLine.InsertValve(database, lines, otherPileLines, ConfigInfo.strMapScale);
                                }
                            }

                            if (ConfigInfo.isMarkSpecif)
                            {
                                brLine.InsertPipeMark(database, ConfigInfo.strMapScale);
                            }
                            brLine.Draw(database);
                        }
                    }

                    pathService.Clear();
                    Active.Editor.WriteMessage("洞的数量:");
                    Active.Editor.WriteMessage(pathService.GetHoleCount().ToString());
                    Active.Editor.WriteMessage("\n");
                    Active.Editor.WriteMessage("A*运行次数:");
                    Active.Editor.WriteMessage(pathService.AStarCount.ToString());
                    Active.Editor.WriteMessage("\n");
                    Active.Editor.WriteMessage(System.DateTime.Now.ToString("yyyy-MM-dd HH：mm：ss：ffff"));
                    Active.Editor.WriteMessage("\n");
                }
            }
            catch (Exception ex)
            {
                Active.Editor.WriteMessage(ex.Message);
            }
        }
        public List<Line> FindListLine(Line l, List<Line> allLine)
        {
            var tmpLines = allLine.Clone().ToList();
            l = l.ExtendLine(10);
            var tmpBox = l.Buffer(10);
            var retLines = new List<Line>();
            foreach (var temp in tmpLines)
            {
                //判断tmpline和l是否连接
                if (tmpBox.Contains(temp.StartPoint) || tmpBox.Contains(temp.EndPoint))
                {
                    retLines.Add(temp);
                }
            }
            tmpLines = tmpLines.Except(retLines).ToList();
            var retLines1 = new List<Line>();
            foreach (var ret in retLines)
            {
                retLines1.AddRange(FindListLine(ret, tmpLines));
            }
            retLines.AddRange(retLines1);
            return retLines;
        }

        private ThTCHTwtPipe CreateThTCHTwtPipe(Line line, double docScale, bool showDim, string system)
        {
            var tchPipe = new ThTCHTwtPipe();
            tchPipe.StartPtID = new ThTCHTwtPoint
            {
                Point = line.StartPoint,
            };
            tchPipe.EndPtID = new ThTCHTwtPoint
            {
                Point = line.EndPoint,
            };
            tchPipe.System = system;
            tchPipe.Material = "镀锌钢管";
            tchPipe.DnType = "DN";
            tchPipe.Dn = 65.0;
            tchPipe.Gradient = 0.0;
            tchPipe.Weight = 3.5;
            tchPipe.HideLevel = 0;
            tchPipe.DocScale = docScale;
            tchPipe.DimID = new ThTCHTwtPipeDimStyle
            {
                ShowDim = showDim && line.Length > 1000.0,
                DnStyle = DnStyle.Type1,
                GradientStyle = GradientStyle.NoDimension,
                LengthStyle = LengthStyle.NoDimension,
                ArrangeStyle = false,
                DelimiterStyle = DelimiterStyle.Blank,
                SortStyle = SortStyle.Type0,
            };
            return tchPipe;
        }

        private ThTCHTwtPipeValve CreateThTCHTwtPipeValve(Point3d blockPosition, double docScale, string system)
        {
            var valve = new ThTCHTwtPipeValve();
            valve.LocationID = new ThTCHTwtPoint
            {
                Point = blockPosition,
            };
            valve.DirectionID = new ThTCHTwtVector
            {
                Vector = new Vector3d(1, 0, 0),
            };
            valve.BlockID = new ThTCHTwtBlock
            {
                Type = "VALVE",
                Number = "00000856",
            };
            //valve.PipeID = tchPipe;
            valve.System = system;
            valve.Length = 240.0;
            valve.Width = 180.0;
            valve.InterruptWidth = 200.0;
            valve.DocScale = docScale;
            return valve;
        }

        public void Dispose()
        {
            //
        }
    }
}
