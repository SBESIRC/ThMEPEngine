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
using ThMEPEngineCore.Algorithm;
using ThCADExtension;
using ThMEPElectrical.Assistant;
using Autodesk.AutoCAD.Runtime;
using NFox.Collections;//树
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Overlay.Snap;
using NetTopologySuite.Operation.Overlay;
using Dreambuild.AutoCAD;
using ThMEPElectrical.AlarmSensorLayout.Method;
using NetTopologySuite.Operation.OverlayNG;

namespace ThMEPElectrical.Command
{
    class ThFireAlarmSystemLayoutCommand : IAcadCommand, IDisposable
    {
        public void Dispose()
        {

        }

        public void Execute()
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                #region GetImput
                // 获取框线
                PromptSelectionOptions options = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    MessageForAdding = "请选择布置区域框线",
                    RejectObjectsOnLockedLayers = true,
                };
                var dxfNames = new string[]
                {
                    RXClass.GetClass(typeof(Polyline)).DxfName,
                };
                var filter = ThSelectionFilterTool.Build(dxfNames);
                var result = Active.Editor.GetSelection(options, filter);
                if (result.Status != PromptStatus.OK)
                {
                    Application.ShowAlertDialog("请选择正确的带洞多边形！");
                    return;
                }
                var ptOri = new Point3d();
                var transformer = new ThMEPOriginTransformer(ptOri);
                var frameList = new List<Polyline>();

                foreach (ObjectId obj in result.Value.GetObjectIds())
                {
                    //获取外包框
                    var frameTemp = acdb.Element<Polyline>(obj);
                    var nFrame = processFrame(frameTemp, transformer);
                    if (nFrame.Area < 1)
                    {
                        continue;
                    }
                    frameList.Add(nFrame);
                }
                var frame = frameList.OrderByDescending(x => x.Area).First();
                var holeList = getPoly(frame, "AI-房间框线", transformer, true);
                var layoutList = getPoly(frame, "AI-可布区域", transformer, false);
                var wallList = getPoly(frame, "AI-墙", transformer, false);

                //类型转换--------------------------------------------------------------------------
                var objs = new DBObjectCollection();
                foreach (var obj in result.Value.GetObjectIds())
                {
                    objs.Add(acdb.Element<Entity>(obj));
                }

                foreach(var hole in holeList)
                {
                    objs.Add(hole);
                }

                MPolygon mPolygon = objs.BuildMPolygon();
                //加入数据库
                acdb.ModelSpace.Add(mPolygon);
                mPolygon.SetDatabaseDefaults();

                PromptDoubleResult area = Active.Editor.GetDistance("\n设备覆盖半径");
                if (area.Status != PromptStatus.OK)
                {
                    return;
                }
                double radius = area.Value;
                #endregion

                #region ShowCenterLine
                /*
                //----------------------------------------------------------------------------------------------------------------------
                //生成带洞多边形的中心线，默认差值距离为300
                var centerlines = ThCADCoreNTSCenterlineBuilder.Centerline(mPolygon.ToNTSPolygon(), 300);
                // 生成、显示中线
                centerlines.Cast<Entity>().ToList().CreateGroup(acdb.Database, 130);
                //----------------------------------------------------------------------------------------------------------------------

                List<Point3d> oriPoints = ThCADCoreNTSCenterlineBuilder.CenterPoints(mPolygon.ToNTSPolygon(), 300);
                oriPoints.Distinct();
                */
                #endregion

                //获取可布置区域中可能布置的点
                Point3dList pointsInLayoutList = PointsInAreas(layoutList, radius);
                pointsInLayoutList.Distinct();

                //1、初选：初步筛选设备布置点
                Point3dList fstPoints = FstStep(pointsInLayoutList, radius);
                fstPoints.Distinct();

                //2、加点：加入点以补全未覆盖区域，此步会产生多余的点
                Point3dList sndPoints = SndStep(mPolygon, fstPoints, pointsInLayoutList, radius);
                sndPoints.Distinct();

