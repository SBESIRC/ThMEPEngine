using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;
using ThMEPEngineCore.Command;
using ThMEPHVAC.FanConnect.Service;
using ThMEPHVAC.FanLayout.Service;

namespace ThMEPHVAC.FanConnect.Command
{
    class ThWaterPipeConnectExtractCmd_Temp : ThMEPBaseCommand, IDisposable
    {
        public void Dispose()
        {
        }
        public ThWaterPipeConnectExtractCmd_Temp()
        {
            CommandName = "THLGTEMP";
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
                //获取起点
                var startPt = ThFanConnectUtils.SelectPoint();
                if (startPt.IsEqualTo(new Point3d()))
                {
                    return;
                }
                //获取风机设备
                var fucs = ThFanConnectUtils.SelectFanCUModel();
                if (fucs.Count == 0)
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
                pipeService.PipeStartPt = startPt;
                pipeService.PipeWidth = 300.0;
                pipeService.EquipModel = fucs;
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
                var plines = pipeService.CreatePipeLine(1);
                var toDbServiece = new ThFanToDBServiece();
                foreach (var pl in plines)
                {
                    toDbServiece.InsertEntity(pl, "AI-水管路由");
                }
                return;
            }
        }
    }
}
