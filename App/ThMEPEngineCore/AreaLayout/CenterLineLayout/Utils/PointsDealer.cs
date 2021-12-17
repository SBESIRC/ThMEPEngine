using System.Linq;
using System.Collections.Generic;
using System.Collections;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using DotNetARX;
using ThCADExtension;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm;

namespace ThMEPEngineCore.AreaLayout.CenterLineLayout.Utils
{
    public static class PointsDealer
    {
        /// <summary>
        /// 获取区域列表中所有可以布置的点
        /// </summary>
        /// <param name="areas">可布置区域列表</param>
        /// <param name="radius">设备覆盖半径</param>
        /// <returns>返回可布置点集</returns>
        public static List<Point3d> PointsInAreas(List<MPolygon> areas, double radius)
        {
            List<Point3d> pointsInAreas = new List<Point3d>();
            foreach (MPolygon mPoly in areas)
            {
                var poly = mPoly.Shell();
                //List<Point3d> pts = PointsOnPolyline(poly.CalObb());
                //double disX = pts[0].DistanceTo(pts[1]);
                //double disY = pts[1].DistanceTo(pts[2]);

                List<Point3d> areaPoints = new List<Point3d>();
                if (poly.Area > radius * radius * 0.2)
                //if(disX > radius || disY > radius)
                {
                    areaPoints = PointsInUncoverArea(poly, 400);//700------------------------------调参侠 此参数可以写一个计算函数，通过面积大小求根号 和半径比较算出 要有上下界(700是相对接近最好的值)
                }
                else
                {
                    //areaPoints = PointsInArea(poly, radius);
                    areaPoints = PointsInUncoverArea(poly, 100);
                }
                foreach (var pt in areaPoints)
                {
                    pointsInAreas.Add(pt);
                }
            }
            return pointsInAreas;
        }

        /// <summary>
        /// 获取多边形上的所有点
        /// </summary>
        /// <param name="area">多边形</param>
        /// <returns>返回多边形上的点</returns>
        public static List<Point3d> PointsOnPolyline(Polyline area)
        {
            List<Point3d> ans = new List<Point3d>();
            //area.VerticesEx(100.0);
            int n = area.NumberOfVertices;
            for (int i = 0; i < n; ++i)
            {
                ans.Add(area.GetPoint3dAt(i));
            }
            //ans = ans.Distinct().ToList();
            return ans;
        }

        /// <summary>
        /// 获取范围内可能放置设备的点
        /// </summary>
        /// <param name="area">多边形</param>
        /// <returns>返回点集（中心点、4个三等分点（左右和上下），长宽中最大值的2个四等分点（上下或左右））</returns>
        public static List<Point3d> PointsInArea(Polyline area, double radius)
        {
            List<Point3d> pts = PointsOnPolyline(area.CalObb());
            Point3d pt0 = pts[0];
            Point3d pt0_5 = CenterOfTwoPoints(pts[0], pts[1]);
            Point3d pt1 = pts[1];
            Point3d pt1_5 = CenterOfTwoPoints(pts[1], pts[2]);
            Point3d pt2 = pts[2];
            Point3d pt2_5 = CenterOfTwoPoints(pts[2], pts[3]);
            Point3d pt3 = pts[3];
            Point3d pt3_5 = CenterOfTwoPoints(pts[3], pts[0]);
            double disX = pt0.DistanceTo(pt1);
            double disY = pt1.DistanceTo(pt2);

            List<Point3d> ans = new List<Point3d>();
            //加入中心点
            ans.Add(new Point3d((pt0.X + pt1.X + pt2.X + pt3.X) / 4, (pt0.Y + pt1.Y + pt2.Y + pt3.Y) / 4, 0));
            //加入三等分点
            if (disX < disY || disY > radius * 0.5)
            //if (disY > 1000)
            {
                ans.Add(new Point3d((pt0_5.X + 2 * pt2_5.X) / 3, (pt0_5.Y + 2 * pt2_5.Y) / 3, 0));
                ans.Add(new Point3d((2 * pt0_5.X + pt2_5.X) / 3, (2 * pt0_5.Y + pt2_5.Y) / 3, 0));
            }
            if (disX > disY || disX > radius * 0.5)
            //if (disX > 1000)
            {
                ans.Add(new Point3d((pt1_5.X + 2 * pt3_5.X) / 3, (pt1_5.Y + 2 * pt3_5.Y) / 3, 0));
                ans.Add(new Point3d((2 * pt1_5.X + pt3_5.X) / 3, (2 * pt1_5.Y + pt3_5.Y) / 3, 0));
            }
            //加入四等分点
            if (disX > radius)
            //if (disX > 2000)
            {
                ans.Add(new Point3d((pt1_5.X + 3 * pt3_5.X) / 4, (pt1_5.Y + 3 * pt3_5.Y) / 4, 0));
                ans.Add(new Point3d((3 * pt1_5.X + pt3_5.X) / 4, (3 * pt1_5.Y + pt3_5.Y) / 4, 0));
                if (disX > radius * 1.5)//加入八等分点
                //if (disX > 3000)
                {
                    ans.Add(new Point3d((pt1_5.X + 7 * pt3_5.X) / 8, (pt1_5.Y + 7 * pt3_5.Y) / 8, 0));
                    ans.Add(new Point3d((7 * pt1_5.X + pt3_5.X) / 8, (7 * pt1_5.Y + pt3_5.Y) / 8, 0));
                }
            }
            if (disY > radius)
            //if (disY > 2000)
            {
                ans.Add(new Point3d((pt0_5.X + 3 * pt2_5.X) / 4, (pt0_5.Y + 3 * pt2_5.Y) / 4, 0));
                ans.Add(new Point3d((3 * pt0_5.X + pt2_5.X) / 4, (3 * pt0_5.Y + pt2_5.Y) / 4, 0));
                if (disY > radius * 1.5)//加入八等分点
                //if (disY > 3000)
                {
                    ans.Add(new Point3d((pt0_5.X + 7 * pt2_5.X) / 8, (pt0_5.Y + 7 * pt2_5.Y) / 8, 0));
                    ans.Add(new Point3d((7 * pt0_5.X + pt2_5.X) / 8, (7 * pt0_5.Y + pt2_5.Y) / 8, 0));
                }
            }
            List<Point3d> ptss = new List<Point3d>();
            foreach (var pt in ans)
            {
                if (area.ContainsOrOnBoundary(pt))
                {
                    ptss.Add(pt);
                }
            }
            return ptss;
        }

