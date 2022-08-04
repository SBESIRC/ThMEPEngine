using AcHelper;
using AcHelper.Commands;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using GeometryExtensions;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Command;
using ThMEPWSS.CADExtensionsNs;
using ThMEPWSS.HydrantConnectPipe.Model;
using ThMEPWSS.HydrantConnectPipe.Service;
using ThMEPWSS.Pipe;
using ThMEPWSS.UndergroundSpraySystem.General;

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
                    acadDb.Layers.Import(blockDb.Layers.ElementOrDefault("W-FRPT-HYDT-PIPE-AI"),true);
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
        public void Dispose()
        {
        }
        override public void SubExecute()
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
                    
                    if(hydrantPipes.Count == 0 || hydrants.Count == 0)
                    {
                        Active.Editor.WriteMessage("找不到立管或者消火栓！\n");
                        return;
                    }
                    var buildRooms = ThHydrantDataManager.GetBuildRoom(range);//获取建筑房间
                    var otherPileLines = ThHydrantDataManager.GetOtherPipeLineList(range);//获取其他管线
                    var hydrantValve = ThHydrantDataManager.GetHydrantValve(range);//获取蝶阀
                    var pipeMark = ThHydrantDataManager.GetHydrantPipeMark(range);//获取管径标记
                    List<Line> loopLines = new List<Line>();
                    List<Line> branchLines = new List<Line>();
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
                            if(ThHydrantConnectPipeUtils.HydrantIsContainPipe1(hydrant, hydrantPipes))
                            {
                                foreach(var l in branchLines)
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
                        while(hydrantPipes.Count != 0)
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

                    foreach (var brLine in brLines)
                    {
                        if (ConfigInfo.isSetupValve)
                        {
                            brLine.InsertValve(database, otherPileLines, ConfigInfo.strMapScale);
                        }

                        if (ConfigInfo.isMarkSpecif)
                        {
                            brLine.InsertPipeMark(database, ConfigInfo.strMapScale);
                        }
                        brLine.Draw(database);
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
    }


}
