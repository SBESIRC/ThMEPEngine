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
                    acadDb.Layers.Import(blockDb.Layers.ElementOrDefault("AI-水管路由"), false);
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
            try
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
                    var pipes = ThEquipElementExtractService.GetFanPipes(startPt);
                    //水管干路和支干路
                    if (pipes.Count == 0)
                    {
                        return;
                    }
                    var fucs = ThFanConnectUtils.SelectFanCUModel(ConfigInfo.WaterSystemConfigInfo.SystemType);
                    if (fucs.Count == 0)
                    {
                        return;
                    }
                    foreach (var p in pipes)
                    {
                        p.TransformBy(mt);
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
                    //获取剪力墙
                    var shearWalls = ThBuildElementExtractService.GetShearWalls();
                    //获取结构柱
                    var columns = ThBuildElementExtractService.GetColumns();
                    //获取房间框线
                    var rooms = ThBuildElementExtractService.GetBuildRooms();
                    //生成管路路由
                    var pipeService = new ThCreatePipeService();
                    pipeService.PipeWidth = pipeWidth;
                    pipeService.EquipModel = fucs;
                    pipeService.TrunkLines = tmpLines;
                    foreach(var f in fucs)
                    {
                        pipeService.AddObstacleHole(f.FanObb);
                    }
                    foreach (var wall in shearWalls)
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
                    //添加需求ID:1001796
                    var allLines = new List<Line>();
                    allLines.AddRange(pipes);
                    foreach (var pl in plines)
                    {
                        allLines.AddRange(pl.ToLines());
                    }
                    var tmpFcus = ThEquipElementExtractService.GetFCUModels(ConfigInfo.WaterSystemConfigInfo.SystemType);
                    if (tmpFcus.Count == 0)
                    {
                        return;
                    }
                    ///处理数据---查找到需要删除的末端
                    var handlePipeService = new ThHandleFanPipeService()
                    {
                        StartPoint = startPt,
                        AllFan = tmpFcus,
                        AllLine = allLines
                    };
                    var tmpTree = handlePipeService.HandleFanPipe(mt);
                    if (tmpTree == null)
                    {
                        return;
                    }
                    var dbObjs = handlePipeService.GetDbPipes(out string layer, out int colorIndex);
                    handlePipeService.RemoveDbPipe(tmpTree, dbObjs, mt);
                    var rightLines = handlePipeService.GetRightLine(tmpTree,mt);//已经处理好的线
                    var toDbServiece = new ThFanToDBServiece();
                    foreach (var path in rightLines)
                    {
                        toDbServiece.InsertEntity(path, layer, colorIndex);
                    }
                    return;
                }
            }
            catch (Exception ex)
            {
                Active.Editor.WriteMessage(ex.Message);
            }

        }
    }
}
