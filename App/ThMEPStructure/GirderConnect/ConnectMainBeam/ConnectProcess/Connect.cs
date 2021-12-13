using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AcHelper;
using Linq2Acad;
using DotNetARX;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPStructure.GirderConnect.ConnectMainBeam.Utils;
using ThMEPStructure.GirderConnect.ConnectMainBeam.Data;
using ThMEPStructure.GirderConnect.Data;

namespace ThMEPStructure.GirderConnect.ConnectMainBeam.ConnectProcess
{
    class Connect
    {
        /// <summary>
        /// Connect Steps
        /// </summary>
        /// <param name="clumnPts"></param>
        /// <param name="outlineWalls"></param>
        /// <returns></returns>
        public static Dictionary<Point3d, HashSet<Point3d>> Calculate(Point3dCollection clumnPts, Dictionary<Polyline, HashSet<Polyline>> outlineWalls,
            Dictionary<Polyline, HashSet<Point3d>> outlineClumns, Dictionary<Polyline, HashSet<Polyline>> outerWalls, ref Dictionary<Polyline, HashSet<Point3d>> olCrossPts)
        {
            //Steps:
            //1.0: Merge "outlineWalls" and "outerWalls"
            Dictionary<Polyline, HashSet<Polyline>> outlineWallsMerged = new Dictionary<Polyline, HashSet<Polyline>>();
            foreach(var o in outlineWalls)
            {
                outlineWallsMerged.Add(o.Key, o.Value);
            }
            foreach (var o in outerWalls)
            {
                outlineWallsMerged.Add(o.Key, o.Value);
            }
            //1.1:Get near points of outlines
            var outlineNearPts = new Dictionary<Polyline, Point3dCollection>();
            foreach (var pl in outlineWallsMerged.Keys)
            {
                outlineNearPts.Add(pl, new Point3dCollection());
            }
            PointsDealer.VoronoiDiagramNearPoints(clumnPts, outlineNearPts);

            var outline2ZeroPts = new Dictionary<Polyline, List<Point3d>>();
            //HashSet<Point3d> zeroPts = new HashSet<Point3d>();
            Point3dCollection allPts = new Point3dCollection();
            HashSet<Point3d> borderPts = new HashSet<Point3d>();

            Point3dCollection zeroPtCollections = new Point3dCollection();
            foreach (Point3d clumnPt in clumnPts)
            {
                allPts.Add(clumnPt);
                zeroPtCollections.Add(clumnPt);
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
                    zeroPtCollections.Add(pt);
                }
            }
            //1.2:Get border points on/in outlines
            var outline2BorderNearPts = new Dictionary<Polyline, Dictionary<Point3d, HashSet<Point3d>>>();
            StructureBuilder.PriorityBorderPoints(outlineNearPts, outlineWallsMerged, outlineClumns, outline2BorderNearPts, ref outline2ZeroPts);
            List<Point3d> zeroPts = new List<Point3d>();
            foreach (var outline2ZeroPt in outline2ZeroPts)
            {
                zeroPts.AddRange(outline2ZeroPt.Value);
                foreach(var pt in outline2ZeroPt.Value)
                {
                    zeroPtCollections.Add(pt);
                }
            }
            
            foreach (var borderPt2NearPts in outline2BorderNearPts.Values)
            {
                foreach (var borderPt in borderPt2NearPts.Keys)
                {
                    borderPts.Add(borderPt);
                    allPts.Add(borderPt);
                    zeroPtCollections.Add(borderPt);
                }
            }
            zeroPtCollections = StructureDealer.ReduceSimilarPoints(zeroPtCollections);

            //1.3:DT/VDconnect points with borderPoints and clumnPoints
            //HashSet<Tuple<Point3d, Point3d>> tuples = StructureBuilder.DelaunayTriangulationConnect(allPts);
            HashSet<Tuple<Point3d, Point3d>> tuples = StructureBuilder.VoronoiDiagramConnect(allPts);
            Dictionary<Point3d, HashSet<Point3d>> dicTuples = LineDealer.TuplesStandardize(tuples, allPts);

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

            //2.0
            LineDealer.DeleteSameClassLine(borderPts, dicTuples);
            LineDealer.DeleteDiffClassLine(borderPts, clumnPts, dicTuples); //重要区分,删除与否要看情况
            LineDealer.AddSpecialLine(outline2BorderNearPts, dicTuples);

            //2.1、delete connect up to four
            StructureDealer.DeleteConnectUpToFourA(dicTuples, outline2BorderNearPts);
            
            //2.2 close border
            Dictionary<Point3d, Point3d> closeBorderLines = StructureDealer.CloseBorder(outline2BorderNearPts);
            closeBorderLines.ForEach(o => StructureDealer.AddLineTodicTuples(o.Key, o.Value, ref dicTuples));

