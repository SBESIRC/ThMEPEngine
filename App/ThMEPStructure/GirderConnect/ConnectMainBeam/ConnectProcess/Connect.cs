using System;
using System.Linq;
using System.Collections.Generic;
using AcHelper;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Algorithm;
using ThMEPStructure.GirderConnect.Data;
using ThMEPStructure.GirderConnect.ConnectMainBeam.Data;
using ThMEPStructure.GirderConnect.ConnectMainBeam.Utils;

namespace ThMEPStructure.GirderConnect.ConnectMainBeam.ConnectProcess
{
    public class Connect
    {
        public double SimilarAngle = Math.PI / 8;
        public double SimilarPointsDis = 500;
        public double SamePointsDis = 1;
        public double MaxBeamLength = 13000;
        public double SplitArea = 52000000;

        private Dictionary<Polyline, Dictionary<Point3d, HashSet<Point3d>>> outline2BorderNearPts = new Dictionary<Polyline, Dictionary<Point3d, HashSet<Point3d>>>();
        private Dictionary<Polyline, HashSet<Polyline>> outlineWallsMerged = new Dictionary<Polyline, HashSet<Polyline>>();
        private Dictionary<Polyline, List<Point3d>> outline2ZeroPts = new Dictionary<Polyline, List<Point3d>>();
        private Dictionary<Polyline, Point3dCollection> outlineNearPts = new Dictionary<Polyline, Point3dCollection>();
        private Point3dCollection allPts = new Point3dCollection();
        private HashSet<Point3d> borderPts = new HashSet<Point3d>();
        private List<Point3d> zeroPts = new List<Point3d>();
        private HashSet<Tuple<Point3d, Point3d>> tuples = new HashSet<Tuple<Point3d, Point3d>>();
        private Dictionary<Point3d, HashSet<Point3d>> dicTuples = new Dictionary<Point3d, HashSet<Point3d>>();
        private List<Polyline> Outlines = new List<Polyline>();

        public Connect()
        {
            //
        }

