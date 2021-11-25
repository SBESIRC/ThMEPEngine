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
            throw new NotImplementedException();
        }
        public override void SubExecute()
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var database = AcadDatabase.Active())
            {
                double pipeWidth = 200.0;
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
                //水管干路和支干路
                var pipes = ThEquipElementExtractServiece.GetFanPipes();
                //获取房间框线
                var rooms = ThBuildElementExtractServiece.GetBuildRooms();
                ////AI洞口
                var holes = ThBuildElementExtractServiece.GetAIHole();
                //生成管路路由
                var pipeService = new ThCreatePipeService();
                pipeService.PipeWidth = pipeWidth;
                pipeService.EquipModel = fucs;
                pipeService.TrunkLines = pipes;
                foreach (var room in rooms)
                {
                    pipeService.AddObstacleRoom(room);
                }
                foreach(var hole in holes)
                {
                    pipeService.AddObstacleHole(hole);
                }
                var plines = pipeService.CreatePipeLine(0);
                var toDbServiece = new ThFanToDBServiece();
                foreach (var pl in plines)
                {
                    toDbServiece.InsertEntity(pl , "AI-水管路由示意");
                }
                return;
            }
        }
    }
}
