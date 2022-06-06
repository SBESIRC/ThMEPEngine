using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using AcHelper;
using Linq2Acad;
using AcHelper.Commands;
using NFox.Cad;
using ThMEPEngineCore.CAD;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Command;
using ThMEPWSS.Pipe;
using ThMEPWSS.Pipe.Engine;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;

using ThMEPWSS.Diagram.ViewModel;
using ThMEPWSS.Pipe.Model;
using ThMEPWSS.WaterWellPumpLayout.Interface;
using ThMEPWSS.WaterWellPumpLayout.Service;
using ThMEPWSS.WaterWellPumpLayout.Model;

namespace ThMEPWSS.Command
{

    public class ThCreateWaterWellPumpCmd : ThMEPBaseCommand, IDisposable
    {
        public WaterWellPumpConfigInfo ConfigInfo;//配置信息
        public WaterwellPumpParamsViewModel _vm;
        public ObservableCollection<ThWaterWellConfigInfo> WellConfigInfo { set; get; }
        public ThCreateWaterWellPumpCmd(WaterwellPumpParamsViewModel vm)
        {
            _vm = vm;
            ActionName = "布置";
            CommandName = "THSJSB";
            ConfigInfo = vm.GetConfigInfo();
        }
        public List<ThWWaterWell> GetWaterWellEntityList(Point3dCollection input)
        {
            List<ThWWaterWell> waterWellList = new List<ThWWaterWell>();
            using (var database = AcadDatabase.Active())
            using (var waterwellEngine = new ThWWaterWellRecognitionEngine(ConfigInfo.WaterWellInfo.identifyInfo))
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

        public List<Line> GetRoomLine(Point3dCollection range)
        {
            using (var database = AcadDatabase.Active())
            using (var acadDb = AcadDatabase.Use(database.Database))
            {
                List<Line> resLine = new List<Line>();
                var roomLines = acadDb.ModelSpace.OfType<Entity>()
                        .Where(o => o.Layer.Contains("AI-房间框线")).ToList();
                if (range.Count == 0)
                {
                    foreach (var l in roomLines)
                    {
                        if (l is Polyline pline)
                        {
                            resLine.AddRange(pline.ToLines());
                        }
                        else if (l is Line line)
                        {
                            resLine.Add(line);
                        }
                    }
                }
                else
                {
                    var spatialIndex = new ThCADCoreNTSSpatialIndex(roomLines.ToCollection());
                    var dbObjects = spatialIndex.SelectCrossingPolygon(range);

                    foreach (var obj in dbObjects)
                    {
                        if (obj is Polyline pline)
                        {
                            resLine.AddRange(pline.ToLines());
                        }
                        else if (obj is Line line)
                        {
                            resLine.Add(line);
                        }
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
                acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(WaterWellBlockNames.DeepWaterPump),true);
                acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(WaterWellBlockNames.LocationRiser), true);
                acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(WaterWellBlockNames.LocationRiser150), true);
                acadDb.Layers.Import(blockDb.Layers.ElementOrDefault("W-EQPM"), true);
                acadDb.Layers.Import(blockDb.Layers.ElementOrDefault("W-DRAI-EQPM"), true);
            }
        }
        public override void SubExecute()
        {
            try
            {
                Common.Utils.FocusMainWindow();
                using (var doclock = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument.LockDocument())
                using (var database = AcadDatabase.Active())
                {
                    if (WellConfigInfo == null || WellConfigInfo.Count == 0)
                    {
                        return;
                    }
                    ImportBlockFile();
                    var input = new Point3dCollection();
                    //获取墙
                    List<Line> wallLine = GetWallColumnEdgesInRange(input);
                    //获取潜水泵
                    ThWaterWellPumpUtils.GetPumpIndex(out var pumpIndex, out var pumpDict);

                    foreach (var info in WellConfigInfo)
                    {
                        foreach (var well in info.WellModelList)
                        {
                            //foreach (var pump in pumpList)
                            //{
                            well.CheckHavePumpIndex(pumpIndex, pumpDict);
                            //}
                            well.NearWall(wallLine, 50.0);
                        }
                    }

                    double fontHeight = 525;
                    switch (ConfigInfo.PumpInfo.strMapScale)
                    {
                        case "1:50":
                            fontHeight = 175;
                            break;
                        case "1:100":
                            fontHeight = 350;
                            break;
                        case "1:150":
                            fontHeight = 525;
                            break;
                        default:
                            break;
                    }
                    var toDbService = new ThWaterWellToDBService();
                    foreach (var info in WellConfigInfo)
                    {
                        foreach (var well in info.WellModelList)
                        {
                            if (ConfigInfo.PumpInfo.isCoveredWaterWell)
                            {
                                if (well.IsHavePump)
                                {
                                    toDbService.RemovePumpInDb(well.PumpModel);
                                    well.PumpModel = null;
                                    well.IsHavePump = false;
                                    //删除对应的水泵
                                }
                                toDbService.InsertPumpToDb(well, int.Parse(info.PumpCount), info.PumpNumber, fontHeight);
                            }
                            else
                            {
                                if (well.IsHavePump)
                                {
                                    continue;
                                }
                                toDbService.InsertPumpToDb(well, int.Parse(info.PumpCount), info.PumpNumber, fontHeight);
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