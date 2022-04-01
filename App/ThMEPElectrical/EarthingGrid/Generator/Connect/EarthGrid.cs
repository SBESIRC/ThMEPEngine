using System;
using System.Linq;
using System.Collections.Generic;
using AcHelper;
using NFox.Cad;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPElectrical.EarthingGrid.Generator.Utils;
using ThMEPElectrical.EarthingGrid.Generator.Data;

namespace ThMEPElectrical.EarthingGrid.Generator.Connect
{
    class EarthGrid
    {
        public Dictionary<Tuple<Point3d, Point3d>, List<Tuple<Point3d, Point3d>>> findPolylineFromLines = new Dictionary<Tuple<Point3d, Point3d>, List<Tuple<Point3d, Point3d>>>();
        public List<Tuple<double, double>> faceSize =  new List<Tuple<double, double>>();

        private Dictionary<Tuple<Point3d, Point3d>, Point3d> lineToCenter = new Dictionary<Tuple<Point3d, Point3d>, Point3d>(); // 通过一条线找到这条线所在多边形对应的中点
        private Dictionary<Point3d, HashSet<Tuple<Point3d, Point3d>>> centerToFace = new Dictionary<Point3d, HashSet<Tuple<Point3d, Point3d>>>(); // 用一个点代表多边形
        private Dictionary<Point3d, HashSet<Point3d>> centerGrid = new Dictionary<Point3d, HashSet<Point3d>>(); // 多边形中点连接形成的图

        public EarthGrid(Dictionary<Tuple<Point3d, Point3d>, List<Tuple<Point3d, Point3d>>> _findPolylineFromLines, List<Tuple<double, double>> _faceSize)
        {
            findPolylineFromLines = _findPolylineFromLines;
            faceSize = _faceSize;
        }

