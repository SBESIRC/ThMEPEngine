using System;
using System.Linq;
using Linq2Acad;
using System.Collections.Generic;
using System.Collections;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using ThCADExtension;
using ThCADCore.NTS;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.OverlayNG;
using ThMEPEngineCore.Diagnostics;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.AreaLayout.CenterLineLayout.Utils;
using ThMEPEngineCore.AreaLayout.GridLayout.Data;
using ThMEPEngineCore.Algorithm;

namespace ThMEPEngineCore.AreaLayout.CenterLineLayout.LayoutProcess
{
    class LayoutOpt
    {
        //input
        public MPolygon MRoom { get; set; }
        public List<MPolygon> LayoutWithHole { get; set; }
        public double Radius { get; set; } = 0;
        public BlindType EquipmentType { get; set; } = BlindType.CoverArea;
        public List<Polyline> DetectArea { get; set; } = new List<Polyline>();
        public List<MPolygon> LayoutList { get; set; }
        //public List<Polyline> nonDeployableArea { get; set; } = new List<Polyline>();
        public List<Point3d> CenterLinePts { get; set; } = new List<Point3d>();

        //inner user
        public List<Point3d> PointsInLayoutList { get; private set; } = new List<Point3d>();
        public Geometry EmptyDetect { get; private set; }
        public ThCADCoreNTSSpatialIndex DetectSpatialIdx { get; private set; }
        public LayoutOpt()
        {

        }

        public List<Point3d> Calculate()
        {
            //GetPosiblePositions();
            GetPosiblePositionsNew();
            SetEmptyDetect();
            SetDetectSptialIdx();

            if (PointsInLayoutList.Count == 0)
            {
                return new List<Point3d>();
            }

            PointsInLayoutList.ForEach(x => DrawUtils.ShowGeometry(x, "l0ptIni", 1, 25, 30));

            List<Point3d> fstPoints = FstStep(); //1、初选
            fstPoints.ForEach(x => DrawUtils.ShowGeometry(x, "l1ptFirst", 3, 25, 30));

            fstPoints = AddDetectAreaPts(fstPoints); //找探测区域内没有点的，在探测区域中心附近加点
            fstPoints.ForEach(x => DrawUtils.ShowGeometry(x, "l2ptAddDetectArea", 3, 25, 30));

            List<Point3d> sndPoints = SndStep(fstPoints); //2、加点
            sndPoints.ForEach(x => DrawUtils.ShowGeometry(x, "l3ptCoverBlind", 150, 25, 30));

            List<Point3d> sndHalfPoints = SndHalfStep(sndPoints); //2.5、加点：针对大盲区
            sndHalfPoints.ForEach(x => DrawUtils.ShowGeometry(x, "l4ptCoverBigBlind", 212, 25, 30));

            List<Point3d> ans = new List<Point3d>();//2.7、针对一个房间只布置一个点
            if (sndHalfPoints.Count == 1)
            {
                var temp = PointsDealer.GetNearestPoint(MRoom.ToNTSPolygon().Centroid.ToAcGePoint3d(), PointsInLayoutList, MRoom.Shell());
                if (temp != Point3d.Origin)
                {
                    ans.Add(temp);
                }

                return ans;
            }

            var movePtToLayoutCt = MovePtToLayoutCt(sndHalfPoints);
            movePtToLayoutCt.ForEach(x => DrawUtils.ShowGeometry(x, "l5ptMoveToCT", 210, 25, 30));

            List<Point3d> fourPoints = FourStep(movePtToLayoutCt); //4、移点：修补需求：将一些点更加靠近中心线
            fourPoints.ForEach(x => DrawUtils.ShowGeometry(x, "l6ptMoveToCL", 141, 25, 30));

            List<Point3d> thdPoints = ThdStep(fourPoints); //5、删点
            thdPoints.ForEach(x => DrawUtils.ShowGeometry(x, "l7ptDelet", 41, 25, 30));

            return thdPoints;
        }

        ///// <summary>
        ///// 获取可布置点位
        ///// </summary>
        ///// <param name="nonDeployableArea"></param>
        ///// <param name="layoutList"></param>
        ///// <param name="radius"></param>
        ///// <returns></returns>
        //private void GetPosiblePositions()
        //{
        //    List<Point3d> ans = new List<Point3d>();
        //    var ptInLayoutArea = PointsDealer.PointsInAreas(layoutList, radius).Distinct().ToList();
        //    Hashtable ht = new Hashtable();
        //    foreach (var pt in ptInLayoutArea)
        //    {
        //        ht[pt] = true;
        //    }
        //    foreach (var pl in nonDeployableArea)
        //    {
        //        foreach (var pt in ptInLayoutArea)
        //        {
        //            if (pl.ContainsOrOnBoundary(pt))
        //            {
        //                ht[pt] = false;
        //            }
        //        }
        //    }

