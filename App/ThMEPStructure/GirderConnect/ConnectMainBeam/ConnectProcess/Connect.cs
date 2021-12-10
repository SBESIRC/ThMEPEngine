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
            Dictionary<Polyline, HashSet<Polyline>> olWallsMerged = new Dictionary<Polyline, HashSet<Polyline>>();
            foreach(var o in outlineWalls)
            {
                olWallsMerged.Add(o.Key, o.Value);
            }
            foreach (var o in outerWalls)
            {
                olWallsMerged.Add(o.Key, o.Value);
            }
            //1.1:Get near points of outlines
            var outlineNearPts = new Dictionary<Polyline, Point3dCollection>();
            foreach (var pl in olWallsMerged.Keys)
            {
                outlineNearPts.Add(pl, new Point3dCollection());
            }
            PointsDealer.VoronoiDiagramNearPoints(clumnPts, outlineNearPts);

            var outline2ZeroPts = new Dictionary<Polyline, List<Point3d>>();
            //HashSet<Point3d> zeroPts = new HashSet<Point3d>();
            Point3dCollection allPts = new Point3dCollection();
            HashSet<Point3d> borderPts = new HashSet<Point3d>();
            foreach (Point3d clumnPt in clumnPts)
            {
                allPts.Add(clumnPt);
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
                }
            }
            //1.2:Get border points on/in outlines
            var outline2BorderNearPts = new Dictionary<Polyline, Dictionary<Point3d, HashSet<Point3d>>>();
            StructureBuilder.PriorityBorderPoints(outlineNearPts, olWallsMerged, outlineClumns, outline2BorderNearPts, ref outline2ZeroPts);

            foreach (var borderPt2NearPts in outline2BorderNearPts.Values)
            {
                foreach (var borderPt in borderPt2NearPts.Keys)
                {
                    borderPts.Add(borderPt);
                    allPts.Add(borderPt);
                }
            }

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
            //ShowDic(closeBorderLines, 2);
            foreach (var closeBorderLine in closeBorderLines)
            {
                if (!dicTuples.ContainsKey(closeBorderLine.Key))
                {
                    dicTuples.Add(closeBorderLine.Key, new HashSet<Point3d>());
                }
                if (!dicTuples[closeBorderLine.Key].Contains(closeBorderLine.Value))
                {
                    dicTuples[closeBorderLine.Key].Add(closeBorderLine.Value);
                }
            }

            //3.0 Split & Merge
            LineDealer.DicTuplesStandardize(ref dicTuples, allPts);
            var findPolylineFromLines = new Dictionary<Tuple<Point3d, Point3d>, List<Tuple<Point3d, Point3d>>>();
            StructureBuilder.BuildPolygons(dicTuples, findPolylineFromLines);
            //3.1、splic polyline
            StructureBuilder.SplitBlock(findPolylineFromLines, closeBorderLines);

            //ShowHash(findPolylineFromLines.Keys.ToList());
            //3.2、merge fragments and split if possible
            StructureBuilder.MergeFragments(findPolylineFromLines, closeBorderLines);
            var mainBeam = findPolylineFromLines.Keys.ToHashSet();
            foreach (var borderLine in closeBorderLines)
            {
                var tuple = new Tuple<Point3d, Point3d>(borderLine.Key, borderLine.Value);
                var converseTuple = new Tuple<Point3d, Point3d>(borderLine.Value, borderLine.Key);
                if (mainBeam.Contains(tuple))
                {
                    mainBeam.Remove(tuple);
                }
                if (mainBeam.Contains(converseTuple))
                {
                    mainBeam.Remove(converseTuple);
                }
            }
            //ShowDic(dicTuples);
            //4.0 Deal with Intersect Near Pointsnetload
            dicTuples.Clear();
            dicTuples = LineDealer.TuplesStandardize(findPolylineFromLines.Keys.ToHashSet(), allPts);
            //ShowDic(dicTuples, 1);
            var itcNearPts = PointsDealer.FindIntersectNearPt(allPts, olWallsMerged, ref outline2BorderNearPts);
            
            //ShowInfo.ShowPoints(itcNearPts.ToList(), 'O', 2, 600);//////
            //ShowInfo.ShowPoints(allPts, 'X', 2, 600);
            LineDealer.DeleteSameClassLine(itcNearPts, mainBeam); //删除相交近点互相之间的连线
            LineDealer.DeleteDiffClassLine(itcNearPts, borderPts, dicTuples);//删除近点和其连接的边界点之间的连线
            //LineDealer.DeleteDiffClassLine(itcNearPts, itcNearPts, dicTuples);

            //StructureDealer.DeleteConnectUpToFour(dicTuples, outline2BorderNearPts);

            //add to four constrained
            foreach(var tup in mainBeam)
            {
                //AddTupleToDicTuple(tup, dicTuples);
                StructureDealer.AddLineTodicTuples(tup.Item1, tup.Item2, ref dicTuples);
                StructureDealer.AddLineTodicTuples(tup.Item2, tup.Item1, ref dicTuples);
            }

            LineDealer.DicTuplesStandardize(ref dicTuples, allPts);
            StructureDealer.AddConnectUpToFour(ref dicTuples, allPts, itcNearPts);
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
            List<Point3d> zeroPts = new List<Point3d>();
            foreach(var outline2ZeroPt in outline2ZeroPts)
            {
                zeroPts.AddRange(outline2ZeroPt.Value);
            }
            //StructureDealer.ReduceSimilarPoints(ref dicTuples, zeroPts);

            StructureDealer.ReduceSimilarLine(ref dicTuples);
            //PointsDealer.UpdateOutline2BorderNearPts(ref outline2BorderNearPts, dicTuples);
            BorderPtsConnect(dicTuples, outlineWalls, outerWalls, olCrossPts, zeroPts);

            //foreach(var tup in closeBorderLineB)
            //{
            //    AddTupleToDicTuple(tup, dicTuples);
            //}
            //foreach (var item in dicTuples.Keys)
            //{
            //    allPts.Add(item);
            //}
            //StructureDealer.AddConnectUpToFour(dicTuples, outline2BorderNearPts, allPts, itcNearPts);
            //StructureBuilder.WallConnect(dicTuples, outline2ZeroPts);
            return dicTuples;
        }
      
        public static void WallConnect(Dictionary<Point3d, HashSet<Point3d>> dicTuples, Dictionary<Polyline, List<Point3d>> outline2ZeroPts)
        {
            //连接不同的边界
            //点上的连接至少为1

            //连接相同的边界
            //连接逻辑：
            //保留连接线中点不在这个边界内的线
            //连接线两端的点不在同一条直线上
            //同线判定：(（a到lineA<500 && b到lineA<500）||（b到lineB<500 && a到lineB<500）)  lineA\lineB分别为a\b最近的线

            //连接一边有墙点，一边没有墙点

        }

        /// <summary>
        /// 分情况连接边界上的点
        /// </summary>
        /// <param name="dicTuples"></param>
        /// <param name="outlineWalls"></param>
        /// <param name="outerWalls"></param>
        /// <param name="olCrossPts"></param>
        public static void BorderPtsConnect(Dictionary<Point3d, HashSet<Point3d>> dicTuples, Dictionary<Polyline, HashSet<Polyline>> outlineWalls, 
            Dictionary<Polyline, HashSet<Polyline>> outerWalls, Dictionary<Polyline, HashSet<Point3d>> olCrossPts, List<Point3d> zeroPts)
        {
            Dictionary<Point3d, Point3d> closeBorderLineA = StructureDealer.CloseBorderA(outlineWalls.Keys.ToHashSet(), dicTuples.Keys.ToList());
            string outlineLayerA = "TH_AI_HOUSEBOUND";
            LayerDealer.AddLayer(outlineLayerA, 1);
            LayerDealer.Output(closeBorderLineA, outlineLayerA);

            var oriPtsB = dicTuples.Keys.ToList();
            foreach (var pts in olCrossPts.Values)
            {
                oriPtsB.AddRange(pts);
            }
            oriPtsB = StructureDealer.ReduceSimilarPoints(oriPtsB, zeroPts);
            Dictionary<Point3d, Point3d> closeBorderLineB = StructureDealer.CloseBorderA(outerWalls.Keys.ToHashSet(), oriPtsB);
            string outlineLayerB = "TH_AI_WALLBOUND";
            LayerDealer.AddLayer(outlineLayerB, 2);
            LayerDealer.Output(closeBorderLineB, outlineLayerB);
        }
    }
}
