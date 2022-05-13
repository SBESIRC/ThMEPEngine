using System;
using System.Linq;
using System.Collections.Generic;
using AcHelper;
using NFox.Cad;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;

namespace ThMEPElectrical.EarthingGrid.Generator.Utils
{
    class MergeGrid
    {
        private Dictionary<Tuple<Point3d, Point3d>, Point3d> LineToCenter { get; set; } // 通过一条线找到这条线所在多边形对应的中点
        private Dictionary<Point3d, HashSet<Tuple<Point3d, Point3d>>> CenterToFace { get; set; } // 用一个点代表多边形
        private Dictionary<Point3d, HashSet<Point3d>> CenterGrid { get; set; } // 多边形中点连接形成的图
        private List<Tuple<double, double>> FaceSize { get; set; }
        private HashSet<Point3d> UnvisitedPts { get; set; }
        private HashSet<Point3d> FirmedPts { get; set; } //固定不会再扩大所在面的点

        public MergeGrid(Dictionary<Tuple<Point3d, Point3d>, Point3d> _lineToCenter,
            Dictionary<Point3d, HashSet<Tuple<Point3d, Point3d>>> _centerToFace, Dictionary<Point3d, HashSet<Point3d>> _centerGrid, List<Tuple<double, double>> _faceSize)
        {
            LineToCenter = _lineToCenter;
            CenterToFace = _centerToFace;
            CenterGrid = _centerGrid;
            FaceSize = _faceSize;
            UnvisitedPts = CenterGrid.Keys.ToHashSet();
            FirmedPts = new HashSet<Point3d>();
        }

        /// <summary>
        /// 进行网格合并
        /// </summary>
        public Dictionary<Point3d, HashSet<Point3d>> Merge()
        {
            var dbPoints = CenterGrid.Keys.Select(p => new DBPoint(p)).ToCollection();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(dbPoints);
            while (UnvisitedPts.Count > 0)
            {
                //找到起点
                var startPt = PointsDealer.GetLeftDownPt(UnvisitedPts);
                UnvisitedPts.Remove(startPt);
                var rectangle = OBB(LineDealer.LinesToConvexHull(CenterToFace[startPt]));
                var firstDirection = rectangle.GetPoint3dAt(2) - rectangle.GetPoint3dAt(1);
                Point3d lockedPt = GetLockedPt(startPt, firstDirection, spatialIndex);

                List<Point3d> horizonPts = new List<Point3d> { lockedPt };
                //1、向右找 右最下
                LoopAddFirmPt(lockedPt, firstDirection, ref horizonPts, spatialIndex);
                //2、向左找 左最上
                LoopAddFirmPt(lockedPt, -firstDirection, ref horizonPts, spatialIndex);

                List<Point3d> verticalPts = new List<Point3d>();
                foreach (var curHorizonPt in horizonPts)
                {
                    //3、向上找 正上右
                    LoopAddFirmPt(curHorizonPt, firstDirection.RotateBy(Math.PI / 2, Vector3d.ZAxis),
                        ref verticalPts, spatialIndex);
                    //4、向下找 正下左
                    LoopAddFirmPt(curHorizonPt, firstDirection.RotateBy(Math.PI / 2, -Vector3d.ZAxis),
                        ref verticalPts, spatialIndex);
                }
            }

            var earthGrid = new Dictionary<Point3d, HashSet<Point3d>>();
            foreach (var l in LineToCenter.Keys)
            {
                GraphDealer.AddLineToGraph(l.Item1, l.Item2, ref earthGrid);
            }
            return earthGrid;
        }

        /// <summary>
        /// 向链表中循环加入某个方向的合成块
        /// </summary>
        private void LoopAddFirmPt(Point3d basePt, Vector3d baseDirection, ref List<Point3d> storagePts, ThCADCoreNTSSpatialIndex spatialIndex)
        {
            var prePt = basePt;
            while (true)
            {
                Point3d curPt = GetUpLeftPt(prePt, baseDirection);
                if (curPt == prePt)
                {
                    break;
                }
                var lockPt = GetLockedPt(curPt, baseDirection, spatialIndex);
                storagePts.Add(lockPt);
                if (lockPt == curPt)
                {
                    break;
                }
                prePt = lockPt;
            }
        }

