﻿using AcHelper.Commands;
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
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Overlay.Snap;
using NetTopologySuite.Operation.Overlay;
using Dreambuild.AutoCAD;
using ThMEPElectrical.AlarmSensorLayout.Method;
using NetTopologySuite.Operation.OverlayNG;
using ThMEPElectrical.AlarmLayout.Utils;
using ThMEPElectrical.AlarmSensorLayout.Data;
using ThCADExtension;

namespace ThMEPElectrical.AlarmLayout.LayoutProcess
{
    class LayoutOpt
    {
        public static List<Point3d> Calculate(MPolygon mPolygon, List<Point3d> pointsInLayoutList, double radius, AcadDatabase acdb, BlindType equipmentType, Dictionary<Point3d, Vector3d> pointsWithDirection)
        {
            //显示中心线
            //CenterLineSimplify.ShowCenterLine(mPolygon, acdb);

            //简化中心线
            //CenterLineSimplify.CLSimplify(mPolygon);//, acdb);

            //1、初选：初步筛选设备布置点
            List<Point3d> fstPoints = FstStep(pointsInLayoutList, radius);

            //2、加点：加入点以补全未覆盖区域，此步会产生多余的点
            List<Point3d> sndPoints = SndStep(mPolygon, fstPoints, pointsInLayoutList, radius, equipmentType);
            //ShowInfo.ShowPoints(sndHalfPoints, 'O', 130, 200);

            //由于第二步加点策略有缺陷（超大区域加不上点放弃策略），可以在中间加一个步骤专门处理剩余的超大区域
            //若将此步骤放入第二步会导致时间复杂度增加，因此单独出来，对总复杂度没影响（常数+1）
            //2.5、加点：对于大区域，以radius拆分区域，每个点找一个最近的pointsInLayoutList加入到sndPoints，最后sndPoints.Distinct();
            List<Point3d> sndHalfPoints = SndHalfStep(mPolygon, sndPoints, pointsInLayoutList, radius, equipmentType);
            //ShowInfo.ShowPoints(sndHalfPoints, 'O', 130, 240);

            //3、删点：删除多余的不需要的点（总覆盖面积不变）
            List<Point3d> thdPoints = ThdStep(mPolygon, sndHalfPoints, radius, equipmentType);

            //4、获取布置点位方向
            FourStep(mPolygon, thdPoints, pointsWithDirection);
            
            return thdPoints;
        }


        /// <summary>
        /// 获取布置设备的方向
        /// </summary>
        /// <param name="mPolygon"></param>
        /// <param name="points"></param>
        /// <param name="pointsWithDirection"></param>
        public static void FourStep(MPolygon mPolygon, List<Point3d> points, Dictionary<Point3d, Vector3d> pointsWithDirection)
        {

            //var shell = mPolygon.Shell();
            var lines = new List<Line>();
            var dbObjs = new DBObjectCollection();
            mPolygon.Shell().Explode(dbObjs);
            foreach (var curve in dbObjs)
            {
                if (curve is Line line)
                {
                    if(line.StartPoint != line.EndPoint)
                    {
                        lines.Add(line);
                        line.ColorIndex = 130;
                        HostApplicationServices.WorkingDatabase.AddToModelSpace(line);
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
                HostApplicationServices.WorkingDatabase.AddToModelSpace(new Line(pt, closestLine.StartPoint));
                pointsWithDirection.Add(pt, (closestLine.EndPoint - closestLine.StartPoint).GetNormal());
            }

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
            double adaptRadius = radius * AdaptRadius(radius);//----------------------------
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
        public static List<Point3d> SndStep(MPolygon mPolygon, List<Point3d> fstPoints, List<Point3d> pointsInLayoutList, double radius, BlindType equipmentType)
        {
            bool flag = true;
            while (flag)
            {
                flag = false;
                //当前未覆盖区域集合
                NetTopologySuite.Geometries.Geometry unCoverRegion = AreaCaculator.BlandArea(mPolygon, fstPoints, radius, equipmentType);

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
                        fstPoints.Add(pt1);
                    }
                }
            }
            return fstPoints.Distinct().ToList();
        }

        /// <summary>
        /// 将巨大未覆盖区域附近可添加的点加入到sndPoints中
        /// </summary>
        /// <param name="mPolygon"></param>
        /// <param name="sndPoints"></param>
        /// <param name="pointsInLayoutList"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public static List<Point3d> SndHalfStep(MPolygon mPolygon, List<Point3d> sndPoints, List<Point3d> pointsInLayoutList, double radius, BlindType equipmentType)
        {
            //1、获得所有大面积未覆盖区域（多个）中所有的未覆盖区域中所有的未覆盖区域中的点（统一处理）
            NetTopologySuite.Geometries.Geometry unCoverRegion = AreaCaculator.BlandArea(mPolygon, sndPoints, radius, equipmentType);

            List<Point3d> pointsInUncoverAreas = new List<Point3d>();
            foreach (Entity obj in unCoverRegion.ToDbCollection())
            {
                if (obj.GetArea() > 500000)
                {
                    List<Point3d> tmpPoints = PointsDealer.PointsInUncoverArea(obj, 400);//-------------------
                    foreach (Point3d pt in tmpPoints)
                    {
                        //ShowInfo.ShowPointAsX(pt, 130);
                        pointsInUncoverAreas.Add(pt);
                    }
                }
            }
            //2、找到以上获得的点（radius或者radius / 2）最近的可布置点
            foreach (Point3d pt in pointsInUncoverAreas)
            {
                //ShowPointAsO(GetNearestPoint(pt, pointsInLayoutList), 130);
                sndPoints.Add(PointsDealer.GetNearestPoint(pt, pointsInLayoutList));
            }
            return sndPoints.Distinct().ToList();
        }

        /// <summary>
        /// 删除不影响覆盖面积的点
        /// </summary>
        /// <param name="mPolygon">应覆盖区域</param>
        /// <param name="sndHalfPoints">第二次操作后的点集</param>
        /// <param name="radius">设备覆盖半径</param>
        /// <returns>返回删除后的点集</returns>
        public static List<Point3d> ThdStep(MPolygon mPolygon, List<Point3d> sndHalfPoints, double radius,BlindType equipmentType)
        {
            Hashtable ht = new Hashtable();
            //PRE PROCESS
            DeletePoints.ReducePoints(ht, sndHalfPoints, radius);

            //CORE PROCESS
            DeletePoints.RemovePoints(mPolygon, ht, sndHalfPoints, radius, equipmentType);

            //AFTER PROCESS
            return DeletePoints.SummaryPoints(ht);
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
