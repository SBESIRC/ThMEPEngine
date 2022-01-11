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
        public const double SimilarAngle = Math.PI / 8;
        public const double SimilarPointsDis = 500;
        public const double SamePointsDis = 1;
        public const double MaxBeamLength = 13000;

        private  Dictionary<Polyline, Dictionary<Point3d, HashSet<Point3d>>> outline2BorderNearPts = new Dictionary<Polyline, Dictionary<Point3d, HashSet<Point3d>>>();
        private  Dictionary<Polyline, HashSet<Polyline>> outlineWallsMerged = new Dictionary<Polyline, HashSet<Polyline>>();
        private  Dictionary<Polyline, List<Point3d>> outline2ZeroPts = new Dictionary<Polyline, List<Point3d>>();
        private  Dictionary<Polyline, Point3dCollection> outlineNearPts = new Dictionary<Polyline, Point3dCollection>();
        private  Point3dCollection allPts = new Point3dCollection();
        private  HashSet<Point3d> borderPts = new HashSet<Point3d>();
        private  List<Point3d> zeroPts = new List<Point3d>();
        private  HashSet<Tuple<Point3d, Point3d>> tuples = new HashSet<Tuple<Point3d, Point3d>>();
        private  Dictionary<Point3d, HashSet<Point3d>> dicTuples = new Dictionary<Point3d, HashSet<Point3d>>();
        private  Dictionary<Point3d, Point3d> closeBorderLines = new Dictionary<Point3d, Point3d>();

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
            if(clumnPts.Count == 0)
            {
                return null;
            }
            //1.0: Merge "outlineWalls" and "outerWalls"
            outlineWalls.ForEach(o => outlineWallsMerged.Add(o.Key, o.Value));
            outerWalls.ForEach(o => outlineWallsMerged.Add(o.Key, o.Value));
            //1.1:Get near points of outlines
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

            //1.2、获取多边形边界和多边形外的连接
            BorderConnectToVDNear(clumnPts, outlineClumns);
            foreach (var outline2ZeroPt in outline2ZeroPts)
            {
                zeroPts.AddRange(outline2ZeroPt.Value);
            }
            foreach (var borderPt2NearPts in outline2BorderNearPts.Values)
            {
                foreach (var borderPt2NearPt in borderPt2NearPts)
                {
                    var borderPt = borderPt2NearPt.Key;
                    borderPts.Add(borderPt);
                    allPts.Add(borderPt);
                }
            }
            //1.3:DT/VDconnect points with borderPoints and clumnPoints
            allPts = PointsDealer.PointsDistinct(allPts, SamePointsDis);
            zeroPts = PointsDealer.PointsDistinct(zeroPts, SamePointsDis);
            //tuples = StructureBuilder.DelaunayTriangulationConnect(allPts);
            tuples = StructureBuilder.VoronoiDiagramConnect(allPts);
            dicTuples = LineDealer.TuplesStandardize(tuples, allPts);
            //1.4:find true border points with it`s near point of a outline
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
            //1.5
            LineDealer.DeleteSameClassLine(borderPts, ref dicTuples);
            LineDealer.DeleteDiffClassLine(borderPts, clumnPts, ref dicTuples);
            LineDealer.AddSpecialLine(outline2BorderNearPts, ref dicTuples);

            SimplifyDicTuples(zeroPts, SimilarPointsDis, SimilarAngle);

            //2.0 Split & Merge areas
            closeBorderLines = StructureDealer.CloseBorder(outline2BorderNearPts);
            closeBorderLines.ForEach(o => StructureDealer.AddLineTodicTuples(o.Key, o.Value, ref dicTuples));
            SimplifyDicTuples(zeroPts, SimilarPointsDis, Math.PI / 12);
            dicTuples = SplitAndMerge(dicTuples, allPts, closeBorderLines);
            var itcBorderPts = PointsDealer.FindIntersectBorderPt(allPts, outlineWallsMerged.Keys.ToList(), ref outline2BorderNearPts);//////////////////////////////////////////////////////
            LineDealer.DeleteSameClassLine(itcBorderPts, ref dicTuples);
            LineDealer.DeleteDiffClassLine(itcBorderPts, borderPts, ref dicTuples);
            //下面那个可能需要移动到这里
            LineDealer.AddSpecialLine(outline2BorderNearPts, ref dicTuples);

            SimplifyDicTuples(zeroPts, SimilarPointsDis, SimilarAngle * 2);
            StructureDealer.AddConnectUpToFour(ref dicTuples, allPts, itcBorderPts, MaxBeamLength);

            outline2BorderNearPts = PointsDealer.CreateOutline2BorderNearPts(dicTuples, outline2BorderNearPts.Keys.ToList());

            StructureDealer.DeleteConnectUpToFourB(ref dicTuples, ref outline2BorderNearPts);
            foreach (var borderPt2NearPts in outline2BorderNearPts.Values)
            {
                foreach (var borderPt in borderPt2NearPts.Keys)
                {
                    if (!borderPts.Contains(borderPt))
                    {
                        borderPts.Add(borderPt);
                    }
                    if (!allPts.Contains(borderPt))
                    {
                        allPts.Add(borderPt);
                    }
                }
            }
            dicTuples.Keys.ForEach(o => allPts.Add(o));
            StructureDealer.AddConnectUpToFour(ref dicTuples, allPts, itcBorderPts, MaxBeamLength);

            SimplifyDicTuples(zeroPts, SimilarPointsDis, SimilarAngle);
            //去除相交线，获得辅助外框
            StructureDealer.RemoveIntersectLines(ref dicTuples);
            List<Tuple<Point3d, Point3d>> closebdLines = BorderPtsConnect(outlineWalls, outerWalls, olCrossPts, ref dicTuples, transformer);
            StructureDealer.RemoveLinesInterSectWithCloseBorderLines(closebdLines, ref dicTuples);
            closebdLines.ForEach(o => StructureDealer.DeleteFromDicTuples(o.Item1, o.Item2, ref dicTuples));

            outline2BorderNearPts = PointsDealer.CreateOutline2BorderNearPts(dicTuples, outline2BorderNearPts.Keys.ToList());
            dicTuples = SplitAndMergeB(52000000);

            itcBorderPts = PointsDealer.FindIntersectBorderPt(allPts, outline2BorderNearPts.Keys.ToList(), ref outline2BorderNearPts);////////////////////////////////////////////////////////////
            itcBorderPts = PointsDealer.PointsDistinct(itcBorderPts);
            LineDealer.DeleteSameClassLine(itcBorderPts, ref dicTuples);
            SimplifyDicTuples(zeroPts, SimilarPointsDis, SimilarAngle);
            return dicTuples;
        }

        /// <summary>
        /// 获取多边形边界和多边形外的连接
        /// </summary>
        private void BorderConnectToVDNear(Point3dCollection clumnPts, Dictionary<Polyline, HashSet<Point3d>> outlineClumns)
        {
            //1、获取NearPt
            PointsDealer.VoronoiDiagramNearPoints(clumnPts, outlineNearPts);
            //2、获取“BorderPt与NearPt的连接”
            Dictionary<Point3d, HashSet<Point3d>> priority1stDicTuples = new Dictionary<Point3d, HashSet<Point3d>>();
            StructureBuilder.PriorityBorderPoints(outlineNearPts, outlineWallsMerged, outlineClumns, ref outline2BorderNearPts, ref outline2ZeroPts, ref priority1stDicTuples, MaxBeamLength, SimilarAngle);
            //3、删减无用的“BorderPt与NearPt的连接”
            outline2BorderNearPts = StructureDealer.UpdateBorder2NearPts(outline2BorderNearPts, priority1stDicTuples, SimilarAngle * 2);
        }

        /// <summary>
        /// 对现有的dicTuples结构进行多边形分割与合并
        /// </summary>
        private Dictionary<Point3d, HashSet<Point3d>> SplitAndMerge(Dictionary<Point3d, HashSet<Point3d>> dicTuples, Point3dCollection allPts,
            Dictionary<Point3d, Point3d> closeBorderLines)
        {
            //Split & Merge
            //LineDealer.DicTuplesStandardize(ref dicTuples, allPts);
            var findPolylineFromLines = new Dictionary<Tuple<Point3d, Point3d>, List<Tuple<Point3d, Point3d>>>();
            StructureBuilder.BuildPolygons(dicTuples, ref findPolylineFromLines);
            //splic polyline
            StructureBuilder.SplitBlock(ref findPolylineFromLines, closeBorderLines);
            //merge fragments and split if possible
            StructureBuilder.MergeFragments(ref findPolylineFromLines, closeBorderLines);
            //Deal with Intersect Near Points
            Dictionary<Point3d, HashSet<Point3d>> newDicTuples = LineDealer.TuplesStandardize(findPolylineFromLines.Keys.ToHashSet(), allPts);
            return newDicTuples;
        }

        /// <summary>
        /// 分情况连接边界上的点
        /// </summary>
        private List<Tuple<Point3d, Point3d>> BorderPtsConnect(Dictionary<Polyline, HashSet<Polyline>> outlineWalls, 
            Dictionary<Polyline, HashSet<Polyline>> outerWalls, Dictionary<Polyline, HashSet<Point3d>> olCrossPts, 
            ref Dictionary<Point3d, HashSet<Point3d>> dicTuples, ThMEPOriginTransformer transformer)
        {
            List<Tuple<Point3d, Point3d>> closeBorderLines = new List<Tuple<Point3d, Point3d>>();
            Dictionary<Point3d, Point3d> closeBorderLineA = StructureDealer.CloseBorderA(outlineWalls.Keys.ToHashSet(), dicTuples.Keys.ToList());

            string outlineLayerA = "TH_AI_HOUSEBOUND";
            LayerDealer.AddLayer(outlineLayerA, 1);
            LayerDealer.Output(closeBorderLineA, outlineLayerA, transformer);
            LayerDealer.HiddenLayer(outlineLayerA);
            foreach (var dic in closeBorderLineA)
            {
                StructureDealer.AddLineTodicTuples(dic.Key, dic.Value, ref dicTuples);
                closeBorderLines.Add(new Tuple<Point3d, Point3d>(dic.Key, dic.Value));
            }
            var oriPtsB = dicTuples.Keys.ToList();
            foreach (var pts in olCrossPts.Values)
            {
                oriPtsB.AddRange(pts);
            }
            //oriPtsB = StructureDealer.ReduceSimilarPoints(oriPtsB, zeroPts);
            Dictionary<Point3d, Point3d> closeBorderLineB = StructureDealer.CloseBorderA(outerWalls.Keys.ToHashSet(), oriPtsB);
            Dictionary<Point3d, HashSet<Point3d>> newdicTuples = LineDealer.TuplesStandardize(closeBorderLineB, dicTuples.Keys.ToList());
            var unifiedTyples = MainBeamPostProcess.UnifyTuples(newdicTuples);
            foreach (var unifiedTyple in unifiedTyples)
            {
                StructureDealer.AddLineTodicTuples(unifiedTyple.Item1, unifiedTyple.Item2, ref dicTuples);
                closeBorderLines.Add(new Tuple<Point3d, Point3d>(unifiedTyple.Item1, unifiedTyple.Item2));
            }
            string outlineLayerB = "TH_AI_WALLBOUND";
            LayerDealer.AddLayer(outlineLayerB, 2);
            LayerDealer.Output(unifiedTyples, outlineLayerB, transformer);
            LayerDealer.HiddenLayer(outlineLayerB);
            return closeBorderLines;
        }

        /// <summary>
        /// 处理墙与墙之间的连接
        /// </summary>
        /// <param name="dicTuples">已经加入房屋边框的dicTuples</param>
        /// <param name="outline2ZeroPts"></param>
        private void WallConnect(Dictionary<Point3d, HashSet<Point3d>> dicTuples, Dictionary<Polyline, List<Point3d>> outline2ZeroPts)
        {
            var itcBorderPts = PointsDealer.FindIntersectBorderPt(dicTuples.Keys.ToList(), outline2ZeroPts.Keys.ToList());
            var outlines = outline2ZeroPts.Keys.ToList();

            //连接相同的边界
            //连接逻辑：
            //保留连接线中点不在这个边界内的线
            //连接线两端的点不在同一条直线上
            //同线判定：(（a到lineA<500 && b到lineA<500）||（b到lineB<500 && a到lineB<500）)  lineA\lineB分别为a\b最近的线
            foreach (var outlineA in outlines)
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

        /// <summary>
        /// 简化dicTuples结构
        /// </summary>
        private void SimplifyDicTuples(List<Point3d> basePts, double deviation = 1, double angle = Math.PI / 8)
        {
            basePts = PointsDealer.PointsDistinct(basePts);
            StructureDealer.ReduceSimilarLine(ref dicTuples, angle);
            StructureDealer.ReduceSimilarPoints(ref dicTuples, basePts);
            LineDealer.DicTuplesStandardize(ref dicTuples, basePts, deviation);
        }

        public Dictionary<Point3d, HashSet<Point3d>> SplitAndMergeB(double splitArea = 0.0)
        {
            //Split & Merge
            SimplifyDicTuples(zeroPts, SimilarPointsDis, SimilarAngle);
            var closeBorderLines = StructureDealer.CloseBorder(outline2BorderNearPts);
            closeBorderLines.ForEach(o => StructureDealer.AddLineTodicTuples(o.Key, o.Value, ref dicTuples));
            StructureDealer.RemoveIntersectLines(ref dicTuples);

            //Split & Merge
            var findPolylineFromLines = new Dictionary<Tuple<Point3d, Point3d>, List<Tuple<Point3d, Point3d>>>();
            StructureBuilder.BuildPolygonsB(dicTuples, ref findPolylineFromLines);
            //splic polyline
            StructureBuilder.SplitBlockB(ref findPolylineFromLines, closeBorderLines, splitArea);

            //merge fragments and split if possible
            var ptList = new HashSet<Point3d>();
            foreach (var tup in findPolylineFromLines.Keys.ToList())
            {
                if (!ptList.Contains(tup.Item1))
                    ptList.Add(tup.Item1);
                if (!ptList.Contains(tup.Item2))
                    ptList.Add(tup.Item2);
            }
            var borderPts = PointsDealer.GetBorderPts(outline2BorderNearPts.Keys.ToList(), ptList);

            StructureBuilder.MergeFragmentsB(ref findPolylineFromLines, borderPts, closeBorderLines, splitArea);
            //或许可以不用closeBorderLines，可以用findPolylineFromLines polyline 包含的 borderPt的数量来判断是否加入这条线，如果多边形有大于等于3个点，则不处理这个多边形
            //Deal with Intersect Near Points
            Dictionary<Point3d, HashSet<Point3d>> newDicTuples = LineDealer.TuplesStandardize(findPolylineFromLines.Keys.ToHashSet(), zeroPts, 100);
            
            var itcBorderPts = PointsDealer.FindIntersectBorderPt(newDicTuples.Keys.ToList(), outline2BorderNearPts.Keys.ToList());
            LineDealer.DeleteSameClassLine(itcBorderPts, ref newDicTuples);
            return newDicTuples;
        }
    }
}