        public Dictionary<Point3d, HashSet<Point3d>> Genterate(PreProcess preProcessData)
        {
            //1、生成连接结构
            CreateCenterLineRelation();

            //2、进行网格分割
            SplitGrid();
            CreateCenterGrid();

            //3、删除大轮廓外的线, 删除单体内的线
            RangeConfine rangeConfine = new RangeConfine(preProcessData, lineToCenter, centerToFace, centerGrid);
            rangeConfine.RemoveExteriorAndInteriorLines(ref lineToCenter, ref centerToFace, ref centerGrid);

            //4、进行网格合并
            var dbPoints = centerGrid.Keys.Select(p => new DBPoint(p)).ToCollection();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(dbPoints);
            MergeGrid(spatialIndex);
            //CreateCenterGrid();

            //5、返回结果
            //ShowInfo.ShowGraph(centerGrid, 2);
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

        /// <summary>
        /// 生成两种数据结构：
        /// a、通过多边形上面的一条线找到当前多边形对应的点
        /// b、生成一种数据结构，可以通过点找到其对应的多边形
        /// </summary>
        public void CreateCenterLineRelation()
        {
            foreach (var findPolylineFromLine in findPolylineFromLines)
            {
                if (findPolylineFromLines.ContainsKey(new Tuple<Point3d, Point3d>(findPolylineFromLine.Key.Item2, findPolylineFromLine.Key.Item1)))
                {

                    var lines = findPolylineFromLine.Value;
                    var pt = GetObjects.GetLinesCenter(lines);
                    if (centerToFace.ContainsKey(pt))
                    {
                        continue;
                    }
                    centerToFace.Add(pt, lines.ToHashSet());
                    foreach (var line in lines)
                    {
                        if (lineToCenter.ContainsKey(line))
                        {
                            break;
                        }
                        lineToCenter.Add(line, pt);
                    }
                }
            }
        }

        /// <summary>
        /// 生成一种数据结构，点和点相连，每个点代表一个多边形
        /// </summary>
        public void CreateCenterGrid()
        {
            centerGrid.Clear();
            foreach (var lineCenter in lineToCenter)
            {
                var curLine = lineCenter.Key;
                var converseLine = new Tuple<Point3d, Point3d>(curLine.Item2, curLine.Item1);
                if (lineToCenter.ContainsKey(converseLine))
                {
                    GraphDealer.AddLineToGraph(lineToCenter[converseLine], lineCenter.Value, ref centerGrid);
                }
            }
        }

        /// <summary>
        /// 进行网格分割
        /// </summary>
        private void SplitGrid()
        {
            foreach (var curCenterPt in centerToFace.Keys.ToList())
            {
                Queue<Point3d> splitedFaces = new Queue<Point3d>();
                splitedFaces.Enqueue(curCenterPt);
                SplitFace(ref splitedFaces);
            }
        }

        /// <summary>
        /// 将一个长方体切割为两个,同时改变结构
        /// </summary>
        /// <param name="splitedFaces"></param>
        private void SplitFace(ref Queue<Point3d> splitedFaces)
        {
            while (splitedFaces.Count > 0)
            {
                var centerPt = splitedFaces.Dequeue();
                
                var polyline = LineDealer.Tuples2Polyline(centerToFace[centerPt].ToList());
                if(polyline.Area < 10000)
                {
                    continue;
                }
                var rectangle = OBB(polyline);
                //if (CheckRectangleA(rectangle) < 0)
                if (CheckRectangle(rectangle) < 0)
                {
                    //1、找到平分线
                    Tuple<Point3d, Point3d> bisector = GetObjects.GetBisectorOfRectangle(rectangle);
                    bisector = LineDealer.ReduceTuple(bisector, -200);
                    //2、找到平分线相交的两条最长的线 和 平分线与其的两个交点
                    var intersecetLines = new List<Tuple<Point3d, Point3d>>();
                    foreach (var line in centerToFace[centerPt])
                    {
                        if (LineDealer.IsIntersect(line.Item1, line.Item2, bisector.Item1, bisector.Item2))
                        {
                            intersecetLines.Add(line);
                        }
                    }
                    if (intersecetLines.Count < 2)
                    {
                        return;
                    }
                    intersecetLines = intersecetLines.OrderByDescending(l => l.Item1.DistanceTo(l.Item2)).ToList();
                    Tuple<Point3d, Point3d> lineA = intersecetLines[0];
                    Tuple<Point3d, Point3d> lineB = intersecetLines[1];
                    Point3d incPtA = GetObjects.GetIntersectPoint(bisector.Item1, bisector.Item2, lineA.Item1, lineA.Item2);
                    Point3d incPtB = GetObjects.GetIntersectPoint(bisector.Item1, bisector.Item2, lineB.Item1, lineB.Item2);

                    //3、找到上面那两条线的中点、两个端点
                    List<Point3d> ptsA = new List<Point3d> { lineA.Item1, lineA.Item2, GetObjects.GetCenterPt(lineA.Item1, lineA.Item2) };
                    List<Point3d> ptsB = new List<Point3d> { lineB.Item1, lineB.Item2, GetObjects.GetCenterPt(lineB.Item1, lineB.Item2) };

                    //4、找到和相交点最近的两个点，形成分割线
                    Point3d splitCenterPtA = GetObjects.GetMinDisPt(incPtA, ptsA);
                    Point3d splitCenterPtB = GetObjects.GetMinDisPt(incPtB, ptsB);

                    //5、分割并修改结构
                    //5.1、获得当前分割后的两个面
                    var faceLinesA = new HashSet<Tuple<Point3d, Point3d>>();
                    var faceLinesB = new HashSet<Tuple<Point3d, Point3d>>();
                    GetSplitedFaceLines(centerPt, splitCenterPtA, splitCenterPtB, lineA, lineB,
                        ref faceLinesA, ref faceLinesB);

                    //5.2、重建结构
                    //获取两个面的中点
                    var centerPtA = GetObjects.GetLinesCenter(faceLinesA.ToList());
                    var centerPtB = GetObjects.GetLinesCenter(faceLinesB.ToList());
                    ReconstructureForSplit(centerPt, faceLinesA, faceLinesB, centerPtA, centerPtB);

                    //6、将代表分割后的两个多边形的点加入队列
                    splitedFaces.Enqueue(centerPtA);
                    splitedFaces.Enqueue(centerPtB);
                }
            }
        }

        /// <summary>
        /// 通过数据获得生成两多边形的各边 
        /// 顺便更新结构lineToCenter 和 centerToFace
        /// </summary>
        private void GetSplitedFaceLines(Point3d centerPt, Point3d splitCenterPtA, Point3d splitCenterPtB,
            Tuple<Point3d, Point3d> lineA, Tuple<Point3d, Point3d> lineB,
            ref HashSet<Tuple<Point3d, Point3d>> faceLinesA, ref HashSet<Tuple<Point3d, Point3d>> faceLinesB)
        {
            var dicLines = new Dictionary<Point3d, Point3d>();
            foreach (var faceLine in centerToFace[centerPt])
            {
                dicLines.Add(faceLine.Item1, faceLine.Item2);
            }
            dicLines.Remove(lineA.Item1);
            dicLines.Remove(lineB.Item1);
            
            var conterLineA = new Tuple<Point3d, Point3d>(lineA.Item2, lineA.Item1);
            var conterLineB = new Tuple<Point3d, Point3d>(lineB.Item2, lineB.Item1);
            Point3d conterCenterA = new Point3d();
            Point3d conterCenterB = new Point3d();
            if (lineToCenter.ContainsKey(conterLineA))
            {
                conterCenterA = lineToCenter[conterLineA];
                lineToCenter.Remove(conterLineA);
                if (centerToFace.ContainsKey(conterCenterA))
                {
                    centerToFace[conterCenterA].Remove(conterLineA);
                }
            }
            if (lineToCenter.ContainsKey(conterLineB))
            {
                conterCenterB = lineToCenter[conterLineB];
                lineToCenter.Remove(conterLineB);
                if (centerToFace.ContainsKey(conterCenterB))
                {
                    centerToFace[conterCenterB].Remove(conterLineB);
                }
            }

            if (lineA.Item1 != splitCenterPtA)
            {
                dicLines.Add(lineA.Item1, splitCenterPtA);

                var tmpConterLine = new Tuple<Point3d, Point3d>(splitCenterPtA, lineA.Item1);
                if (conterCenterA != new Point3d() && !lineToCenter.ContainsKey(tmpConterLine))
                {
                    lineToCenter.Add(tmpConterLine, conterCenterA);
                }
                if (centerToFace.ContainsKey(conterCenterA))
                {
                    centerToFace[conterCenterA].Add(tmpConterLine);
                }
            }
            if (!dicLines.ContainsKey(splitCenterPtA) && splitCenterPtA != lineA.Item2)
            {
                dicLines.Add(splitCenterPtA, lineA.Item2);

                var tmpConterLine = new Tuple<Point3d, Point3d>(lineA.Item2, splitCenterPtA);
                if (conterCenterA != new Point3d() && !lineToCenter.ContainsKey(tmpConterLine))
                {
                    lineToCenter.Add(tmpConterLine, conterCenterA);
                }
                if (centerToFace.ContainsKey(conterCenterA))
                {
                    centerToFace[conterCenterA].Add(tmpConterLine);
                }
            }
            if (lineB.Item1 != splitCenterPtB)
            {
                dicLines.Add(lineB.Item1, splitCenterPtB);

                var tmpConterLine = new Tuple<Point3d, Point3d>(splitCenterPtB, lineB.Item1);
                if (conterCenterB != new Point3d() && !lineToCenter.ContainsKey(tmpConterLine))
                {
                    lineToCenter.Add(tmpConterLine, conterCenterB);
                }
                if (centerToFace.ContainsKey(conterCenterB))
                {
                    centerToFace[conterCenterB].Add(tmpConterLine);
                }
            }
            if (!dicLines.ContainsKey(splitCenterPtB) && splitCenterPtB != lineB.Item2)
            {
                dicLines.Add(splitCenterPtB, lineB.Item2);

                var tmpConterLine = new Tuple<Point3d, Point3d>(lineB.Item2, splitCenterPtB);
                if (conterCenterB != new Point3d() && !lineToCenter.ContainsKey(tmpConterLine))
                {
                    lineToCenter.Add(tmpConterLine, conterCenterB);
                }
                if (centerToFace.ContainsKey(conterCenterB))
                {
                    centerToFace[conterCenterB].Add(tmpConterLine);
                }
            }

            faceLinesA.Add(new Tuple<Point3d, Point3d>(splitCenterPtA, splitCenterPtB));
            faceLinesB.Add(new Tuple<Point3d, Point3d>(splitCenterPtB, splitCenterPtA));

            var stPt = splitCenterPtB;
            while (dicLines.ContainsKey(stPt) && stPt != splitCenterPtA)
            {
                faceLinesA.Add(new Tuple<Point3d, Point3d>(stPt, dicLines[stPt]));
                stPt = dicLines[stPt];
            }
            stPt = splitCenterPtA;
            while (dicLines.ContainsKey(stPt) && stPt != splitCenterPtB)
            {
                faceLinesB.Add(new Tuple<Point3d, Point3d>(stPt, dicLines[stPt]));
                stPt = dicLines[stPt];
            }
        }

        /// <summary>
        /// 为分割的两个面重建结构
        /// </summary>
        private void ReconstructureForSplit(Point3d basePt, HashSet<Tuple<Point3d, Point3d>> faceLinesA,
            HashSet<Tuple<Point3d, Point3d>> faceLinesB, Point3d centerPtA, Point3d centerPtB)
        {
            //2、建立新连接
            //2.1、update lineToCenter
            foreach (var faceLineA in faceLinesA)
            {
                if (lineToCenter.ContainsKey(faceLineA))
                {
                    lineToCenter.Remove(faceLineA);
                }
                lineToCenter.Add(faceLineA, centerPtA);
            }
            foreach (var faceLineB in faceLinesB)
            {
                if (lineToCenter.ContainsKey(faceLineB))
                {
                    lineToCenter.Remove(faceLineB);
                }
                lineToCenter.Add(faceLineB, centerPtB);
            }
            //2.2、update centerToFace
            centerToFace.Remove(basePt);
            centerToFace.Add(centerPtA, faceLinesA);
            centerToFace.Add(centerPtB, faceLinesB); 
        }


        /// <summary>
        /// 查看一个矩形是否符合所给数据
        /// </summary>
        /// <param name="centerPt"></param>
        /// <returns>成功返回匹配的模式，失败返回-1</returns>
        private int CheckRectangleA(Polyline rectangle)
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
                if (length < faceSize[i].Item1 / 4 && height < faceSize[i].Item2 / 4)
                {
                    if (faceSize[i].Item1 / 4 / length < 1.5 && faceSize[i].Item2 / 4 / height < 1.5)
                    {
                        return 10;
                    }
                    return i;
                }
            }
            return -1;
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

