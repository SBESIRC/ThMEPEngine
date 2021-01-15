using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using AcHelper;
using Linq2Acad;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Service;
using ThMEPLighting.EmgLight;
using ThMEPLighting.EmgLight.Service;
using ThMEPLighting.Common;
using ThMEPEngineCore.LaneLine;

namespace ThMEPLighting
{
    public class ThEmgLightCmds
    {
        int bufferLength = 100;

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

                //获取外包框
                List<Curve> frameLst = new List<Curve>();
                foreach (ObjectId obj in result.Value.GetObjectIds())
                {
                    var frame = acdb.Element<Polyline>(obj);
                    var plFrame = ThMEPFrameService.Normalize(frame);
                    frameLst.Add(frame);

                }

                //处理外包框线
                //
                //  var plines = HandleFrame(frameLst);

                foreach (Polyline plFrame in frameLst)
                {

                    ////删除原有构建
                    // plFrame.ClearBroadCast();
                    // plFrame.ClearBlindArea();
                    //}

                    //foreach (ObjectId obj in result.Value.GetObjectIds())
                    //{
                    //    var frame = acdb.Element<Polyline>(obj);

                    //获取车道线
                    var lanes = GetLanes(plFrame, acdb);


                  

                    if (lanes.Count > 0)
                    {
                        //处理车道线
                        var handleLines = ThMEPLineExtension.LineSimplifier(lanes.ToCollection(), 500, 20.0, 2.0, Math.PI / 180.0);
                        var parkingLinesService = new ParkingLinesService();
                        var parkingLines = parkingLinesService.CreateNodedParkingLines(plFrame, handleLines, out List<List<Line>> otherPLines);

                        //将车道线排序,点按排序方向排列,合并连续线段
                        List<List<Line>> mergedOrderedLane = LaneServer.getMergedOrderedLane(parkingLines, otherPLines);


                        for (int i = 0; i < mergedOrderedLane.Count; i++)
                        {
                            for (int j = 0; j < mergedOrderedLane[i].Count; j++)
                            {
                                InsertLightService.ShowGeometry(mergedOrderedLane[i][j].StartPoint, string.Format("orderM {0}-{1}-start", i, j), 161);
                                //InsertLightService.ShowGeometry(OrderedMergedLane[i][j].EndPoint, string.Format("orderM {0}-{1}-end", i, j), 161);
                            }
                        }
                        InsertLightService.ShowGeometry(mergedOrderedLane[0][0].StartPoint, string.Format("start!"), 20, LineWeight.LineWeight050);


                        bool debug = false;
                        if (debug == false)
                        {

                            //获取构建信息
                            var bufferFrame = plFrame.Buffer(bufferLength)[0] as Polyline;
                            GetStructureInfo(acdb, bufferFrame, out List<Polyline> columns, out List<Polyline> walls);

                            //主车道布置信息
                            LayoutWithParkingLineForLight layoutService = new LayoutWithParkingLineForLight();
                            var layoutInfo = layoutService.LayoutLight(plFrame, mergedOrderedLane, columns, walls);

                            InsertLightService.InsertSprayBlock(layoutInfo);

                        }
                    }
                    
                }

            }
        }

        /// <summary>
        /// 处理外包框线
        /// </summary>
        /// <param name="frameLst"></param>
        /// <returns></returns>
        //private List<Polyline> HandleFrame(List<Curve> frameLst)
        //{
        //    var polygonInfos = NoUserCoordinateWorker.MakeNoUserCoordinateWorker(frameLst);
        //    List<Polyline> resPLines = new List<Polyline>();
        //    foreach (var pInfo in polygonInfos)
        //    {
        //        resPLines.Add(pInfo.ExternalProfile);
        //        resPLines.AddRange(pInfo.InnerProfiles);
        //    }

        //    return resPLines;
        //}

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

            //var bufferPoly = polyline.Buffer(1)[0] as Polyline;
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

        //public void THExtractArchWall()
        //{
        //    using (AcadDatabase acadDatabase = AcadDatabase.Active())
        //    using (var archWallEngine = new ThArchitectureWallRecognitionEngine())
        //    {
        //        var result = Active.Editor.GetEntity("\n选择框线");
        //        if (result.Status != PromptStatus.OK)
        //        {
        //            return;
        //        }
        //        Polyline frame = acadDatabase.Element<Polyline>(result.ObjectId);
        //        archWallEngine.Recognize(acadDatabase.Database, frame.Vertices());
        //        archWallEngine.Elements.ForEach(o =>
        //        {
        //            if (o.Outline is Curve curve)
        //            {
        //                acadDatabase.ModelSpace.Add(curve.WashClone());
        //            }
        //            else if (o.Outline is MPolygon mPolygon)
        //            {
        //                acadDatabase.ModelSpace.Add(mPolygon);
        //            }
        //        });
        //    }
        //}


    }
}
