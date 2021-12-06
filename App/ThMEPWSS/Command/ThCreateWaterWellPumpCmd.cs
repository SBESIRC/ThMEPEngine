using System;
using System.Linq;
using Linq2Acad;
using AcHelper.Commands;
using System.Collections.Generic;
using ThMEPWSS.Pipe.Model;
using AcHelper;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPWSS.Pipe;
using ThMEPWSS.Pipe.Engine;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using ThCADCore.NTS;
using NFox.Cad;
using System.IO;
using ThCADExtension;
using ThMEPWSS.Diagram.ViewModel;
using ThMEPEngineCore.CAD;
using ThMEPWSS.WaterWellPumpLayout.Interface;
using ThMEPWSS.WaterWellPumpLayout.Service;
using DotNetARX;
using ThMEPEngineCore.Command;

namespace ThMEPWSS.Command
{

    public class ThCreateWaterWellPumpCmd : ThMEPBaseCommand, IDisposable
    {
        WaterWellPumpConfigInfo configInfo;//配置信息
        WaterwellPumpParamsViewModel _vm;
        public ThCreateWaterWellPumpCmd(WaterwellPumpParamsViewModel vm)
        {
            _vm = vm;
            ActionName = "布置";
            CommandName = "THSJSB";
            configInfo = vm.GetConfigInfo();
        }
        public List<ThWWaterWell> GetWaterWellEntityList(Point3dCollection input)
        {
            List<ThWWaterWell> waterWellList = new List<ThWWaterWell>();
            using (var database = AcadDatabase.Active())
            using (var waterwellEngine = new ThWWaterWellRecognitionEngine(configInfo.WaterWellInfo.identifyInfo))
            {
                waterwellEngine.Recognize(database.Database, input);
                waterwellEngine.RecognizeMS(database.Database, input);
                var objIds = new ObjectIdCollection(); // Print
                foreach (var element in waterwellEngine.Datas)
                {
                    ThWWaterWell waterWell = ThWWaterWell.Create(element);
                    waterWell.Init();
                    waterWellList.Add(waterWell);
                }              
            }
            return waterWellList;
        }
        public List<ThWDeepWellPump> GetDeepWellPumpList()
        {
            List<ThWDeepWellPump> deepWellPump = new List<ThWDeepWellPump>();
            using (var database = AcadDatabase.Active())
            using (var engine = new ThWDeepWellPumpEngine())
            {
                var range = new Point3dCollection();
                engine.RecognizeMS(database.Database, range);
                foreach (ThIfcDistributionFlowElement element in engine.Elements)
                {
                    ThWDeepWellPump pump = ThWDeepWellPump.Create(element.Outline.ObjectId);
                    deepWellPump.Add(pump);
                }
            }
            return deepWellPump;
        }


