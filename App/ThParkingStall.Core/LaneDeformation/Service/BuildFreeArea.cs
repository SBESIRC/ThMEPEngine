using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using NetTopologySuite.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThParkingStall.Core.LaneDeformation;

using NetTopologySuite.Operation.OverlayNG;
using ThParkingStall.Core.MPartitionLayout;

namespace ThParkingStall.Core.LaneDeformation
{
    public class BuildFreeArea
    {
        
        //类公共变量
        public Vector2D MoveDir = new Vector2D();
        public List<Polygon> OriginalFreeAreaList = new List<Polygon>();

        public List<List<FreeAreaRec>> FreeAreaRecsList = new List<List<FreeAreaRec>>();


        //单个Area临时变量
        double maxY = 0;
        double minY = 0;
        List<FreeAreaRec> tmpRecs = new List<FreeAreaRec>();
        List<List<FreeAreaRec>> tmpRecsX = new List<List<FreeAreaRec>>();
        List<List<FreeAreaRec>> clearedTmpRecsX = new List<List<FreeAreaRec>>(); //只取上下两个矩形的tmpRecsX
        Polygon nowBoundary;

        public BuildFreeArea(List<Polygon> originalFreeAreaList,Vector2D dir) 
        {
            MoveDir = dir;
            OriginalFreeAreaList = originalFreeAreaList;
        }

        public void Pipeline() 
        {
            for (int i = 0; i < OriginalFreeAreaList.Count; i++) 
            {
                if (i == 7) 
                {
                    int stop = 0;
                }
                nowBoundary = OriginalFreeAreaList[i];
                FromSingleFreeArea(OriginalFreeAreaList[i]);
            }
        }

        public void FromSingleFreeArea(Polygon singleFreeArea) 
        {
            var obbPointList = singleFreeArea.Coordinates.ToList();


            List<double> yList = GetYList(obbPointList);
            maxY = yList.Last();
            minY = yList.First();

            List<double> xList = GetXList(obbPointList);

            //清除部分重合点
            xList = IgnoreSmall(xList);


            //两种切分方式
            //GetCutRecs0(singleFreeArea, xList);
            GetCutRecs1(singleFreeArea, xList);



            FreeAreaRecsList.Add(tmpRecs);
            tmpRecs = new List<FreeAreaRec>();
        }

        public List<double> GetXList(List<Coordinate> pointList) 
        {
            List<double> xList = new List<double>();
            for (int i = 0;i< pointList.Count; i++) 
            {
                xList.Add(pointList[i].X);
            }
            return xList.OrderBy(x => x).ToList();
        }

        public List<double> GetYList(List<Coordinate> pointList)
        {
            List<double> yList = new List<double>();
            for (int i = 0; i < pointList.Count; i++)
            {
                yList.Add(pointList[i].Y);
            }
            return yList.OrderBy(x => x).ToList();
        }

        public List<double> IgnoreSmall(List<double> numberList, double threshold = 50)
        {
            double nowX = numberList[0];
            List<int> deleteIndexList = new List<int>();
            for (int i = 0; i < numberList.Count - 1; i++)
            {
                if (numberList[i + 1] - nowX < threshold)
                {
                    deleteIndexList.Add(i + 1);
                }
                else nowX = numberList[i + 1];
            }

            List<double> newList = new List<double>();
            for (int i = 0; i < numberList.Count - 1; i++)
            {
                if (!deleteIndexList.Contains(i))
                {
                    newList.Add(numberList[i]);
                }
            }

            return newList;

        }


        //第一类分割法
        public void GetCutRecs0(Polygon singleFreeArea,List<double> xList) 
        {
            List<List<double>> XYListMap = GetXYMap(singleFreeArea, xList);
            //XYListMap = CombineXYMap(ref xList, XYListMap);

            //
            FromPointToRecs(xList, XYListMap);

            //修改数值
            //ModifiedRecs();

            //
        }

        public List<List<double>> GetXYMap(Polygon oPl, List<double> xList)
        {
            List<List<double>> xyMap = new List<List<double>>();
            for (int i = 0; i < xList.Count; i++)
            {
                double nowx = xList[i];
                LineSegment line = new LineSegment(xList[i],minY-100, xList[i], maxY+100);
                List<Coordinate> coordinates = line.IntersectPoint(oPl).ToList();
                if (coordinates.Count < 2)
                {
                    coordinates.Add(new Coordinate(nowx, minY));
                    coordinates.Add(new Coordinate(nowx, maxY));
                }

                List<double> nowyList = GetYList(coordinates);
                xyMap.Add(nowyList);
            }



            return xyMap;
        }

