using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AcHelper;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Linq2Acad;
using NetTopologySuite.Geometries;
using NetTopologySuite.Triangulate;
using ThCADCore.NTS;

namespace ThMEPStructure.GirderConnect.ConnectMainBeam.Utils
{
    class StructureDealer
    {
        /// <summary>
        /// 将多边形分割成四边形或三角形列表
        ///  每次只切一刀，将一个多边形切成两个多边形，然后分别对切割后的多边形进行递归切割
        /// </summary>
        /// <param name="tuples">要分割的图形（多于4边）</param>
        /// <returns>分割后的图形</returns>
        public static void SplitPolyline(List<Tuple<Point3d, Point3d>> tuples, List<List<Tuple<Point3d, Point3d>>> tupleLines)
        {
            //Recursion boundary
            int n = tuples.Count;
            if (n == 0)
            {
                return;
            }
            if (n <= 5) // 只有当符合条件的时候才在数据库中加入这个多边形（不会再分割）
            {
                tupleLines.Add(tuples);
                return;
            }

            //Initialization
            Polyline polyline = LineDealer.Tuples2Polyline(tuples);
            tuples = LineDealer.OrderTuples(tuples);
            double area = polyline.Area;
            double halfArea = area / 2.0;
            double minCmp = double.MaxValue;
            double curCmp;
            int halfCnt = n / 2;
            List<Tuple<Point3d, Point3d>> tmpTuplesA = new List<Tuple<Point3d, Point3d>>();
            List<Tuple<Point3d, Point3d>> tmpTuplesB = new List<Tuple<Point3d, Point3d>>();
            List<Tuple<Point3d, Point3d>> tuplesA = new List<Tuple<Point3d, Point3d>>();
            List<Tuple<Point3d, Point3d>> tuplesB = new List<Tuple<Point3d, Point3d>>();
            double areaA;
            double areaB;
            int splitA;
            int splitB;
            int flag;
            //double mindis = double.MaxValue;
            //double curdis;

            Tuple<Point3d, Point3d> tmpTuple;
            //Catulate
            //find best split
            //如果边的数量是奇数：遍历n次，每次间隔(n - 1)/2边
            //如果边的数量是偶数：遍历n/2次，每次间隔n/2边
            int loopCnt = (n & 1) == 1 ? n : n / 2;
            for (int i = 0; i < loopCnt; ++i)
            {
                splitA = (i - 1 + n) % n;
                splitB = (i - 1 + halfCnt) % n;
                //splitA = i;
                //splitB = (i + halfCnt) % n;

                flag = 0;
                tmpTuple = new Tuple<Point3d, Point3d>(tuples[i].Item1, tuples[(i + halfCnt) % n].Item1);
                foreach (var curTuple in tuples)
                {
                    if (LineDealer.IsIntersect(tmpTuple.Item1, tmpTuple.Item2, curTuple.Item1, curTuple.Item2))
                    //if ((new Line(tmpTuple.Item1, tmpTuple.Item2).Intersect(new Line(curTuple.Item1, curTuple.Item2), 0)) != null)
                    {
                        flag = 1;
                        break;
                    }
                }
                if (flag == 1)
                {
                    continue;
                }

                Split2Order(tuples, splitA, splitB, tmpTuplesA, tmpTuplesB);
                areaA = LineDealer.Tuples2Polyline(tmpTuplesA).Area;
                areaB = LineDealer.Tuples2Polyline(tmpTuplesB).Area;
                //每一🔪尽可能从中间去切开，找到能把切开后面积的方差最小的，如有面积相似的，找连接线长最短的(连接线长*面积的方差和最小的(此处可调参))
                //curCmp = (Math.Pow(areaA - halfArea, 2.0) + Math.Pow(areaB - halfArea, 2.0)) * Math.Pow(tuples[i].Item1.DistanceTo(tuples[(i + halfCnt) % n].Item1), 4);
                curCmp = (Math.Pow(areaA - halfArea, 2.0) + Math.Pow(areaB - halfArea, 2.0)) * Math.Pow(tuples[splitA].Item1.DistanceTo(tuples[splitB].Item1), 4);

                //curdis = tuples[splitA].Item1.DistanceTo(tuples[splitB].Item1);
                if (curCmp < minCmp)
                //if(curdis < mindis)
                {
                    //mindis = curdis;
                    minCmp = curCmp;
                    tuplesA = tmpTuplesA;
                    tuplesB = tmpTuplesB;
                }
            }

            //Tail Recursion
            SplitPolyline(tuplesA, tupleLines);
            SplitPolyline(tuplesB, tupleLines);
        }

