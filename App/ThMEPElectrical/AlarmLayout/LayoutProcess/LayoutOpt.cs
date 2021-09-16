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
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Overlay.Snap;
using NetTopologySuite.Operation.Overlay;
using Dreambuild.AutoCAD;
using ThMEPElectrical.AlarmSensorLayout.Method;
using NetTopologySuite.Operation.OverlayNG;
using ThMEPElectrical.AlarmLayout.Utils;
using ThMEPElectrical.AlarmSensorLayout.Data;

namespace ThMEPElectrical.AlarmLayout.LayoutProcess
{
    class LayoutOpt
    {

        public static List<Point3d> Calculate(MPolygon mPolygon, List<Point3d> pointsInLayoutList, double radius, AcadDatabase acdb, BlindType equipmentType)
        {
            //显示中心线
            CenterLineSimplify.ShowCenterLine(mPolygon, acdb);

            //1、初选：初步筛选设备布置点
            List<Point3d> fstPoints = FstStep(pointsInLayoutList, radius);
            fstPoints.Distinct();

            //2、加点：加入点以补全未覆盖区域，此步会产生多余的点
            List<Point3d> sndPoints = SndStep(mPolygon, fstPoints, pointsInLayoutList, radius);
            sndPoints.Distinct();

            //由于第二步加点策略有缺陷（超大区域加不上点放弃策略），可以在中间加一个步骤专门处理剩余的超大区域
            //若将此步骤放入第二步会导致时间复杂度增加，因此单独出来，对总复杂度没影响（常数+1）
            //2.5、加点：对于大区域，以radius拆分区域，每个点找一个最近的pointsInLayoutList加入到sndPoints，最后sndPoints.Distinct();
            List<Point3d> sndHalfPoints = SndHalfStep(mPolygon, sndPoints, pointsInLayoutList, radius);

            //3、删点：删除多余的不需要的点（总覆盖面积不变）
            List<Point3d> thdPoints = ThdStep(mPolygon, sndHalfPoints, radius);
            //thdPoints.Distinct();


            List<Point3d> showPoints = new List<Point3d>();
            if (equipmentType == BlindType.CoverArea)
            {
                //4、5两步骤为光照盲区专门处理，会产生较大的时间开销，且实际使用并未产生一些布置提升。

                //4、加点：在光照盲区最近的pointsInLayoutList加到thdPoints，然后thdPoints.Distinct();
                List<Point3d> fourPoints = FourStep(mPolygon, thdPoints, pointsInLayoutList, radius);
                fourPoints.Distinct();

                //5、删点：仅对加入点附近直径（两倍半径）为半径范围内的非“孤独点（范围内就这一个）”进行删点测试
                List<Point3d> fivPoints = FivStep(mPolygon, fourPoints, pointsInLayoutList, radius);
                showPoints = fivPoints;
            }
            else
            {
                showPoints = thdPoints;
            }


            ShowInfo.SafetyCaculate(mPolygon, showPoints, radius);

            #region ShowInfo
            foreach (Point3d pt in showPoints)
            {
                ShowInfo.ShowPointAsX(pt);
                //ShowArea(pt, radius, 130);
            }
            var coveredRegion1 = AreaCaculator.GetUnion(showPoints, radius);
            ShowInfo.ShowGeometry(coveredRegion1, acdb, 1);
            //当前未覆盖区域集合
            var unCoverRegion = SnapIfNeededOverlayOp.Difference(mPolygon.ToNTSPolygon(), coveredRegion1);
            ShowInfo.ShowGeometry(unCoverRegion, acdb, 130);

            //删除之前生成的带洞多边形，以防影响之后操作
            mPolygon.UpgradeOpen();
            mPolygon.Erase();
            mPolygon.DowngradeOpen();
            #endregion
            return showPoints;
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
        public static List<Point3d> SndStep(MPolygon mPolygon, List<Point3d> fstPoints, List<Point3d> pointsInLayoutList, double radius)
        {
            bool flag = true;
            while (flag)
            {
                flag = false;
                //当前未覆盖区域集合
                var unCoverRegion = SnapIfNeededOverlayOp.Difference(mPolygon.ToNTSPolygon(), AreaCaculator.GetUnion(fstPoints, radius));

                //在未覆盖区域附近加点
                foreach (Entity obj in unCoverRegion.ToDbCollection())
                {
                    if (obj.GetArea() > 50000.0)
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
            return fstPoints;
        }

        /// <summary>
        /// 将巨大未覆盖区域附近可添加的点加入到sndPoints中
        /// </summary>
        /// <param name="mPolygon"></param>
        /// <param name="sndPoints"></param>
        /// <param name="pointsInLayoutList"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public static List<Point3d> SndHalfStep(MPolygon mPolygon, List<Point3d> sndPoints, List<Point3d> pointsInLayoutList, double radius)
        {
            //1、获得所有大面积未覆盖区域（多个）中所有的未覆盖区域中所有的未覆盖区域中的点（统一处理）
            var unCoverRegion = SnapIfNeededOverlayOp.Difference(mPolygon.ToNTSPolygon(), AreaCaculator.GetUnion(sndPoints, radius));
            List<Point3d> pointsInUncoverAreas = new List<Point3d>();
            foreach (Entity obj in unCoverRegion.ToDbCollection())
            {
                if (obj.GetArea() > 50000.0)
                {
                    List<Point3d> tmpPoints = PointsDealer.PointsInUncoverArea(obj, radius / 2);
                    foreach (Point3d pt in tmpPoints)
                    {
                        //ShowPointAsX(pt, 130);
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
            sndPoints.Distinct();

            return sndPoints;
        }

        /// <summary>
        /// 删除不影响覆盖面积的点
        /// </summary>
        /// <param name="mPolygon">应覆盖区域</param>
        /// <param name="sndPoints">第二次操作后的点集</param>
        /// <param name="radius">设备覆盖半径</param>
        /// <returns>返回删除后的点集</returns>
        public static List<Point3d> ThdStep(MPolygon mPolygon, List<Point3d> sndHalfPoints, double radius)
        {
            Hashtable ht = new Hashtable();
            //PRE PROCESS
            DeletePoints.ReducePoints(ht, sndHalfPoints, radius);

            //CORE PROCESS
            DeletePoints.RemovePoints(mPolygon, ht, sndHalfPoints, radius);

            //AFTER PROCESS
            return DeletePoints.SummaryPoints(ht);
        }



        /// <summary>
        /// 找到光照盲区附近的可布置点
        /// </summary>
        /// <param name="mPolygon"></param>
        /// <param name="thdPoints"></param>
        /// <param name="pointsInLayoutList"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public static List<Point3d> FourStep(MPolygon mPolygon, List<Point3d> thdPoints, List<Point3d> pointsInLayoutList, double radius)
        {
            //计算过点集覆盖的真实面积
            List<Polygon> Detect = new List<Polygon>();
            foreach (Point3d pt in thdPoints)
            {
                Detect.Add(DetectCalculator.CalculateDetect(new Coordinate(pt.X, pt.Y), mPolygon.ToNTSPolygon(), radius, true));
            }
            //计算光照盲区
            NetTopologySuite.Geometries.Geometry poly = OverlayNGRobust.Union(Detect.ToArray());
            NetTopologySuite.Geometries.Geometry blind = mPolygon.ToNTSPolygon().Difference(poly);

            //Point3dList lightBlandPoints = new Point3dList();//PointsInAreas(blind.ToDbCollection(), radius);
            foreach (Entity et in blind.ToDbCollection())
            {
                //lightBlandPoints.Add(et.GetCenter());
                thdPoints.Add(PointsDealer.GetNearestPoint(et.GetCenter(), pointsInLayoutList));
            }

            //foreach (Point3d pt in lightBlandPoints)
            //{
            //    thdPoints.Add(GetNearestPoint(pt, pointsInLayoutList));
            //}
            //return lightBlandPoints;
            return thdPoints;
        }

        /// <summary>
        /// 最后的删点：仅对加入点附近直径（两倍半径）为半径范围内的非“孤独点（范围内就这一个）”进行删点测试
        /// </summary>
        /// <param name="mPolygon"></param>
        /// <param name="points"></param>
        /// <param name="pointsInLayoutList"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public static List<Point3d> FivStep(MPolygon mPolygon, List<Point3d> points, List<Point3d> pointsInLayoutList, double radius)
        {
            //PRE PROCESS
            //toDoList 要进行删除测试的点集
            List<Point3d> toDoPoints = new List<Point3d>();
            foreach (Point3d pt in points)
            {
                foreach (Point3d ptt in pointsInLayoutList)
                {
                    if (pt.DistanceTo(ptt) < radius) //  radius * 2 越大越准越慢
                    {
                        toDoPoints.Add(pt);
                    }
                }
            }
            toDoPoints.Distinct();
            Hashtable ht = new Hashtable();
            DeletePoints.ReducePoints(ht, points, radius);

            //CORE PROCESS
            DeletePoints.RemovePoints(mPolygon, ht, toDoPoints, radius);

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
            double adaptRadius = 1 + (radius - 3650) / 13300;// 11500;----------------------------调参侠
            if (adaptRadius < 0.5) return 0.5;
            else if (adaptRadius > 1.8) return 1.8;
            else return adaptRadius;
        }
    }
}
