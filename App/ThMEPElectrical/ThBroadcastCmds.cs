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

namespace ThMEPElectrical
{
    public class ThBroadcastCmds
    {

        //[CommandMethod("TIANHUACAD", "THPL", CommandFlags.Modal)]
        //public void ThParkingline()
        //{
        //    using (AcadDatabase acdb = AcadDatabase.Active())
        //    {
        //        // 获取框线
        //        PromptSelectionOptions options = new PromptSelectionOptions()
        //        {
        //            AllowDuplicates = false,
        //            MessageForAdding = "选择区域",
        //            RejectObjectsOnLockedLayers = true,
        //        };
        //        var dxfNames = new string[]
        //        {
        //            RXClass.GetClass(typeof(Polyline)).DxfName,
        //        };
        //        var filter = ThSelectionFilterTool.Build(dxfNames);
        //        var result = Active.Editor.GetSelection(options, filter);
        //        if (result.Status != PromptStatus.OK)
        //        {
        //            return;
        //        }

        //        foreach (ObjectId obj in result.Value.GetObjectIds())
        //        {
        //            var frame = acdb.Element<Polyline>(obj);
        //            var objs = new DBObjectCollection();
        //            var pLines = acdb.ModelSpace
        //                .OfType<Curve>()
        //                .Where(o => o.Layer == "AD-SIGN");
        //            pLines.ForEach(x => objs.Add(x));

        //            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
        //            var lanes = thCADCoreNTSSpatialIndex.SelectWindowPolygon(frame).Cast<Curve>().ToList();

        //            var parkingLinesService = new ParkingLinesService();
        //            var parkingLines = parkingLinesService.CreateParkingLines(frame, lanes);

        //            foreach (var line in parkingLines)
        //            {
        //                acdb.ModelSpace.Add(line.Clone() as Curve);
        //            }
        //        }
        //    }
        //}

        [CommandMethod("TIANHUACAD", "THFBS", CommandFlags.Modal)]
        public void ThBroadcast()
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                // 获取车道线
                var laneLineEngine = new ThLaneRecognitionEngine();
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
                //// 获取车道线
                //var laneLineEngine = new ThLaneRecognitionEngine();
                //laneLineEngine.Recognize(acdb.Database, new Point3dCollection());
                //// 暂时假设车道线绘制符合要求
                //var lanes = laneLineEngine.Spaces.Select(o => o.Boundary).Cast<Line>().ToList();
                //if (lanes.Count == 0)
                //{
                //    return;
                //}

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

                    //获取车道线
                    var lanes = GetLanes(frame, acdb);

                    //处理车道线
                    var handleLines = ThMEPLineExtension.LineSimplifier(lanes.ToCollection(), 500, 20.0, 2.0, Math.PI / 180.0);
                    var parkingLinesService = new ParkingLinesService();
                    var parkingLines = parkingLinesService.CreateNodedParkingLines(frame, handleLines, out List<List<Line>> otherPLines);

                    //获取构建信息
                    GetStructureInfo(acdb, frame, out List<Polyline> columns, out List<Polyline> walls);

                    //主车道布置信息
                    LayoutWithParkingLineService layoutService = new LayoutWithParkingLineService();
                    var layoutInfo = layoutService.LayoutBraodcast(parkingLines, columns, walls);

                    //副车道布置信息
                    LayoutWithSecondaryParkingLineService layoutWithSecondaryParkingLineService = new LayoutWithSecondaryParkingLineService();
                    var resLayoutInfo = layoutWithSecondaryParkingLineService.LayoutBraodcast(layoutInfo, otherPLines, columns, walls);

                    InsertBroadcastService.InsertSprayBlock(resLayoutInfo);
                }
            }
        }

        /// <summary>
        /// 获取车道线
        /// </summary>
        /// <param name="polyline"></param>
        public List<Polyline> GetLanes(Polyline polyline, AcadDatabase acdb)
        {
            var objs = new DBObjectCollection();
            var laneLines = acdb.ModelSpace
                .OfType<Polyline>()
                .Where(o => o.Layer == ThMEPCommon.NewParkingLineLayer);
            laneLines.ForEach(x => objs.Add(x));

            var bufferPoly = polyline.Buffer(1)[0] as Polyline;
            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            var sprayLines = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(bufferPoly).Cast<Polyline>().ToList();

            return sprayLines.SelectMany(x=>polyline.Trim(x).Cast<Polyline>().ToList()).ToList();
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
        }
    }
}