                //由于第二步加点策略有缺陷（超大区域加不上点放弃策略），可以在中间加一个步骤专门处理剩余的超大区域
                //若将此步骤放入第二步会导致时间复杂度增加，因此单独出来，对总复杂度没影响（常数+1）
                //2.5、加点：对于大区域，以radius拆分区域，每个点找一个最近的pointsInLayoutList加入到sndPoints，最后sndPoints.Distinct();
                Point3dList sndHalfPoints = SndHalfStep(mPolygon, sndPoints, pointsInLayoutList, radius);

                //3、删点：删除多余的不需要的点（总覆盖面积不变）
                Point3dList thdPoints = ThdStep(mPolygon, sndHalfPoints, radius);
                //thdPoints.Distinct();
                
                //4、5两步骤为光照盲区专门处理，会产生较大的时间开销，且实际使用并未产生一些布置提升。

                //4、加点：在光照盲区最近的pointsInLayoutList加到thdPoints，然后thdPoints.Distinct();
                //Point3dList fourPoints = FourStep(mPolygon, thdPoints, pointsInLayoutList, radius);
                //fourPoints.Distinct();

                //5、删点：仅对加入点附近直径（两倍半径）为半径范围内的非“孤独点（范围内就这一个）”进行删点测试
                //Point3dList fivPoints = FivStep(mPolygon, fourPoints, pointsInLayoutList, radius);

                Point3dList showPoints = thdPoints;//fivPoints;

                SafetyCaculate(mPolygon, showPoints, radius);

                #region ShowInfo
                foreach (Point3d pt in showPoints)
                {
                    ShowPointAsX(pt);
                    //ShowArea(pt, radius, 130);
                }
                var coveredRegion1 = GetUnion(showPoints, radius);
                ShowGeometry(coveredRegion1, acdb, 1);
                //当前未覆盖区域集合
                var unCoverRegion = SnapIfNeededOverlayOp.Difference(mPolygon.ToNTSPolygon(), coveredRegion1);
                ShowGeometry(unCoverRegion, acdb, 130);

