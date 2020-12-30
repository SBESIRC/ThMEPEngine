using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Service;
using ThMEPLighting.EmgLight;
using ThMEPLighting.EmgLight.Service;
using ThMEPLighting.Common;

namespace ThMEPLighting
{
    public class ThEmgLightCmds
    {
        int bufferLength = 100;

        [CommandMethod("TIANHUACAD", "THEL", CommandFlags.Modal)]
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

                bool debug = false;
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

                    //处理车道线
                    var handleLines = ThMEPLineExtension.LineSimplifier(lanes.ToCollection(), 500, 20.0, 2.0, Math.PI / 180.0);
                    var parkingLinesService = new ParkingLinesService();
                    var parkingLines = parkingLinesService.CreateNodedParkingLines(plFrame, handleLines, out List<List<Line>> otherPLines);

                    //车道线顺序
                    List<ThLightEdge> edges = new List<ThLightEdge>();

                    //  parkingLines.ForEach(ls => ls.ForEach(l => edges.Add(new ThLightEdge(l))));
                    // otherPLines.ForEach(ls => ls.ForEach(l => edges.Add(new ThLightEdge(l))));



                    // handleLines.ForEach(x => edges.Add(new ThLightEdge(x)));


                    foreach (List<Line> ls in parkingLines)
                    {
                        foreach (Line l in ls)
                        {
                            edges.Add(new ThLightEdge(l));
                        }
                    }

                    foreach (List<Line> ls in otherPLines)
                    {
                        foreach (Line l in ls)
                        {
                            edges.Add(new ThLightEdge(l));
                        }
                    }

                    //foreach (Line l in handleLines)
                    //{
                    //    edges.Add(new ThLightEdge(l));
                    //}




                    //ThLightGraphService testLight = ThLightGraphService.Build(edges, edges[2].Edge.StartPoint);
                    //InsertLightService.ShowGeometry(edges[2].Edge.StartPoint, "Start", 20);

                    ThLightGraphService testLight = ThLightGraphService.Build(edges, edges[3].Edge.EndPoint);
                    
                    InsertLightService.ShowGeometry(edges[3].Edge.EndPoint, "Start", 20);

                    //按顺序排布车道线点并合并同一条线的车道线
                    List<List<Line>> TempOrderedEdge = new List<List<Line>>();

                    for (int i = 0; i < testLight.Links.Count; i++)
                    {

                        List<Line> tempLine = new List<Line>();
                        TempOrderedEdge.Add(tempLine);

                        for (int j = 0; j < testLight.Links[i].Path.Count; j++)
                        {
                            if (j==0)
                            {
                                if (testLight.Links[i].Path[j].Edge.StartPoint != testLight.Links[i].Start)
                                {
                                     testLight.Links[i].Path[j].Edge.ReverseCurve();
                                 
                                   
                                }
                                tempLine.Add(testLight.Links[i].Path[j].Edge);

                            }
                            else
                            {
                                if (testLight.Links[i].Path[j].Edge.StartPoint != testLight.Links[i].Path[j-1].Edge.EndPoint)
                                {
                                    testLight.Links[i].Path[j].Edge.ReverseCurve();
                                }
                                var nowEdge = (testLight.Links[i].Path[j].Edge.EndPoint - testLight.Links[i].Path[j].Edge.StartPoint).GetNormal();
                                var PreEdge = (testLight.Links[i].Path[j-1].Edge.EndPoint - testLight.Links[i].Path[j-1].Edge.StartPoint).GetNormal();
                               bool bAngle = Math.Abs(nowEdge.DotProduct(PreEdge)) / (nowEdge.Length * PreEdge.Length) < Math.Abs(Math.Cos(45 * Math.PI / 180));
                                if (bAngle)
                                {
                                    
                                    tempLine = new List<Line>();
                                    TempOrderedEdge.Add(tempLine);
                                }
                                
                                tempLine.Add(testLight.Links[i].Path[j].Edge);
                            }
                            
                        }
                        

                    }

                    
                    
                    //for (int i = 0; i < testLight.Links.Count; i++)
                    //{
                    //    for (int j = 0; j < testLight.Links[i].Path.Count; j++)
                    //    {
                    //        InsertLightService.ShowGeometry(testLight.Links[i].Path[j].Edge.StartPoint, string.Format("ordered{0}-{1}-start", i, j), 20);
                    //        InsertLightService.ShowGeometry(testLight.Links[i].Path[j].Edge.EndPoint, string.Format("ordered{0}-{1}-end", i, j), 20);
                    //    }
                    //}



                    //for (int i = 0; i < TempOrderedEdge.Count; i++)
                    //{
                    //    for (int j = 0; j < TempOrderedEdge[i].Count; j++)
                    //    {
                    //        InsertLightService.ShowGeometry(TempOrderedEdge[i][j].StartPoint, string.Format("new Line {0}-{1}-start", i, j), 161);
                    //        InsertLightService.ShowGeometry(TempOrderedEdge[i][j].EndPoint, string.Format("new Line {0}-{1}-end", i, j), 161);
                    //    }
                    //}




                    if (debug == false)
                    {
                        ////debug
                        //foreach (List<Line> parkinglineString in parkingLines)
                        //{
                        //    InsertLightService.ShowGeometry(parkinglineString, 80);
                        //}

                        //foreach (List<Line> parkinglineString in otherPLines)
                        //{

                        //    InsertLightService.ShowGeometry(parkinglineString, 10);

                        //}


                        //获取构建信息
                        var bufferFrame = plFrame.Buffer(bufferLength)[0] as Polyline;
                        GetStructureInfo(acdb, bufferFrame, out List<Polyline> columns, out List<Polyline> walls);


                        //主车道布置信息
                        LayoutWithParkingLineForLight layoutService = new LayoutWithParkingLineForLight();
                        var layoutInfo = layoutService.LayoutLight(plFrame, TempOrderedEdge, columns, walls);
                        //layoutInfo = layoutService.LayoutLight(plFrame, otherPLines, columns, walls);

                        //InsertLightService.InsertSprayBlock(layoutInfo);

                        ////副车道布置信息
                        // LayoutWithParkingLineForLight layoutSecondaryService = new LayoutWithParkingLineForLight();
                        //  var resLayoutInfo = layoutService.LayoutLight(frame, otherPLines, columns, walls);

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


    }
}
