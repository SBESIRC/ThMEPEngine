﻿using System;
using System.Collections.Generic;
using System.Linq;
using AcHelper;
using NFox.Cad;
using DotNetARX;
using Linq2Acad;
using ThCADCore.NTS;
using ThCADExtension;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.EditorInput;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.LaneLine;
using ThMEPEngineCore.Algorithm;
using ThMEPLighting.EmgLight.Assistant;
using ThMEPLighting.Common;


namespace ThMEPLighting.EmgLight.Common
{
    public static class GetSourceDataService
    {
        public static List<List<Line>> BuildLanes(Polyline shrinkFrame, Polyline bufferFrame, AcadDatabase acdb, ThMEPOriginTransformer transformer)
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

        /// <summary>
        /// 获取车道线
        /// </summary>
        /// <param name="polyline"></param>
        private static List<Curve> GetLanes(Polyline polyline, AcadDatabase acdb, ThMEPOriginTransformer transformer)
        {
            var objs = new DBObjectCollection();
            var laneLines = acdb.ModelSpace
                .OfType<Curve>()
                .Where(o => o.Layer == ThMEPLightingCommon.LANELINE_LAYER_NAME);

            List<Curve> laneList = laneLines.Select(x => x.WashClone()).ToList();

            laneList = laneList.Where(x => x != null).ToList();
            laneList.ForEach(x => transformer.Transform(x));
            laneList.ForEach(x => objs.Add(x));

            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            var sprayLines = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(polyline).Cast<Curve>().ToList();

            return sprayLines.SelectMany(x => polyline.Trim(x).Cast<Curve>().ToList()).ToList();

        }

        /// <summary>
        /// 获取构建信息
        /// </summary>
        /// <param name="acdb"></param>
        /// <param name="transBufferFrame"></param>
        /// <param name="columns"></param>
        /// <param name="beams"></param>
        /// <param name="walls"></param>
        public static void GetStructureInfo(AcadDatabase acdb, Polyline transBufferFrame, ThMEPOriginTransformer transformer, out List<Polyline> columns, out List<Polyline> walls)
        {
            var ColumnExtractEngine = new ThColumnExtractionEngine();
            ColumnExtractEngine.Extract(acdb.Database);
            DrawUtils.ShowGeometry(ColumnExtractEngine.Results.Select(x => x.Geometry as Polyline).ToList(), "l0clolmn");
            ColumnExtractEngine.Results.ForEach(x => transformer.Transform(x.Geometry));
            DrawUtils.ShowGeometry(ColumnExtractEngine.Results.Select(x => x.Geometry as Polyline).ToList(), "l0clolmn");

            var ColumnEngine = new ThColumnRecognitionEngine();
            ColumnEngine.Recognize(ColumnExtractEngine.Results, transBufferFrame.Vertices());

            // 启动墙识别引擎
            var ShearWallExtractEngine = new ThShearWallExtractionEngine();
            ShearWallExtractEngine.Extract(acdb.Database);
            ShearWallExtractEngine.Results.ForEach(x => transformer.Transform(x.Geometry));
            var ShearWallEngine = new ThShearWallRecognitionEngine();
            ShearWallEngine.Recognize(ShearWallExtractEngine.Results, transBufferFrame.Vertices());

            var archWallExtractEngine = new ThArchitectureWallExtractionEngine();
            archWallExtractEngine.Extract(acdb.Database);
            archWallExtractEngine.Results.ForEach(x => transformer.Transform(x.Geometry));
            var archWallEngine = new ThArchitectureWallRecognitionEngine();
            archWallEngine.Recognize(archWallExtractEngine.Results, transBufferFrame.Vertices());

            ////获取柱
            columns = new List<Polyline>();
            columns = ColumnEngine.Elements.Select(o => o.Outline).Cast<Polyline>().ToList();
            var objs = new DBObjectCollection();
            columns.ForEach(x => objs.Add(x));
            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            columns = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(transBufferFrame).Cast<Polyline>().ToList();

            //获取剪力墙
            walls = new List<Polyline>();
            walls = ShearWallEngine.Elements.Select(o => o.Outline).Cast<Polyline>().ToList();
            objs = new DBObjectCollection();
            walls.ForEach(x => objs.Add(x));
            thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            walls = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(transBufferFrame).Cast<Polyline>().ToList();

            //获取建筑墙
            foreach (var o in archWallEngine.Elements)
            {
                if (o.Outline is Polyline wall)
                {
                    walls.Add(wall);
                }
            }
        }

        /// <summary>
        /// dictionary: key: original, value: transfered
        /// </summary>
        /// <param name="transformer"></param>
        /// <param name="LayerName"></param>
        /// <param name="BlockName"></param>
        /// <param name="bufferFrame"></param>
        /// <returns></returns>
        public static Dictionary<BlockReference, BlockReference> ExtractBlock(Polyline bufferFrame, string LayerName, string BlockName, ThMEPOriginTransformer transformer, List<Polyline> holes = null)
        {
            var tol = new Tolerance(1, 1);
            var blk = new Dictionary<BlockReference, BlockReference>();
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                acadDatabase.Database.UnFrozenLayer(LayerName);
                acadDatabase.Database.UnLockLayer(LayerName);
                acadDatabase.Database.UnOffLayer(LayerName);

                var items = acadDatabase.ModelSpace
                .OfType<BlockReference>()
                .Where(o => o.Layer == LayerName);

                foreach (BlockReference block in items)
                {
                    if (block.Name == BlockName)
                    {
                        var blockTrans = block.Clone() as BlockReference;

                       if (blk.Where(x => x.Key.Position.IsEqualTo(blockTrans.Position, tol)).Count() == 0)
                        {
                            transformer.Transform(blockTrans);
                            blockTrans.Position = new Point3d(blockTrans.Position.X, blockTrans.Position.Y, 0);
                            blk.Add(block, blockTrans);
                        }

                    }
                }

                blk = blk.Where(o => bufferFrame.Contains(o.Value.Position)).ToDictionary(x => x.Key, x => x.Value);

                if (holes != null && holes.Count > 0)
                {
                    foreach (var hole in holes)
                    {
                        blk = blk.Where(o => hole.Contains(o.Value.Position) == false).ToDictionary(x => x.Key, x => x.Value);
                    }
                }
            }

            return blk;
        }

        public static Dictionary<BlockReference, BlockReference> ExtractBlockNoLayer(Polyline bufferFrame, string BlockName, ThMEPOriginTransformer transformer)
        {
            var emgLight = new Dictionary<BlockReference, BlockReference>();
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                //  acadDatabase.Database.UnFrozenLayer(LayerName);
                // acadDatabase.Database.UnLockLayer(LayerName);
                // acadDatabase.Database.UnOffLayer(LayerName);


                //有超多bug，不能用
                var items = acadDatabase.ModelSpace
                .OfType<Entity>();

                foreach (BlockReference block in items)
                {
                    if (block.Name == BlockName)
                    {
                        var blockTrans = block.Clone() as BlockReference;
                        transformer.Transform(blockTrans);
                        emgLight.Add(block, blockTrans);
                    }
                }

                emgLight = emgLight.Where(o => bufferFrame.Contains(o.Value.Position)).ToDictionary(x => x.Key, x => x.Value);

            }

            return emgLight;
        }
    }
}
