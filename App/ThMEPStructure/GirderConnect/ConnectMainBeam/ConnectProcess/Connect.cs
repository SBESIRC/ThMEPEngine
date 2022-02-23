using System;
using System.Linq;
using System.Collections.Generic;
using AcHelper;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Algorithm;
using ThMEPStructure.GirderConnect.ConnectMainBeam.Data;
using ThMEPStructure.GirderConnect.ConnectMainBeam.Utils;
using ThMEPStructure.GirderConnect.Data;

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
            Dictionary<Polyline, Polyline> outline2OriOutline, ref Dictionary<Polyline, HashSet<Point3d>> olCrossPts, 
            ThMEPOriginTransformer transformer, HashSet<Point3d> allColumnPts)
        {
            //0、预处理
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
            BorderConnectToNear borderConnectToNear = new BorderConnectToNear(clumnPts, outlineClumns, outlineNearPts,
                outlineWallsMerged, outline2BorderNearPts, outline2ZeroPts, MaxBeamLength, SimilarAngle);
            List<Tuple<Point3d, Point3d>> priority1stBorderNearTuples = new List<Tuple<Point3d, Point3d>>();
            Dictionary<Polyline, HashSet<Point3d>> outline2WallPts = new Dictionary<Polyline, HashSet<Point3d>>();
            outline2BorderNearPts = borderConnectToNear.BorderConnectToVDNear(ref priority1stBorderNearTuples, ref outline2WallPts);
            //1.5、获取被忽视墙点的连接
            Dictionary<Point3d, HashSet<Point3d>> wallConnectWall = new Dictionary<Point3d, HashSet<Point3d>>();
            WallConnect.WallConnection(ref wallConnectWall, ref outline2BorderNearPts, outline2WallPts, outerWalls.Keys.ToHashSet(),
                clumnPts.Cast<Point3d>().ToHashSet(), priority1stBorderNearTuples, outline2OriOutline, allColumnPts, 11000, SimilarAngle);
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
            //tuples = StructureDealer.DelaunayTriangulationConnect(allPts);
            tuples = StructureDealer.VoronoiDiagramConnect(allPts);
            dicTuples = TypeConvertor.Tuples2DicTuples(tuples, allPts.Cast<Point3d>().ToList());
            outline2BorderNearPts.Values.ForEach(borderPt2NearPts => {
                borderPt2NearPts.Keys.ForEach(bordrPt => {
                    if (dicTuples.ContainsKey(bordrPt)) {
                        dicTuples[bordrPt].ForEach(nearPt => {
                            if (!borderPt2NearPts[bordrPt].Contains(nearPt) && !borderPt2NearPts.ContainsKey(nearPt)) {
                                borderPt2NearPts[bordrPt].Add(nearPt);
                            }
                        });
                    }
                });
            });
            LineDealer.DeleteSameClassLine(borderPts, ref dicTuples);
            LineDealer.DeleteDiffClassLine(borderPts, clumnPts, ref dicTuples);
            DicTuplesDealer.AddSpecialLine(outline2BorderNearPts, ref dicTuples);
            DicTuplesDealer.SimplifyDicTuples(ref dicTuples, zeroPts, SimilarPointsDis, SimilarAngle * 2);

            //3、网格优化
            var itcBorderPts = PointsDealer.FindIntersectBorderPt(Outlines, allPts.Cast<Point3d>().ToHashSet());
            DicTuplesDealer.AddConnectUpToFour(ref dicTuples, allPts, itcBorderPts, MaxBeamLength);
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
            DicTuplesDealer.AddConnectUpToFour(ref dicTuples, allPts, itcBorderPts, MaxBeamLength);
            DicTuplesDealer.SimplifyDicTuples(ref dicTuples, zeroPts, SimilarPointsDis, SimilarAngle);

            //4、分割合并网格 & 处理墙连接
            AreaDealer areaDealer = new AreaDealer(SimilarAngle, SimilarPointsDis, SplitArea, dicTuples, Outlines, zeroPts);
            dicTuples = areaDealer.SplitAndMerge();
            StructureDealer.RemoveLinesInterSectWithImportantLines(dicTuples, ref wallConnectWall);
            foreach (var dicLines in wallConnectWall)
            {
                foreach (var pt in dicLines.Value)
                {
                    DicTuplesDealer.AddLineTodicTuples(dicLines.Key, pt, ref dicTuples);
                    //ShowInfo.DrawLine(dicLines.Key, pt, 4);
                }
            }
            DicTuplesDealer.SimplifyDicTuples(ref dicTuples, zeroPts, SimilarPointsDis, SimilarAngle);
            StructureDealer.RemoveLinesNearOutlines(Outlines, ref dicTuples);

            //5、获得辅助外框线
            List<Tuple<Point3d, Point3d>> closebdLines = BorderPtsConnect(outlineWalls, outerWalls, olCrossPts, transformer);
            closebdLines.ForEach(o => DicTuplesDealer.DeleteFromDicTuples(o.Item1, o.Item2, ref dicTuples));
            DicTuplesDealer.SimplifyDicTuples(ref dicTuples, zeroPts, SimilarPointsDis, SimilarAngle);
            StructureDealer.RemoveLinesInterSectWithOutlines(Outlines, ref dicTuples);

            //6、返回结果
            return dicTuples;
        }

        /// <summary>
        /// 分情况连接边界上的点
        /// </summary>
        private List<Tuple<Point3d, Point3d>> BorderPtsConnect(Dictionary<Polyline, HashSet<Polyline>> outlineWalls,
            Dictionary<Polyline, HashSet<Polyline>> outerWalls, Dictionary<Polyline, HashSet<Point3d>> olCrossPts,
            ThMEPOriginTransformer transformer)
        {
            DicTuplesDealer.RemoveIntersectLines(ref dicTuples);

            List<Tuple<Point3d, Point3d>> closeBorderLines = new List<Tuple<Point3d, Point3d>>();
            HashSet<Tuple<Point3d, Point3d>> closeBorderLineA = StructureDealer.CloseBorderA(outlineWalls.Keys.ToList(), dicTuples.Keys.ToList());

            string outlineLayerA = BeamConfig.HouseBoundLayerName;
            LayerDealer.AddLayer(outlineLayerA, 1);
            LayerDealer.Output(closeBorderLineA, outlineLayerA, transformer);
            LayerDealer.HiddenLayer(outlineLayerA);
            closeBorderLineA.ForEach(tup => {
                DicTuplesDealer.AddLineTodicTuples(tup.Item1, tup.Item2, ref dicTuples);
                closeBorderLines.Add(tup);
            });
            var oriPtsB = dicTuples.Keys.ToList();
            olCrossPts.Values.ForEach(pts => oriPtsB.AddRange(pts));
            HashSet<Tuple<Point3d, Point3d>> closeBorderLineB = StructureDealer.CloseBorderA(outerWalls.Keys.ToList(), oriPtsB);
            Dictionary<Point3d, HashSet<Point3d>> newdicTuples = TypeConvertor.Tuples2DicTuples(closeBorderLineB, dicTuples.Keys.ToList());
            var unifiedTyples = LineDealer.UnifyTuples(newdicTuples);
            unifiedTyples.ForEach(tup => {
                DicTuplesDealer.AddLineTodicTuples(tup.Item1, tup.Item2, ref dicTuples);
                closeBorderLines.Add(tup);
            });
            string outlineLayerB = BeamConfig.WallBoundLayerName;
            LayerDealer.AddLayer(outlineLayerB, 2);
            LayerDealer.Output(unifiedTyples, outlineLayerB, transformer);
            LayerDealer.HiddenLayer(outlineLayerB);
            return closeBorderLines;
        }
    }
}
