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
using ThMEPWSS.HydrantConnectPipe.Model;
using ThMEPWSS.HydrantConnectPipe.Service;
using ThMEPWSS.Pipe;

namespace ThMEPWSS.HydrantConnectPipe.Command
{
    public class ThHydrantConnectPipeCmd : IAcadCommand, IDisposable
    {
        private ThHydrantConnectPipeConfigInfo ConfigInfo;
        public ThHydrantConnectPipeCmd(ThHydrantConnectPipeConfigInfo configInfo)
        {
            ConfigInfo = configInfo;
        }
        public void Dispose()
        {
        }
        public void Execute()
        {
            ThMEPWSS.Common.Utils.FocusMainWindow();
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var database = AcadDatabase.Active())
            {
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
                    pathService.SetObstacle(stairsRoom.Outline);
                }
                foreach (var buildRoom in buildRooms)
                {
                    pathService.SetRoom(buildRoom.Outline);
                }

                //添加约束终止线
                pathService.SetTermination(loopLines);
                pathService.InitData();
                //foreach (var hydrant in hydrants)
                //{
                //    if(ThHydrantConnectPipeUtils.HydrantIsContainPipe(hydrant, hydrantPipes))
                //    {
                //        //创建路径
                //        pathService.SetStartPoint(hydrant.FireHydrantPipe.PipePosition);//设置立管点为起始点
                //        pathService.SetHydrantAngle(hydrant.GetRotationAngle());
                //        var path = pathService.CreateHydrantPath(true);
                //        var brLine = ThHydrantBranchLine.Create(path);
                //        if (ConfigInfo.isSetupValve)
                //        {
                //            brLine.InsertValve();
                //        }

                //        if (ConfigInfo.isMarkSpecif)
                //        {
                //            brLine.InsertPipeMark(ConfigInfo.strMapScale);
                //        }
                //        var objcets = path.BufferPL(50)[0];
                //        var obb = objcets as Polyline;
                //        pathService.SetObstacle(obb);
                //    }
                //}
                foreach (var hydrantPipe in hydrantPipes)
                {
                    //创建路径
                    pathService.SetStartPoint(hydrantPipe.PipePosition);//设置立管点为起始点
                    var path = pathService.CreateHydrantPath(false);
                    if (path != null)
                    {
                        var brLine = ThHydrantBranchLine.Create(path);
                        if (ConfigInfo.isSetupValve)
                        {
                            brLine.InsertValve();
                        }

                        if (ConfigInfo.isMarkSpecif)
                        {
                            brLine.InsertPipeMark(ConfigInfo.strMapScale);
                        }
                        var objcets = path.BufferPL(200)[0];
                        var obb = objcets as Polyline;
                        pathService.AddObstacle(obb);
                    }
                }
                Active.Editor.WriteMessage(System.DateTime.Now.ToString("yyyy-MM-dd HH：mm：ss：ffff"));
                Active.Editor.WriteMessage("\n");
            }
            //try
            //{

            //}
            //catch (Exception ex)
            //{
            //    Active.Editor.WriteMessage(ex.Message);
            //}
        }
    }
}