        /// <summary>
        /// Connect Steps
        /// </summary>
        /// <param name="clumnPts"></param>
        /// <param name="outlineWalls"></param>
        /// <returns></returns>
        public Dictionary<Point3d, HashSet<Point3d>> Calculate(Point3dCollection clumnPts, Dictionary<Polyline, HashSet<Polyline>> outlineWalls,
            Dictionary<Polyline, HashSet<Point3d>> outlineClumns, Dictionary<Polyline, HashSet<Polyline>> outerWalls,
            ref Dictionary<Polyline, HashSet<Point3d>> olCrossPts, ThMEPOriginTransformer transformer)
        {
            if (clumnPts.Count == 0)
            {
                return null;
            }
            outlineWalls.ForEach(o => outlineWallsMerged.Add(o.Key, o.Value));
            outerWalls.ForEach(o => outlineWallsMerged.Add(o.Key, o.Value));
            Outlines = outlineWallsMerged.Keys.ToList();
            foreach (var pl in outlineWallsMerged.Keys)
            {
                if (!outlineNearPts.ContainsKey(pl))
                {
                    outlineNearPts.Add(pl, new Point3dCollection());
                }
            }
            foreach (Point3d clumnPt in clumnPts)
            {
                allPts.Add(clumnPt);
                zeroPts.Add(clumnPt);
            }
            foreach (var outlineClumn in outlineClumns)
            {
                if (!outline2ZeroPts.ContainsKey(outlineClumn.Key))
                {
                    outline2ZeroPts.Add(outlineClumn.Key, new List<Point3d>());
                }
                foreach (Point3d pt in outlineClumn.Value)
                {
                    allPts.Add(pt);
                    borderPts.Add(pt);
                    outline2ZeroPts[outlineClumn.Key].Add(pt);
                    zeroPts.Add(pt);
                }
            }

            //1、获取多边形边界和多边形外的连接
            BorderConnectToVDNear(clumnPts, outlineClumns);
            outline2ZeroPts.Values.ForEach(pts => zeroPts.AddRange(pts));
            outline2BorderNearPts.Values.ForEach(b2ns => {
                b2ns.Keys.ForEach(b => {
                    borderPts.Add(b);
                    allPts.Add(b);
                });
            });
            allPts = PointsDealer.PointsDistinct(allPts, SamePointsDis);
            zeroPts = PointsDealer.PointsDistinct(zeroPts, SamePointsDis);

            //2、生成初始网格
            //tuples = StructureBuilder.DelaunayTriangulationConnect(allPts);
            tuples = StructureBuilder.VoronoiDiagramConnect(allPts);
            dicTuples = LineDealer.TuplesStandardize(tuples, allPts.Cast<Point3d>().ToList());

            foreach (var borderPt2NearPts in outline2BorderNearPts.Values)
            {
                foreach (var bordrPt in borderPt2NearPts.Keys)
                {
                    if (dicTuples.ContainsKey(bordrPt))
                    {
                        foreach (var nearPt in dicTuples[bordrPt])
                        {
                            if (!borderPt2NearPts[bordrPt].Contains(nearPt) && !borderPt2NearPts.ContainsKey(nearPt))
                            {
                                borderPt2NearPts[bordrPt].Add(nearPt);
                            }
                        }
                    }
                }
            }
            LineDealer.DeleteSameClassLine(borderPts, ref dicTuples);
            LineDealer.DeleteDiffClassLine(borderPts, clumnPts, ref dicTuples);
            LineDealer.AddSpecialLine(outline2BorderNearPts, ref dicTuples);
            SimplifyDicTuples(zeroPts, SimilarPointsDis, SimilarAngle * 2);

            //3、网格优化
            var itcBorderPts = PointsDealer.FindIntersectBorderPt(Outlines, allPts.Cast<Point3d>().ToHashSet());
            StructureDealer.AddConnectUpToFour(ref dicTuples, allPts, itcBorderPts, MaxBeamLength);
            outline2BorderNearPts = PointsDealer.CreateOutline2BorderNearPts(dicTuples, Outlines);
            StructureDealer.DeleteConnectUpToFourB(ref dicTuples, ref outline2BorderNearPts);
            outline2BorderNearPts.Values.ForEach(b2ns => {
                b2ns.Keys.ForEach(b => {
                    if (!borderPts.Contains(b))
                    {
                        borderPts.Add(b);
                    }
                    if (!allPts.Contains(b))
                    {
                        allPts.Add(b);
                    }
                });
            });
            dicTuples.Keys.ForEach(o => allPts.Add(o));
            StructureDealer.AddConnectUpToFour(ref dicTuples, allPts, itcBorderPts, MaxBeamLength);
            SimplifyDicTuples(zeroPts, SimilarPointsDis, SimilarAngle);

            //4、分割合并网格
            dicTuples = SplitAndMerge(SplitArea);
            SimplifyDicTuples(zeroPts, SimilarPointsDis, SimilarAngle);

            //5、获得辅助外框线
            List<Tuple<Point3d, Point3d>> closebdLines = BorderPtsConnect(outlineWalls, outerWalls, olCrossPts, transformer);
            StructureDealer.RemoveLinesInterSectWithCloseBorderLines(closebdLines, ref dicTuples);
            closebdLines.ForEach(o => StructureDealer.DeleteFromDicTuples(o.Item1, o.Item2, ref dicTuples));
            SimplifyDicTuples(zeroPts, SimilarPointsDis, SimilarAngle);

            return dicTuples;
        }

        /// <summary>
        /// 简化dicTuples结构
        /// </summary>
        private void SimplifyDicTuples(List<Point3d> basePts, double deviation = 1, double angle = Math.PI / 8)
        {
            basePts = PointsDealer.PointsDistinct(basePts);
            StructureDealer.ReduceSimilarPoints(ref dicTuples, basePts);
            StructureDealer.ReduceSimilarLine(ref dicTuples, angle);
            LineDealer.DicTuplesStandardize(ref dicTuples, basePts, deviation);
        }