        /// <summary>
        /// 根据方向在图中获得要求的点
        /// </summary>
        /// <param name="mode">1、获得基准方向最右边的点(<90)，2、获得基准方向右边偏角最小的点</param>
        /// <returns></returns>
        private Point3d GetUpRightPt(Point3d basePt, Vector3d baseDirection)
        {
            if (!CenterGrid.ContainsKey(basePt))
            {
                return basePt;
            }
            Point3d ansPt = basePt;
            double minRotate = double.MaxValue;
            foreach (var curPt in CenterGrid[basePt])
            {
                var curRotate = (curPt - basePt).GetAngleTo(baseDirection, Vector3d.ZAxis);
                if (curRotate < Math.PI / 2 && curRotate < minRotate && UnvisitedPts.Contains(curPt))
                {
                    minRotate = curRotate;
                    ansPt = curPt;
                }
            }
            return ansPt;
        }

        private Point3d GetUpLeftPt(Point3d basePt, Vector3d baseDirection)
        {
            if (!CenterGrid.ContainsKey(basePt))
            {
                return basePt;
            }
            Point3d ansPt = basePt;
            double minRotate = double.MaxValue;
            foreach (var curPt in CenterGrid[basePt])
            {
                var curRotate = (curPt - basePt).GetAngleTo(baseDirection, -Vector3d.ZAxis);
                if (curRotate < Math.PI / 2 && curRotate < minRotate && UnvisitedPts.Contains(curPt))
                {
                    minRotate = curRotate;
                    ansPt = curPt;
                }
            }
            return ansPt;
        }

        /// <summary>
        /// 生成锁定点
        /// </summary>
        /// <param name="basePt">基准来时点</param>
        /// <param name="baseDirection">基准来时正方向</param>
        /// <returns></returns>
        private Point3d GetLockedPt(Point3d basePt, Vector3d baseDirection, ThCADCoreNTSSpatialIndex spatialIndex)
        {
            var lockedPt = basePt;
            var curDirection = baseDirection;
            for (int i = 0, loopCnt = 0; loopCnt < 4; ++i, curDirection = curDirection.RotateBy(Math.PI / 2, Vector3d.ZAxis))
            {
                var curPt = GetUpRightPt(lockedPt, curDirection);
                if (curPt == lockedPt)
                {
                    ++loopCnt;
                    continue;
                }
                var containPoints = new HashSet<Point3d> { lockedPt, curPt };
                int mergeTest = MergeTestContionCenterPts(spatialIndex, ref containPoints);
                if (mergeTest >= 0)
                {
                    var ansPt = MergeFaces(containPoints);
                    if(ansPt == new Point3d())
                    {
                        ++loopCnt;
                        continue;
                    }
                    if (mergeTest == 10) //结束合并
                    {
                        return ansPt;
                    }
                    lockedPt = ansPt;
                }
                else
                {
                    ++loopCnt;
                }
            }
            FirmedPts.Add(lockedPt);
            return lockedPt;
        }

        /// <summary>
        /// 测试合并后形成的矩形包含了哪些点，然后加入这些点后生成的图形是否符合规则
        /// </summary>
        private int MergeTestContionCenterPts(ThCADCoreNTSSpatialIndex spatialIndex, ref HashSet<Point3d> containPoints)
        {
            //1、生成当前多边形并集所构成的最小矩形
            var faceLinesA = GetFaceLines(containPoints);
            var rectangle = OBB(LineDealer.LinesToConvexHull(faceLinesA));
            var pl = LineDealer.Tuples2Polyline(faceLinesA.ToList());

            //2、查看此矩形所包含的中点
            foreach (var pt in spatialIndex.SelectWindowPolygon(rectangle).OfType<DBPoint>().Select(d => d.Position))
            {
                if(pl.Area > 100 && !pl.Contains(pt) && !UnvisitedPts.Contains(pt))
                {
                    return -1;
                }
                containPoints.Add(pt);
            }

            //3、查看矩形包含的中点所在多边形的并集是否符合要求
            var faceLinesB = GetFaceLines(containPoints);
            var plB = LineDealer.LinesToConvexHull(faceLinesB);
            return CheckRectangle(OBB(plB));
        }