            //3.0 Split & Merge
            LineDealer.DicTuplesStandardize(ref dicTuples, allPts);
            var findPolylineFromLines = new Dictionary<Tuple<Point3d, Point3d>, List<Tuple<Point3d, Point3d>>>();
            StructureBuilder.BuildPolygons(dicTuples, findPolylineFromLines);
            //3.1、splic polyline
            StructureBuilder.SplitBlock(findPolylineFromLines, closeBorderLines);

            //ShowHash(findPolylineFromLines.Keys.ToList());
            //3.2、merge fragments and split if possible


            StructureBuilder.MergeFragments(findPolylineFromLines, closeBorderLines);
            //var mainBeam = findPolylineFromLines.Keys.ToHashSet();
            //foreach (var borderLine in closeBorderLines)
            //{
            //    var tuple = new Tuple<Point3d, Point3d>(borderLine.Key, borderLine.Value);
            //    var converseTuple = new Tuple<Point3d, Point3d>(borderLine.Value, borderLine.Key);
            //    if (mainBeam.Contains(tuple))
            //    {
            //        mainBeam.Remove(tuple);
            //    }
            //    if (mainBeam.Contains(converseTuple))
            //    {
            //        mainBeam.Remove(converseTuple);
            //    }
            //}

            //4.0 Deal with Intersect Near Pointsnetload
            dicTuples.Clear();
            dicTuples = LineDealer.TuplesStandardize(findPolylineFromLines.Keys.ToHashSet(), allPts);
            var itcBorderPts = PointsDealer.FindIntersectBorderPt(allPts, outlineWallsMerged.Keys.ToList(), ref outline2BorderNearPts);
            
            LineDealer.DeleteSameClassLine(itcBorderPts, dicTuples);//删除相交近点互相之间的连线
            LineDealer.DeleteDiffClassLine(itcBorderPts, borderPts, dicTuples);//删除近点和其连接的边界点之间的连线
            //LineDealer.DeleteDiffClassLine(itcNearPts, itcNearPts, dicTuples);

            //StructureDealer.DeleteConnectUpToFour(dicTuples, outline2BorderNearPts);


            LineDealer.DicTuplesStandardize(ref dicTuples, allPts);
            Point3dCollection zeroPtCs = new Point3dCollection();
            zeroPts.ForEach(o => zeroPtCs.Add(o));
            StructureDealer.AddConnectUpToFour(ref dicTuples, allPts, itcBorderPts);
            PointsDealer.UpdateOutline2BorderNearPts(ref outline2BorderNearPts, dicTuples);
            //delete to four
            StructureDealer.DeleteConnectUpToFourB(dicTuples, outline2BorderNearPts); //删除的时候，如果删除点的对点是外边线上的并且只有这一个连线，则不能删掉，其他都可删
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
            //在此之前 outline2BorderNearPts 有问题， 有些点是空的，可能是较好的点但被删除了。、、、、、、、、、、、、、、、、、、、、、、、、、、、、、、、、、、、、、、、、、、、
            //StructureDealer.ReduceSimilarPoints(ref dicTuples, zeroPts);

            //PointsDealer.UpdateOutline2BorderNearPts(ref outline2BorderNearPts, dicTuples);
            StructureDealer.ReduceSimilarLine(ref dicTuples);
            List<Tuple<Point3d, Point3d>> closebdLines = BorderPtsConnect(ref dicTuples, outlineWalls, outerWalls, olCrossPts, zeroPts);
            LineDealer.DicTuplesStandardize(ref dicTuples, zeroPts);
            StructureDealer.ReduceSimilarPoints(ref dicTuples, zeroPts);
            foreach (var item in dicTuples.Keys)
            {
                allPts.Add(item);
            }
            StructureDealer.AddConnectUpToFour(ref dicTuples, allPts, itcBorderPts);

            //SplitAndMerge(ref dicTuples, allPts, outline2BorderNearPts);

            StructureDealer.ReduceIntersectLines(ref dicTuples);

            //Dictionary<Point3d, Point3d> closeBorderLineAll_a = StructureDealer.CloseBorderA(outlineWallsMerged.Keys.ToHashSet(), zeroPtCollections);
            //foreach (var closeBorderLineA_A in closeBorderLineAll_a)
            //{
            //    StructureDealer.AddLineTodicTuples(closeBorderLineA_A.Key, closeBorderLineA_A.Value, ref dicTuples);
            //    StructureDealer.AddLineTodicTuples(closeBorderLineA_A.Value, closeBorderLineA_A.Key, ref dicTuples);
            //}
            //WallConnect(dicTuples, outline2ZeroPts);