        /// <summary>
        /// 获取多边形边界和多边形外的连接
        /// </summary>
        private void BorderConnectToVDNear(Point3dCollection clumnPts, Dictionary<Polyline, HashSet<Point3d>> outlineClumns)
        {
            //1、获取近点
            PointsDealer.VoronoiDiagramNearPoints(clumnPts, outlineNearPts);

            //2、获取“BorderPt与NearPt的连接”
            List<Tuple<Point3d, Point3d>> priority1stBorderNearTuples = new List<Tuple<Point3d, Point3d>>();
            StructureBuilder.PriorityBorderPoints(outlineNearPts, outlineWallsMerged, outlineClumns, ref outline2BorderNearPts, ref outline2ZeroPts, ref priority1stBorderNearTuples, MaxBeamLength, SimilarAngle);

            //3、删减无用的“BorderPt与NearPt的连接”
            outline2BorderNearPts = StructureDealer.UpdateBorder2NearPts(outline2BorderNearPts, priority1stBorderNearTuples, SimilarAngle * 2);
        }

        /// <summary>
        /// 分情况连接边界上的点
        /// </summary>
        private List<Tuple<Point3d, Point3d>> BorderPtsConnect(Dictionary<Polyline, HashSet<Polyline>> outlineWalls,
            Dictionary<Polyline, HashSet<Polyline>> outerWalls, Dictionary<Polyline, HashSet<Point3d>> olCrossPts,
            ThMEPOriginTransformer transformer)
        {
            StructureDealer.RemoveIntersectLines(ref dicTuples);
            List<Tuple<Point3d, Point3d>> closeBorderLines = new List<Tuple<Point3d, Point3d>>();
            HashSet<Tuple<Point3d, Point3d>> closeBorderLineA = StructureDealer.CloseBorderA(outlineWalls.Keys.ToList(), dicTuples.Keys.ToList());

            string outlineLayerA = "TH_AI_HOUSEBOUND";
            LayerDealer.AddLayer(outlineLayerA, 1);
            LayerDealer.Output(closeBorderLineA, outlineLayerA, transformer);
            LayerDealer.HiddenLayer(outlineLayerA);
            closeBorderLineA.ForEach(tup => {
                StructureDealer.AddLineTodicTuples(tup.Item1, tup.Item2, ref dicTuples);
                closeBorderLines.Add(tup);
            });
            var oriPtsB = dicTuples.Keys.ToList();
            olCrossPts.Values.ForEach(pts => oriPtsB.AddRange(pts));
            HashSet<Tuple<Point3d, Point3d>> closeBorderLineB = StructureDealer.CloseBorderA(outerWalls.Keys.ToList(), oriPtsB);
            Dictionary<Point3d, HashSet<Point3d>> newdicTuples = LineDealer.TuplesStandardize(closeBorderLineB, dicTuples.Keys.ToList());
            var unifiedTyples = LineDealer.UnifyTuples(newdicTuples);
            unifiedTyples.ForEach(tup => {
                StructureDealer.AddLineTodicTuples(tup.Item1, tup.Item2, ref dicTuples);
                closeBorderLines.Add(tup);
            });
            string outlineLayerB = "TH_AI_WALLBOUND";
            LayerDealer.AddLayer(outlineLayerB, 2);
            LayerDealer.Output(unifiedTyples, outlineLayerB, transformer);
            LayerDealer.HiddenLayer(outlineLayerB);
            return closeBorderLines;
        }

