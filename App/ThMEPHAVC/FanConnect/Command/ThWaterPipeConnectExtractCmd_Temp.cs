﻿using AcHelper;
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
using ThMEPEngineCore.Algorithm;
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
            try
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
                    var fucs = ThFanConnectUtils.SelectFanCUModel(0);
                    if (fucs.Count == 0)
                    {
                        return;
                    }
                    var transformer = new ThMEPOriginTransformer(startPt);
                    var mt = Matrix3d.Displacement(startPt.GetVectorTo(Point3d.Origin));
                    //获取剪力墙
                    var shearWalls = ThBuildElementExtractService.GetShearWalls();
                    //获取结构柱
                    var columns = ThBuildElementExtractService.GetColumns();
                    //获取房间框线
                    var rooms = ThBuildElementExtractService.GetBuildRooms();
                    
                    foreach (var f in fucs)
                    {
                        f.FanPoint = transformer.Transform(f.FanPoint);
                        transformer.Transform(f.FanObb);
                    }
                    
                    //生成管路路由
                    var pipeService = new ThCreatePipeService();
                    pipeService.PipeStartPt = transformer.Transform(startPt);
                    pipeService.PipeWidth = 300.0;
                    pipeService.EquipModel = fucs;
                    foreach (var wall in shearWalls)
                    {
                        transformer.Transform(wall.Outline);
                        pipeService.AddObstacleHole(wall.Outline);
                    }
                    foreach (var column in columns)
                    {
                        transformer.Transform(column.Outline);
                        pipeService.AddObstacleHole(column.Outline);
                    }
                    foreach (var room in rooms)
                    {
                        room.UpgradeOpen();
                        transformer.Transform(room);
                        room.DowngradeOpen();
                        pipeService.AddObstacleRoom(room);
                    }
                    var plines = pipeService.CreatePipeLine(1);
                    var toDbServiece = new ThFanToDBServiece();
                    foreach (var pl in plines)
                    {
                        toDbServiece.InsertEntity(pl, "AI-水管路由");
                        transformer.Reset(pl);
                    }
                    foreach (var wall in shearWalls)
                    {
                        transformer.Reset(wall.Outline);
                    }
                    foreach (var column in columns)
                    {
                        transformer.Reset(column.Outline);
                    }
                    foreach (var room in rooms)
                    {
                        room.UpgradeOpen();
                        transformer.Reset(room);
                        room.DowngradeOpen();
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