        //    foreach (DictionaryEntry xx in ht)
        //    {
        //        if ((bool)xx.Value == true)
        //        {
        //            ans.Add((Point3d)xx.Key);
        //        }
        //    }

        //    pointsInLayoutList = ans.Distinct().ToList();

        //}

        private void GetPosiblePositionsNew()
        {
            var rateThreshold = 0.04; //试了几个奇怪的带洞区域或者很窄区域的经验值
            List<Point3d> ans = new List<Point3d>();

            for (int i = 0; i < LayoutWithHole.Count; i++)
            {
                var layout = LayoutWithHole[i];
                var areaPoints = new List<Point3d>();

                if (layout.Area > Radius * Radius * 0.2)
                //if(disX > radius || disY > radius)
                {
                    var obb = (layout.Shell()).CalObb();
                    areaPoints = PointsDealer.PointsInUncoverAreaNew(layout, 400, out var ptsInOBB);//700------------------------------调参侠 此参数可以写一个计算函数，通过面积大小求根号 和半径比较算出 要有上下界(700是相对接近最好的值)

                    double rate = (double)areaPoints.Count / (double)ptsInOBB.Count;
                    var rateArea = layout.Area / obb.Area;
                    var pt0 = obb.GetPoint3dAt(0);
                    DrawUtils.ShowGeometry(pt0, string.Format("all:{0},in:{1},rate：{2}", ptsInOBB.Count, areaPoints.Count, rate), "l0PtInitInfo", colorIndex: 3, hight: 30);
                    DrawUtils.ShowGeometry(new Point3d(pt0.X, pt0.Y - 1 * 35, 0), string.Format("obb:{0},frame:{1},rate：{2}", obb.Area, layout.Area, rateArea), "l0PtInitInfo", colorIndex: 3, hight: 30);
                    ptsInOBB.ForEach(x => DrawUtils.ShowGeometry(x, "l0ptInitInOBB", colorIndex: 150, r: 30));
                    
                    if (Math.Abs(rate - rateArea) > rateThreshold)
                    {
                        areaPoints = PointsDealer.PointsInUncoverAreaNew(layout, 100, out ptsInOBB);
                    }
                }
                else
                {
                    //areaPoints = PointsInArea(poly, radius);
                    areaPoints = PointsDealer.PointsInUncoverAreaNew(layout, 100, out var ptsInOBB);
                }

                ans.AddRange(areaPoints);

            }

            PointsInLayoutList = ans.Distinct().ToList();

        }

        private void SetDetectSptialIdx()
        {
            if (DetectArea.Count > 0)
            {
                DBObjectCollection dBObjectCollection = new DBObjectCollection();
                foreach (var detect in DetectArea)
                {
                    dBObjectCollection.Add(ThMPolygonTool.CreateMPolygon(detect));
                }

                DetectSpatialIdx = new ThCADCoreNTSSpatialIndex(dBObjectCollection);
            }
        }

        private void SetEmptyDetect()
        {
            if (DetectArea.Count > 0)
            {
                var emptyDetect = new List<Polygon>();
                var obj = new DBObjectCollection();
                foreach (var p in PointsInLayoutList)
                {
                    obj.Add(new DBPoint(p));
                }
                var pointIdx = new ThCADCoreNTSSpatialIndex(obj);

                foreach (var d in DetectArea)
                {
                    var hasLayout = LayoutList.Where(x => d.Contains(x.Shell()));

                    if (hasLayout.Count() == 0)
                    {
                        emptyDetect.Add(d.ToNTSPolygon());
                    }
                    else
                    {
                        hasLayout = hasLayout.OrderByDescending(x => x.Area);
                        if (hasLayout.First().Area <= (Radius * Radius * 0.2 / 50)) //根据初始点位生成得出
                        {
                            var ptInDete = pointIdx.SelectCrossingPolygon(d);
                            if (ptInDete.Count == 0)
                            {
                                emptyDetect.Add(d.ToNTSPolygon());
                            }
                        }
                    }
                }
                EmptyDetect = OverlayNGRobust.Union(emptyDetect.ToArray());
            }
        }

