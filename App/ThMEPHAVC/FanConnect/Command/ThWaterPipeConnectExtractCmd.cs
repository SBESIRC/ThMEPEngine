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
                var fucs = ThFanConnectUtils.SelectFanCUModel();
                if(fucs.Count == 0)
                {
                    return;
                }
                //水管干路和支干路
                var pipes = ThFanConnectUtils.SelectPipes();
                if(pipes.Count == 0)
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
                pipeService.TrunkLines = pipes;
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
                return;
            }
        }
    }
}
