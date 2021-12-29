using System;
using System.Linq;
using System.Collections.Generic;
using AcHelper;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPStructure.GirderConnect.Data;
using ThMEPStructure.GirderConnect.ConnectMainBeam.Data;
using ThMEPStructure.GirderConnect.ConnectMainBeam.Utils;
using DotNetARX;

namespace ThMEPStructure.GirderConnect.ConnectMainBeam.ConnectProcess
{
    public class Connect
    {
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
            Dictionary<Polyline, HashSet<Point3d>> outlineClumns, Dictionary<Polyline, HashSet<Polyline>> outerWalls, ref Dictionary<Polyline, HashSet<Point3d>> olCrossPts)
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
            allPts = PointsDealer.PointsDistinct(allPts, 1);
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



            //2.1、delete similar connect
            SimplifyDicTuples(zeroPts, 500, Math.PI / 12);
            //2.2 close border

            closeBorderLines = StructureDealer.CloseBorder(outline2BorderNearPts);

            //3.0 Split & Merge areas
            dicTuples = SplitAndMerge(dicTuples, allPts, closeBorderLines);
            var itcBorderPts = PointsDealer.FindIntersectBorderPt(allPts, outlineWallsMerged.Keys.ToList(), ref outline2BorderNearPts); //这步可能outliborder错导致dictupe多添加一遍
            LineDealer.DeleteSameClassLine(itcBorderPts, ref dicTuples);
            LineDealer.DeleteDiffClassLine(itcBorderPts, borderPts, ref dicTuples);
            LineDealer.AddSpecialLine(outline2BorderNearPts, ref dicTuples);
            LineDealer.DicTuplesStandardize(ref dicTuples, allPts, 500);


            StructureDealer.AddConnectUpToFour(ref dicTuples, allPts, itcBorderPts);
            PointsDealer.UpdateOutline2BorderNearPts(ref outline2BorderNearPts, dicTuples);
            //delete to four
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

            SimplifyDicTuples(zeroPts, 500, Math.PI / 12);

            dicTuples.Keys.ForEach(o => allPts.Add(o));
            StructureDealer.AddConnectUpToFour(ref dicTuples, allPts, itcBorderPts);

            StructureDealer.RemoveIntersectLines(ref dicTuples);
            List<Tuple<Point3d, Point3d>> closebdLines = BorderPtsConnect(outlineWalls, outerWalls, olCrossPts, ref dicTuples);
            StructureDealer.RemoveLinesInterSectWithCloseBorderLines(closebdLines, ref dicTuples);
            
            closebdLines.ForEach(o => StructureDealer.DeleteFromDicTuples(o.Item1, o.Item2, ref dicTuples));

            SimplifyDicTuples(zeroPts, 500);

            PointsDealer.UpdateOutline2BorderNearPts(ref outline2BorderNearPts, dicTuples);
            itcBorderPts = PointsDealer.FindIntersectBorderPt(allPts, outline2BorderNearPts.Keys.ToList(), ref outline2BorderNearPts);
            itcBorderPts = PointsDealer.PointsDistinct(itcBorderPts);
            LineDealer.DeleteSameClassLine(itcBorderPts, ref dicTuples);
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
            StructureBuilder.PriorityBorderPoints(outlineNearPts, outlineWallsMerged, outlineClumns, ref outline2BorderNearPts, ref outline2ZeroPts, ref priority1stDicTuples);
            //3、删减无用的“BorderPt与NearPt的连接”
            outline2BorderNearPts = StructureDealer.UpdateBorder2NearPts(outline2BorderNearPts, priority1stDicTuples);
        }

        /// <summary>
        /// 对现有的dicTuples结构进行多边形分割与合并
        /// </summary>
        private Dictionary<Point3d, HashSet<Point3d>> SplitAndMerge(Dictionary<Point3d, HashSet<Point3d>> dicTuples, Point3dCollection allPts, Dictionary<Point3d, Point3d> closeBorderLines)
        {
            closeBorderLines.ForEach(o => StructureDealer.AddLineTodicTuples(o.Key, o.Value, ref dicTuples));
            //Split & Merge
            //LineDealer.DicTuplesStandardize(ref dicTuples, allPts);.
            SimplifyDicTuples(zeroPts, 500, Math.PI / 12);
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
            Dictionary<Polyline, HashSet<Polyline>> outerWalls, Dictionary<Polyline, HashSet<Point3d>> olCrossPts, ref Dictionary<Point3d, HashSet<Point3d>> dicTuples)
        {
            List<Tuple<Point3d, Point3d>> closeBorderLines = new List<Tuple<Point3d, Point3d>>();
            Dictionary<Point3d, Point3d> closeBorderLineA = StructureDealer.CloseBorderA(outlineWalls.Keys.ToHashSet(), dicTuples.Keys.ToList());
            string outlineLayerA = "TH_AI_HOUSEBOUND";
            LayerDealer.AddLayer(outlineLayerA, 1);
            LayerDealer.Output(closeBorderLineA, outlineLayerA);
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
            Dictionary<Point3d, Point3d> closeBorderLineB = StructureDealer.CloseBorderA(outerWalls.Keys.ToHashSet(), oriPtsB);//oriPtsB
            Dictionary<Point3d, HashSet<Point3d>> newdicTuples = LineDealer.TuplesStandardize(closeBorderLineB, dicTuples.Keys.ToList());
            var unifiedTyples = MainBeamPostProcess.UnifyTuples(newdicTuples);
            foreach (var unifiedTyple in unifiedTyples)
            {
                StructureDealer.AddLineTodicTuples(unifiedTyple.Item1, unifiedTyple.Item2, ref dicTuples);
                closeBorderLines.Add(new Tuple<Point3d, Point3d>(unifiedTyple.Item1, unifiedTyple.Item2));
            }
            string outlineLayerB = "TH_AI_WALLBOUND";
            LayerDealer.AddLayer(outlineLayerB, 2);
            LayerDealer.Output(unifiedTyples, outlineLayerB);
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
    }
}