        /// <summary>
        /// Split a polylin from certain point to two polyline
        /// </summary>
        /// <param name="tuples"></param>
        /// <param name="splitA"></param>
        /// <param name="splitB"></param>
        /// <param name="tuplesA">in & out</param>
        /// <param name="tuplesB">in & out</param>
        public static void Split2Order(List<Tuple<Point3d, Point3d>> tuples, int splitA, int splitB, List<Tuple<Point3d, Point3d>> tuplesA, List<Tuple<Point3d, Point3d>> tuplesB)
        {
            tuplesA.Clear();
            tuplesB.Clear();
            int n = tuples.Count;
            if (splitA > n || splitA < 0 || splitB > n || splitB < 0)
            {
                return;
            }
            if (splitA > splitB)
            {
                int tmp;
                tmp = splitA;
                splitA = splitB;
                splitB = tmp;
            }
            for (int i = 0; i < tuples.Count; ++i)
            {
                if (i >= splitA && i < splitB)
                {
                    tuplesA.Add(tuples[i]);
                }
                else
                {
                    tuplesB.Add(tuples[i]);
                }
            }
            tuplesA.Add(new Tuple<Point3d, Point3d>(tuples[splitB].Item1, tuples[splitA].Item1));
            tuplesB.Add(new Tuple<Point3d, Point3d>(tuples[splitA].Item1, tuples[splitB].Item1));
        }

        /// <summary>
        /// Merge Two Polyline to One
        /// </summary>
        /// <param name="polylineA"></param>
        /// <param name="polylineB"></param>
        /// <returns></returns>
        public static List<Tuple<Point3d, Point3d>> MergePolyline(List<Tuple<Point3d, Point3d>> polylineA, List<Tuple<Point3d, Point3d>> polylineB, double tolerance = 1)
        {
            HashSet<Tuple<Point3d, Point3d>> lineVisited = new HashSet<Tuple<Point3d, Point3d>>();
            foreach (var line in polylineA)
            {
                lineVisited.Add(line);
            }
            foreach (var line in polylineB)
            {
                var converseLine = new Tuple<Point3d, Point3d>(line.Item2, line.Item1);
                if (lineVisited.Contains(converseLine))
                {
                    lineVisited.Remove(converseLine);
                    continue;
                }
                lineVisited.Add(line);
            }
            return LineDealer.OrderTuples(lineVisited.ToList());
        }

        /// <summary>
        /// For(2*n+1) + (3)edges case，Convert to even edges polyline,  then split to some small polyline
        /// </summary>
        /// <param name="polylineA"></param>
        /// <param name="polylineB"></param>
        /// <returns></returns>
        public static void CaseOddP3(List<Tuple<Point3d, Point3d>> polylineA, List<Tuple<Point3d, Point3d>> polylineB)
        {
            List<Tuple<Point3d, Point3d>> evenLines = MergePolyline(polylineA, polylineB);
            List<List<Tuple<Point3d, Point3d>>> polylines = new List<List<Tuple<Point3d, Point3d>>>();
            SplitPolyline(evenLines, polylines);
            foreach (var lines in polylines)
            {
                foreach (var line in lines)
                {
                    ShowInfo.DrawLine(line.Item1, line.Item2, 130);
                }
            }
        }

