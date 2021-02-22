using System;
using AcHelper;
using NFox.Cad;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.LaneLine;
using ThMEPEngineCore.Algorithm;
using ThMEPLighting.Common;
using ThMEPLighting.EmgLight;
using ThMEPLighting.EmgLight.Service;
using ThMEPLighting.EmgLight.Assistant;

namespace ThMEPLighting
{
    public class ThEmgLightCmds
    {
        [CommandMethod("TIANHUACAD", "THYJZM", CommandFlags.Modal)]
        public void ThEmgLight()
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

                foreach (ObjectId obj in result.Value.GetObjectIds())
                {
                    //获取外包框
                    var frame = acdb.Element<Polyline>(obj);
                    var frameClone = frame.WashClone() as Polyline;
                    var centerPt = frameClone.StartPoint;

                    //处理外包框
                    var transformer = new ThMEPOriginTransformer(centerPt);
                    transformer.Transform(frameClone);
                    var nFrame = ThMEPFrameService.NormalizeEx(frameClone);
                    if (nFrame.Area < 1)
                    {
                        continue;
                    }

                    //为了获取卡在外包框的建筑元素，这里做了一个Buffer处理
                    var bufferFrame = ThMEPFrameService.Buffer(nFrame, EmgLightCommon.BufferFrame);
                    var shrinkFrame = ThMEPFrameService.Buffer(nFrame, -EmgLightCommon.BufferFrame);
                    DrawUtils.ShowGeometry(bufferFrame, EmgLightCommon.LayerFrame, Color.FromColorIndex(ColorMethod.ByColor, 130), LineWeight.LineWeight035);
                    DrawUtils.ShowGeometry(shrinkFrame, EmgLightCommon.LayerFrame, Color.FromColorIndex(ColorMethod.ByColor, 130), LineWeight.LineWeight035);

                    //如果没有layer 创建layer
                    DrawUtils.CreateLayer(ThMEPLightingCommon.EmgLightLayerName, Color.FromColorIndex(ColorMethod.ByLayer, ThMEPLightingCommon.EmgLightLayerColor), true);

                    //清除layer,has bug if change the coordiantes of dwg
                    var block = RemoveBlockService.ExtractClearEmergencyLight(transformer);
                    bufferFrame.ClearEmergencyLight(block);

                    //获取车道线
                    var mergedOrderedLane = BuildLanes(shrinkFrame, bufferFrame, acdb, transformer);

                    //获取建筑信息（柱和墙）
                    GetStructureInfo(acdb, bufferFrame, transformer, out List<Polyline> columns, out List<Polyline> walls);

                    //主车道布置信息
                    LayoutEmgLightEngine layoutEngine = new LayoutEmgLightEngine();
                    var layoutInfo = layoutEngine.LayoutLight(bufferFrame, mergedOrderedLane, columns, walls);

                    //换回布置
                    ResetResult(ref layoutInfo, transformer);

                    //布置构建
                    InsertLightService.InsertSprayBlock(layoutInfo);
                }
            }
        }

        /// <summary>
        /// 获取车道线
        /// </summary>
        /// <param name="polyline"></param>
        public List<Curve> GetLanes(Polyline polyline, AcadDatabase acdb, ThMEPOriginTransformer transformer)
        {
            var objs = new DBObjectCollection();
            var laneLines = acdb.ModelSpace
                .OfType<Curve>()
                .Where(o => o.Layer == ThMEPLightingCommon.LANELINE_LAYER_NAME);

            List<Curve> laneList = laneLines.Select(x => x.WashClone()).ToList();

            laneList.ForEach(x => transformer.Transform(x));
            laneList.ForEach(x => objs.Add(x));

            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            var sprayLines = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(polyline).Cast<Curve>().ToList();

            return sprayLines.SelectMany(x => polyline.Trim(x).Cast<Curve>().ToList()).ToList();

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
                .Where(o => o.Layer == ThMEPLightingCommon.LANELINE_LAYER_NAME);
            laneLines.ForEach(x => objs.Add(x));

            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            var sprayLines = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(polyline).Cast<Curve>().ToList();

            return sprayLines.SelectMany(x => polyline.Trim(x).Cast<Curve>().ToList()).ToList();

        }

        private List<List<Line>> BuildLanes(Polyline shrinkFrame, Polyline bufferFrame, AcadDatabase acdb, ThMEPOriginTransformer transformer)
        {
            var lanes = GetLanes(shrinkFrame, acdb, transformer);
            List<List<Line>> mergedOrderedLane = new List<List<Line>>();
            if (lanes.Count > 0)
            {
                //处理车道线
                var service = new ThLaneLineCleanService()
                {
                    CollinearGap = 150.0,
                    ExtendDistance = 150.0,
                };
                var handleLines = service.Clean(lanes.ToCollection());
                var parkingLinesService = new ParkingLinesService();
                var parkingLines = parkingLinesService.CreateNodedParkingLines(
                    bufferFrame,
                    handleLines.Cast<Line>().ToList(),
                    out List<List<Line>> otherPLines);

                //将车道线排序,点按排序方向排列,合并连续线段
                mergedOrderedLane = LaneServer.getMergedOrderedLane(parkingLines, otherPLines);
                for (int i = 0; i < mergedOrderedLane.Count; i++)
                {
                    for (int j = 0; j < mergedOrderedLane[i].Count; j++)
                    {
                        DrawUtils.ShowGeometry(mergedOrderedLane[i][j].StartPoint, string.Format("orderM {0}-{1}-start", i, j), EmgLightCommon.LayerLane, Color.FromColorIndex(ColorMethod.ByColor, 161));
                    }
                    DrawUtils.ShowGeometry(mergedOrderedLane[i], EmgLightCommon.LayerLane, Color.FromColorIndex(ColorMethod.ByColor, 210));
                }
                DrawUtils.ShowGeometry(mergedOrderedLane[0][0].StartPoint, string.Format("start!"), EmgLightCommon.LayerLane, Color.FromColorIndex(ColorMethod.ByColor, 1));

                /////
            }
            return mergedOrderedLane;
        }

        ///// <summary>
        ///// 获取构建信息
        ///// </summary>
        ///// <param name="acdb"></param>
        ///// <param name="polyline"></param>
        ///// <param name="columns"></param>
        ///// <param name="beams"></param>
        ///// <param name="walls"></param>
        //private void GetStructureInfo(AcadDatabase acdb, Polyline polyline, out List<Polyline> columns, out List<Polyline> walls)
        //{
        //    var allStructure = ThBeamConnectRecogitionEngine.ExecutePreprocess(acdb.Database, polyline.Vertices());

        //    //获取柱
        //    columns = allStructure.ColumnEngine.Elements.Select(o => o.Outline).Cast<Polyline>().ToList();

        //    var objs = new DBObjectCollection();
        //    columns.ForEach(x => objs.Add(x));
        //    ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
        //    columns = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(polyline).Cast<Polyline>().ToList();

        //    //获取剪力墙
        //    walls = allStructure.ShearWallEngine.Elements.Select(o => o.Outline).Cast<Polyline>().ToList();
        //    objs = new DBObjectCollection();
        //    walls.ForEach(x => objs.Add(x));
        //    thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
        //    walls = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(polyline).Cast<Polyline>().ToList();
        //}

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

            //获取柱
            var ColumnEngine = new ThColumnRecognitionEngine();
            ColumnEngine.Recognize(acdb.Database, polyline.Vertices());
            columns = ColumnEngine.Elements.Select(o => o.Outline).Cast<Polyline>().ToList();

            var objs = new DBObjectCollection();
            columns.ForEach(x => objs.Add(x));
            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            columns = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(polyline).Cast<Polyline>().ToList();

            //获取剪力墙
            var ShearWallEngine = new ThShearWallRecognitionEngine();
            ShearWallEngine.Recognize(acdb.Database, polyline.Vertices());
            walls = ShearWallEngine.Elements.Select(o => o.Outline).Cast<Polyline>().ToList();

            objs = new DBObjectCollection();
            walls.ForEach(x => objs.Add(x));
            thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            walls = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(polyline).Cast<Polyline>().ToList();
        }


        /// <summary>
        /// 获取构建信息
        /// </summary>
        /// <param name="acdb"></param>
        /// <param name="polyline"></param>
        /// <param name="columns"></param>
        /// <param name="beams"></param>
        /// <param name="walls"></param>
        private void GetStructureInfo(AcadDatabase acdb, Polyline polyline, ThMEPOriginTransformer transformer, out List<Polyline> columns, out List<Polyline> walls)
        {
            var ColumnExtractEngine = new ThColumnExtractionEngine();
            ColumnExtractEngine.Extract(acdb.Database);
            ColumnExtractEngine.Results.ForEach(x => transformer.Transform(x.Geometry));
            var ColumnEngine = new ThColumnRecognitionEngine();
            ColumnEngine.Recognize(ColumnExtractEngine.Results, polyline.Vertices());

            // 启动墙识别引擎
            var ShearWallExtractEngine = new ThShearWallExtractionEngine();
            ShearWallExtractEngine.Extract(acdb.Database);
            ShearWallExtractEngine.Results.ForEach(x => transformer.Transform(x.Geometry));
            var ShearWallEngine = new ThShearWallRecognitionEngine();
            ShearWallEngine.Recognize(ShearWallExtractEngine.Results, polyline.Vertices());

            var archWallExtractEngine = new ThArchitectureWallExtractionEngine();
            archWallExtractEngine.Extract(acdb.Database);
            archWallExtractEngine.Results.ForEach(x => transformer.Transform(x.Geometry));
            var archWallEngine = new ThArchitectureWallRecognitionEngine();
            archWallEngine.Recognize(archWallExtractEngine.Results, polyline.Vertices());

            ////获取柱
            columns = new List<Polyline>();
            columns = ColumnEngine.Elements.Select(o => o.Outline).Cast<Polyline>().ToList();
            var objs = new DBObjectCollection();
            columns.ForEach(x => objs.Add(x));
            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            columns = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(polyline).Cast<Polyline>().ToList();

            //获取剪力墙
            walls = new List<Polyline>();
            walls = ShearWallEngine.Elements.Select(o => o.Outline).Cast<Polyline>().ToList();
            objs = new DBObjectCollection();
            walls.ForEach(x => objs.Add(x));
            thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            walls = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(polyline).Cast<Polyline>().ToList();

            //获取建筑墙
            foreach (var o in archWallEngine.Elements)
            {
                if (o.Outline is Polyline wall)
                {
                    walls.Add(wall);
                }
            }
        }

        private void ResetResult(ref Dictionary<Polyline, (Point3d, Vector3d)> layoutInfo, ThMEPOriginTransformer transformer)
        {

            Dictionary<Polyline, (Point3d, Vector3d)> resetResult = new Dictionary<Polyline, (Point3d, Vector3d)>();

            layoutInfo.ForEach(x =>
            {
                var pt = new Point3d(x.Value.Item1.X, x.Value.Item1.Y, x.Value.Item1.Z);
                transformer.Reset(ref pt);
                resetResult.Add(x.Key, (pt, x.Value.Item2));
            });

            //layoutInfo.ForEach(x => transformer.Transform(ref x.Value.Item1));

            //resetResult.Clear();

            //foreach (var x in layoutInfo)
            //{
            //    var pt = new Point3d(x.Value.Item1.X, x.Value.Item1.Y, x.Value.Item1.Z);
            //    transformer.Reset(ref pt);
            //    resetResult.Add(x.Key, (pt, x.Value.Item2));
            //}

            layoutInfo = resetResult;
        }


        /// <summary>
        /// 取建筑墙
        /// </summary>
        /// <param name="acdb"></param>
        /// <param name="bufferedFrame"></param>
        /// <param name="walls"></param>
        public void getArchWall(AcadDatabase acdb, Polyline bufferedFrame, ref List<Polyline> walls)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (var archWallEngine = new ThArchitectureWallRecognitionEngine())
            {
                archWallEngine.Recognize(acadDatabase.Database, bufferedFrame.Vertices());

                foreach (var o in archWallEngine.Elements)
                {
                    if (o.Outline is Polyline polyline)
                    {
                        walls.Add(polyline);
                    }
                }
            }
        }
    }
}
