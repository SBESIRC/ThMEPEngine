using NFox.Cad;
using AcHelper;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using ThMEPElectrical.Model;
using ThMEPEngineCore.Engine;
using ThMEPElectrical.Broadcast;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Algorithm;
using System;
using ThMEPElectrical.Broadcast.Service.ClearService;
using ThMEPElectrical.Broadcast.Service;
using DotNetARX;
using ThMEPElectrical.Business.Procedure;
using ThMEPEngineCore.Service;

namespace ThMEPElectrical
{
    public class ThBroadcastCmds
    {
        readonly double bufferLength = 100;
        readonly double BlindAreaRadius = 12500;

        [CommandMethod("TIANHUACAD", "THFBS", CommandFlags.Modal)]
        public void ThBroadcast()
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                // 获取车道线
                var laneLineEngine = new ThLaneLineRecognitionEngine();
                laneLineEngine.Recognize(acdb.Database, new Point3dCollection());
                // 暂时假设车道线绘制符合要求
                var lanes = laneLineEngine.Spaces.Select(o => o.Boundary).Cast<Line>().ToList();
                if (lanes.Count == 0)
                {
                    return;
                }

                // 获取框线
                PromptSelectionOptions options = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    MessageForAdding = "选择区域",
                    RejectObjectsOnLockedLayers = true,
                };
                var dxfNames = new string[]
                {
                    RXClass.GetClass(typeof(Polyline)).DxfName,
                };
                var filter = ThSelectionFilterTool.Build(dxfNames);
                var result = Active.Editor.GetSelection(options, filter);
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                foreach (ObjectId obj in result.Value.GetObjectIds())
                {
                    var frame = acdb.Element<Polyline>(obj);

                    var parkingLinesService = new ParkingLinesService();
                    var parkingLines = parkingLinesService.CreateParkingLines(frame, lanes, out List<List<Line>> otherPLines);

                    //获取构建信息
                    var allStructure = ThBeamConnectRecogitionEngine.ExecutePreprocess(acdb.Database, frame.Vertices());

                    //获取柱
                    var columns = allStructure.ColumnEngine.Elements.Select(o => o.Outline).Cast<Polyline>().ToList();
                    var objs = new DBObjectCollection();
                    columns.ForEach(x => objs.Add(x));
                    ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                    columns = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(frame).Cast<Polyline>().ToList();

                    //获取剪力墙
                    var walls = allStructure.ShearWallEngine.Elements.Select(o => o.Outline).Cast<Polyline>().ToList();
                    objs = new DBObjectCollection();
                    walls.ForEach(x => objs.Add(x));
                    thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                    walls = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(frame).Cast<Polyline>().ToList();

                    var columnEngine = new ThColumnRecognitionEngine();
                    columnEngine.Recognize(acdb.Database, frame.Vertices());
                    var columPoly = columnEngine.Elements.Select(o => o.Outline).Cast<Polyline>().ToList();

                    ColumnService columnService = new ColumnService();
                    columnService.HandleColumns(parkingLines, otherPLines, columPoly,
                        out Dictionary<List<Line>, List<ColumnModel>> mainColumns,
                        out Dictionary<List<Line>, List<ColumnModel>> otherColumns);

                    LayoutService layoutService = new LayoutService();
                    var layoutCols = layoutService.LayoutBraodcast(frame, mainColumns, otherColumns);

                    InsertBroadcastService.InsertSprayBlock(layoutCols);
                }
            }
        }

        [CommandMethod("TIANHUACAD", "THFBS2", CommandFlags.Modal)]
        public void ThBroadcast2()
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                // 获取框线
                PromptSelectionOptions options = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    MessageForAdding = "选择区域",
                    RejectObjectsOnLockedLayers = true,
                };
                var dxfNames = new string[]
                {
                    RXClass.GetClass(typeof(Polyline)).DxfName,
                };
                var filter = ThSelectionFilterTool.Build(dxfNames);
                var result = Active.Editor.GetSelection(options, filter);
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                //获取外包框
                List<Curve> frameLst = new List<Curve>();
                foreach (ObjectId obj in result.Value.GetObjectIds())
                {
                    var frame = acdb.Element<Polyline>(obj);
                    var plFrame = ThMEPFrameService.Normalize(frame);
                    frameLst.Add(plFrame);
                    
                }

                //处理外包框线
                var plines = HandleFrame(frameLst);
                foreach (var plFrame in plines)
                {
                    //删除原有构建
                    plFrame.ClearBroadCast();
                    plFrame.ClearBlindArea();

                    //获取车道线
                    var lanes = GetLanes(plFrame, acdb);

                    //处理车道线
                    var handleLines = ThMEPLineExtension.LineSimplifier(lanes.ToCollection(), 500, 20.0, 2.0, Math.PI / 180.0);
                    var parkingLinesService = new ParkingLinesService();
                    var parkingLines = parkingLinesService.CreateNodedParkingLines(plFrame, handleLines, out List<List<Line>> otherPLines);

                    //获取构建信息
                    var bufferFrame = plFrame.Buffer(bufferLength)[0] as Polyline;
                    GetStructureInfo(acdb, bufferFrame, out List<Polyline> columns, out List<Polyline> walls);

                    //主车道布置信息
                    LayoutWithParkingLineService layoutService = new LayoutWithParkingLineService();
                    var layoutInfo = layoutService.LayoutBraodcast(plFrame, parkingLines, columns, walls);

                    //副车道布置信息
                    LayoutWithSecondaryParkingLineService layoutWithSecondaryParkingLineService = new LayoutWithSecondaryParkingLineService();
                    var resLayoutInfo = layoutWithSecondaryParkingLineService.LayoutBraodcast(layoutInfo, otherPLines, columns, walls, plFrame);

                    //计算广播盲区
                    var layoutPts = resLayoutInfo.SelectMany(x => x.Value.Keys).ToList();
                    PrintBlindAreaService blindAreaService = new PrintBlindAreaService();
                    blindAreaService.PrintBlindArea(layoutPts, plFrame, BlindAreaRadius);

                    //放置广播
                    InsertBroadcastService.InsertSprayBlock(resLayoutInfo);
                }
            }
        }

        [CommandMethod("TIANHUACAD", "THGBMQ", CommandFlags.Modal)]
        public void ThBroadcastBlindArea()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                // 获取框线
                PromptSelectionOptions options = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    MessageForAdding = "选择区域",
                    RejectObjectsOnLockedLayers = true,
                };
                var dxfNames = new string[]
                {
                    RXClass.GetClass(typeof(Polyline)).DxfName,
                };
                var filter = ThSelectionFilterTool.Build(dxfNames);
                var result = Active.Editor.GetSelection(options, filter);
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                
                acadDatabase.Database.UnFrozenLayer(ThMEPCommon.BroadcastLayerName);
                acadDatabase.Database.UnLockLayer(ThMEPCommon.BroadcastLayerName);
                acadDatabase.Database.UnOffLayer(ThMEPCommon.BroadcastLayerName);
                List<Curve> frameLst = new List<Curve>();
                foreach (ObjectId obj in result.Value.GetObjectIds())
                {
                    var frame = acadDatabase.Element<Polyline>(obj);
                    var plFrame = ThMEPFrameService.Normalize(frame);
                    frameLst.Add(plFrame);
                }
                var plines = HandleFrame(frameLst);
                foreach (var pline in plines)
                {
                    //删除原有盲区
                    pline.ClearBlindArea();

                    //获取广播布置点
                    var pts = GetLayoutBroadcastPoints(acadDatabase, pline);

                    //打印盲区
                    PrintBlindAreaService blindAreaService = new PrintBlindAreaService();
                    blindAreaService.PrintBlindArea(pts, pline, BlindAreaRadius);
                }
            }
        }

        /// <summary>
        /// 处理外包框线
        /// </summary>
        /// <param name="frameLst"></param>
        /// <returns></returns>
        private List<Polyline> HandleFrame(List<Curve> frameLst)
        {
            var polygonInfos = NoUserCoordinateWorker.MakeNoUserCoordinateWorker(frameLst);
            List<Polyline> resPLines = new List<Polyline>();
            foreach (var pInfo in polygonInfos)
            {
                resPLines.Add(pInfo.ExternalProfile);
                resPLines.AddRange(pInfo.InnerProfiles);
            }

            return resPLines;
        }

        /// <summary>
        /// 获取车道线
        /// </summary>
        /// <param name="polyline"></param>
        public List<Curve> GetLanes(Polyline polyline, AcadDatabase acdb)
        {
            var objs = new DBObjectCollection();
            var laneLines = acdb.ModelSpace
                .OfType<Curve>()
                .Where(o => o.Layer == ThMEPCommon.LANELINE_LAYER_NAME);
            laneLines.ForEach(x => objs.Add(x));

            //var bufferPoly = polyline.Buffer(1)[0] as Polyline;
            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            var sprayLines = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(polyline).Cast<Curve>().ToList();

            return sprayLines.SelectMany(x=>polyline.Trim(x).Cast<Curve>().ToList()).ToList();
        }

        /// <summary>
        /// 获取构建信息
        /// </summary>
        /// <param name="acdb"></param>
        /// <param name="polyline"></param>
        /// <param name="columns"></param>
        /// <param name="beams"></param>
        /// <param name="walls"></param>
        private void GetStructureInfo(AcadDatabase acdb, Polyline polyline, out List<Polyline> columns, out List<Polyline> walls)
        {
            //结构构建
            var allStructure = ThBeamConnectRecogitionEngine.ExecutePreprocess(acdb.Database, polyline.Vertices());
            //获取柱
            columns = allStructure.ColumnEngine.Elements.Select(o => o.Outline).Cast<Polyline>().ToList();
            var objs = new DBObjectCollection();
            columns.ForEach(x => objs.Add(x));
            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            columns = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(polyline).Cast<Polyline>().ToList();

            //获取剪力墙
            walls = allStructure.ShearWallEngine.Elements.Select(o => o.Outline).Cast<Polyline>().ToList();
            objs = new DBObjectCollection();
            walls.ForEach(x => objs.Add(x));
            thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            walls = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(polyline).Cast<Polyline>().ToList();

            //建筑构建
            using (var archWallEngine = new ThArchitectureWallRecognitionEngine())
            {
                //建筑墙
                archWallEngine.Recognize(acdb.Database, polyline.Vertices());
                var arcWall = archWallEngine.Elements.Select(x => x.Outline).Where(x => x is Polyline).Cast<Polyline>().ToList();
                objs = new DBObjectCollection();
                arcWall.ForEach(x => objs.Add(x));
                thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                walls.AddRange(thCADCoreNTSSpatialIndex.SelectCrossingPolygon(polyline).Cast<Polyline>().ToList());
            }
        }

        /// <summary>
        /// 获取广播布置点位
        /// </summary>
        /// <param name="acdb"></param>
        /// <param name="polyline"></param>
        /// <returns></returns>
        private List<Point3d> GetLayoutBroadcastPoints(AcadDatabase acdb, Polyline polyline)
        {
            //获取广播
            var dxfNames = new string[]
            {
                    RXClass.GetClass(typeof(BlockReference)).DxfName,
            };
            var filterlist = OpFilter.Bulid(o =>
            o.Dxf((int)DxfCode.LayerName) == ThMEPCommon.BroadcastLayerName &
            o.Dxf((int)DxfCode.Start) == string.Join(",", dxfNames));
            var braodcasts = new List<BlockReference>();
            var allBraodcasts = Active.Editor.SelectAll(filterlist);
            if (allBraodcasts.Status == PromptStatus.OK)
            {
                foreach (ObjectId obj in allBraodcasts.Value.GetObjectIds())
                {
                    braodcasts.Add(acdb.Element<BlockReference>(obj));
                }
            }
            var objs = new DBObjectCollection();
            braodcasts.Where(o => polyline.Contains(o.Position)).ForEachDbObject(o => objs.Add(o));

            return braodcasts.Select(o => o.Position).ToList();
        }
    }
}
