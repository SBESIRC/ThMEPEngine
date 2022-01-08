using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Command;
using ThMEPEngineCore.Service;
using ThMEPHVAC.FanConnect.Model;
using ThMEPHVAC.FanConnect.Service;
using ThMEPHVAC.FanConnect.ViewModel;
using ThMEPHVAC.FanLayout.Service;

namespace ThMEPHVAC.FanConnect.Command
{
    public class ThWaterPipeConnectExtractCmd : ThMEPBaseCommand, IDisposable
    {
        public ThWaterPipeConfigInfo ConfigInfo { set; get; }//界面输入信息
        public void Dispose()
        {
        }
        public ThWaterPipeConnectExtractCmd()
        {
            CommandName = "THSPM";
            ActionName = "生成水管路由";
        }
        public void ImportBlockFile()
        {
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.HvacPipeDwgPath(), DwgOpenMode.ReadOnly, false))//引用模块的位置
            using (var acadDb = Linq2Acad.AcadDatabase.Active())
            {
                if (blockDb.Layers.Contains("AI-水管路由"))
                {
                    acadDb.Layers.Import(blockDb.Layers.ElementOrDefault("AI-水管路由"), true);
                }
            }
            using (var acadDb = Linq2Acad.AcadDatabase.Active())
            {
                acadDb.Database.UnFrozenLayer("AI-水管路由");
                acadDb.Database.UnLockLayer("AI-水管路由");
                acadDb.Database.UnOffLayer("AI-水管路由");
            }
        }
        public override void SubExecute()
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var database = AcadDatabase.Active())
            {
                ImportBlockFile();
                double pipeWidth = 300.0;
                switch (ConfigInfo.WaterSystemConfigInfo.SystemType)//系统
                {
                    case 0://水系统
                        {
                            switch (ConfigInfo.WaterSystemConfigInfo.PipeSystemType)//管制
                            {
                                case 0://两管制
                                    {
                                        pipeWidth = 200.0;
                                    }
                                    break;
                                case 1://四管制
                                    {
                                        pipeWidth = 400.0;
                                    }
                                    break;
                                default:
                                    break;
                            }
                        }
                        break;
                    case 1://冷媒系统
                        {
                            pipeWidth = 200.0;
                        }
                        break;
                    default:
                        break;
                }
                //获取风机设备
                var startPt = ThFanConnectUtils.SelectPoint();
                if (startPt.IsEqualTo(new Point3d()))
                {
                    return;
                }
                //提取水管路由
                var mt = Matrix3d.Displacement(startPt.GetVectorTo(Point3d.Origin));
                var pipes = ThEquipElementExtractServiece.GetFanPipes(startPt);
                foreach(var p in pipes)
                {
                    p.TransformBy(mt);
                }
                //水管干路和支干路
                if (pipes.Count == 0)
                {
                    return;
                }
                //处理pipes 1.清除重复线段 ；2.将同线的线段连接起来；
                ThLaneLineCleanService cleanServiec = new ThLaneLineCleanService();
                var lineColl = cleanServiec.CleanNoding(pipes.ToCollection());
                foreach (var p in pipes)
                {
                    p.TransformBy(mt.Inverse());
                }
                var tmpLines = new List<Line>();
                foreach (var l in lineColl)
                {
                    var line = l as Line;
                    line.TransformBy(mt.Inverse());
                    tmpLines.Add(line);
                }

                var fucs = ThFanConnectUtils.SelectFanCUModel();
                if(fucs.Count == 0)
                {
                    return;
                }
                //获取剪力墙
                var shearWalls = ThBuildElementExtractServiece.GetShearWalls();
                //获取结构柱
                var columns = ThBuildElementExtractServiece.GetColumns();
                //获取房间框线
                var rooms = ThBuildElementExtractServiece.GetBuildRooms();
                //生成管路路由
                var pipeService = new ThCreatePipeService();
                pipeService.PipeWidth = pipeWidth;
                pipeService.EquipModel = fucs;
                pipeService.TrunkLines = tmpLines;
                foreach(var wall in shearWalls)
                {
                    pipeService.AddObstacleHole(wall.Outline);
                }
                foreach (var column in columns)
                {
                    pipeService.AddObstacleHole(column.Outline);
                }
                foreach (var room in rooms)
                {
                    pipeService.AddObstacleRoom(room);
                }
                var plines = pipeService.CreatePipeLine(0);
                var toDbServiece = new ThFanToDBServiece();
                foreach (var pl in plines)
                {
                    toDbServiece.InsertEntity(pl , "AI-水管路由");
                }

                //添加需求ID:1001796
                var allLines = new List<Line>();
                allLines.AddRange(pipes);
                foreach(var pl in plines)
                {
                    allLines.AddRange(pl.ToLines());
                }
                var tmpFcus = ThEquipElementExtractServiece.GetFCUModels();
                if (tmpFcus.Count == 0)
                {
                    return;
                }
                var remSurplusPipe = new ThRemSurplusPipe()
                {
                    StartPoint = startPt,
                    AllLine = allLines,
                    AllFan = tmpFcus
                };
                remSurplusPipe.RemSurplusPipe();
                return;
            }
        }
    }
}
