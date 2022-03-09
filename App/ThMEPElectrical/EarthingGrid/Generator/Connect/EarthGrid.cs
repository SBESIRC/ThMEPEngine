using System;
using System.Linq;
using System.Collections.Generic;
using AcHelper;
using NFox.Cad;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Algorithm;
using ThMEPElectrical.EarthingGrid.Generator.Utils;

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

        public Dictionary<Point3d, HashSet<Point3d>> Genterate()
        {
            //1、生成连接结构
            CreateCenterLineRelation();
            CreateCenterGrid();

            //2.1、进行网格分割
            SplitGrid();
            //2.1、进行网格合并
            var dbPoints = centerGrid.Keys.Select(p => new DBPoint(p)).ToCollection();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(dbPoints);
            MergeGrid(spatialIndex);
            
            return new Dictionary<Point3d, HashSet<Point3d>>();
        }

        /// <summary>
        /// 进行网格分割
        /// </summary>
        private void SplitGrid()
        {
            foreach(var centerPt in centerGrid.Keys)
            {
                Queue<Point3d> splitedFaces = new Queue<Point3d>();
                splitedFaces.Enqueue(centerPt);
                while (splitedFaces.Count > 0)
                {
                    SplitFace(ref splitedFaces);
                }
            }
        }

        /// <summary>
        /// 将一个长方体切割为两个,同时改变结构
        /// </summary>
        /// <param name="splitedFaces"></param>
        private void SplitFace(ref Queue<Point3d> splitedFaces)
        {
            var centerPt = splitedFaces.Dequeue();
            var polyline = LineDealer.Tuples2Polyline(centerToFace[centerPt].ToList());
            var rectangle = OBB(polyline);
            if (CheckRectangle(rectangle) < 0)
            {
                //1、找到平分线
                Tuple<Point3d, Point3d> bisector = GetObjects.GetBisectorOfRectangle(rectangle);

                //2、找到平分线相交的两条最长的线 和 平分线与其的两个交点
                var intersecetLines = new List<Tuple<Point3d, Point3d>>();
                foreach (var line in centerToFace[centerPt])
                {
                    if(LineDealer.IsIntersect(line.Item1, line.Item2, bisector.Item1, bisector.Item2))
                    {
                        intersecetLines.Add(line);
                    }
                }
                if(intersecetLines.Count < 2)
                {
                    return;
                }
                intersecetLines = intersecetLines.OrderByDescending(l => l.Item1.DistanceTo(l.Item2)).ToList();
                Tuple<Point3d, Point3d> lineA = intersecetLines[0];
                Tuple<Point3d, Point3d> lineB = intersecetLines[1];
                Point3d incPtA = new Point3d();
                Point3d incPtB = new Point3d();/////////////////////////////////////

                //3、找到上面那两条线的中点、两个端点
                List<Point3d> ptsA = new List<Point3d> { lineA.Item1, lineA.Item2, GetObjects.GetCenterPt(lineA.Item1, lineA.Item2) };
                List<Point3d> ptsB = new List<Point3d> { lineB.Item1, lineB.Item2, GetObjects.GetCenterPt(lineB.Item1, lineB.Item2) };

                //4、找到和相交点最近的两个点，形成分割线
                Point3d splitCenterPtA = GetObjects.GetMinDisPt(incPtA, ptsA);
                Point3d splitCenterPtB = GetObjects.GetMinDisPt(incPtB, ptsB);

                //5、分割并修改结构


                //6、将代表分割后的两个多边形的点加入队列
                splitedFaces.Enqueue(splitCenterPtA);
                splitedFaces.Enqueue(splitCenterPtB);
            }
        }

        /// <summary>
        /// 进行网格合并
        /// </summary>
        /// <param name="spatialIndex"></param>
        private void MergeGrid(ThCADCoreNTSSpatialIndex spatialIndex)
        {
            //var innerColumnPoints = spatialIndex.SelectWindowPolygon(mPolygon).OfType<DBPoint>().Select(d => d.Position).Distinct().ToHashSet();

            //当进行合并之后，要做一次矩形比较，看是否样子像矩形、、如果不像、则看生成的矩形是否包含其他点，如果包含，则加入后再比较

            //附加要求，尽量少出现丁字、、、

            //正方形的大小应该大于4/9、 长方形&长条形大小应该大于2/3

            //要有一个计算多边形是否符合所给大小的计算，返回应该怎么处理的预言：分割、合并、

            Polyline polyline = new Polyline();
            //var minRecBox = polyline.OBB();
            var minRecBox = polyline.CalObb();
        }

        /// <summary>
        /// 生成两种数据结构：
        /// a、通过多边形上面的一条线找到当前多边形对应的点
        /// b、生成一种数据结构，可以通过点找到其对应的多边形
        /// </summary>
        public void CreateCenterLineRelation()
        {
            foreach (var lines in findPolylineFromLines.Values)
            {
                var pt = GetObjects.GetLinesCenter(lines);

                foreach (var line in lines)
                {
                    lineToCenter.Add(line, pt);
                }
                centerToFace.Add(pt, lines.ToHashSet());
            }
        }

        /// <summary>
        /// 生成一种数据结构，点和点相连，每个点代表一个多边形
        /// </summary>
        public void CreateCenterGrid()
        {
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
                    return i;
                }
            }
            return -1;
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
        private int MergeTestContionCenterPts(List<Point3d> centerPoints, ThCADCoreNTSSpatialIndex spatialIndex)
        {
            //0、chenck feasiblity
            foreach (var cneterPoint in centerPoints)
            {
                if (!centerGrid.ContainsKey(cneterPoint) || !centerToFace.ContainsKey(cneterPoint))
                {
                    return -1;
                }
            }

            //1、生成当前多边形并集所构成的最小矩形
            var faceLinesA = GetFaceLines(centerPoints);
            var rectangle = OBB(LineDealer.Tuples2Polyline(faceLinesA.ToList()));

            //2、查看此矩形所包含的中点
            var innerColumnPoints = spatialIndex.SelectWindowPolygon(rectangle).OfType<DBPoint>().Select(d => d.Position).Distinct().ToList();

            //3、查看举行包含的中点所在多边形的并集是否符合要求
            var faceLinesB = GetFaceLines(innerColumnPoints);
            return CheckRectangle(OBB(LineDealer.Tuples2Polyline(faceLinesB.ToList())));
        }

        /// <summary>
        /// 合并多个多边形，修改对应的结构
        /// </summary>
        /// <param name="centerPoints">多边形所被代表的点所组成的集合</param>
        private void MergeFaces(List<Point3d> centerPoints)
        {
            //0、chenck feasiblity
            foreach (var cneterPoint in centerPoints)
            {
                if(!centerGrid.ContainsKey(cneterPoint) || !centerToFace.ContainsKey(cneterPoint))
                {
                    return;
                }
            }

            //1、get faceLines
            var faceLines = GetFaceLines(centerPoints);

            //2、get centerPt
            var centerPt = GetObjects.GetLinesCenter(faceLines.ToList());

            //3、update lineToCenter
            foreach (var faceLine in faceLines)
            {
                lineToCenter.Add(faceLine, centerPt);
            }

            //4、update centerToFace
            foreach (var cneterPoint in centerPoints)
            {
                centerToFace.Remove(cneterPoint);
            }
            centerToFace.Add(centerPt, faceLines);

            //5、update centerGrid
            HashSet<Point3d> cntPts = new HashSet<Point3d>();
            foreach (var cneterPoint in centerPoints)
            {
                foreach (var cntPt in centerGrid[cneterPoint])
                {
                    if (cntPt != cneterPoint)
                    {
                        cntPts.Add(cntPt);
                    }
                }
            }
            foreach (var cntPt in cntPts)
            {
                GraphDealer.AddLineToGraph(cntPt, centerPt, ref centerGrid);
            }

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
