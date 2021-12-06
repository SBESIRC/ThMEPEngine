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
            Dictionary<Polyline, HashSet<Point3d>> outlineClumns, AcadDatabase acdb = null)
        {
            //Steps:
            //1.1:Get near points of outlines
            Dictionary<Polyline, Point3dCollection> outlineNearPts = new Dictionary<Polyline, Point3dCollection>();
            foreach (var pl in outlineWalls.Keys)
            {
                outlineNearPts.Add(pl, new Point3dCollection());
            }
            PointsDealer.VoronoiDiagramNearPoints(clumnPts, outlineNearPts);

            Dictionary<Polyline, List<Point3d>> outline2ZeroPts = new Dictionary<Polyline, List<Point3d>>();
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
            Dictionary<Polyline, Dictionary<Point3d, HashSet<Point3d>>> outline2BorderNearPts = new Dictionary<Polyline, Dictionary<Point3d, HashSet<Point3d>>>();
            StructureBuilder.PriorityBorderPoints(outlineNearPts, outlineWalls, outlineClumns, outline2BorderNearPts, ref outline2ZeroPts);

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
            LineDealer.DicTuplesStandardize(dicTuples, allPts);
            Dictionary<Tuple<Point3d, Point3d>, List<Tuple<Point3d, Point3d>>> findPolylineFromLines = new Dictionary<Tuple<Point3d, Point3d>, List<Tuple<Point3d, Point3d>>>();
            StructureBuilder.BuildPolygons(dicTuples, findPolylineFromLines, acdb);
            //3.1、splic polyline
            StructureBuilder.SplitBlock(findPolylineFromLines, acdb, closeBorderLines);

            //ShowHash(findPolylineFromLines.Keys.ToList());
            //3.2、merge fragments and split if possible
            StructureBuilder.MergeFragments(findPolylineFromLines, acdb, closeBorderLines);
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
            var itcNearPts = PointsDealer.FindIntersectNearPt(allPts, outlineWalls, ref outline2BorderNearPts);
            
            //ShowInfo.ShowPoints(itcNearPts.ToList(), 'O', 2, 600);///////////
            //ShowInfo.ShowPoints(allPts, 'X', 2, 600);
            LineDealer.DeleteSameClassLine(itcNearPts, mainBeam); //删除相交近点互相之间的连线
            LineDealer.DeleteDiffClassLine(itcNearPts, borderPts, dicTuples);//删除近点和其连接的边界点之间的连线
            //LineDealer.DeleteDiffClassLine(itcNearPts, itcNearPts, dicTuples);

            //StructureDealer.DeleteConnectUpToFour(dicTuples, outline2BorderNearPts);

            //add to four constrained
            foreach(var tup in mainBeam)
            {
                AddTupleToDicTuple(tup, dicTuples);
                StructureDealer.AddLineTodicTuples(tup.Item1, tup.Item2, ref dicTuples);
                StructureDealer.AddLineTodicTuples(tup.Item2, tup.Item1, ref dicTuples);
            }

            StructureDealer.AddConnectUpToFour(dicTuples, outline2BorderNearPts, allPts, itcNearPts);
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
            StructureDealer.ReduceSimilarLine(ref dicTuples);
            List<Point3d> zeroPts = new List<Point3d>();
            foreach(var outline2ZeroPt in outline2ZeroPts)
            {
                zeroPts.AddRange(outline2ZeroPt.Value);
            }
            StructureDealer.ReduceSimilarPoints(ref dicTuples, zeroPts);
            //PointsDealer.UpdateOutline2BorderNearPts(ref outline2BorderNearPts, dicTuples);
            HashSet<Tuple<Point3d, Point3d>> closeBorderLineB = StructureDealer.CloseBorder(outline2BorderNearPts, outline2ZeroPts);
            string outlineLayer = "TH_AI_OUTLINE";
            LayerDealer.AddLayer(outlineLayer, 1);
            LayerDealer.Output(closeBorderLineB, outlineLayer);

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

        public static void ShowDic(Dictionary<Point3d, HashSet<Point3d>> dicTuples, int color = 1)
        {
            foreach(var dic in dicTuples)
            {
                foreach(var pt in dic.Value)
                {
                    ShowInfo.DrawLine(dic.Key, pt, color);
                }
            }
        }
        public static void ShowDic(Dictionary<Point3d, Point3d> dicTuples, int color = 2)
        {
            foreach (var dic in dicTuples)
            {
                ShowInfo.DrawLine(dic.Key, dic.Value, color);
            }
        }
        public static void ShowHash(List<Tuple<Point3d, Point3d>> tuples, int color = 2)
        {
            foreach(var tuple in tuples)
            {
                ShowInfo.DrawLine(tuple.Item1, tuple.Item2, color);
            }
        }

        public static void AddTupleToDicTuple(Tuple<Point3d, Point3d> tuple , Dictionary<Point3d, HashSet<Point3d>> dicTuples)
        {
            if (!dicTuples.ContainsKey(tuple.Item1))
            {
                dicTuples.Add(tuple.Item1, new HashSet<Point3d>());
            }
            if (!dicTuples[tuple.Item1].Contains(tuple.Item2))
            {
                dicTuples[tuple.Item1].Add(tuple.Item2);
            }
            if (!dicTuples.ContainsKey(tuple.Item2))
            {
                dicTuples.Add(tuple.Item2, new HashSet<Point3d>());
            }
            if (!dicTuples[tuple.Item2].Contains(tuple.Item1))
            {
                dicTuples[tuple.Item2].Add(tuple.Item1);
            }
        }
    }
}
