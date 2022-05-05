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
            MergeGrid mergeGrid = new MergeGrid(lineToCenter, centerToFace, centerGrid, faceSize);
            return mergeGrid.Merge();
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
                var polyline = LineDealer.LinesToConvexHull(centerToFace[centerPt]);
                if(polyline.Area < 10000)
                {
                    continue;
                }
                var rectangle = OBB(polyline);
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
                    var plA = LineDealer.LinesToConvexHull(faceLinesA);
                    var plB = LineDealer.LinesToConvexHull(faceLinesB);
                    if(plA.Area < 1000 || plB.Area < 1000)
                    {
                        continue;////////////////////////////////
                    }
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
            centerToFace.Remove(centerPtA);
            centerToFace.Remove(centerPtB);
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

        private Polyline OBB(Polyline polygon)
        {
            return polygon.GetMinimumRectangle();
        }
    }
}
