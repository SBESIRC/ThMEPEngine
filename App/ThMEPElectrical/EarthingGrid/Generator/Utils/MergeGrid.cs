using System;
using System.Linq;
using System.Collections.Generic;
using AcHelper;
using NFox.Cad;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPElectrical.EarthingGrid.Generator.Data;

namespace ThMEPElectrical.EarthingGrid.Generator.Utils
{
    class MergeGrid
    {
        private Dictionary<Tuple<Point3d, Point3d>, Point3d> lineToCenter = new Dictionary<Tuple<Point3d, Point3d>, Point3d>(); // 通过一条线找到这条线所在多边形对应的中点
        private Dictionary<Point3d, HashSet<Tuple<Point3d, Point3d>>> centerToFace = new Dictionary<Point3d, HashSet<Tuple<Point3d, Point3d>>>(); // 用一个点代表多边形
        private Dictionary<Point3d, HashSet<Point3d>> centerGrid = new Dictionary<Point3d, HashSet<Point3d>>(); // 多边形中点连接形成的图
        private List<Tuple<double, double>> faceSize = new List<Tuple<double, double>>();

        public MergeGrid(Dictionary<Tuple<Point3d, Point3d>, Point3d> _lineToCenter,
            Dictionary<Point3d, HashSet<Tuple<Point3d, Point3d>>> _centerToFace, Dictionary<Point3d, HashSet<Point3d>> _centerGrid, List<Tuple<double, double>> _faceSize)
        {
            lineToCenter = _lineToCenter;
            centerToFace = _centerToFace;
            centerGrid = _centerGrid;
            faceSize = _faceSize;
        }

        /// <summary>
        /// 进行网格合并
        /// </summary>
        public Dictionary<Point3d, HashSet<Point3d>> Merge()
        {
            var dbPoints = centerGrid.Keys.Select(p => new DBPoint(p)).ToCollection();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(dbPoints);

            MergeLoop(spatialIndex);

            var earthGrid = new Dictionary<Point3d, HashSet<Point3d>>();
            foreach (var face in centerToFace.Values)
            {
                foreach (var l in face)
                {
                    GraphDealer.AddLineToGraph(l.Item1, l.Item2, ref earthGrid);
                }
            }
            return earthGrid;
        }

        private void MergeLoop(ThCADCoreNTSSpatialIndex spatialIndex)
        {
            HashSet<Point3d> unvisitedPts = centerGrid.Keys.ToHashSet();
            HashSet<Point3d> firmedPts = new HashSet<Point3d>(); //固定不会再扩大所在面的点
            while (unvisitedPts.Count > 0)
            {
                //找到起点
                var startPt = PointsDealer.GetLeftDownPt(unvisitedPts);
                //ShowInfo.ShowPointAsO(startPt, 1, 600);
                unvisitedPts.Remove(startPt);
                if (!centerToFace.ContainsKey(startPt))
                {
                    continue;
                }
                var ol = LineDealer.Tuples2Polyline(centerToFace[startPt].ToList());
                if (ol.Area < 10000)
                {
                    continue;
                }
                var rectangle = OBB(ol);
                var firstDirection = rectangle.GetPoint3dAt(1) - rectangle.GetPoint3dAt(0);
                Point3d lockedPt = GetLockedPt(startPt, firstDirection, ref unvisitedPts, spatialIndex, ref firmedPts);
                firmedPts.Add(lockedPt);

                List<Point3d> horizonPts = new List<Point3d> { lockedPt };
                //1、向右找 右最下
                LoopAddFirmPt(startPt, firstDirection, ref horizonPts, ref firmedPts, ref unvisitedPts, spatialIndex, 1);//1
                //2、向左找 左最上
                LoopAddFirmPt(startPt, -firstDirection, ref horizonPts, ref firmedPts, ref unvisitedPts, spatialIndex, 1);//1

                List<Point3d> verticalPts = new List<Point3d>();
                foreach (var curHorizonPt in horizonPts)
                {
                    //3、向上找 正上右
                    LoopAddFirmPt(curHorizonPt, firstDirection.RotateBy(Math.PI / 2, Vector3d.ZAxis),
                        ref verticalPts, ref firmedPts, ref unvisitedPts, spatialIndex, 2); //2
                    //4、向下找 正下左
                    LoopAddFirmPt(curHorizonPt, firstDirection.RotateBy(Math.PI / 2, -Vector3d.ZAxis),
                        ref verticalPts, ref firmedPts, ref unvisitedPts, spatialIndex, 2); //2
                }
            }
        }