        /// <summary>
        /// 对现有的dicTuples结构进行多边形分割与合并
        /// </summary>
        public Dictionary<Point3d, HashSet<Point3d>> SplitAndMerge(double splitArea = 0.0)
        {
            //0、预处理
            outline2BorderNearPts = PointsDealer.CreateOutline2BorderNearPts(dicTuples, Outlines);
            HashSet<Tuple<Point3d, Point3d>> closeBorderLine = StructureDealer.CloseBorderA(Outlines, dicTuples.Keys.ToList());
            closeBorderLine.ForEach(o => StructureDealer.AddLineTodicTuples(o.Item1, o.Item2, ref dicTuples));
            StructureDealer.RemoveIntersectLines(ref dicTuples);

            //1、分割
            var findPolylineFromLines = new Dictionary<Tuple<Point3d, Point3d>, List<Tuple<Point3d, Point3d>>>();
            StructureBuilder.BuildPolygonsC(dicTuples, ref findPolylineFromLines);
            StructureBuilder.SplitBlock(ref findPolylineFromLines, splitArea);

            //1.5、处理数据
            dicTuples.Clear();
            dicTuples = LineDealer.TuplesStandardize(findPolylineFromLines.Keys.ToHashSet(), zeroPts, 100);
            SimplifyDicTuples(zeroPts, SimilarPointsDis, SimilarAngle);
            findPolylineFromLines.Clear();
            StructureBuilder.BuildPolygons(dicTuples, ref findPolylineFromLines);
            var ptList = new HashSet<Point3d>();
            findPolylineFromLines.Keys.ForEach(t => {
                if (!ptList.Contains(t.Item1))
                {
                    ptList.Add(t.Item1);
                }
                if (!ptList.Contains(t.Item2))
                {
                    ptList.Add(t.Item2);
                }
            });
            HashSet<Point3d> borderPts = PointsDealer.FindIntersectBorderPt(Outlines, ptList);

            //2、合并
            StructureBuilder.MergeFragments(ref findPolylineFromLines, borderPts, splitArea);

            //3、生成结果
            Dictionary<Point3d, HashSet<Point3d>> newDicTuples = LineDealer.TuplesStandardize(findPolylineFromLines.Keys.ToHashSet(), zeroPts, 100);
            HashSet<Point3d> itcBorderPts = PointsDealer.FindIntersectBorderPt(Outlines, newDicTuples.Keys.ToHashSet());
            LineDealer.DeleteSameClassLine(itcBorderPts, ref newDicTuples);
            StructureDealer.RemoveIntersectLines(ref newDicTuples);
            return newDicTuples;
        }

        /// <summary>
        /// 处理墙与墙之间的连接
        /// </summary>
        /// <param name="dicTuples">已经加入房屋边框的dicTuples</param>
        /// <param name="outline2ZeroPts"></param>
        private void WallConnect(Dictionary<Point3d, HashSet<Point3d>> dicTuples, Dictionary<Polyline, List<Point3d>> outline2ZeroPts)
        {
            var itcBorderPts = PointsDealer.FindIntersectBorderPt(Outlines, dicTuples.Keys.ToHashSet());

            //连接相同的边界
            //连接逻辑：
            //保留连接线中点不在这个边界内的线
            //连接线两端的点不在同一条直线上
            //同线判定：(（a到lineA<500 && b到lineA<500）||（b到lineB<500 && a到lineB<500）)  lineA\lineB分别为a\b最近的线
            foreach (var outlineA in Outlines)
            {
                var outlineAPts = outline2ZeroPts[outlineA];
                //创建适合当前情况的dictuples
                foreach (var outlineAPtA in outlineAPts)
                {
                    if (dicTuples[outlineAPtA].Count > 2)
                    {
                        continue;
                    }
                    Point3d verticalPt = outlineA.GetClosePoint(outlineAPtA);
                    Line closetLine = GetObject.FindLineContainPoint(outlineA, verticalPt);

                    //StructureDealer.AddConnectUpToFour(ref dicTuples, outlineAPts, itcBorderPts);
                    //foreach (var outlineAPtB in outlineAPts)
                    //{
                    //    if(closetLine.DistanceTo(outlineAPtB, false) > 500 && dicTuples[outlineAPtB].Count < 4)
                    //    {
                    //        StructureDealer.AddConnectUpToFour(ref dicTuples, outlineAPts, itcBorderPts);
                    //    }
                    //}
                }
            }
            //连接不同的边界
            //点上的连接至少为1
            //连接一边有墙点，一边没有墙点
        }
    }
}
