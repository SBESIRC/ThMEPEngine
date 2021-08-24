using AcHelper;
using AcHelper.Commands;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPWSS.CADExtensionsNs;
using ThMEPWSS.HydrantConnectPipe.Model;
using ThMEPWSS.HydrantConnectPipe.Service;
using ThMEPWSS.Pipe;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;

namespace ThMEPWSS.HydrantConnectPipe.Command
{
    public class ThHydrantConnectPipeCmd : IAcadCommand, IDisposable
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
                if (!acadDb.Blocks.Contains("蝶阀") && blockDb.Blocks.Contains("蝶阀"))
                {
                    acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault("蝶阀"));
                }
                if (!acadDb.Blocks.Contains("消火栓管线管径") && blockDb.Blocks.Contains("消火栓管线管径"))
                {
                    acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault("消火栓管线管径"));
                }
                if (!acadDb.Blocks.Contains("消火栓管径150") && blockDb.Blocks.Contains("消火栓管径150"))
                {
                    acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault("消火栓管径150"));
                }
                if (!acadDb.Layers.Contains("W-FRPT-HYDT-PIPE-AI") && blockDb.Layers.Contains("W-FRPT-HYDT-PIPE-AI"))
                {
                    acadDb.Layers.Import(blockDb.Layers.ElementOrDefault("W-FRPT-HYDT-PIPE-AI"));
                    
                }
            }
            using (var acadDb = Linq2Acad.AcadDatabase.Active())
            {
                DbHelper.EnsureLayerOn("W-FRPT-HYDT-PIPE-AI");
            }
        }
        public void Dispose()
        {
        }
        public void Execute()
        {
            try
            {
                ThMEPWSS.Common.Utils.FocusMainWindow();
                using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
                using (var database = AcadDatabase.Active())
                {
                    ImportBlockFile();
                    
                    var input = ThWGeUtils.SelectPoints();//获取范围
                    if (input.Item1.IsEqualTo(input.Item2))
                    {
                        return;
                    }
                    Active.Editor.WriteMessage(System.DateTime.Now.ToString("yyyy-MM-dd HH：mm：ss：ffff"));
                    Active.Editor.WriteMessage("\n");
                    var range = new Point3dCollection();
                    range.Add(input.Item1);
                    range.Add(new Point3d(input.Item1.X, input.Item2.Y, 0));
                    range.Add(input.Item2);
                    range.Add(new Point3d(input.Item2.X, input.Item1.Y, 0));

                    var electricWells = ThHydrantDataManager.GetElectricWells(range);//获取电井
                    var shearWalls = ThHydrantDataManager.GetShearWalls(range);//获取剪力墙
                    var stairsRooms = ThHydrantDataManager.GetStairsRooms(range);//获取楼梯间
                    var structureCols = ThHydrantDataManager.GetStructuralCols(range);//获取结构柱
                    var windWells = ThHydrantDataManager.GetWindWells(range);//获取风井
                    var hydrants = ThHydrantDataManager.GetFireHydrants(range);//获取消火栓
                    var hydrantPipes = ThHydrantDataManager.GetFireHydrantPipes(range);//获取立管
                    var buildRooms = ThHydrantDataManager.GetBuildRoom(range);//获取建筑房间

                    List<Line> loopLines = new List<Line>();
                    List<Line> branchLines = new List<Line>();
                    ThHydrantDataManager.GetHydrantLoopAndBranchLines(ref loopLines, ref branchLines, range);//获取环管和支路
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
                        ThHydrantDataManager.RemoveBranchLines(branchLines, loopLines, range);
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

                                var objcets = path.BufferPL(200)[0];
                                var obb = objcets as Polyline;
                                pathService.AddObstacle(obb);
                                path.Dispose();
                            }
                        }
                    }

                    foreach (var brLine in brLines)
                    {
                        brLine.Draw(database);
                        if (ConfigInfo.isSetupValve)
                        {
                            brLine.InsertValve(database);
                        }

                        if (ConfigInfo.isMarkSpecif)
                        {
                            brLine.InsertPipeMark(database, ConfigInfo.strMapScale);
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
    }
}