        /// <summary>
        /// 合并多个多边形，修改对应的结构`
        /// </summary>
        /// <param name="centerPoints">多边形所被代表的点所组成的集合</param>
        private Point3d MergeFaces(HashSet<Point3d> centerPoints)
        {
            //1、get faceLines
            var faceLines = GetFaceLines(centerPoints);

            //2、get centerPt
            var centerPt = GetObjects.GetLinesCenter(faceLines.ToList());

            //3、update centerToFace & lineToCenter
            foreach (var cneterPoint in centerPoints)
            {
                if (CenterToFace.ContainsKey(cneterPoint))
                {
                    foreach(var line in CenterToFace[cneterPoint])
                    {
                        if (LineToCenter.ContainsKey(line))
                        {
                            LineToCenter.Remove(line);
                        }
                    }
                    CenterToFace.Remove(cneterPoint);
                }
                if (UnvisitedPts.Contains(cneterPoint))
                {
                    UnvisitedPts.Remove(cneterPoint);
                }
            }
            CenterToFace.Add(centerPt, faceLines);
            foreach (var faceLine in faceLines)
            {
                LineToCenter.Add(faceLine, centerPt);
            }

            //4、update centerGrid
            HashSet<Point3d> connectPts = new HashSet<Point3d>();
            foreach (var centerPoint in centerPoints)
            {
                if (CenterGrid.ContainsKey(centerPoint))
                {
                    foreach(var curConnectPt in CenterGrid[centerPoint])
                    {
                        if (!centerPoints.Contains(curConnectPt))
                        {
                            connectPts.Add(curConnectPt);
                        }
                    }
                    foreach (var pt in CenterGrid[centerPoint].ToList())
                    {
                        DeleteFromGraph(pt, centerPoint);
                    }
                }
                else
                {

                }
            }

            foreach (var connectPt in connectPts)
            {
                AddLineToGraph(connectPt, centerPt);
            }

            foreach (var pt in centerPoints)
            {
                FirmedPts.Add(pt);
            }
            return centerPt;
        }

        /// <summary>
        /// 获取点所代表多边形的并集多边形的边界线
        /// </summary>
        /// <param name="centerPoints"></param>
        /// <returns></returns>
        private HashSet<Tuple<Point3d, Point3d>> GetFaceLines(HashSet<Point3d> centerPoints)
        {
            var faceLines = new HashSet<Tuple<Point3d, Point3d>>();
            foreach (var centerPt in centerPoints)
            {
                if (!CenterToFace.ContainsKey(centerPt))
                {
                    continue;
                }
                foreach (var curLine in CenterToFace[centerPt])
                {
                    var converseLine = new Tuple<Point3d, Point3d>(curLine.Item2, curLine.Item1);
                    if (faceLines.Contains(converseLine))
                    {
                        faceLines.Remove(converseLine);
                    }
                    else
                    {
                        faceLines.Add(curLine);
                    }
                }
            }
            return faceLines;
        }

        private Polyline OBB(Polyline polygon)
        {
            return polygon.GetMinimumRectangle();
        }

        /// <summary>
        /// 查看一个矩形是否符合所给数据
        /// </summary>
        /// <returns>成功返回匹配的模式，失败返回-1</returns>
        private int CheckRectangle(Polyline rectangle)
        {
            double recLineA = rectangle.GetPoint3dAt(0).DistanceTo(rectangle.GetPoint3dAt(1));
            double recLineB = rectangle.GetPoint3dAt(1).DistanceTo(rectangle.GetPoint3dAt(2));
            double length, height;
            if (recLineA > recLineB)
            {
                length = recLineA;
                height = recLineB;
            }
            else
            {
                length = recLineB;
                height = recLineA;
            }
            for (int i = 0; i < FaceSize.Count; ++i)
            {
                if (length < FaceSize[i].Item1 && height < FaceSize[i].Item2)
                {
                    if (FaceSize[i].Item1 / length < 1.5 && FaceSize[i].Item2 / height < 1.5)
                    {
                        return 10;
                    }
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// 从DicTuple中删除一条双向线
        /// </summary>
        private void DeleteFromGraph(Point3d ptA, Point3d ptB)
        {
            if (CenterGrid.ContainsKey(ptA) && CenterGrid[ptA].Contains(ptB))
            {
                CenterGrid[ptA].Remove(ptB);
                if (CenterGrid[ptA].Count == 0)
                {
                    CenterGrid.Remove(ptA);
                }
            }
            if (CenterGrid.ContainsKey(ptB) && CenterGrid[ptB].Contains(ptA))
            {
                CenterGrid[ptB].Remove(ptA);
                if (CenterGrid[ptB].Count == 0)
                {
                    CenterGrid.Remove(ptB);
                }
            }
        }

        /// <summary>
        /// 将一条线加入字典结构
        /// </summary>
        private void AddLineToGraph(Point3d ptA, Point3d ptB)
        {
            if (!CenterGrid.ContainsKey(ptA))
            {
                CenterGrid.Add(ptA, new HashSet<Point3d>());
            }
            if (!CenterGrid[ptA].Contains(ptB))
            {
                CenterGrid[ptA].Add(ptB);
            }
            if (!CenterGrid.ContainsKey(ptB))
            {
                CenterGrid.Add(ptB, new HashSet<Point3d>());
            }
            if (!CenterGrid[ptB].Contains(ptA))
            {
                CenterGrid[ptB].Add(ptA);
            }
        }
    }
}