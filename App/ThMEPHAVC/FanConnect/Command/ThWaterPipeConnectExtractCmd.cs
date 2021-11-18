using Dreambuild.AutoCAD;
using Linq2Acad;
using System;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Command;
using ThMEPHVAC.FanConnect.Service;
using ThMEPHVAC.FanConnect.ViewModel;

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
                //选择一个点
                var pt = ThFanConnectUtils.SelectPoint();
                var startPt = ThBuildElementExtractServiece.GetPipeStartPt(pt.CreateSquare(50).Vertices());
                //获取范围
                var area = ThFanConnectUtils.SelectArea();
                //获取风机设备
                var fucs = ThEquipElementExtractServiece.GetFCUModels(area);
                //获取剪力墙
                var shearWalls = ThBuildElementExtractServiece.GetShearWalls(area);
                //获取结构柱
                var columns = ThBuildElementExtractServiece.GetColumns(area);
                //获取房间框线
                var rooms = ThBuildElementExtractServiece.GetBuildRooms(area);
                //AI洞口
                var holes = ThBuildElementExtractServiece.GetAIHole(area);
                //生成管路路由
                var pipeService = new ThCreatePipeService();
                pipeService.PipeStartPt = startPt;

                foreach (var shearWall in shearWalls)
                {
                    pipeService.AddObstacleHole(shearWall.Outline);
                }
                foreach (var column in columns)
                {
                    pipeService.AddObstacleHole(column.Outline);
                }
                foreach (var room in rooms)
                {
                    pipeService.AddObstacleRoom(room.Outline);
                }
                foreach(var hole in holes)
                {
                    pipeService.AddObstacleHole(hole);
                }
                foreach(var fuc in fucs)
                {
                    pipeService.AddEquipPoint(fuc.FanPoint);
                    pipeService.AddEquipmentObbs(fuc.FanObb);
                }
                var pipeTree = pipeService.CreatePipeLine(0);
                //扩展管路
                ThWaterPipeExtendServiece pipeExtendServiece = new ThWaterPipeExtendServiece();
                pipeExtendServiece.ConfigInfo = ConfigInfo;
                pipeExtendServiece.PipeExtend(pipeTree);
            }
        }
    }
}