        /// <summary>
        /// 向链表中循环加入某个方向的合成块
        /// </summary>
        private void LoopAddFirmPt(Point3d basePt, Vector3d baseDirection, ref List<Point3d> storagePts,
            ref HashSet<Point3d> firmedPts, ref HashSet<Point3d> unvisitedPts, ThCADCoreNTSSpatialIndex spatialIndex, int mode)
        {
            baseDirection = baseDirection.RotateBy(Math.PI / 2, -Vector3d.ZAxis);
            var prePt = basePt;
            //Random rd = new Random();
            //var color = rd.Next(1, 10) % 6 + 1;
            while (true)
            {
                Point3d curPt = GetUpRightPt(prePt, baseDirection, mode);
                if (curPt == prePt)
                {
                    break;
                }
                var lockPt = GetLockedPt(curPt, baseDirection, ref unvisitedPts, spatialIndex, ref firmedPts);

                //ShowInfo.ShowPointAsO(lockPt, color, 500);
                storagePts.Add(lockPt);
                firmedPts.Add(lockPt);
                if (lockPt == curPt)
                {
                    //ShowInfo.ShowPointAsU(lockPt, color, 800);
                    //ShowInfo.ShowGeometry(LineDealer.Tuples2Polyline(centerToFace[lockPt].ToList()).ToNTSGeometry(), color); //这里会有关键字不在的报错
                    break;
                }
                prePt = curPt;
            }
        }