        /// <summary>
        /// 在可覆盖点中初步筛选出布置点
        /// </summary>
        /// <param name="pointsInAreas"></param>
        /// <param name="radius">设备覆盖半径</param>
        /// <returns>返回初步筛选的点的集合</returns>
        private List<Point3d> FstStep()
        {
            List<Point3d> fstPoints = new List<Point3d>();
            Hashtable ht = new Hashtable();
            foreach (Point3d pt in PointsInLayoutList)
            {
                ht.Add(pt, false);
            }
            bool flag;
            double adaptRadius = Radius * AdaptRadius(Radius);
            foreach (Point3d pt in PointsInLayoutList)
            {
                flag = false;
                foreach (Point3d pt2 in PointsInLayoutList)
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
        /// 检查DetectArea里面是否有点，如果没有加一个离中点最近的点
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        private List<Point3d> AddDetectAreaPts(List<Point3d> points)
        {
            var returnPts = new List<Point3d>();
            returnPts.AddRange(points);

            var addPtInDetect = new List<Point3d>();
            for (int i = 0; i < DetectArea.Count; i++)
            {
                var detect = DetectArea[i];
                var pInDetect = points.Where(x => detect.Contains(x));
                if (pInDetect.Count() == 0)
                {
                    var centerPt = detect.Centroid();
                    var ptNearCenter = PointsDealer.GetNearestPoint(centerPt, PointsInLayoutList, detect);
                    if (ptNearCenter != Point3d.Origin)
                    {
                        addPtInDetect.Add(ptNearCenter);
                    }
                }
            }

            returnPts.AddRange(addPtInDetect);
            return returnPts;

        }

        /// <summary>
        /// 对第一步布置后的情况进行加点以覆盖完全部需覆盖区域（此步会产生冗余点）
        /// </summary>
        /// <param name="mPolygon">需覆盖带洞多边形边界</param>
        /// <param name="points">第一步布置的点位</param>
        /// <param name="pointsInLayoutList">可能布置的点位</param>
        /// <param name="radius">设备覆盖半径</param>
        /// <returns>加电后的点集合</returns>
        private List<Point3d> SndStep(List<Point3d> points)
        {
            int loopCnt = 0;
            bool flag = true;
            while (flag && loopCnt < 200)
            {
                ++loopCnt;
                flag = false;
                //当前未覆盖区域集合
                var unCoverRegion = AreaCaculator.BlandArea(MRoom, points, Radius, EquipmentType, DetectSpatialIdx, EmptyDetect);

                //在未覆盖区域附近加点
                foreach (Entity obj in unCoverRegion.ToDbCollection())
                {
                    if (obj.GetArea() > 500000)
                    {
                        DrawUtils.ShowGeometry(obj, "l3Blind", 3);
                        flag = true;
                        Point3d pt = ((Polyline)obj).Centroid();
                        var detect = new Polyline();
                        if (DetectArea.Count > 0)
                        {
                            detect = AreaCaculator.GetDetectPolyline(pt, DetectSpatialIdx);
                        }
                        Point3d pt1 = PointsDealer.GetNearestPoint(pt, PointsInLayoutList, detect);

                        //如果存在永不可能覆盖的位置，放弃覆盖
                        if (pt1 == Point3d.Origin || pt.DistanceTo(pt1) > Radius)
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
        private List<Point3d> SndHalfStep(List<Point3d> points)
        {
            //1、获得所有大面积未覆盖区域（多个）中所有的未覆盖区域中所有的未覆盖区域中的点（统一处理）
            NetTopologySuite.Geometries.Geometry unCoverRegion = AreaCaculator.BlandArea(MRoom, points, Radius, EquipmentType, DetectSpatialIdx, EmptyDetect);
            List<Point3d> pointsInUncoverAreas = new List<Point3d>();
            foreach (Entity obj in unCoverRegion.ToDbCollection())
            {
                if (obj.GetArea() > 500000)
                {
                    DrawUtils.ShowGeometry(obj, "l4BigBlind", 3);
                    List<Point3d> tmpPoints = PointsDealer.PointsInUncoverArea(obj, 400);//-------------------
                    pointsInUncoverAreas.AddRange(tmpPoints);
                }
            }
            //2、找到以上获得的点（radius或者radius / 2）最近的可布置点
            foreach (Point3d pt in pointsInUncoverAreas)
            {
                var detect = new Polyline();
                if (DetectArea.Count > 0)
                {
                    detect = AreaCaculator.GetDetectPolyline(pt, DetectSpatialIdx);
                }
                var newPt = PointsDealer.GetNearestPoint(pt, PointsInLayoutList, detect);
                if (newPt != Point3d.Origin)
                {
                    points.Add(newPt);
                }
            }
            points = points.Distinct().ToList();
            return points;
        }

        /// <summary>
        /// 点移动到中点附近
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        private List<Point3d> MovePtToLayoutCt(List<Point3d> points)
        {
            var movePtToCenter = new List<Point3d>();
            for (int i = 0; i < points.Count; i++)
            {
                var layout = LayoutList.Where(x => x.Contains(points[i])).First();
                var ptInLayout = points.Where(x => layout.Contains(x));
                if (ptInLayout.Count() == 1)
                {
                    var centerPt = layout.Shell().Centroid();
                    var ptNearCenter = PointsDealer.GetNearestPoint(centerPt, PointsInLayoutList, layout.Shell());
                    if (ptNearCenter != Point3d.Origin)
                    {
                        movePtToCenter.Add(ptNearCenter);
                    }
                }
                else
                {
                    movePtToCenter.Add(points[i]);
                }

            }
            return movePtToCenter;
        }


        /// <summary>
        /// 删除不影响覆盖面积的点
        /// </summary>
        /// <param name="mPolygon">应覆盖区域</param>
        /// <param name="sndHalfPoints">第二次操作后的点集</param>
        /// <param name="radius">设备覆盖半径</param>
        /// <returns>返回删除后的点集</returns>
        private List<Point3d> ThdStep(List<Point3d> points)
        {
            Hashtable ht = new Hashtable();
            DeletePoints.ReducePoints(ht, points, Radius);
            DeletePoints.RemovePoints(MRoom, ht, points, Radius, EquipmentType, DetectSpatialIdx, EmptyDetect);
            return DeletePoints.SummaryPoints(ht);
        }

        /// <summary>
        /// 移动布置好的点位，使之尽量靠近中间线。
        /// </summary>
        /// <param name="mPolygonShell"></param>
        /// <param name="points"></param>
        /// <param name="radius"></param>
        /// <param name="equipmentType"></param>
        /// <returns></returns>
        private List<Point3d> FourStep(List<Point3d> points)
        {
            double preBlandArea = AreaCaculator.BlandArea(MRoom, points, Radius, EquipmentType, DetectSpatialIdx, EmptyDetect).Area;

            //key原始点，value原始点最近的中心点
            Dictionary<Point3d, Point3d> pt2center = new Dictionary<Point3d, Point3d>();
            foreach (var pt in points)
            {
                pt2center[pt] = PointsDealer.GetNearestPoint(pt, CenterLinePts, null);
            }
            //key原始点，value原始点最近的中心点最近的可布置点
            Dictionary<Point3d, Point3d> pt2move = new Dictionary<Point3d, Point3d>();
            foreach (var node in pt2center)
            {
                var layout = LayoutList.Where(x => x.Contains(node.Key)).First();
                pt2move[node.Key] = PointsDealer.GetNearestPoint(node.Value, PointsInLayoutList, layout.Shell());
            }
            foreach (var node in pt2move)
            {
                if (node.Value != Point3d.Origin)
                {
                    points.Remove(node.Key);
                    points.Add(node.Value);
                    double curBlandArea = AreaCaculator.BlandArea(MRoom, points, Radius, EquipmentType, DetectSpatialIdx, EmptyDetect).Area;
                    if (Math.Abs(preBlandArea - curBlandArea) > 500000)
                    {
                        //points.Remove(node.Value);
                        points.Add(node.Key);
                    }
                }
            }
            return points.Distinct().ToList();
        }


        /// <summary>
        /// 根据半径大小改变布点的密集程度
        /// </summary>
        /// <param name="radius">设备覆盖半径</param>
        /// <returns>两点相聚距离</returns>
        private static double AdaptRadius(double radius)
        {
            double adaptRadius = 1 + (radius - 3600) / 11500;// 11500;----------------------------调参侠
            if (adaptRadius < 0.5) return 0.5;
            else if (adaptRadius > 1.8) return 1.8;
            else return adaptRadius;
        }
    }
}

