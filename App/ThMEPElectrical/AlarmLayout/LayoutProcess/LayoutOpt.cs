using AcHelper.Commands;
using System;
using System.Linq;
using AcHelper;
using Linq2Acad;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using DotNetARX;
using Autodesk.AutoCAD.ApplicationServices;
using ThMEPElectrical.Broadcast.Service;
using System.Collections.Generic;
using System.Collections;
using Dreambuild.AutoCAD;
using ThMEPElectrical.AlarmLayout.Utils;
using ThMEPElectrical.AlarmSensorLayout.Data;
using ThCADExtension;

namespace ThMEPElectrical.AlarmLayout.LayoutProcess
{
    class LayoutOpt
    {
        public static List<Point3d> Calculate(MPolygon mPolygon, List<Point3d> pointsInLayoutList, double radius, BlindType equipmentType, AcadDatabase acdb)
        {
            
            List<Point3d> fstPoints = FstStep(pointsInLayoutList, radius); //1、初选

            List<Point3d> sndPoints = SndStep(mPolygon, fstPoints, pointsInLayoutList, radius, equipmentType); //2、加点

            List<Point3d> sndHalfPoints = SndHalfStep(mPolygon, sndPoints, pointsInLayoutList, radius, equipmentType); //2.5、加点：针对大盲区

            List<Point3d> ans = new List<Point3d>();//2.7、针对一个房间只布置一个点
            if (sndHalfPoints.Count == 1)
            {
                ans.Add(PointsDealer.GetNearestPoint(mPolygon.ToNTSPolygon().Centroid.ToAcGePoint3d(), pointsInLayoutList));
                return ans;
            }
           
            List<Point3d> fourPoints = FourStep(mPolygon, sndHalfPoints, pointsInLayoutList, radius, equipmentType); //4、移点：修补需求：将一些点更加靠近中心线

            List<Point3d> thdPoints = ThdStep(mPolygon, fourPoints, radius, equipmentType); //3、删点

            return thdPoints;
        }

        /// <summary>
        /// 获取可布置点位
        /// </summary>
        /// <param name="nonDeployableArea"></param>
        /// <param name="layoutList"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public static List<Point3d> GetPosiblePositions(List<Polyline> nonDeployableArea, List<Polyline> layoutList, double radius)
        {
            List<Point3d> pointsInLayoutList = PointsDealer.PointsInAreas(layoutList, radius).Distinct().ToList();
            Hashtable ht = new Hashtable();
            foreach (var pt in pointsInLayoutList)
            {
                ht[pt] = true;
            }
            foreach (var pl in nonDeployableArea)
            {
                foreach (var pt in pointsInLayoutList)
                {
                    if (pl.ContainsOrOnBoundary(pt))
                    {
                        ht[pt] = false;
                    }
                }
            }
            List<Point3d> ans = new List<Point3d>();
            foreach (DictionaryEntry xx in ht)
            {
                if ((bool)xx.Value == true)
                {
                    ans.Add((Point3d)xx.Key);
                }
            }
            return ans.Distinct().ToList();
        }

        /// <summary>
        /// 在可覆盖点中初步筛选出布置点
        /// </summary>
        /// <param name="pointsInAreas"></param>
        /// <param name="radius">设备覆盖半径</param>
        /// <returns>返回初步筛选的点的集合</returns>
        public static List<Point3d> FstStep(List<Point3d> pointsInAreas, double radius)
        {
            List<Point3d> fstPoints = new List<Point3d>();
            Hashtable ht = new Hashtable();
            foreach (Point3d pt in pointsInAreas)
            {
                ht.Add(pt, false);
            }
            bool flag;
            double adaptRadius = radius * AdaptRadius(radius);
            foreach (Point3d pt in pointsInAreas)
            {
                flag = false;
                foreach (Point3d pt2 in pointsInAreas)
                {
                    //距离在范围外，跳过
                    if (pt.DistanceTo(pt2) > adaptRadius)
                    {
                        continue;
                    }
                    //范围内有别的点，跳出循环
                    if ((bool)ht[pt2] == true)
                    {
                        flag = true;
                        break;
                    }
                }
                if (flag == false)
                {
                    ht[pt] = true;
                    fstPoints.Add(pt);
                }
            }
            return fstPoints;
        }

