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
                waterwellEngine.Recognize(database.Database, range);
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
        public List<Line> GetWallColumnEdges(Point3dCollection polygon)
        {
            var results = new List<Line>();
            using (var acadDatabase = AcadDatabase.Active())
            using (var columnEngine = new ThColumnRecognitionEngine())
            using (var shearWallEngine = new ThShearWallRecognitionEngine())
            using (var archWallEngine = new ThDB3ArchWallRecognitionEngine())
            {
                columnEngine.Recognize(acadDatabase.Database, polygon);
                shearWallEngine.Recognize(acadDatabase.Database, polygon);
                archWallEngine.Recognize(acadDatabase.Database, polygon);
                results.AddRange(ToLines(columnEngine.Elements.Cast<ThIfcColumn>().Select(o => o.Outline).ToList()));
                results.AddRange(ToLines(shearWallEngine.Elements.Cast<ThIfcWall>().Select(o => o.Outline).ToList()));
                results.AddRange(ToLines(archWallEngine.Elements.Cast<ThIfcWall>().Select(o => o.Outline).ToList()));
                return results;
            }
        }
        private List<Line> ToLines(List<Entity> entities)
        {
            //要设置分割长度TesslateLength
            var results = new List<Line>();
            entities.ForEach(o =>
            {
                if(o is Polyline polyline)
                {
                    results.AddRange(polyline.ToLines());
                }
                else if(o is MPolygon mPolygon)
                {
                    results.AddRange(mPolygon.Loops().SelectMany(l => l.ToLines()));
                }
                else if(o is Circle circle)
                {
                    results.AddRange(circle.Tessellate(50.0).ToLines());
                }
                else
                {
                    throw new NotSupportedException();
                }
            });
            return results;
        }
        public List<Line> GetWallColumnEdgesInRange(Tuple<Point3d, Point3d> input)
        {
            var range = new Point3dCollection();
            range.Add(input.Item1);
            range.Add(new Point3d(input.Item1.X, input.Item2.Y, 0));
            range.Add(input.Item2);
            range.Add(new Point3d(input.Item2.X, input.Item1.Y, 0));
            var allEdges = GetWallColumnEdges(range);
            //var spatialIndex = new ThCADCoreNTSSpatialIndex(allEdges.ToCollection());
            //var dbObjects = spatialIndex.SelectCrossingPolygon(range);

            //var rst = new List<Line>();
            //foreach(var obj in dbObjects)
            //{ 
            //    if(obj is Line)
            //    {
            //        rst.Add(obj as Line);
            //    }
            //}

            return allEdges;
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
        public List<BlockReference> GetPipeInRange(Tuple<Point3d, Point3d> input)
        {
            using (var database = AcadDatabase.Active())
            using (var acadDb = AcadDatabase.Use(database.Database))
            {
                var range = new Point3dCollection();
                range.Add(input.Item1);
                range.Add(new Point3d(input.Item1.X, input.Item2.Y, 0));
                range.Add(input.Item2);
                range.Add(new Point3d(input.Item2.X, input.Item1.Y, 0));

                var partSpace = acadDb.ModelSpace.OfType<BlockReference>().Where(o => o.GetEffectiveName() == "带定位立管" || o.GetEffectiveName() == "带定位立管150").ToList();
                var spatialIndex = new ThCADCoreNTSSpatialIndex(partSpace.ToCollection());
                var dbObjects = spatialIndex.SelectCrossingPolygon(range);

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
        public static bool BlockImported = false;
        public void ImportBlockFile()
        {
            if(BlockImported)
            {
                return;
            }
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

            BlockImported = true;
        }
        public void Execute()
        {
            try
            {
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
                        var input = ThWGeUtils.SelectPoints();
                        if(input.Item1.IsEqualTo(input.Item2))
                        {
                            return;
                        }

                        //获取集水井
                        var water_well_list = GetWaterWellEntityList(input);
                        if (water_well_list.IsNull())
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