        public List<Line> GetRoomLine(Point3dCollection range)
        {
            using (var database = AcadDatabase.Active())
            using (var acadDb = AcadDatabase.Use(database.Database))
            {
                List<Line> resLine = new List<Line>();
                var partSpace = acadDb.ModelSpace.OfType<Entity>()
                        .Where(o => o.Layer.Contains("AI-房间框线")).ToList();
                var spatialIndex = new ThCADCoreNTSSpatialIndex(partSpace.ToCollection());
                var dbObjects = spatialIndex.SelectCrossingPolygon(range);

                foreach (var obj in dbObjects)
                {
                    if (obj is Polyline)
                    {
                        var pl = obj as Polyline;
                        resLine.AddRange(pl.ToLines());
                    }
                }
                return resLine;
            }
        }
        public List<Line> GetWallColumnEdgesInRange(Point3dCollection input)
        {
            IWallEdgeData wallData = new ThDB3WallEdgeService();
            List<Line> resLine = new List<Line>();
            resLine.AddRange(GetRoomLine(input));
            resLine.AddRange(wallData.GetWallEdges(Active.Database, input));
            return resLine;
        }
        public List<Point3d> GetParkSpacePointInRange(Point3dCollection input)
        {
            using (var database = AcadDatabase.Active())
            using (var acadDb = AcadDatabase.Use(database.Database))
            {
                var partSpace = acadDb.ModelSpace.OfType<BlockReference>().Where(o => o.Layer == "AE-EQPM-CARS").ToList();
                var spatialIndex = new ThCADCoreNTSSpatialIndex(partSpace.ToCollection());
                var dbObjects = spatialIndex.SelectCrossingPolygon(input);

                var rst = new List<Point3d>();
                foreach (var obj in dbObjects)
                {
                    if (obj is BlockReference)
                    {
                        var blk = obj as BlockReference;
                        rst.Add(blk.Position);
                    }
                }
                return rst;
            }
        }
        public List<BlockReference> GetPipeInRange(Point3dCollection input)
        {
            using (var database = AcadDatabase.Active())
            using (var acadDb = AcadDatabase.Use(database.Database))
            {
                var partSpace = acadDb.ModelSpace.OfType<BlockReference>()
                    .Where(o => !o.BlockTableRecord.IsNull && (o.GetEffectiveName() == "带定位立管" || o.GetEffectiveName() == "带定位立管150")).ToList();
                var spatialIndex = new ThCADCoreNTSSpatialIndex(partSpace.ToCollection());
                var dbObjects = spatialIndex.SelectCrossingPolygon(input);

                var rst = new List<BlockReference>();
                foreach (var obj in dbObjects)
                {
                    if (obj is BlockReference)
                    {
                        var blk = obj as BlockReference;
                        rst.Add(blk);
                    }
                }
                return rst;
            }
        }
        public void Dispose()
        {
            //
        }
        public string WaterWellBlockFilePath
        {
            get
            {
                var path = ThCADCommon.WSSDwgPath();
                return path;
            }
        }
        public void ImportBlockFile()
        {
            //导入一个块
            using (AcadDatabase blockDb = AcadDatabase.Open(WaterWellBlockFilePath, DwgOpenMode.ReadOnly, false))//引用模块的位置
            using (var acadDb = Linq2Acad.AcadDatabase.Active())
            {
                if (blockDb.Blocks.Contains(WaterWellBlockNames.DeepWaterPump))
                {
                    acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(WaterWellBlockNames.DeepWaterPump));
                }
                if (blockDb.Blocks.Contains(WaterWellBlockNames.LocationRiser))
                {
                    acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(WaterWellBlockNames.LocationRiser));
                }
                if (blockDb.Blocks.Contains(WaterWellBlockNames.LocationRiser150))
                {
                    acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(WaterWellBlockNames.LocationRiser150));
                }
                if (blockDb.Layers.Contains("W-EQPM"))
                {
                    acadDb.Layers.Import(blockDb.Layers.ElementOrDefault("W-EQPM"));
                }
                if (blockDb.Layers.Contains("W-DRAI-EQPM"))
                {
                    acadDb.Layers.Import(blockDb.Layers.ElementOrDefault("W-DRAI-EQPM"));
                }
            }
        }
        public override void SubExecute()
        {
            try
            {
                ThMEPWSS.Common.Utils.FocusMainWindow();
                using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
                using (var database = AcadDatabase.Active())
                {
                    ImportBlockFile();

                    //获取配置信息
                    if (configInfo.PumpInfo.PumpLyoutType == LAYOUTTYPE.DOTCHOICE)
                    {
                        //获取选择数据 
                        //获取集水井区域 
                    }
                    else if (configInfo.PumpInfo.PumpLyoutType == LAYOUTTYPE.BOXCHOICE)
                    {
                        var input = Common.Utils.SelectAreas();
                        //获取集水井
                        var water_well_list = GetWaterWellEntityList(input);
                        if (water_well_list.Count == 0)
                        {
                            //命令栏提示“未选中集水井”
                            //退出本次布置动作
                            return;
                        }
                        //获取墙
                        List<Line> wallLine = GetWallColumnEdgesInRange(input);
                        //获取车位
                        List<Point3d> parkPoint = GetParkSpacePointInRange(input);
                        //获取潜水泵
                        List<ThWDeepWellPump> pumpList = GetDeepWellPumpList();
                        //获取带定位水管
                        List<BlockReference> pipeList = GetPipeInRange(input);
                        //添加排水泵
                        foreach (ThWWaterWell waterWell in water_well_list)
                        {
                            //开启尺寸过滤
                            if (configInfo.WaterWellInfo.isWaterWellSizeFilter)
                            {
                                double area = waterWell.GetAcreage();
                                if (area < configInfo.WaterWellInfo.fMinacreage)
                                {
                                    continue;
                                }
                            }
                            //计算集水井是否靠近墙
                            waterWell.ParkSpacePoint = parkPoint;
                            waterWell.NearWall(wallLine, 50);
                            //计算潜水泵是否在集水井内
                            foreach (ThWDeepWellPump pump in pumpList)
                            {
                                if (waterWell.ContainPump(pump))
                                {
                                    break;
                                }
                            }
                            //计算水管是否在集水井内
                            foreach (var pump in pipeList)
                            {
                                waterWell.ContainPipe(pump);
                            }

                            if (configInfo.PumpInfo.isCoveredWaterWell)
                            {
                                if (waterWell.IsHavePump)
                                {
                                    waterWell.RemovePump();
                                }
                                waterWell.RemovePipe();
                                if (!waterWell.AddDeepWellPump(configInfo))
                                {
                                    //提示或写入日志表当前集水井添加泵失败
                                }
                            }
                            else
                            {
                                if (waterWell.IsHavePump)
                                {
                                    continue;
                                }
                                if (!waterWell.AddDeepWellPump(configInfo))
                                {
                                    //提示或写入日志表当前集水井添加泵失败
                                }
                            }
                        }
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