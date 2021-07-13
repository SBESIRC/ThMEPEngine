using AcHelper;
using AcHelper.Commands;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using Linq2Acad;
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
    class ThHydrantConnectPipeCmd : IAcadCommand, IDisposable
    {
        public void Dispose()
        {
        }
        public void Execute()
        {
            try
            {
                using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
                using (var database = AcadDatabase.Active())
                {
                    var range = ThWGeUtils.SelectRange();//获取范围
                    var civilAirWalls = ThHydrantDataManager.GetCivilAirWalls(range);//获取人防墙
                    var electricWells = ThHydrantDataManager.GetElectricWells(range);//获取电井
                    var fireShutters = ThHydrantDataManager.GetFireShutters(range);//获取防火卷帘
                    var shearWalls = ThHydrantDataManager.GetShearWalls(range);//获取剪力墙
                    var stairsRooms = ThHydrantDataManager.GetStairsRooms(range);//获取楼梯间
                    var structureCols = ThHydrantDataManager.GetStructuralCols(range);//获取结构柱
                    var structureWalls = ThHydrantDataManager.GetStructureWalls(range);//获取结构墙
                    var windWells = ThHydrantDataManager.GetWindWells(range);//获取风井
                    var hydrants = ThHydrantDataManager.GetFireHydrants(range);//获取消火栓 
                    var hydrantPipes = ThHydrantDataManager.GetFireHydrantPipes(range);//获取立管

                    List<Line> loopLines = new List<Line>();
                    List<Line> branchLines = new List<Line>();
                    ThHydrantDataManager.GetHydrantLoopAndBranchLines(ref loopLines,ref branchLines, range);//获取环管和支路
                    
                    var pathService = new ThCreateHydrantPathService();
                    //添加障碍
                    foreach (var civilAirWall in civilAirWalls)
                    {
                        pathService.SetObstacle(civilAirWall.ElementObb);
                    }
                    foreach (var electricWell in electricWells)
                    {
                        pathService.SetObstacle(electricWell.ElementObb);
                    }
                    foreach (var fireShutter in fireShutters)
                    {
                        pathService.SetObstacle(fireShutter.ElementObb);
                    }
                    foreach (var shearWall in shearWalls)
                    {
                        pathService.SetObstacle(shearWall.ElementObb);
                    }
                    foreach (var stairsRoom in stairsRooms)
                    {
                        pathService.SetObstacle(stairsRoom.ElementObb);
                    }
                    foreach (var structureCol in structureCols)
                    {
                        pathService.SetObstacle(structureCol.ElementObb);
                    }
                    foreach (var structureWall in structureWalls)
                    {
                        pathService.SetObstacle(structureWall.ElementObb);
                    }
                    foreach (var windWell in windWells)
                    {
                        pathService.SetObstacle(windWell.ElementObb);
                    }
                    //foreach (var line in branchLines)
                    //{
                    //    var objcets = line.Buffer(50);
                    //    var obb = objcets as Polyline;
                    //    pathService.SetObstacle(obb);
                    //}

                    //添加约束终止线
                    pathService.SetTermination(loopLines);

                    foreach (var hydrant in hydrants)
                    {
                        if(ThHydrantConnectPipeUtils.HydrantIsContainPipe(hydrant, hydrantPipes))
                        {
                            //创建路径
                            pathService.SetStartPoint(hydrant.FireHydrantPipe.PipePosition);//设置立管点为起始点
                            pathService.SetHydrantAngle(hydrant.GetRotationAngle());
                            var path = pathService.CreateHydrantPath(false);
                        }
                    }
                    
                    foreach (var hydrantPipe in hydrantPipes)
                    {
                        //创建路径
                        pathService.SetStartPoint(hydrantPipe.PipePosition);//设置立管点为起始点
                        var path = pathService.CreateHydrantPath(false);
                    }
                }
            }
            catch (Exception ex)
            {
                Active.Editor.WriteMessage(ex.Message);
            }

            
        }
    }
}