        /// <summary>
        /// 进行网格合并
        /// </summary>
        /// <param name="spatialIndex"></param>
        private void MergeGrid(ThCADCoreNTSSpatialIndex spatialIndex)
        {
            HashSet<Point3d> unvisitedPts = centerGrid.Keys.ToHashSet();
            HashSet<Point3d> firmedPts = new HashSet<Point3d>(); //固定不会再扩大所在面的点
            while(unvisitedPts.Count > 0)
            {
                //找到起点
                var startPt = PointsDealer.GetLeftDownPt(unvisitedPts);
                //ShowInfo.ShowPointAsO(startPt, 1, 600);
                unvisitedPts.Remove(startPt);
                if (!centerToFace.ContainsKey(startPt))
                {
                    continue;
                }
                var rectangle = OBB(LineDealer.Tuples2Polyline(centerToFace[startPt].ToList())); //关键字不在字典中
                var firstDirection = rectangle.GetPoint3dAt(1) - rectangle.GetPoint3dAt(0);
                Point3d lockedPt = GetLockedPt(startPt, firstDirection, ref unvisitedPts, spatialIndex, ref firmedPts);
                firmedPts.Add(lockedPt);

                List<Point3d> horizonPts = new List<Point3d> { lockedPt };
                //1、向右找 右最下
                LoopAddFirmPt(startPt, firstDirection, ref horizonPts, ref firmedPts, ref unvisitedPts, spatialIndex, 1);//1
                //2、向左找 左最上
                LoopAddFirmPt(startPt, -firstDirection, ref horizonPts, ref firmedPts, ref unvisitedPts, spatialIndex, 1);//1

                List<Point3d> verticalPts = new List<Point3d>();
                foreach(var curHorizonPt in horizonPts)
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
                    if(curRotate < Math.PI / 2 && curRotate > maxRotate)
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
                    if(curRotate < Math.PI / 2 && curRotate < minRotate)
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
                if(HaveFirmedPt(lockedPt, curDirection, firmedPts))
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
                    if(lockedPt == containPoints[0])
                    {
                        ++loopCnt;
                        //continue;
                    }
                }
                else if(mergeTest == -1)
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
            foreach(var pt in centerGrid[basePt])
            {
                if((pt - basePt).GetAngleTo(baseDirection) < Math.PI / 2 && firmedPts.Contains(pt))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 查看一个面是否符合所给数据
        /// </summary>
        /// <param name="centerPt"></param>
        /// <returns>成功返回匹配的模式，失败返回-1</returns>
        private int CheckFace(Point3d centerPt)
        {
            if (!centerToFace.ContainsKey(centerPt))
            {
                return -1;
            }
            var polyline = LineDealer.Tuples2Polyline(centerToFace[centerPt].ToList());
            return CheckRectangle(OBB(polyline));
        }

        /// <summary>
        /// 测试合并两个多边形后是否与矩形相似
        /// </summary>
        /// <param name="ptA"></param>
        /// <param name="ptB"></param>
        private bool MergeTestLikeRectangular(Point3d ptA, Point3d ptB, double degree = 0.8)
        {
            if (centerToFace.ContainsKey(ptA) && centerToFace.ContainsKey(ptB))
            {
                //get faceLines
                var faceLines = new HashSet<Tuple<Point3d, Point3d>>();
                GetFaceLine(ptA, ref faceLines);
                GetFaceLine(ptB, ref faceLines);

                var polyline = LineDealer.Tuples2Polyline(faceLines.ToList());

                //此多边形和其最小包围矩形的面积比，和degree系数进行比较
                return IsRectangle(polyline, degree);
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
            if(faceLinesB.Count < 3)
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
                if(!centerGrid.ContainsKey(cneterPoint) || !centerToFace.ContainsKey(cneterPoint))
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
            foreach(var grid in centerGrid)
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
        private void GetFaceLine(Point3d centerPt, ref HashSet<Tuple<Point3d, Point3d>> faceLines)
        {
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
    }
}