        /// <summary>
        /// 根据方向在图中获得要求的点
        /// </summary>
        /// <param name="mode">1、获得基准方向最右边的点(<90)，2、获得基准方向右边偏角最小的点</param>
        /// <returns></returns>
        private Point3d GetUpRightPt(Point3d basePt, Vector3d baseDirection, int mode)
        //这个函数可以通过矩阵运算来减少运算时间，但我只是知道结果和0、1比较，忘了怎么算了还不想推
        {
            if (!centerGrid.ContainsKey(basePt))
            {
                return basePt;
            }
            Point3d ansPt = basePt;
            if (mode == 1)
            {
                double maxRotate = 0;
                foreach (var curPt in centerGrid[basePt])
                {
                    var curRotate = (curPt - basePt).GetAngleTo(baseDirection);
                    if (curRotate < Math.PI / 2 && curRotate > maxRotate)
                    {
                        maxRotate = curRotate;
                        ansPt = curPt;
                    }
                }
            }
            else
            {
                double minRotate = double.MaxValue;
                foreach (var curPt in centerGrid[basePt])
                {
                    var curRotate = (curPt - basePt).GetAngleTo(baseDirection);
                    if (curRotate < Math.PI / 2 && curRotate < minRotate)
                    {
                        minRotate = curRotate;
                        ansPt = curPt;
                    }
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
        private Point3d GetLockedPt(Point3d basePt, Vector3d baseDirection, ref HashSet<Point3d> unvisitedPts,
            ThCADCoreNTSSpatialIndex spatialIndex, ref HashSet<Point3d> firmedPts)
        {
            var lockedPt = basePt;
            for (int i = 0, loopCnt = 0; ; ++i)
            {
                if (loopCnt >= 4)
                {
                    break;
                }

                Vector3d curDirection = baseDirection.RotateBy(i % 4 / 2 * Math.PI, Vector3d.ZAxis);
                if (HaveFirmedPt(lockedPt, curDirection, firmedPts))
                {
                    ++loopCnt;
                    continue;
                }
                else
                {
                    loopCnt = 0;  //此处有死循环
                }
                firmedPts.Add(lockedPt); //看看是不是一直走这个，会不会报错
                Point3d curPt = GetUpRightPt(lockedPt, curDirection, 2); // 2

                var tmpList = new List<Point3d> { lockedPt, curPt };
                var containPoints = new List<Point3d>();
                int mergeTest = MergeTestContionCenterPts(tmpList, spatialIndex, ref containPoints); //这一步计算包含的点有问题，不知道为何有些点没包含在内

                if (mergeTest >= 0)
                {
                    if (mergeTest == 10) //结束合并
                    {
                        unvisitedPts.Remove(lockedPt);
                        unvisitedPts.Remove(curPt);
                        return MergeFaces(containPoints, ref unvisitedPts);
                    }
                    lockedPt = MergeFaces(containPoints, ref unvisitedPts);
                    if (lockedPt == containPoints[0])
                    {
                        ++loopCnt;
                        //continue;
                    }
                }
                else if (mergeTest == -1)
                {
                    unvisitedPts.Remove(lockedPt);
                    unvisitedPts.Remove(curPt);
                    break;
                }
                ++loopCnt;//当一直返回-1就不能退出循环了、、、、、、、、、、、、、、想想原因
            }
            return lockedPt;
        }

        /// <summary>
        /// 查看点的某个方向是否含有锁定点
        /// </summary>
        private bool HaveFirmedPt(Point3d basePt, Vector3d baseDirection, HashSet<Point3d> firmedPts)
        {
            if (!centerGrid.ContainsKey(basePt))
            {
                return true;
            }
            foreach (var pt in centerGrid[basePt])
            {
                if ((pt - basePt).GetAngleTo(baseDirection) < Math.PI / 2 && firmedPts.Contains(pt))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 测试合并后形成的矩形包含了哪些点，然后加入这些点后生成的图形是否符合规则
        /// </summary>
        private int MergeTestContionCenterPts(List<Point3d> centerPoints, ThCADCoreNTSSpatialIndex spatialIndex, ref List<Point3d> containPoints)
        {
            //0、chenck feasiblity
            foreach (var cneterPoint in centerPoints)
            {
                if (!centerGrid.ContainsKey(cneterPoint) || !centerToFace.ContainsKey(cneterPoint))
                {
                    return -1;////全返回-1？
                }
            }

            //1、生成当前多边形并集所构成的最小矩形
            var faceLinesA = GetFaceLines(centerPoints);
            var rectangle = OBB(LineDealer.Tuples2Polyline(faceLinesA.ToList()));

            //2、查看此矩形所包含的中点
            containPoints = spatialIndex.SelectWindowPolygon(rectangle).OfType<DBPoint>().Select(d => d.Position).Distinct().ToList();

            //3、查看矩形包含的中点所在多边形的并集是否符合要求
            var faceLinesB = GetFaceLines(containPoints);
            if (faceLinesB.Count < 3)
            {
                //ShowInfo.ShowPoints(containPoints, 'X', 1, 300);
                return -1;
            }
            return CheckRectangle(OBB(LineDealer.Tuples2Polyline(faceLinesB.ToList())));
        }


        /// <summary>
        /// 合并多个多边形，修改对应的结构
        /// </summary>
        /// <param name="centerPoints">多边形所被代表的点所组成的集合</param>
        private Point3d MergeFaces(List<Point3d> centerPoints, ref HashSet<Point3d> unvisitedPts)
        {
            //0、chenck feasiblity
            foreach (var cneterPoint in centerPoints)
            {
                if (!centerGrid.ContainsKey(cneterPoint) || !centerToFace.ContainsKey(cneterPoint))
                {
                    return centerPoints[0];
                }
            }

            //1、get faceLines
            var faceLines = GetFaceLines(centerPoints);

            //2、get centerPt
            var centerPt = GetObjects.GetLinesCenter(faceLines.ToList());

            //3、update lineToCenter
            foreach (var faceLine in faceLines)
            {
                if (lineToCenter.ContainsKey(faceLine)) ////////////想想这里为什么会重复
                {
                    lineToCenter.Remove(faceLine);
                }
                lineToCenter.Add(faceLine, centerPt);
            }

            //4、update centerToFace
            foreach (var cneterPoint in centerPoints)
            {
                centerToFace.Remove(cneterPoint);
                if (unvisitedPts.Contains(cneterPoint))
                {
                    unvisitedPts.Remove(cneterPoint);
                }
            }
            centerToFace.Add(centerPt, faceLines);

            //5、update centerGrid
            HashSet<Point3d> cntPts = new HashSet<Point3d>();
            var ctPts = centerPoints.ToHashSet();
            var tmpGrid = new Dictionary<Point3d, HashSet<Point3d>>();
            foreach (var grid in centerGrid)
            {
                tmpGrid.Add(grid.Key, grid.Value.ToHashSet());
            }
            foreach (var cneterPoint in centerPoints)
            {
                foreach (var cntPt in tmpGrid[cneterPoint])
                {
                    //if (cntPt != cneterPoint)
                    if (!ctPts.Contains(cntPt))
                    {
                        cntPts.Add(cntPt); //互相之间不能连接
                    }
                    GraphDealer.DeleteFromGraph(cntPt, cneterPoint, ref centerGrid);
                }
            }
            foreach (var cntPt in cntPts)
            {
                GraphDealer.AddLineToGraph(cntPt, centerPt, ref centerGrid);
            }

            return centerPt;
        }

        /// <summary>
        /// 获取点所代表多边形的并集多边形的边界线
        /// </summary>
        /// <param name="centerPoints"></param>
        /// <returns></returns>
        private HashSet<Tuple<Point3d, Point3d>> GetFaceLines(List<Point3d> centerPoints)
        {
            var faceLines = new HashSet<Tuple<Point3d, Point3d>>();
            foreach (var centerPt in centerPoints)
            {
                if (!centerToFace.ContainsKey(centerPt)) ///////临时修补，不知道为啥没有
                {
                    continue;
                }
                foreach (var curLine in centerToFace[centerPt])
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

        private bool IsRectangle(Polyline polygon, double degree)
        {
            return polygon.IsSimilar(OBB(polygon), degree);
        }

        private Polyline OBB(Polyline polygon)
        {
            return polygon.GetMinimumRectangle();
        }

        /// <summary>
        /// 查看一个矩形是否符合所给数据
        /// </summary>
        /// <param name="centerPt"></param>
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
            for (int i = 0; i < faceSize.Count; ++i)
            {
                if (length < faceSize[i].Item1 && height < faceSize[i].Item2)
                {
                    if (faceSize[i].Item1 / length < 1.5 && faceSize[i].Item2 / height < 1.5)
                    {
                        return 10;
                    }
                    return i;
                }
            }
            return -1;
        }
    }
}