        /// <summary>
        /// Let near point connect only one line to inner point(double edge)
        /// </summary>
        /// <param name="points"></param>
        /// <param name=""></param>
        public static void DeleteLineConnectToSingle(Dictionary<Polyline, Point3dCollection> outlineNearPts, Point3dCollection clumnPts, Dictionary<Point3d, HashSet<Point3d>> dicTuples)
        {
            Dictionary<Point3d, HashSet<Point3d>> tmpDicTuples = new Dictionary<Point3d, HashSet<Point3d>>();
            foreach(var dic in dicTuples)
            {
                tmpDicTuples.Add(dic.Key, new HashSet<Point3d>());
                foreach(var pt in dic.Value)
                {
                    tmpDicTuples[dic.Key].Add(pt);
                }
            }
            Polyline outline;
            //Point3d preCntPt;
            Point3d stayPt;
            Point3dCollection innerPts;
            double minDis;
            double curDis;
            foreach (var dicOutl2Pts in outlineNearPts) //for each outline
            {
                outline = dicOutl2Pts.Key;
                innerPts = PointsDealer.RemoveSimmilerPoint(clumnPts, dicOutl2Pts.Value);

                foreach (Point3d nearPt in dicOutl2Pts.Value) //for each near point on a outline
                {
                    ShowInfo.ShowPointAsX(nearPt, 210, 520);
                    if (tmpDicTuples.ContainsKey(nearPt))
                    {
                        minDis = double.MaxValue;
                        stayPt = nearPt;
                        //find all the lines connect with this point, and only leave the line which is the shortest line in lines which do not connect with nearPt
                        foreach (Point3d curCntPt in tmpDicTuples[nearPt]) //no more than 4
                        {
                            if (innerPts.Contains(curCntPt))
                            {
                                ShowInfo.ShowPointAsO(curCntPt, 1, 300);
                                curDis = curCntPt.DistanceTo(nearPt);
                                if (curDis < minDis)//; && preCntPt != nearPt)
                                {
                                    minDis = curDis;
                                    if (dicTuples.ContainsKey(nearPt) && dicTuples[nearPt].Contains(curCntPt))
                                    {
                                        stayPt = curCntPt;
                                    }
                                }
                                if (dicTuples.ContainsKey(nearPt) && dicTuples[nearPt].Contains(curCntPt))
                                {
                                    dicTuples[nearPt].Remove(curCntPt);
                                }
                                if (dicTuples.ContainsKey(curCntPt) && dicTuples[curCntPt].Contains(nearPt))
                                {
                                    dicTuples[curCntPt].Remove(nearPt);
                                }
                            }
                        }
                        if(stayPt != nearPt)
                        {
                            if (!dicTuples.ContainsKey(stayPt))
                            {
                                dicTuples.Add(stayPt, new HashSet<Point3d>());
                            }
                            if (!dicTuples[stayPt].Contains(nearPt))
                            {
                                dicTuples[stayPt].Add(nearPt);
                                ShowInfo.DrawLine(stayPt, nearPt);
                            }
                            if (!dicTuples.ContainsKey(nearPt))
                            {
                                dicTuples.Add(nearPt, new HashSet<Point3d>());
                            }
                            if (!dicTuples[nearPt].Contains(stayPt))
                            {
                                dicTuples[nearPt].Add(stayPt);
                                ShowInfo.DrawLine(nearPt, stayPt);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Find Best Connect Point
        /// </summary>
        /// <param name="basePt"></param>
        /// <param name="verticalPt"></param>
        /// <param name="fstPts"></param>
        /// <param name="thdPts"></param>
        /// <param name="walls"></param>
        /// <param name="outline"></param>
        /// <param name="toleranceDegree"></param>
        /// <returns></returns>
        public static Point3d BestConnectPt(Point3d basePt, Point3d verticalPt, List<Point3d> fstPts, List<Point3d> thdPts, List<Polyline> walls, Line closetLine, double toleranceDegree = Math.PI / 4)
        {
            double baseRadius = basePt.DistanceTo(verticalPt) / Math.Cos(toleranceDegree); // * sec(x)
            baseRadius = baseRadius > 9000 ? 9000 : baseRadius;
            //double findRadius = basePt.DistanceTo(verticalPt) * Math.Tan(toleranceDegree);
            double curDis;
            Point3d tmpPt = verticalPt;
            double minDis = baseRadius;

            //1、Find the nearest Cross Point
            foreach (var fstPt in fstPts)
            {
                if(fstPt.DistanceTo(basePt) > baseRadius || fstPt.DistanceTo(closetLine.GetClosestPointTo(fstPt, false)) > 400)
                {
                    continue;
                }
                curDis = fstPt.DistanceTo(verticalPt);
                if (curDis < minDis)
                {
                    minDis = curDis;
                    tmpPt = fstPt;
                }
            }
            if(tmpPt != verticalPt)
            {
                //ShowInfo.ShowPointAsO(tmpPt);
                return tmpPt;
            }

            //2、If there is a near wall, get vertical point on wall
            Circle circle = new Circle(verticalPt, new Vector3d(), 300);
            foreach (var wall in walls)
            {
                if (wall.Intersects(circle) || wall.Contains(circle))
                {
                    //ShowInfo.ShowPointAsU(verticalPt, 210, 100);
                    return verticalPt;
                }
            }

            //3、Find apex point in range(45degree)
            minDis = baseRadius;
            foreach (var thdPt in thdPts)
            {
                if (thdPt.DistanceTo(basePt) > baseRadius || thdPt.DistanceTo(closetLine.GetClosestPointTo(thdPt, false)) > 400)
                {
                    continue;
                }
                curDis = thdPt.DistanceTo(verticalPt);
                if (curDis < minDis)
                {
                    minDis = curDis;
                    tmpPt = thdPt;
                }
            }
            if (tmpPt != verticalPt)
            {
                //ShowInfo.ShowPointAsO(tmpPt, 240, 100);
                return tmpPt;
            }

            //4、Return the vertical point on outline
            ShowInfo.ShowPointAsU(verticalPt, 7, 200); //common or do not delete
            return verticalPt;
        }

        /// <summary>
        /// reduce degree up to 4 for each point(删除最小夹角中长度最短的那个)
        /// </summary>
        /// <param name="dicTuples"></param>
        /// <param name="outline2BorderNearPts"></param>
        public static void DeleteConnectUpToFour(Dictionary<Point3d, HashSet<Point3d>> dicTuples, Dictionary<Polyline, Dictionary<Point3d, HashSet<Point3d>>> outline2BorderNearPts)
        {
            foreach(var dic in dicTuples)
            {
                int n = dic.Value.Count;
                while (n > 4)
                {
                    List<Point3d> cntPts = dic.Value.ToList();
                    Vector3d baseVec = cntPts[0] - dic.Key;
                    cntPts = cntPts.OrderBy(pt => (pt - dic.Key).GetAngleTo(baseVec, Vector3d.ZAxis)).ToList();
                    Tuple<Point3d, Point3d> minDegreePairPt = new Tuple<Point3d, Point3d>(cntPts[0], cntPts[1]);
                    double minDegree = double.MaxValue;
                    double curDegree;
                    for(int i = 1; i <= n; ++i)
                    {
                        curDegree = (cntPts[i % n] - dic.Key).GetAngleTo(cntPts[i - 1] - dic.Key);
                        if(curDegree < minDegree)
                        {
                            minDegree = curDegree;
                            minDegreePairPt = new Tuple<Point3d, Point3d>(cntPts[i % n], cntPts[i - 1]);
                        }
                    }
                    Point3d rmPt = new Point3d();
                    if(minDegreePairPt.Item1.DistanceTo(dic.Key) <= minDegreePairPt.Item2.DistanceTo(dic.Key))
                    {
                        rmPt = minDegreePairPt.Item1;
                        --n;
                    }
                    else
                    {
                        rmPt = minDegreePairPt.Item2;
                        --n;
                    }

                    dic.Value.Remove(rmPt);
                    foreach (var borderPt2NearPts in outline2BorderNearPts.Values)
                    {
                        if (borderPt2NearPts.ContainsKey(dic.Key) && borderPt2NearPts[dic.Key].Contains(rmPt))
                        {
                            borderPt2NearPts[dic.Key].Remove(rmPt);
                            if (borderPt2NearPts[dic.Key].Count == 0)
                            {
                                borderPt2NearPts.Remove(dic.Key);
                            }
                        }
                    }
                    if (dicTuples.ContainsKey(rmPt))
                    {
                        if (dicTuples[rmPt].Contains(dic.Key))
                        {
                            dicTuples[rmPt].Remove(dic.Key);
                        }
                        //if(dicTuples[rmPt].Count == 0)
                        //{
                        //    dicTuples.Remove(rmPt);
                        //}
                    }
                    foreach (var borderPt2NearPts in outline2BorderNearPts.Values)
                    {
                        if (borderPt2NearPts.ContainsKey(rmPt) && borderPt2NearPts[rmPt].Contains(dic.Key))
                        {
                            borderPt2NearPts[rmPt].Remove(dic.Key);
                            if (borderPt2NearPts[rmPt].Count == 0)
                            {
                                borderPt2NearPts.Remove(dic.Key);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 将点的连接增加至4个
        /// </summary>
        /// <param name="dicTuples"></param>
        public static void AddConnectUpToFour(Dictionary<Point3d, HashSet<Point3d>> dicTuples)
        {
            foreach(var dic in dicTuples)
            {
                int cnt = dic.Value.Count;
                if(cnt == 2 || cnt == 3)
                {
                    //找到其中角度最接近90度或者180度的
                    //向另一个方向找点
                    
                }
            }
        }

        /// <summary>
        /// Close a polyline by its border points
        /// 注意：要考虑最外边框和包含型边框的区别
        /// </summary>
        public static Dictionary<Point3d, Point3d> CloseBorder(Dictionary<Polyline, Dictionary<Point3d, HashSet<Point3d>>> outline2BorderNearPts)
        {
            Dictionary<Point3d, Point3d> ansDic = new Dictionary<Point3d, Point3d>();
            foreach (var dic in outline2BorderNearPts)
            {
                Polyline polyline = dic.Key;
                List<Point3d> points = new List<Point3d>();
                int n = polyline.NumberOfVertices;
                for(int i = 0; i < n; ++i)
                {
                    Line tmpLine = new Line(polyline.GetPoint3dAt(i), polyline.GetPoint3dAt((i + 1) % n));
                    List<Point3d> tmpPts = new List<Point3d>();
                    foreach(var borderPt in dic.Value.Keys)
                    {
                        //ShowInfo.ShowPointAsU(borderPt, 1);
                        if(borderPt.DistanceTo(tmpLine.GetClosestPointTo(borderPt, false)) < 500 && !points.Contains(borderPt))
                        {
                            tmpPts.Add(borderPt);
                            //ShowInfo.DrawLine(tmpLine.StartPoint, tmpLine.EndPoint, 30);
                        }
                    }
                    tmpPts = tmpPts.OrderBy(p => p.DistanceTo(tmpLine.StartPoint)).ToList();
                    points.AddRange(tmpPts);
                }
                for(int i = 1; i <= points.Count; i++)
                {
                    if (points[i % points.Count].DistanceTo(points[i - 1]) < 9000 * 2)
                    {
                        if (!ansDic.ContainsKey(points[i % points.Count]))
                        {
                            ansDic.Add(points[i % points.Count], points[i - 1]);
                            //ansDic.Add(points[i - 1], points[i % points.Count]);
                        }
                    }
                }
            }
            return ansDic;
        }
    }
}