        /// <summary>
        /// 以radious为分割巨大区域为矩阵点
        /// </summary>
        /// <param name="uncoverArea"></param>
        /// <param name="dis">两点相聚</param>
        /// <returns></returns>
        public static List<Point3d> PointsInUncoverArea(Entity uncoverArea, double dis)
        {
            List<Point3d> ptsInUncoverRectangle = new List<Point3d>();
            List<Point3d> pts = PointsOnPolyline(((Polyline)uncoverArea).CalObb());
            Point3d pt0 = pts[0];
            Point3d pt01 = CenterOfTwoPoints(pts[0], pts[1]);
            Point3d pt1 = pts[1];
            Point3d pt2 = pts[2];
            Point3d pt23 = CenterOfTwoPoints(pts[2], pts[3]);
            Point3d pt3 = pts[3];
            ptsInUncoverRectangle.Add(CenterOfTwoPoints(pt0, pt2));
            double disX = pt0.DistanceTo(pt1);
            double disY = pt1.DistanceTo(pt2);
            int Xcnt = (int)(disX / dis);
            Xcnt -= Xcnt % 2;
            int Ycnt = (int)(disY / dis);
            Ycnt -= Ycnt % 2;
            //int mode = 0; // 1: Y比较大，削减Y的数量 2: X比较长
            //Ycnt = Ycnt > 1 ? (Xcnt > 3 ? Ycnt - 2 : Ycnt) : Ycnt;
            //Xcnt = Xcnt > 1 ? (Ycnt > 3 ? Xcnt - 2 : Xcnt) : Xcnt;

            for (int i = 0; i < Xcnt; i += 2)
            //for (int i = Xcnt - 1; i >= 0; i -= 2)
            {
                Point3d ptA = new Point3d(((Xcnt - i) * pt01.X + i * pt0.X) / Xcnt, ((Xcnt - i) * pt01.Y + i * pt0.Y) / Xcnt, 0);//Apt01_0
                Point3d ptB = new Point3d(((Xcnt - i) * pt01.X + i * pt1.X) / Xcnt, ((Xcnt - i) * pt01.Y + i * pt1.Y) / Xcnt, 0);//Bpt01_1
                Point3d ptC = new Point3d(((Xcnt - i) * pt23.X + i * pt2.X) / Xcnt, ((Xcnt - i) * pt23.Y + i * pt2.Y) / Xcnt, 0);//Cpt23_2
                Point3d ptD = new Point3d(((Xcnt - i) * pt23.X + i * pt3.X) / Xcnt, ((Xcnt - i) * pt23.Y + i * pt3.Y) / Xcnt, 0);//Dpt23_3

                Point3d ptAD = CenterOfTwoPoints(ptA, ptD);//AD中点
                Point3d ptBC = CenterOfTwoPoints(ptB, ptC);//BC中点
                bool flag = true;
                //for (int j = 0; j > 0 && j < (Xcnt > 3 ? Ycnt - 2 : Ycnt); j += 2)
                for (int j = 0; j < Ycnt; j += 2)
                //for (int j = Ycnt - 1; j >= 0; j -= 2)
                {
                    Point3d ptAD_A = new Point3d(((Ycnt - j) * ptAD.X + j * ptA.X) / Ycnt, ((Ycnt - j) * ptAD.Y + j * ptA.Y) / Ycnt, 0);
                    Point3d ptAD_D = new Point3d(((Ycnt - j) * ptAD.X + j * ptD.X) / Ycnt, ((Ycnt - j) * ptAD.Y + j * ptD.Y) / Ycnt, 0);
                    Point3d ptBC_B = new Point3d(((Ycnt - j) * ptBC.X + j * ptB.X) / Ycnt, ((Ycnt - j) * ptBC.Y + j * ptB.Y) / Ycnt, 0);
                    Point3d ptBC_C = new Point3d(((Ycnt - j) * ptBC.X + j * ptC.X) / Ycnt, ((Ycnt - j) * ptBC.Y + j * ptC.Y) / Ycnt, 0);
                    if (flag)
                    {
                        flag = false;
                        ptsInUncoverRectangle.Add(CenterOfTwoPoints(ptAD_A, ptBC_B));
                        ptsInUncoverRectangle.Add(CenterOfTwoPoints(ptAD_D, ptBC_C));
                    }
                    ptsInUncoverRectangle.Add(ptAD_A);//ptAD_A
                    ptsInUncoverRectangle.Add(ptAD_D);//ptAD_D
                    ptsInUncoverRectangle.Add(ptBC_B);//ptBC_B
                    ptsInUncoverRectangle.Add(ptBC_C);//ptBC_C
                }
                for (int j = 0; j < Ycnt; ++j)
                {
                    ptsInUncoverRectangle.Add(new Point3d(((Ycnt - j) * pt01.X + j * pt23.X) / Ycnt, ((Ycnt - j) * pt01.Y + j * pt23.Y) / Ycnt, 0));
                }
                ptsInUncoverRectangle.Add(ptAD);//
                ptsInUncoverRectangle.Add(ptBC);//
            }
            //只将在覆盖区域中的点加入ans
            List<Point3d> ptss = new List<Point3d>();
            foreach (var pt in ptsInUncoverRectangle)
            {
                if (((Polyline)uncoverArea).Contains(pt))
                {
                    //ShowInfo.ShowPointAsX(pt, 1, 25);
                    ptss.Add(pt);
                }
            }
            ptss = ptss.Distinct().ToList();
            return ptss;
        }

