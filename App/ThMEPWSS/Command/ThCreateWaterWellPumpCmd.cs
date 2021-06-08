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

namespace ThMEPWSS.Command
{

    public class ThCreateWaterWellPumpCmd : IAcadCommand, IDisposable
    {
        WaterWellPumpConfigInfo configInfo;//配置信息
        WaterwellPumpParamsViewModel _vm;
        public ThCreateWaterWellPumpCmd(WaterwellPumpParamsViewModel vm)
        {
            _vm = vm;
            configInfo = vm.GetConfigInfo();
        }
        public List<ThWWaterWell> GetWaterWellEntityList(Tuple<Point3d, Point3d> input)
        {
            List<ThWWaterWell> waterWellList = new List<ThWWaterWell>();
            using (var database = AcadDatabase.Active())
            using (var waterwellEngine = new ThWWaterWellRecognitionEngine(configInfo.WaterWellInfo.identifyInfo))
            {
                var range = new Point3dCollection();
                range.Add(input.Item1);
                range.Add(new Point3d(input.Item1.X, input.Item2.Y, 0));
                range.Add(input.Item2);
                range.Add(new Point3d(input.Item2.X, input.Item1.Y, 0));
                waterwellEngine.RecognizeMS(database.Database, range);
                foreach (ThIfcDistributionFlowElement element in waterwellEngine.Elements)
                {
                    ThWWaterWell waterWell = ThWWaterWell.Create(element.Outline);
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
        public List<Line> GetWallColumnEdges()
        {
            using (var database = AcadDatabase.Active())
            using (var acadDb = AcadDatabase.Use(database.Database))
            {
                return acadDb.ModelSpace.OfType<Line>().Where(o => o.Layer == "墙柱边线").ToList();
            }
        }
        public List<Line> GetWallColumnEdgesInRange(Tuple<Point3d, Point3d> input)
        {
            var allEdges = GetWallColumnEdges();
            var range = new Point3dCollection();
            range.Add(input.Item1);
            range.Add(new Point3d(input.Item1.X, input.Item2.Y, 0));
            range.Add(input.Item2);
            range.Add(new Point3d(input.Item2.X, input.Item1.Y, 0));
            var spatialIndex = new ThCADCoreNTSSpatialIndex(allEdges.ToCollection());
            var dbObjects = spatialIndex.SelectCrossingPolygon(range);

            var rst = new List<Line>();
            foreach(var obj in dbObjects)
            { 
                if(obj is Line)
                {
                    rst.Add(obj as Line);
                }
            }

            return rst;
        }
        public List<Point3d> GetParkSpacePointInRange(Tuple<Point3d, Point3d> input)
        {
            using (var database = AcadDatabase.Active())
            using (var acadDb = AcadDatabase.Use(database.Database))
            {
                var range = new Point3dCollection();
                range.Add(input.Item1);
                range.Add(new Point3d(input.Item1.X, input.Item2.Y, 0));
                range.Add(input.Item2);
                range.Add(new Point3d(input.Item2.X, input.Item1.Y, 0));

                var partSpace = acadDb.ModelSpace.OfType<BlockReference>().Where(o => o.Layer == "AE-EQPM-CARS").ToList();
                var spatialIndex = new ThCADCoreNTSSpatialIndex(partSpace.ToCollection());
                var dbObjects = spatialIndex.SelectCrossingPolygon(range);

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
        public void Dispose()
        {
            //
        }
        public string WaterWellBlockFilePath
        {
            get
            {
                var path = Path.Combine(ThCADCommon.SupportPath(), "地上给水排水平面图模板_20210517.dwg");
                return path;
            }
        }
        public static int ImportBlockCount = 0;
        public void ImportBlockFile()
        {
            if(ImportBlockCount!=0)
            {
                return;
            }
            ImportBlockCount++;
            //导入一个块
            using (AcadDatabase blockDb = AcadDatabase.Open(WaterWellBlockFilePath, DwgOpenMode.ReadOnly, false))//引用模块的位置
            using (var acadDb = Linq2Acad.AcadDatabase.Active())
            {
                acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(WaterWellBlockNames.DeepWaterPump));
                acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(WaterWellBlockNames.LocationRiser));
                acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(WaterWellBlockNames.LocationRiser150));
                acadDb.Layers.Import(blockDb.Layers.ElementOrDefault("W-EQPM"));
                acadDb.Layers.Import(blockDb.Layers.ElementOrDefault("W-DRAI-EQPM"));
            }
        }
        public void Execute()
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
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
                    var input = ThWGeUtils.SelectPoints();
                    //获取集水井
                    var water_well_list = GetWaterWellEntityList(input);
                    if (water_well_list.IsNull())
                    {
                        //命令栏提示“未选中集水井”
                        //退出本次布置动作
                        return;
                    }
                    //获取墙
                    List<Line> wellLine = GetWallColumnEdgesInRange(input);
                    //获取车位
                    List<Point3d> parkPoint = GetParkSpacePointInRange(input);
                    //获取潜水泵
                    List<ThWDeepWellPump> pumpList = GetDeepWellPumpList();

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
                        waterWell.NearWall(wellLine, 50);
                        //计算集水井是否包含潜水泵
                        foreach (ThWDeepWellPump pump in pumpList)
                        {
                            if (waterWell.ContainPump(pump))
                            {
                                break;
                            }
                        }

                        if (configInfo.PumpInfo.isCoveredWaterWell)
                        {
                            if (waterWell.IsHavePump)
                            {
                                waterWell.RemovePump();
                            }
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
    }
}