                //删除之前生成的带洞多边形，以防影响之后操作
                mPolygon.UpgradeOpen();
                mPolygon.Erase();
                mPolygon.DowngradeOpen();
                #endregion
            }
        }

        /// <summary>
        /// 日志：查看点的数量是否符合安全要求
        /// </summary>
        /// <param name="mPolygon"></param>
        /// <param name="points"></param>
        /// <param name="radius"></param>
        public static void SafetyCaculate(MPolygon mPolygon, Point3dList points, double radius)
        {
            double safeArea = Math.PI * radius * radius / 2;
            if(radius == 3600)
            {
                safeArea = 20000000;
            }
            else if(radius == 4400)
            {
                safeArea = 30000000;
            }
            else if(radius == 5800)
            {
                safeArea = 60000000;
            }
            else if(radius == 7200)
            {
                safeArea = 80000000;
            }
            Active.Editor.WriteMessageWithReturn($"Area: {mPolygon.Area / 1000000}");
            Active.Editor.WriteMessageWithReturn($"Layout count: {points.Count}");
            Active.Editor.WriteMessageWithReturn($"Target count: { mPolygon.Area / safeArea}");
        }

        /// <summary>
        /// 找到光照盲区附近的可布置点
        /// </summary>
        /// <param name="mPolygon"></param>
        /// <param name="thdPoints"></param>
        /// <param name="pointsInLayoutList"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public static Point3dList FourStep(MPolygon mPolygon, Point3dList thdPoints, Point3dList pointsInLayoutList, double radius)
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
            foreach(Entity et in blind.ToDbCollection())
            {
                //lightBlandPoints.Add(et.GetCenter());
                thdPoints.Add(GetNearestPoint(et.GetCenter(), pointsInLayoutList));
            }

            //foreach (Point3d pt in lightBlandPoints)
            //{
            //    thdPoints.Add(GetNearestPoint(pt, pointsInLayoutList));
            //}
            //return lightBlandPoints;
            return thdPoints;
        }

        /// <summary>
        /// 将巨大未覆盖区域附近可添加的点加入到sndPoints中
        /// </summary>
        /// <param name="mPolygon"></param>
        /// <param name="sndPoints"></param>
        /// <param name="pointsInLayoutList"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public static Point3dList SndHalfStep(MPolygon mPolygon, Point3dList sndPoints, Point3dList pointsInLayoutList, double radius)
        {
            //1、获得所有大面积未覆盖区域（多个）中所有的未覆盖区域中所有的未覆盖区域中的点（统一处理）
            var unCoverRegion = SnapIfNeededOverlayOp.Difference(mPolygon.ToNTSPolygon(), GetUnion(sndPoints, radius));
            Point3dList pointsInUncoverAreas = new Point3dList();
            foreach (Entity obj in unCoverRegion.ToDbCollection())
            {
                if (obj.GetArea() > 50000.0)
                {
                    Point3dList tmpPoints = PointsInUncoverArea(obj, radius / 2);
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
                sndPoints.Add(GetNearestPoint(pt, pointsInLayoutList));
            }
            sndPoints.Distinct();

            return sndPoints;
        }

        /// <summary>
        /// 最后的删点：仅对加入点附近直径（两倍半径）为半径范围内的非“孤独点（范围内就这一个）”进行删点测试
        /// </summary>
        /// <param name="mPolygon"></param>
        /// <param name="points"></param>
        /// <param name="pointsInLayoutList"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public static Point3dList FivStep(MPolygon mPolygon, Point3dList points, Point3dList pointsInLayoutList, double radius)
        {
            //PRE PROCESS
            //toDoList 要进行删除测试的点集
            Point3dList toDoPoints = new Point3dList();
            foreach (Point3d pt in points)
            {
                foreach (Point3d ptt in pointsInLayoutList)
                {
                    if (pt.DistanceTo(ptt) < radius * 2)
                    {
                        toDoPoints.Add(pt);
                    }
                }
            }
            toDoPoints.Distinct();
            Hashtable ht = new Hashtable();
            ReducePoints(ht, points, radius);

            //CORE PROCESS
            RemovePoints(mPolygon, ht, toDoPoints, radius);

            //AFTER PROCESS
            return SummaryPoints(ht);
        }

        /// <summary>
        /// 删点测试的预先操作，排除极大可能不会被删除的点
        /// </summary>
        /// <param name="ht">记录点是否被删</param>
        /// <param name="points"></param>
        /// <param name="radius"></param>
        public static void ReducePoints(Hashtable ht, Point3dList points, double radius)
        {
            //ht<Point3d, int>:0要删除的点，1需要进行删除测试的点，2"孤独点"不需要进行删除测试的点
            int cntNear, cntMiddle;
            double distence;
            int middleCmp = radius < 3300 ? 10 : 1;//20 : 2//越小越准确：两个值至少为1：1
            double centerCmp = radius < 3300 ? 0.5 : 0.8;//越大越准确：两个值最大为1
            foreach (Point3d pt in points)
            {
                cntNear = 0;
                cntMiddle = 0;
                foreach (Point3d p in points)
                {
                    distence = pt.DistanceTo(p);
                    if (distence < radius * centerCmp)
                    {
                        ++cntNear;//此处可以优化 提前break（cntNear > 1） 但优化与否影响不大
                    }
                    else if (distence < radius * 1.1)//* 1.2
                    {
                        ++cntMiddle;
                    }
                }
                if (cntNear == 1 && cntMiddle <= middleCmp)
                {
                    ht[pt] = 2;
                }
                if (ht.Contains(pt))
                {
                    continue;
                }
                else ht.Add(pt, 1);
            }
        }

        /// <summary>
        /// 删点的核心操作，尝试依次删除点集中的点
        /// </summary>
        /// <param name="mPolygon"></param>
        /// <param name="ht">记录点是否被删</param>
        /// <param name="points"></param>
        /// <param name="radius"></param>
        public static void RemovePoints(MPolygon mPolygon, Hashtable ht, Point3dList points, double radius)
        {
            //计算过当前剩余总面积
            double totalUncoverArea = SnapIfNeededOverlayOp.Difference(mPolygon.ToNTSPolygon(), GetUnion(points, radius)).Area;

            foreach (Point3d pt in points)
            {
                if ((int)ht[pt] == 1)
                {
                    Point3dList tmpPt = new Point3dList();
                    ht[pt] = 0;
                    foreach (DictionaryEntry xx in ht)
                    {
                        if ((int)xx.Value != 0)
                        {
                            tmpPt.Add((Point3d)xx.Key);
                        }
                    }
                    //获取删除这个点后的得到的未覆盖区域
                    var unCoverRegion = SnapIfNeededOverlayOp.Difference(mPolygon.ToNTSPolygon(), GetUnion(tmpPt, radius));

                    bool flag = true;//默认删点,false不删点
                    foreach (Entity obj in unCoverRegion.ToDbCollection())
                    {
                        if (obj.GetArea() > 500000.0 && unCoverRegion.Area - totalUncoverArea > 500000.0)//如果删除后面积变大超过要求
                        {
                            flag = false;
                        }
                    }
                    if (flag == true)
                    {
                        //ShowPointAsX(pt, 1);
                        continue;
                    }
                    ht[pt] = 1;
                }
            }
        }

        /// <summary>
        /// 获取哈希表中不能被删除的点
        /// </summary>
        /// <param name="ht"></param>
        /// <returns></returns>
        public static Point3dList SummaryPoints(Hashtable ht)
        {
            Point3dList points = new Point3dList();
            foreach (DictionaryEntry x in ht)
            {
                if ((int)x.Value > 0)
                {
                    /*
                    //用O显示被优化的点
                    if ((int)x.Value == 2)
                    {
                        ShowPointAsO((Point3d)x.Key);
                    }
                    */
                    points.Add((Point3d)x.Key);
                }
            }
            return points;
        }

        /// <summary>
        /// 删除不影响覆盖面积的点
        /// </summary>
        /// <param name="mPolygon">应覆盖区域</param>
        /// <param name="sndPoints">第二次操作后的点集</param>
        /// <param name="radius">设备覆盖半径</param>
        /// <returns>返回删除后的点集</returns>
        public static Point3dList ThdStep(MPolygon mPolygon, Point3dList sndHalfPoints, double radius)
        {
            Hashtable ht = new Hashtable();
            //PRE PROCESS
            ReducePoints(ht, sndHalfPoints, radius);

            //CORE PROCESS
            RemovePoints(mPolygon, ht, sndHalfPoints, radius);

            //AFTER PROCESS
            return SummaryPoints(ht);
        }

        /// <summary>
        /// 对第一步布置后的情况进行加点以覆盖完全部需覆盖区域（此步会产生冗余点）
        /// </summary>
        /// <param name="mPolygon">需覆盖带洞多边形边界</param>
        /// <param name="fstPoints">第一步布置的点位</param>
        /// <param name="pointsInLayoutList">可能布置的点位</param>
        /// <param name="radius">设备覆盖半径</param>
        /// <returns>加电后的点集合</returns>
        public static Point3dList SndStep(MPolygon mPolygon, Point3dList fstPoints, Point3dList pointsInLayoutList, double radius)
        {
            bool flag = true;
            while (flag)
            {
                flag = false;
                //当前未覆盖区域集合
                var unCoverRegion = SnapIfNeededOverlayOp.Difference(mPolygon.ToNTSPolygon(), GetUnion(fstPoints, radius));

                //在未覆盖区域附近加点
                foreach (Entity obj in unCoverRegion.ToDbCollection())
                {
                    if (obj.GetArea() > 50000.0)
                    {
                        flag = true;
                        Point3d pt = ((Polyline)obj).Centroid();
                        Point3d pt1 = GetNearestPoint(pt, pointsInLayoutList);
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
        /// 获取设备覆盖区域集合
        /// </summary>
        /// <param name="pts">设备布置点集</param>
        /// <param name="radius">设备覆盖半径</param>
        /// <returns>返回设备覆盖区域集合</returns>
        public static NetTopologySuite.Geometries.Geometry GetUnion(Point3dList pts, double radius)
        {
            List<Circle> carryAreaUnion = new List<Circle>();
            foreach (Point3d pt in pts)
            {
                carryAreaUnion.Add(new Circle(pt, Vector3d.ZAxis, radius));
            }
            NetTopologySuite.Geometries.Geometry coveredRegion = SnapIfNeededOverlayOp.Union(carryAreaUnion.First().ToNTSPolygon(), carryAreaUnion.Last().ToNTSPolygon());
            foreach (Circle a in carryAreaUnion)
            {
                coveredRegion = SnapIfNeededOverlayOp.Union(coveredRegion, a.ToNTSPolygon());
            }
            //ShowGeometry(coveredRegion, acdb, 1);

            //var objs = new DBObjectCollection();
            //foreach (var a in carryAreaUnion)
            //{
            //    objs.Add(a.Tessellate(100.0));
            //}
            //var results = mPolygon.ToNTSPolygon().Difference(objs, true);
            return coveredRegion;
        }

        /// <summary>
        /// 显示一个Geometry集合
        /// </summary>
        /// <param name="geometry"></param>
        /// <param name="acdb"></param>
        /// <param name="colerIndex"></param>
        public static void ShowGeometry(NetTopologySuite.Geometries.Geometry geometry, AcadDatabase acdb, int colerIndex = 1)
        {
            foreach (Entity obj in geometry.ToDbCollection())
            {
                obj.ColorIndex = colerIndex;
                acdb.ModelSpace.Add(obj);
            }
        }

        /// <summary>
        /// 在可覆盖点中初步筛选出布置点
        /// </summary>
        /// <param name="pointsInAreas"></param>
        /// <param name="radius">设备覆盖半径</param>
        /// <returns>返回初步筛选的点的集合</returns>
        public static Point3dList FstStep(Point3dList pointsInAreas, double radius)
        {
            Point3dList fstPoints = new Point3dList();
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
        /// 根据半径大小改变布点的密集程度
        /// </summary>
        /// <param name="radius">设备覆盖半径</param>
        /// <returns>两点相聚距离</returns>
        public static double AdaptRadius(double radius)
        {
            double adaptRadius = 1 + (radius - 3600) / 10000;// 11500;
            if (adaptRadius < 0.5) return 0.5;
            else if (adaptRadius > 1.8) return 1.8;
            else return adaptRadius;
        }

        /// <summary>
        /// 获取区域列表中所有可以布置的点
        /// </summary>
        /// <param name="areas">可布置区域列表</param>
        /// <param name="radius">设备覆盖半径</param>
        /// <returns>返回可布置点集</returns>
        public static Point3dList PointsInAreas(List<Polyline> areas, double radius)
        {
            Point3dList pointsInAreas = new Point3dList();
            foreach (Polyline poly in areas)
            {
                Point3dList areaPoints = PointsInArea(poly, radius);
                foreach (var pt in areaPoints)
                {
                    pointsInAreas.Add(pt);
                }
            }
            return pointsInAreas;
        }

        /// <summary>
        /// 获取多边形上的所有点------------var points = area.GetPoints().ToList();可用代替(目前不要代替，会出bug)
        /// </summary>
        /// <param name="area">多边形</param>
        /// <returns>返回多边形上的点</returns>
        public static Point3dList PointsOnPolyline(Polyline area)
        {
            Point3dList ans = new Point3dList();
            //area.VerticesEx(100.0);
            int n = area.NumberOfVertices;
            for (int i = 0; i < n; ++i)
            {
                ans.Add(area.GetPoint3dAt(i));
                //ShowPointAsX(area.GetPoint3dAt(i));
            }
            ans.Distinct();
            return ans;
        }

        /// <summary>
        /// 获取范围内可能放置设备的点
        /// </summary>
        /// <param name="area">多边形</param>
        /// <returns>返回点集（中心点、4个三等分点（左右和上下），长宽中最大值的2个四等分点（上下或左右））</returns>
        public static Point3dList PointsInArea(Polyline area, double radius)
        {
            Point3dList pts = PointsOnPolyline(area.CalObb());
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

            Point3dList ans = new Point3dList();
            //加入中心点
            ans.Add(new Point3d((pt0.X + pt1.X + pt2.X + pt3.X) / 4, (pt0.Y + pt1.Y + pt2.Y + pt3.Y) / 4, 0));
            //加入三等分点
            if (disY > radius * 0.5)
            {
                ans.Add(new Point3d((pt0_5.X + 2 * pt2_5.X) / 3, (pt0_5.Y + 2 * pt2_5.Y) / 3, 0));
                ans.Add(new Point3d((2 * pt0_5.X + pt2_5.X) / 3, (2 * pt0_5.Y + pt2_5.Y) / 3, 0));
            }
            if (disX > radius * 0.5)
            {
                ans.Add(new Point3d((pt1_5.X + 2 * pt3_5.X) / 3, (pt1_5.Y + 2 * pt3_5.Y) / 3, 0));
                ans.Add(new Point3d((2 * pt1_5.X + pt3_5.X) / 3, (2 * pt1_5.Y + pt3_5.Y) / 3, 0));
            }
            //加入四等分点
            if (disX > radius)
            {

                ans.Add(new Point3d((pt1_5.X + 3 * pt3_5.X) / 4, (pt1_5.Y + 3 * pt3_5.Y) / 4, 0));
                ans.Add(new Point3d((3 * pt1_5.X + pt3_5.X) / 4, (3 * pt1_5.Y + pt3_5.Y) / 4, 0));
                if (disX > radius * 1.5)//加入八等分点
                {
                    ans.Add(new Point3d((pt1_5.X + 7 * pt3_5.X) / 8, (pt1_5.Y + 7 * pt3_5.Y) / 8, 0));
                    ans.Add(new Point3d((7 * pt1_5.X + pt3_5.X) / 8, (7 * pt1_5.Y + pt3_5.Y) / 8, 0));
                }
            }
            if (disY > radius)
            {
                ans.Add(new Point3d((pt0_5.X + 3 * pt2_5.X) / 4, (pt0_5.Y + 3 * pt2_5.Y) / 4, 0));
                ans.Add(new Point3d((3 * pt0_5.X + pt2_5.X) / 4, (3 * pt0_5.Y + pt2_5.Y) / 4, 0));
                if (disY > radius * 1.5)//加入八等分点
                {
                    ans.Add(new Point3d((pt0_5.X + 7 * pt2_5.X) / 8, (pt0_5.Y + 7 * pt2_5.Y) / 8, 0));
                    ans.Add(new Point3d((7 * pt0_5.X + pt2_5.X) / 8, (7 * pt0_5.Y + pt2_5.Y) / 8, 0));
                }
            }
            return ans;
        }

        /// <summary>
        /// 以radious为分割巨大区域为矩阵点
        /// </summary>
        /// <param name="uncoverArea"></param>
        /// <param name="radious">两点相聚</param>
        /// <returns></returns>
        public static Point3dList PointsInUncoverArea(Entity uncoverArea, double dis)
        {
            Point3dList ptsInUncoverRectangle = new Point3dList();
            Point3dList pts = PointsOnPolyline(((Polyline)uncoverArea).CalObb());
            Point3d pt0 = pts[0];
            Point3d pt01 = CenterOfTwoPoints(pts[0], pts[1]);
            Point3d pt1 = pts[1];
            //Point3d pt12 = CenterOfTwoPoints(pts[1], pts[2]);
            Point3d pt2 = pts[2];
            Point3d pt23 = CenterOfTwoPoints(pts[2], pts[3]);
            Point3d pt3 = pts[3];
            //Point3d pt30 = CenterOfTwoPoints(pts[3], pts[0]);
            double disX = pt0.DistanceTo(pt1);
            double disY = pt1.DistanceTo(pt2);
            int Xcnt = (int)(disX / dis);
            int Ycnt = (int)(disY / dis);
            for(int i = Xcnt % 2 + 1; i <= Xcnt; i += 2)
            {
                Point3d ptA = new Point3d((i * pt01.X + (Xcnt - i) * pt0.X) / Xcnt, (i * pt01.Y + (Xcnt - i) * pt0.Y) / Xcnt, 0);//Apt01_0
                Point3d ptB = new Point3d((i * pt01.X + (Xcnt - i) * pt1.X) / Xcnt, (i * pt01.Y + (Xcnt - i) * pt1.Y) / Xcnt, 0);//Bpt01_1
                Point3d ptD = new Point3d((i * pt23.X + (Xcnt - i) * pt3.X) / Xcnt, (i * pt23.Y + (Xcnt - i) * pt3.Y) / Xcnt, 0);//Dpt23_3
                Point3d ptC = new Point3d((i * pt23.X + (Xcnt - i) * pt2.X) / Xcnt, (i * pt23.Y + (Xcnt - i) * pt2.Y) / Xcnt, 0);//Cpt23_2
                Point3d ptAD = CenterOfTwoPoints(ptA, ptD);//AD中点
                Point3d ptBC = CenterOfTwoPoints(ptB, ptC);//BC中点
                for (int j = Ycnt % 2 + 1; j <= Ycnt; j += 2)
                {
                    ptsInUncoverRectangle.Add(new Point3d((j * ptAD.X + (Ycnt - j) * ptA.X) / Ycnt, (j * ptAD.Y + (Ycnt - j) * ptA.Y) / Ycnt, 0));//ptAD_A
                    ptsInUncoverRectangle.Add(new Point3d((j * ptAD.X + (Ycnt - j) * ptD.X) / Ycnt, (j * ptAD.Y + (Ycnt - j) * ptD.Y) / Ycnt, 0));//ptAD_D
                    ptsInUncoverRectangle.Add(new Point3d((j * ptBC.X + (Ycnt - j) * ptB.X) / Ycnt, (j * ptBC.Y + (Ycnt - j) * ptB.Y) / Ycnt, 0));//ptBC_B
                    ptsInUncoverRectangle.Add(new Point3d((j * ptBC.X + (Ycnt - j) * ptC.X) / Ycnt, (j * ptBC.Y + (Ycnt - j) * ptC.Y) / Ycnt, 0));//ptBC_C
                }
            }
            ptsInUncoverRectangle.Distinct();
            /*
            //只将在覆盖区域中的点加入ans（但没必要且会增加时间开销）
            Point3dList ptsInUncoverArea = new Point3dList();
            foreach (Point3d pt in ptsInUncoverRectangle)
            {
                //if (pt在多边形内部)
                {
                    ptsInUncoverArea.Add(pt);
                }
            }
            return ptsInUncoverArea;
            */
            return ptsInUncoverRectangle;
        }

        /// <summary>
        /// 获取点集points中距离中心点半径为radius范围内最近的n个点(有序)
        /// </summary>
        /// <param name="center">中心点</param>
        /// <param name="points">可选的点</param>
        /// <param name="n">要获取点的数量</param>
        /// <param name="radius">探查半径（一般为设备覆盖半径的两倍）</param>
        /// <returns>返回点集</returns>
        public static Point3dList GetNearestNPoints(Point3d center, Point3dList points, int n, double radius)
        {
            Point3dList ans = new Point3dList();
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
        public static Point3dList GetNearestPoints(Point3d center, Point3dList points, double radius)
        {
            Point3dList ans = new Point3dList();
            foreach (Point3d pt in points)
            {
                if (center.DistanceTo(pt) < radius)
                {
                    ans.Add(pt);
                }
            }
            return ans;
        }

        /// <summary>
        /// 再点集中寻找距中心位置最近的那个点
        /// </summary>
        /// <param name="center"></param>
        /// <param name="points"></param>
        /// <returns></returns>
        public static Point3d GetNearestPoint(Point3d center, Point3dList points)
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

        /// <summary>
        /// 显示设备所能探测的范围
        /// </summary>
        /// <param name="pt">设备位置</param>
        /// <param name="radius">设备可覆盖半径</param>
        public static void ShowArea(Point3d pt, double radius, int colorIndex = 90)
        {
            Circle circle = new Circle(pt, Vector3d.ZAxis, radius);
            circle.ColorIndex = colorIndex;
            HostApplicationServices.WorkingDatabase.AddToModelSpace(circle);
        }

        /// <summary>
        /// 用O显示一个点
        /// </summary>
        /// <param name="pt">点的位置</param>
        public static void ShowPointAsO(Point3d pt, int colorIndex = 80)
        {
            Circle circle = new Circle(pt, Vector3d.ZAxis, 141.59265);
            circle.ColorIndex = colorIndex;
            HostApplicationServices.WorkingDatabase.AddToModelSpace(circle);
        }

        /// <summary>
        /// 用X显示一个点
        /// </summary>
        /// <param name="pt">点的位置</param>
        public static void ShowPointAsX(Point3d pt, int colorIndex = 80)
        {
            Point3d p1 = new Point3d(pt.X - 100, pt.Y - 100, 0);
            Point3d p2 = new Point3d(pt.X - 100, pt.Y + 100, 0);
            Point3d p3 = new Point3d(pt.X + 100, pt.Y + 100, 0);
            Point3d p4 = new Point3d(pt.X + 100, pt.Y - 100, 0);

            Line line1 = new Line(p1, p3);
            line1.ColorIndex = colorIndex;
            Line line2 = new Line(p2, p4);
            line2.ColorIndex = colorIndex;
            HostApplicationServices.WorkingDatabase.AddToModelSpace(line1, line2);
        }

        /// <summary>
        /// 用带有方向的箭头表示一个布置设备的方向
        /// </summary>
        /// <param name="pt">设备布置位置</param>
        /// <param name="vector">方向</param>
        /// <param name="colorIndex">颜色</param>
        public static void ShowPointWithDirection(Point3d pt, Vector3d vector, int colorIndex = 210)
        {
            Point3d pt1 = new Point3d(pt.X - 100, pt.Y - 100, 0);
            Point3d pt2 = new Point3d(pt.X + 100, pt.Y - 100, 0);
            Point3d pt3 = new Point3d(pt.X, pt.Y + 100, 0);
            Polyline polyline = new Polyline();
            //polyline.AddVertexAt()
            //Line line1 = new Line(p1, p3);
            vector.GetNormal();//单位化

        }
        private static List<Polyline> getPoly(Polyline frame, string sLayer, ThMEPOriginTransformer transformer, bool onlyContains)
        {
            var layoutArea = ExtractPolyline(frame, sLayer, transformer, onlyContains);
            var layoutList = layoutArea.Select(x => x.Value).ToList();
            return layoutList;
        }
        private static Dictionary<Polyline, Polyline> ExtractPolyline(Polyline bufferFrame, string LayerName, ThMEPOriginTransformer transformer, bool onlyContain)
        {
            var objs = new DBObjectCollection();
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var line = acadDatabase.ModelSpace
                      .OfType<Polyline>()
                      .Where(o => o.Layer == LayerName);

                List<Polyline> lineList = line.Select(x => x.WashClone() as Polyline).ToList();

                var plInFrame = new Dictionary<Polyline, Polyline>();

                foreach (Polyline pl in lineList)
                {
                    if (pl != null)
                    {
                        var plTrans = pl.Clone() as Polyline;

                        transformer.Transform(plTrans);
                        plInFrame.Add(pl, plTrans);
                    }
                }
                if (onlyContain == false)
                {
                    plInFrame = plInFrame.Where(o => bufferFrame.Contains(o.Value) || bufferFrame.Intersects(o.Value)).ToDictionary(x => x.Key, x => x.Value);
                }
                else
                {
                    plInFrame = plInFrame.Where(o => bufferFrame.Contains(o.Value)).ToDictionary(x => x.Key, x => x.Value);
                }
                return plInFrame;
            }
        }
        private static Polyline processFrame(Polyline frame, ThMEPOriginTransformer transformer)
        {
            var tol = 1000;
            //获取外包框
            var frameClone = frame.WashClone() as Polyline;
            //处理外包框
            transformer.Transform(frameClone);
            Polyline nFrame = ThMEPFrameService.NormalizeEx(frameClone, tol);

            return nFrame;
        }
    }
}