        public List<List<double>> CombineXYMap(ref List<double> xList, List<List<double>> xyMap,double threshold = 5) 
        {
            List<double> newxList = new List<double>();
            List<List<double>> newxyMap = new List<List<double>>();
            List<double> tmpyList = new List<double>();
            tmpyList = xyMap[0];
            double nowX = xList[0];
            for (int i = 0; i < xList.Count - 1; i++)
            {
                //继续前进
                if (xList[i + 1] - nowX < threshold)
                {
                    tmpyList.AddRange(newxyMap[i + 1]);

                    //到最后一个了
                    if (i + 1 == xList.Count - 1)
                    {
                        newxList.Add(nowX);
                        tmpyList = tmpyList.OrderBy(x => x).ToList();
                        newxyMap.Add(tmpyList);
                    }
                }
                else  //直接停止 
                {
                    newxList.Add(nowX);
                    tmpyList= tmpyList.OrderBy(x => x).ToList();
                    newxyMap.Add(tmpyList);

                    nowX = xList[i + 1];
                    tmpyList = xyMap[i + 1];

                    //到最后一个了
                    if (i + 1 == xList.Count - 1)
                    {
                        newxList.Add(nowX);
                        tmpyList = tmpyList.OrderBy(x => x).ToList();
                        newxyMap.Add(tmpyList);
                    }
                }
            }

            xList = newxList;
            return newxyMap;
        }

        public void FromPointToRecs(List<double> xList,List<List<double>> XYListMap) 
        {
            tmpRecs.Clear();

            for (int i = 0; i < XYListMap.Count - 1; i++) 
            {
                if (XYListMap[i].Count < 2 || XYListMap[i+1].Count < 2) continue;
                double ymin0 = XYListMap[i].First();
                double ymax0 = XYListMap[i].Last();

                double ymin1 = XYListMap[i+1].First();
                double ymax1 = XYListMap[i+1].Last();

                double ymax = Math.Min(ymax1, ymax0);
                double ymin = Math.Max(ymin1, ymin0);

                List<double> newyList = new List<double>();
                foreach (double singley in XYListMap[i]) 
                {
                    if (singley <= ymax && singley >= ymin) newyList.Add(singley);
                }
                foreach (double singley in XYListMap[i+1])
                {
                    if (singley <= ymax && singley >= ymin) newyList.Add(singley);
                }
                newyList = newyList.OrderBy(x => x).ToList();

                GetRecsFromColumn(xList[i], xList[i+1], newyList);

                //tmpRecs.Add(new FreeAreaRec(
                //    new Coordinate(xList[i], ymin), new Coordinate(xList[i + 1], ymin),
                //    new Coordinate(xList[i + 1], ymax), new Coordinate(xList[i], ymax)));
            }
        }

        public void GetRecsFromColumn(double x0,double x1,List<double> newyList) 
        {
            if (newyList.Count < 2) 
            {
                int stop = 0;
                return;
            }
            double starty = newyList[0];
            double endy = newyList[0];
            for (int i = 0; i < newyList.Count - 1; i++) 
            {
                double y0 = starty;
                double y1 = newyList[i+1];

                Polygon testPl = PolygonUtils.CreatePolygonRec(x0, x1, y0, y1);
                var smallPl = testPl.Buffer(-2);

                int thisOK = 0;
                if (smallPl is Polygon)
                {
                    if (nowBoundary.Contains(smallPl)) thisOK = 1;
                }

                if (thisOK == 1)  //继续找下一个
                {
                    endy = y1;

                    //处理最后一个特殊情况
                    if (i + 1 == newyList.Count - 1) 
                    {
                        if (endy - starty > 5) 
                        {
                            tmpRecs.Add(new FreeAreaRec(
                               new Coordinate(x0,starty), new Coordinate(x1, starty),
                               new Coordinate(x1,endy), new Coordinate(x0, endy)));
                        }
                    }
                }
                else  //结束上一轮，开启下一轮
                {
                    //结束上一轮
                    if (endy - starty > 5)
                    {
                        tmpRecs.Add(new FreeAreaRec(
                           new Coordinate(x0, starty), new Coordinate(x1, starty),
                           new Coordinate(x1, endy), new Coordinate(x0, endy)));
                    }

                    //开启下一轮
                    starty = y1;
                    endy = y1;
                }
            }
        }



