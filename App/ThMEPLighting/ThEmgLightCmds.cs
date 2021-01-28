using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using AcHelper;
using Linq2Acad;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Engine;
using ThMEPLighting.EmgLight;
using ThMEPLighting.EmgLight.Service;
using ThMEPLighting.Common;
using ThMEPEngineCore.LaneLine;
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
                    var nFrame = ThMEPFrameService.NormalizeEx(frame);
                    if (nFrame.Area <1)
                    {
                        continue;
                    }

                    var bufferFrame = nFrame.Buffer(EmgLightCommon .BufferFrame)[0] as Polyline;
                    var shrinkFrame = nFrame.Buffer(EmgLightCommon.shrinkFrame)[0] as Polyline;

                    //如果没有layer 创建layer
                    DrawUtils.CreateLayer(ThMEPLightingCommon.EmgLightLayerName, Color.FromColorIndex(ColorMethod.ByLayer, ThMEPLightingCommon.EmgLightLayerColor), true);

                    //清除layer
                    bufferFrame.ClearEmergencyLight();

                    //获取车道线
                    var lanes = GetLanes(shrinkFrame, acdb);

                    if (lanes.Count > 0)
                    {
                        //处理车道线
                        var handleLines = ThMEPLineExtension.LineSimplifier(lanes.ToCollection(), 500, 20.0, 2.0, Math.PI / 180.0);
                        var parkingLinesService = new ParkingLinesService();
                        var parkingLines = parkingLinesService.CreateNodedParkingLines(bufferFrame, handleLines, out List<List<Line>> otherPLines);

                        //将车道线排序,点按排序方向排列,合并连续线段
                        List<List<Line>> mergedOrderedLane = LaneServer.getMergedOrderedLane(parkingLines, otherPLines);
                        for (int i = 0; i < mergedOrderedLane.Count; i++)
                        {
                            for (int j = 0; j < mergedOrderedLane[i].Count; j++)
                            {
                                DrawUtils.ShowGeometry(mergedOrderedLane[i][j].StartPoint, string.Format("orderM {0}-{1}-start", i, j),EmgLightCommon. LayerLane, Color.FromRgb(128, 159, 225));
                            }
                        }
                        DrawUtils.ShowGeometry(mergedOrderedLane[0][0].StartPoint, string.Format("start!"),EmgLightCommon. LayerLane, Color.FromRgb(255, 0, 0));

                        //获取构建信息
                        GetStructureInfo(acdb, bufferFrame, out List<Polyline> columns, out List<Polyline> walls);
                        getArchWall(acdb, bufferFrame, ref walls);

                        var b = false;
                        if (b == true)
                        {
                            return;
                        }

                        //主车道布置信息
                        LayoutEmgLightEngine layoutEngine = new LayoutEmgLightEngine();
                        var layoutInfo = layoutEngine.LayoutLight(bufferFrame, mergedOrderedLane, columns, walls);

                        //布置构建
                        InsertLightService.InsertSprayBlock(layoutInfo);

                    }
                }
            }
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