        /// <summary>
        /// 对第一步布置后的情况进行加点以覆盖完全部需覆盖区域（此步会产生冗余点）
        /// </summary>
        /// <param name="mPolygon">需覆盖带洞多边形边界</param>
        /// <param name="fstPoints">第一步布置的点位</param>
        /// <param name="pointsInLayoutList">可能布置的点位</param>
        /// <param name="radius">设备覆盖半径</param>
        /// <returns>加电后的点集合</returns>
        public static List<Point3d> SndStep(MPolygon mPolygon, List<Point3d> points, List<Point3d> pointsInLayoutList, double radius, BlindType equipmentType)
        {
            int loopCnt = 0;
            bool flag = true;
            while (flag && loopCnt < 200)
            {
                ++loopCnt;
                flag = false;
                //当前未覆盖区域集合
                NetTopologySuite.Geometries.Geometry unCoverRegion = AreaCaculator.BlandArea(mPolygon, points, radius, equipmentType);

                //在未覆盖区域附近加点
                foreach (Entity obj in unCoverRegion.ToDbCollection())
                {
                    if (obj.GetArea() > 500000)
                    {
                        flag = true;
                        Point3d pt = ((Polyline)obj).Centroid();
                        Point3d pt1 = PointsDealer.GetNearestPoint(pt, pointsInLayoutList);
                        //如果存在永不可能覆盖的位置，放弃覆盖
                        if (pt.DistanceTo(pt1) > radius)
                        {
                            flag = false;
                            continue;
                        }
                        points.Add(pt1);
                    }
                }
            }
            points = points.Distinct().ToList();
            return points;
        }

        /// <summary>
        /// 将巨大未覆盖区域附近可添加的点加入到sndPoints中
        /// </summary>
        /// <param name="mPolygon"></param>
        /// <param name="sndPoints"></param>
        /// <param name="pointsInLayoutList"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public static List<Point3d> SndHalfStep(MPolygon mPolygon, List<Point3d> points, List<Point3d> pointsInLayoutList, double radius, BlindType equipmentType)
        {
            //1、获得所有大面积未覆盖区域（多个）中所有的未覆盖区域中所有的未覆盖区域中的点（统一处理）
            NetTopologySuite.Geometries.Geometry unCoverRegion = AreaCaculator.BlandArea(mPolygon, points, radius, equipmentType);
            List<Point3d> pointsInUncoverAreas = new List<Point3d>();
            foreach (Entity obj in unCoverRegion.ToDbCollection())
            {
                if (obj.GetArea() > 500000)
                {
                    List<Point3d> tmpPoints = PointsDealer.PointsInUncoverArea(obj, 400);//-------------------
                    foreach (Point3d pt in tmpPoints)
                    {
                        pointsInUncoverAreas.Add(pt);
                    }
                }
            }
            //2、找到以上获得的点（radius或者radius / 2）最近的可布置点
            foreach (Point3d pt in pointsInUncoverAreas)
            {
                points.Add(PointsDealer.GetNearestPoint(pt, pointsInLayoutList));
            }
            points = points.Distinct().ToList();
            return points;
        }

        /// <summary>
        /// 删除不影响覆盖面积的点
        /// </summary>
        /// <param name="mPolygon">应覆盖区域</param>
        /// <param name="sndHalfPoints">第二次操作后的点集</param>
        /// <param name="radius">设备覆盖半径</param>
        /// <returns>返回删除后的点集</returns>
        public static List<Point3d> ThdStep(MPolygon mPolygon, List<Point3d> points, double radius,BlindType equipmentType)
        {
            Hashtable ht = new Hashtable();
            DeletePoints.ReducePoints(ht, points, radius);
            DeletePoints.RemovePoints(mPolygon, ht, points, radius, equipmentType);
            return DeletePoints.SummaryPoints(ht);
        }