        //第二类分割法
        public void GetCutRecs1(Polygon singleFreeArea, List<double> xList)
        {
       
            FromPointToRecs2(xList);

            //仅仅保留上下两个矩形
            GetUsefulRecs();

            //修改数值
            //ModifiedRecs();


            //展平
            List<FreeAreaRec> flatten = clearedTmpRecsX.SelectMany(x => x).ToList();
            tmpRecs = flatten;
        }

        public void FromPointToRecs2(List<double> xList)
        {
            tmpRecsX = new List<List<FreeAreaRec>>();
            for (int i = 0; i < xList.Count - 1; i++)
            {
                List<FreeAreaRec> tmpRecSingleX = new List<FreeAreaRec>();
                double x0 = xList[i];
                double x1 = xList[i + 1];
                Polygon tmpRec = PolygonUtils.CreatePolygonRec(x0, x1, minY - 100, maxY + 100);
                var result =
                OverlayNGRobust.Overlay(tmpRec, nowBoundary, NetTopologySuite.Operation.Overlay.SpatialFunction.Intersection);

                List<Polygon> pendingPolygons = new List<Polygon>();
                if (result is GeometryCollection collection)
                {
                    foreach (var e in collection)
                    {
                        if (e is Polygon)
                        {
                            List<Polygon> polygons = PolygonUtils.ClearBufferHelper((Polygon)e, -30, 30);
                            //pendingPolygons.Add((Polygon)e);
                            pendingPolygons.AddRange(polygons);
                        }
                    }
                }
                else if (result is Polygon)
                {
                    List<Polygon> polygons = PolygonUtils.ClearBufferHelper((Polygon)result, -30, 30);
                    //pendingPolygons.Add((Polygon)result);
                    pendingPolygons.AddRange(polygons);
                }

                LDOutput.DrawTmpOutPut0.TmpCutRecs.AddRange(pendingPolygons);


                for (int j = 0; j < pendingPolygons.Count ; j++) {
                    List<Coordinate> isRec = RecVerification(pendingPolygons[j]);
                    if (isRec.Count > 0) 
                    {
                        tmpRecSingleX.Add(new FreeAreaRec(isRec[0], isRec[1], isRec[2], isRec[3]));
                    }
                }
                if (tmpRecSingleX.Count > 0)
                {
                    tmpRecSingleX = tmpRecSingleX.OrderBy(x => x.LeftDownPoint.Y).ToList();
                }

                tmpRecsX.Add(tmpRecSingleX);
            }
        }

        public List<Coordinate> RecVerification(Polygon maybeRec) 
        {
            List<Coordinate> coordinates = new List<Coordinate>();
            var xlist = GetXList(maybeRec.Coordinates.ToList());
            var ylist = GetYList(maybeRec.Coordinates.ToList());
            double deltay = ylist.Last() - ylist.First();
            double deltax = xlist.Last() - xlist.First();
            double recArea = deltax * deltay;
            //if (maybeRec.Area > recArea * 0.1 && deltax > 50) 
            if(true)
            {
                coordinates.Add(new Coordinate(xlist.First(),ylist.First()));
                coordinates.Add(new Coordinate(xlist.Last(),ylist.First()));
                coordinates.Add(new Coordinate(xlist.Last(), ylist.Last()));
                coordinates.Add(new Coordinate(xlist.First(), ylist.Last()));
            }

            return coordinates;
        }


        public void GetUsefulRecs() 
        {
            clearedTmpRecsX = new List<List<FreeAreaRec>>();
            for (int i = 0; i < tmpRecsX.Count; i++) 
            {
                List<FreeAreaRec> tmpNewSingleRecs = new List<FreeAreaRec>();
                if (tmpRecsX[i].Count >= 2)
                {
                    tmpNewSingleRecs.Add(tmpRecsX[i].First());
                    tmpNewSingleRecs.Add(tmpRecsX[i].Last());

                }
                else if (tmpRecsX[i].Count == 1) 
                {
                    tmpNewSingleRecs.Add(tmpRecsX[i].First());
                }

                clearedTmpRecsX.Add(tmpNewSingleRecs);
            }
        }

        //public void ModifiedRecs(double threshold = 5) 
        //{
        //    for (int i = 0; i < tmpRecs.Count; i++) 
        //    {
        //        if (tmpRecs[i].Width < )

        //    }
        //}
    }
}