            foreach (var tup in closebdLines)
            {
                StructureDealer.DeleteFromDicTuples(tup.Item1, tup.Item2, ref dicTuples);
                StructureDealer.DeleteFromDicTuples(tup.Item2, tup.Item1, ref dicTuples);
            }
            return dicTuples;
        }
      
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dicTuples">已经加入房屋边框的dicTuples</param>
        /// <param name="outline2ZeroPts"></param>
        public static void WallConnect(Dictionary<Point3d, HashSet<Point3d>> dicTuples, Dictionary<Polyline, List<Point3d>> outline2ZeroPts)
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
                foreach(var outlineAPtA in outlineAPts)
                {
                    if(dicTuples[outlineAPtA].Count > 2)
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
        /// 分情况连接边界上的点
        /// </summary>
        /// <param name="dicTuples"></param>
        /// <param name="outlineWalls"></param>
        /// <param name="outerWalls"></param>
        /// <param name="olCrossPts"></param>
        public static List<Tuple<Point3d, Point3d>> BorderPtsConnect(ref Dictionary<Point3d, HashSet<Point3d>> dicTuples, Dictionary<Polyline, HashSet<Polyline>> outlineWalls, 
            Dictionary<Polyline, HashSet<Polyline>> outerWalls, Dictionary<Polyline, HashSet<Point3d>> olCrossPts, List<Point3d> zeroPts)
        {
            List<Tuple<Point3d, Point3d>> closeBorderLines = new List<Tuple<Point3d, Point3d>>();
            Dictionary<Point3d, Point3d> closeBorderLineA = StructureDealer.CloseBorderA(outlineWalls.Keys.ToHashSet(), dicTuples.Keys.ToList());
            string outlineLayerA = "TH_AI_HOUSEBOUND";
            LayerDealer.AddLayer(outlineLayerA, 1);
            LayerDealer.Output(closeBorderLineA, outlineLayerA);
            foreach(var dic in closeBorderLineA)
            {
                StructureDealer.AddLineTodicTuples(dic.Key, dic.Value, ref dicTuples);
                StructureDealer.AddLineTodicTuples(dic.Value, dic.Key, ref dicTuples);
                closeBorderLines.Add(new Tuple<Point3d, Point3d>(dic.Key, dic.Value));
            }

            var oriPtsB = dicTuples.Keys.ToList();
            foreach (var pts in olCrossPts.Values)
            {
                oriPtsB.AddRange(pts);
            }
            oriPtsB = StructureDealer.ReduceSimilarPoints(oriPtsB, zeroPts);
            Dictionary<Point3d, Point3d> closeBorderLineB = StructureDealer.CloseBorderA(outerWalls.Keys.ToHashSet(), oriPtsB);
            Dictionary<Point3d, HashSet<Point3d>> newdicTuples = LineDealer.TuplesStandardize(closeBorderLineB, zeroPts);
            var unifiedTyples = MainBeamPostProcess.UnifyTuples(newdicTuples);
            foreach (var unifiedTyple in unifiedTyples)
            {
                StructureDealer.AddLineTodicTuples(unifiedTyple.Item1, unifiedTyple.Item2, ref dicTuples);
                StructureDealer.AddLineTodicTuples(unifiedTyple.Item2, unifiedTyple.Item1, ref dicTuples);
                closeBorderLines.Add(new Tuple<Point3d, Point3d>(unifiedTyple.Item1, unifiedTyple.Item2));
            }
            string outlineLayerB = "TH_AI_WALLBOUND";
            LayerDealer.AddLayer(outlineLayerB, 2);
            LayerDealer.Output(unifiedTyples, outlineLayerB);
            return closeBorderLines;
        }

        public static void SplitAndMerge(ref Dictionary<Point3d, HashSet<Point3d>> dicTuples, Point3dCollection allPts, 
            Dictionary<Polyline, Dictionary<Point3d, HashSet<Point3d>>> outline2BorderNearPts)
        {
            Dictionary<Point3d, Point3d> closeBorderLines = StructureDealer.CloseBorderA(outline2BorderNearPts.Keys.ToHashSet(), dicTuples.Keys.ToList());
            foreach (var closeBorderLine in closeBorderLines)
            {
                StructureDealer.AddLineTodicTuples(closeBorderLine.Key, closeBorderLine.Value, ref dicTuples);
            }
            LineDealer.DicTuplesStandardize(ref dicTuples, allPts);
            var findPolylineFromLines = new Dictionary<Tuple<Point3d, Point3d>, List<Tuple<Point3d, Point3d>>>();
            StructureBuilder.BuildPolygons(dicTuples, findPolylineFromLines);
            StructureBuilder.SplitBlock(findPolylineFromLines, closeBorderLines);
            dicTuples.Clear();
            dicTuples = LineDealer.TuplesStandardize(findPolylineFromLines.Keys.ToHashSet(), allPts);

            var itcBorderPts = PointsDealer.FindIntersectBorderPt(allPts, outline2BorderNearPts.Keys.ToList(), ref outline2BorderNearPts);
            LineDealer.DeleteSameClassLine(itcBorderPts, dicTuples);
        }
    }
}