        /// <summary>
        /// 移动布置好的点位，使之尽量靠近中间线。
        /// </summary>
        /// <param name="mPolygon"></param>
        /// <param name="points"></param>
        /// <param name="radius"></param>
        /// <param name="equipmentType"></param>
        /// <returns></returns>
        public static List<Point3d> FourStep(MPolygon mPolygon, List<Point3d> points, List<Point3d> pointsInLayoutList, double radius, BlindType equipmentType)
        {
            double preBlandArea = AreaCaculator.BlandArea(mPolygon, points, radius, equipmentType).Area;
            List<Point3d> centerPts = CenterLineSimplify.CLSimplifyPts(mPolygon);
            //key原始点，value原始点最近的中心点
            Dictionary<Point3d, Point3d> pt2center = new Dictionary<Point3d, Point3d>();
            foreach (var pt in points)
            {
                pt2center[pt] = PointsDealer.GetNearestPoint(pt, centerPts);
            }
            //key原始点，value原始点最近的中心点最近的可布置点
            Dictionary<Point3d, Point3d> pt2move = new Dictionary<Point3d, Point3d>();
            foreach(var node in pt2center)
            {
                pt2move[node.Key] = PointsDealer.GetNearestPoint(node.Value, pointsInLayoutList);
            }
            foreach(var node in pt2move)
            {
                points.Remove(node.Key);
                points.Add(node.Value);
                double curBlandArea = AreaCaculator.BlandArea(mPolygon, points, radius, equipmentType).Area;
                if (Math.Abs(preBlandArea - curBlandArea) > 500000)
                {
                    points.Add(node.Key);
                }
            }
            return points;
        }

        /// <summary>
        /// 获取布置点的方向
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="holeList"></param>
        /// <param name="points"></param>
        /// <param name="pointsWithDirection"></param>
        public static void PointsWithDirection(Polyline frame, List<Polyline> holeList, List<Point3d> points, Dictionary<Point3d, Vector3d> pointsWithDirection)
        {
            var lines = new List<Line>();
            var dbObjs = new DBObjectCollection();
            frame.Explode(dbObjs);
            foreach (var pl in holeList)
            {
                lines.AddRange(pl.ToLines());
            }
            foreach (var curve in dbObjs)
            {
                if (curve is Line line)
                {
                    if (line.StartPoint != line.EndPoint)
                    {
                        lines.Add(line);
                    }
                }
                else if (curve is Polyline poly)
                {
                    lines.AddRange(poly.ToLines());
                }
                else if (curve is Circle circle)
                {
                    lines.AddRange(circle.Tessellate(100.0).ToLines());
                }
            }
            foreach (var pt in points)
            {
                var closestLine = lines.OrderBy(o => o.GetClosestPointTo(pt, false).DistanceTo(pt)).First();
                //HostApplicationServices.WorkingDatabase.AddToModelSpace(new Line(pt, closestLine.StartPoint));//--------------------显示链接线
                pointsWithDirection.Add(pt, (closestLine.EndPoint - closestLine.StartPoint).GetNormal());
            }
        }

        /// <summary>
        /// 根据半径大小改变布点的密集程度
        /// </summary>
        /// <param name="radius">设备覆盖半径</param>
        /// <returns>两点相聚距离</returns>
        public static double AdaptRadius(double radius)
        {
            double adaptRadius = 1 + (radius - 3600) / 11500;// 11500;----------------------------调参侠
            if (adaptRadius < 0.5) return 0.5;
            else if (adaptRadius > 1.8) return 1.8;
            else return adaptRadius;
        }
    }
}