        /// <summary>
        /// 获取点集points中距离中心点半径为radius范围内最近的n个点(有序)
        /// </summary>
        /// <param name="center">中心点</param>
        /// <param name="points">可选的点</param>
        /// <param name="n">要获取点的数量</param>
        /// <param name="radius">探查半径（一般为设备覆盖半径的两倍）</param>
        /// <returns>返回点集</returns>
        public static List<Point3d> GetNearestNPoints(Point3d center, Point3dList points, int n, double radius)
        {
            List<Point3d> ans = new List<Point3d>();
            SortedList sl = new SortedList();
            foreach (Point3d pt in points)
            {
                double dis = center.DistanceTo(pt);
                if (dis < radius)
                {
                    sl.Add(dis, pt);
                }
            }
            int cnt = 0;
            foreach (var pt in sl.Values)
            {
                if (cnt == n) break;
                ans.Add((Point3d)pt);
                ++cnt;
            }
            return ans;
        }

        /// <summary>
        /// 获得距中心点规定范围内的点
        /// </summary>
        /// <param name="center"></param>
        /// <param name="points"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public static List<Point3d> GetNearestPoints(Point3d center, List<Point3d> points, double radius)
        {
            List<Point3d> ans = new List<Point3d>();
            SortedList sl = new SortedList();
            foreach (Point3d pt in points)
            {
                double dis = center.DistanceTo(pt);
                if (dis < radius)
                {
                    sl.Add(dis, pt);
                }
            }
            foreach (var pt in sl.Values)
            {
                ans.Add((Point3d)pt);
            }
            return ans;
        }

        /// <summary>
        /// 再点集中寻找距中心位置最近的那个点
        /// </summary>
        /// <param name="center"></param>
        /// <param name="points"></param>
        /// <returns></returns>
        public static Point3d GetNearestPoint(Point3d center, List<Point3d> points, Polyline Area)
        {
            Point3d ans = new Point3d();
            var newPtList = new List<Point3d>();
            if (Area != null && Area.Area > 0)
            {
                newPtList = points.Where(x => Area.ContainsOrOnBoundary(x)).ToList();
            }
            else
            {
                newPtList = points;
            }

            ans = GetNearestPoint(center, newPtList);

            return ans;
        }

        private static Point3d GetNearestPoint(Point3d center, List<Point3d> points)
        {
            double minDis = double.MaxValue;
            double tmpDis;
            Point3d ans = new Point3d();

            foreach (Point3d pt in points)
            {
                tmpDis = center.DistanceTo(pt);
                if (tmpDis < minDis)
                {
                    minDis = tmpDis;
                    ans = pt;
                }
            }
            return ans;
        }



        /// <summary>
        /// 获取两个点的中心点
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Point3d CenterOfTwoPoints(Point3d a, Point3d b)
        {
            return new Point3d((a.X + b.X) / 2, (a.Y + b.Y) / 2, (a.Z + b.Z) / 2);
        }
    }
}

