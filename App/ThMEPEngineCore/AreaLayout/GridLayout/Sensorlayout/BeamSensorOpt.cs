using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Algorithm.Distance;
using NetTopologySuite.Algorithm.Locate;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Overlay;
using NetTopologySuite.Operation.OverlayNG;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.AreaLayout.GridLayout.Data;
using ThMEPEngineCore.AreaLayout.GridLayout.Method;

namespace ThMEPEngineCore.AreaLayout.GridLayout.Sensorlayout
{
    public class BeamSensorOpt : AlarmSensorLayout
    {
        public List<SmallDetectUCSOpt> underUCSs { get; set; } = new List<SmallDetectUCSOpt>();//地下UCS列表
        public List<LargeDetectUCSOpt> upUCSs { get; set; } = new List<LargeDetectUCSOpt>();//地上UCS列表
        public List<NormalUCSOpt> testUCSs { get; set; } = new List<NormalUCSOpt>();//地上UCS列表
        public List<Coordinate> Positions { get; set; } = new List<Coordinate>();//交点位置
        public List<Polygon> Detect { get; set; } = new List<Polygon>();//每个点的探测范围
        public List<Geometry> Resp { get; set; } = new List<Geometry>();//每个点单独负责的范围

        public List<LineSegment> lines { get; set; } = new List<LineSegment>();//网格线
        public Geometry EmptyDetect { get; set; }//没有可布置区域的探测区域

        private ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndexLayout;
        private ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndexDetect;
        private double bufferDist = 500;
        private double boundBufferArea { get; set; } = 500000;//边界盲区
        private double innerBufferArea { get; set; } = 30000;//内部盲区
        private Geometry blind;
        public bool IsUpOrUnder;//地上图纸还是地下图纸
        private int innerPointCount;

        public BeamSensorOpt(InputArea inputArea, EquipmentParameter parameter)
            : base(inputArea, parameter)
        {
            //建立关于可布置区域的索引
            DBObjectCollection dBObjectCollection = new DBObjectCollection();
            foreach (var layout in layouts)
                dBObjectCollection.Add(layout.ToDbMPolygon());
            thCADCoreNTSSpatialIndexLayout = new ThCADCoreNTSSpatialIndex(dBObjectCollection);
            //建立关于探测区域的索引
            dBObjectCollection.Clear();
            foreach (var detect in detects)
                dBObjectCollection.Add(detect.ToDbMPolygon());
            thCADCoreNTSSpatialIndexDetect = new ThCADCoreNTSSpatialIndex(dBObjectCollection);
            //查找没有可布置区域的探测区域
            var emptyDetect = new List<Polygon>();
            foreach (var detect in detects)
            {
                var minRect = detect.EnvelopeInternal;
                var min = new Point3d(minRect.MinX, minRect.MinY, 0);
                var max = new Point3d(minRect.MaxX, minRect.MaxY, 0);
                var dblayouts = thCADCoreNTSSpatialIndexLayout.SelectCrossingWindow(min, max).Cast<MPolygon>().ToList();
                bool flag = false;
                foreach (var layout in dblayouts)
                {
                    var polygon_layout = layout.ToNTSPolygon();
                    if (detect.Contains(polygon_layout))
                    {
                        flag = true;
                        break;
                    }
                }
                if (!flag)
                    emptyDetect.Add(detect);
            }
            EmptyDetect = OverlayNGRobust.Union(emptyDetect.ToArray());

            //IsUpOrUnder = CheckUpOrUnder();
            if (detects.Count == 0)
            {
                foreach (var record in UCS)
                    testUCSs.Add(new NormalUCSOpt(record.Key, record.Value, room, layouts, MaxGap, Radius, bufferDist));
            }
            else if (detects[0].Area / room.Area > 0.8)
            {
                foreach (var record in UCS)
                    upUCSs.Add(new LargeDetectUCSOpt(record.Key, record.Value, room, layouts, detects, MaxGap, Radius, bufferDist));
            }
            else
            {
                foreach (var record in UCS)
                    underUCSs.Add(new SmallDetectUCSOpt(record.Key, record.Value, room, layouts, detects, MaxGap, Radius, bufferDist));
            }
        }

        public override void Calculate()
        {
            List<Coordinate> innerPoints = new List<Coordinate>();
            List<Coordinate> boundPoints = new List<Coordinate>();
            if (testUCSs.Count > 0)
            {
                foreach (var testUCS in testUCSs)
                {
                    testUCS.Calculate();
                    foreach (var p in testUCS.PlacePoints)
                        innerPoints.Add(p);
                    foreach (var p in testUCS.PlaceBoundPoints)
                        boundPoints.Add(p);
                    foreach (var hline in testUCS.hLines)
                    {
                        var line = hline.ToDbLine();
                        line.Rotate(testUCS.center, testUCS.angle);
                        lines.Add(line.ToNTSLineSegment());
                        line.Dispose();
                    }
                    foreach (var vline in testUCS.vLines)
                    {
                        var line = vline.ToDbLine();
                        line.Rotate(testUCS.center, testUCS.angle);
                        lines.Add(line.ToNTSLineSegment());
                        line.Dispose();
                    }
                }
            }
            else if (upUCSs.Count > 0)
            {
                foreach (var upUCS in upUCSs)
                {
                    upUCS.Calculate();
                    foreach (var p in upUCS.PlacePoints)
                        innerPoints.Add(p);
                    foreach (var p in upUCS.PlaceBoundPoints)
                        boundPoints.Add(p);
                    foreach (var hline in upUCS.hLines)
                    {
                        var line = hline.ToDbLine();
                        line.Rotate(upUCS.center, upUCS.angle);
                        lines.Add(line.ToNTSLineSegment());
                        line.Dispose();
                    }
                    foreach (var vline in upUCS.vLines)
                    {
                        var line = vline.ToDbLine();
                        line.Rotate(upUCS.center, upUCS.angle);
                        lines.Add(line.ToNTSLineSegment());
                        line.Dispose();
                    }
                }
            }
            else
            {
                foreach (var underUCS in underUCSs)
                {
                    underUCS.Calculate();
                    foreach (var p in underUCS.PlacePoints)
                        innerPoints.Add(p);
                    foreach (var p in underUCS.PlaceBoundPoints)
                        boundPoints.Add(p);
                    foreach (var hline in underUCS.hLines)
                    {
                        var line = hline.ToDbLine();
                        line.Rotate(underUCS.center, underUCS.angle);
                        lines.Add(line.ToNTSLineSegment());
                        line.Dispose();
                    }
                    foreach (var vline in underUCS.vLines)
                    {
                        var line = vline.ToDbLine();
                        line.Rotate(underUCS.center, underUCS.angle);
                        lines.Add(line.ToNTSLineSegment());
                        line.Dispose();
                    }
                }
            }

            foreach (var p in innerPoints)
                Positions.Add(p);
            foreach (var p in boundPoints)
                Positions.Add(p);
            innerPointCount = innerPoints.Count;

            //计算每个点的覆盖区域
            CalDetectArea();
            //CalResponsibleArea();
            //计算盲区
            CalBlindArea();
            //加点
            AddPoints();
            //删点
            DeletePoints();
            //调整点位
            AdjustPoints();
            //重新计算盲区
            CalDetectArea();
            CalBlindArea();
            //转化点位
            ConvertPoints();
            //转化盲区
            ConvertBlind();
        }

        //判断是地上图纸还是地下图纸
        private bool CheckUpOrUnder()
        {
            double AverageLayoutArea = layouts.Sum(o => o.Area) / layouts.Count;//可布置区域平均面积

            int squareNum = 0;
            foreach (var layout in layouts)
            {
                var MinRect = layout.Shell.ToDbPolyline().OBB();
                double length = MinRect.GetLineSegmentAt(0).Length;
                double width = MinRect.GetLineSegmentAt(1).Length;
                var l_w = Math.Abs(length / width);
                if (l_w > 0.5 && l_w < 2)
                    squareNum++;
            }
            double SquareProportion = 1.0 * squareNum / layouts.Count;

            return (AverageLayoutArea > 1e7 && SquareProportion > 0.5 && layouts.Count > 5 && columnCenters.Count > 10);
        }

        //计算探测范围
        public void CalDetectArea()
        {
            Detect.Clear();
            foreach (var p in Positions)
            {
                if (detects.Count > 0)
                {
                    var detect = GetDetect(p);
                    Detect.Add(DetectCalculator.CalculateDetect(p, detect, Radius, IsDetectVisible));
                }
                else
                    Detect.Add(DetectCalculator.CalculateDetect(p, room, Radius, IsDetectVisible));
            }

        }
        //计算盲区
        public void CalBlindArea()
        {
            var poly = OverlayNGRobust.Union(Detect.ToArray());
            blind = OverlayNGRobust.Overlay(room, EmptyDetect, SpatialFunction.Difference);
            blind = OverlayNGRobust.Overlay(blind, poly, SpatialFunction.Difference);

        }
        //计算单独负责的区域
        public void CalResponsibleArea()
        {
            for (int i = 0; i < Positions.Count; i++)
            {
                //计算该点单独负责的区域
                var resp = Detect[i].Copy();
                var nears = FindNearPoints(i);
                var polylist = new List<Polygon>();
                foreach (var j in nears)
                    polylist.Add(Detect[j]);
                var unionpoly = OverlayNGRobust.Union(polylist.ToArray());
                Resp.Add(OverlayNGRobust.Overlay(resp, unionpoly, SpatialFunction.Difference).Buffer(-10));
            }
        }
        //加点
        public void AddPoints()
        {
            while (blind.Area > 100)
            {
                //先处理掉非polygon的元素
                if (blind is GeometryCollection geom)
                {
                    var geometryCollection = new List<Polygon>();
                    foreach (var geo in geom)
                        if (geo is Polygon polygon && polygon.Area > 10)
                        {
                            //对于边界上面积小于bufferArea的盲区，直接忽略它
                            if ((!room.Contains(polygon.Buffer(10)) && polygon.Area < boundBufferArea) || (polygon.Area < innerBufferArea))
                                continue;
                            geometryCollection.Add(polygon);
                        }
                    blind = ThCADCoreNTSService.Instance.GeometryFactory.CreateMultiPolygon(geometryCollection.ToArray());
                }
                //一次只处理一个polygon
                if (!blind.IsEmpty)
                    RemoveBlind(blind);
            }
        }
        //删点
        public void DeletePoints()
        {
            for (int i = Positions.Count - 1; i > innerPointCount; i--)
            {
                if (IsMovable(i))
                {
                    Positions.RemoveAt(i);
                    Detect.RemoveAt(i);
                }
            }
        }
        //移点
        public void AdjustPoints()
        {
            for (int i = 0; i < Positions.Count; i++)
            {
                //计算包含该点的可布置区域
                var min = new Point3d(Positions[i].X - 50, Positions[i].Y - 50, 0);
                var max = new Point3d(Positions[i].X + 50, Positions[i].Y + 50, 0);
                var layout = thCADCoreNTSSpatialIndexLayout.SelectCrossingWindow(min, max).Cast<MPolygon>().First().ToNTSPolygon();
                //计算该点单独负责的区域
                var resp = Detect[i].Copy();
                var nears = FindNearPoints(i);
                //合并两个离得很近的点
                if (nears.Count > 0)
                {
                    var nearest = nears.OrderBy(o => Positions[o].Distance(Positions[i])).First();
                    if (Positions[nearest].Distance(Positions[i]) < 1500)
                    {
                        var midPoint = new Coordinate((Positions[nearest].X + Positions[i].X) / 2, (Positions[nearest].Y + Positions[i].Y) / 2);
                        if (FireAlarmUtils.PolygonContainPoint(layout, midPoint))
                        {
                            var small_idx = i < nearest ? i : nearest;
                            var large_idx = i > nearest ? i : nearest;
                            Positions.RemoveAt(large_idx);
                            Positions.RemoveAt(small_idx);
                            Detect.RemoveAt(large_idx);
                            Detect.RemoveAt(small_idx);
                            Positions.Add(midPoint);
                            Detect.Add(DetectCalculator.CalculateDetect(midPoint, room, Radius, IsDetectVisible));
                        }
                        continue;
                    }
                }
                var polylist = new List<Polygon>();
                foreach (var j in nears)
                    polylist.Add(Detect[j]);
                var unionpoly = OverlayNGRobust.Union(polylist.ToArray());
                resp = OverlayNGRobust.Overlay(resp, unionpoly, SpatialFunction.Difference).Buffer(-10);
                //获取缩小后的可布置区域
                var smaller = layout.Buffer(-400);
                Coordinate adjustPoint = null;
                if (smaller.IsEmpty)
                {
                    var minRect = layout.Shell.ToDbPolyline().OBB();
                    var width = (minRect.GetPoint2dAt(1) - minRect.GetPoint2dAt(0)).Length;
                    var height = (minRect.GetPoint2dAt(2) - minRect.GetPoint2dAt(1)).Length;
                    //如果某个可布置区域是长条形的，且不可缩小，那么该点不需要调整
                    if (width > 800 || height > 800)
                        continue;
                    //如果某个可布置区域非常小，那么该点移动到可布置区域的几何中心
                    adjustPoint = FireAlarmUtils.AdjustedCenterPoint(layout);
                    if (DetectCalculator.CalculateDetect(adjustPoint, room, Radius, IsDetectVisible).Contains(resp))
                    {
                        Positions[i] = adjustPoint;
                        continue;
                    }
                    continue;
                }

                var locator = new SimplePointInAreaLocator(smaller);
                if (locator.Locate(Positions[i]) == Location.Interior)
                    continue;
                if (smaller is Polygon polygon)
                    adjustPoint = FireAlarmUtils.GetClosePointOnPolygon(polygon, Positions[i]);
                else if (smaller is MultiPolygon multiPolygon)
                    adjustPoint = FireAlarmUtils.GetClosePointOnMultiPolygon(multiPolygon, Positions[i]);
                var Detecti = DetectCalculator.CalculateDetect(adjustPoint, room, Radius, IsDetectVisible);
                if (Detecti.Contains(resp) || resp.Area < 1)
                {
                    Positions[i] = adjustPoint;
                    Detect[i] = Detecti;
                }
            }
        }
        //寻找附近的点
        private List<int> FindNearPoints(int index)
        {
            var nears = new List<int>();
            for (int i = 0; i < Positions.Count; i++)
            {
                if (i != index && Positions[i].Distance(Positions[index]) < 2 * Radius)
                    nears.Add(i);
            }
            return nears;
        }
        //寻找所在的探测区域
        private Polygon GetDetect(Coordinate point)
        {
            //计算包含该点的可布置区域
            var min = new Point3d(point.X - 1, point.Y - 1, 0);
            var max = new Point3d(point.X + 1, point.Y + 1, 0);

            return thCADCoreNTSSpatialIndexDetect.SelectCrossingWindow(min, max).Cast<MPolygon>().First().ToNTSPolygon();
        }
        //处理盲区
        private void RemoveBlind(NetTopologySuite.Geometries.Geometry blind)
        {
            var oldArea = blind.Area;
            Polygon targetToMove = null;
            if (blind is Polygon polygon)
                targetToMove = polygon;
            else if (blind is MultiPolygon multi)
                targetToMove = multi.First() as Polygon;
            //需要去除的区域的中心点
            var center = FireAlarmUtils.AdjustedCenterPoint(targetToMove);
            //中心点所在的探测区域
            Polygon detect = detects.Count > 0 ? GetDetect(center) : null;
            //能探测到中心点的布置区域
            Polygon vis = DetectCalculator.CalculateDetect(center, (detect != null ? detect : room), Radius, IsDetectVisible);
            //附近的可布置区域
            var minRect = vis.EnvelopeInternal;
            var min = new Point3d(minRect.MinX, minRect.MinY, 0);
            var max = new Point3d(minRect.MaxX, minRect.MaxY, 0);
            var dblayouts = thCADCoreNTSSpatialIndexLayout.SelectCrossingWindow(min, max).Cast<MPolygon>().ToList();
            var polygon_layouts = new List<Polygon>();
            foreach (var layout in dblayouts)
            {
                var polygon_layout = layout.ToNTSPolygon();
                if (detect == null || detect.Contains(polygon_layout))
                    polygon_layouts.Add(polygon_layout);
            }
            //存在无解的盲区
            if (polygon_layouts.Count == 0)
            {
                this.blind = this.blind.Difference(targetToMove);
                return;
            }
            ////能够探测到中心点的可布置区域
            //var target_layouts = OverlayNGRobust.Overlay(vis,
            //    ThCADCoreNTSService.Instance.GeometryFactory.CreateMultiPolygon(polygon_layouts.ToArray()), SpatialFunction.Intersection);
            //初始化目标点
            var target = new Coordinate(center.X + Radius, center.Y + Radius);
            ////距离盲区中心最近的四条线
            //var nearlines = lines.OrderBy(o => o.Distance(center)).Take(lines.Count > 4 ? 4 : lines.Count).ToList();
            //var targetPoints = new List<Coordinate>();
            //foreach(var line in nearlines)
            //{
            //    var geo=OverlayNGRobust.Overlay(target_layouts, line.ToDbLine().ToNTSLineString(), SpatialFunction.Intersection);
            //    if(geo is MultiLineString)
            //    {
            //        foreach(LineString line1 in geo as MultiLineString)
            //            targetPoints.Add(new Coordinate((line1[0].X + line1[1].X)/2, (line1[0].Y + line1[1].Y)/2));
            //    }
            //    else if(geo is LineString ls)
            //    {
            //        if(ls.Count>0)
            //            targetPoints.Add(new Coordinate((ls[0].X + ls[1].X)/2, (ls[0].Y + ls[1].Y)/2));
            //    }
            //}
            //if (targetPoints.Count > 0)
            //    target = targetPoints.OrderBy(o => o.Distance(center)).First();
            if (target.Distance(center) > Radius)
            {
                //优先找可布置区域的中心点
                foreach (var poly in polygon_layouts)
                {
                    Coordinate temp = FireAlarmUtils.AdjustedCenterPoint(poly);
                    if (FireAlarmUtils.PolygonContainPoint(vis, temp) && temp.Distance(center) < target.Distance(center))
                        target = temp;
                }
            }

            //找不到就找交集的中心点
            if (target.Distance(center) > Radius)
            {
                foreach (var poly in polygon_layouts)
                {
                    var intersect = poly.Intersection(vis);
                    Coordinate inter_center = null;
                    if (intersect.Area == 0) continue;
                    if (intersect is Polygon polygon1)
                        inter_center = FireAlarmUtils.AdjustedCenterPoint(polygon1);
                    else if (intersect is MultiPolygon multi)
                        inter_center = FireAlarmUtils.AdjustedCenterPoint(multi.First() as Polygon);
                    if (inter_center.Distance(center) < target.Distance(center))
                        target = inter_center;
                }
            }
            var det = DetectCalculator.CalculateDetect(target, (detect != null ? detect : room), Radius, IsDetectVisible);
            if (lines.Count > 0)
            {
                var nearline = lines.OrderBy(o => o.Distance(target)).First();
                var target1 = nearline.ClosestPoint(target);
                var det1 = DetectCalculator.CalculateDetect(target1, (detect != null ? detect : room), Radius, IsDetectVisible);
                if (FireAlarmUtils.MultiPolygonContainPoint(polygon_layouts, target1) && FireAlarmUtils.PolygonContainPoint(det1, center))
                {
                    Positions.Add(target1);
                    Detect.Add(det1);
                    this.blind = OverlayNGRobust.Overlay(this.blind, det1, SpatialFunction.Difference);
                }
                else
                {
                    Positions.Add(target);
                    Detect.Add(det);
                    this.blind = OverlayNGRobust.Overlay(this.blind, det, SpatialFunction.Difference);
                }
            }
            else
            {
                Positions.Add(target);
                Detect.Add(det);
                this.blind = OverlayNGRobust.Overlay(this.blind, det, SpatialFunction.Difference);
            }

        }
        //是否可以删除
        private bool IsMovable(int index)
        {
            var resp = Detect[index].Copy();
            var points = FindNearPoints(index);
            if (points.Count <= 0)
                return false;
            else
            {
                var nearDetects = new List<Polygon>();
                for (int i = 0; i < points.Count; i++)
                    nearDetects.Add(Detect[points[i]]);
                var poly = OverlayNGRobust.Union(nearDetects.ToArray());
                resp = OverlayNGRobust.Overlay(resp, poly, SpatialFunction.Difference);
            }

            return (!room.Contains(resp.Buffer(10)) && resp.Area < boundBufferArea) || (resp.Area < innerBufferArea);
        }
        //转化布置点集
        private void ConvertPoints()
        {
            foreach (var point in Positions)
                m_placePoints.Add(new Point3d(point.X, point.Y, 0));
        }
        //转化盲区
        private void ConvertBlind()
        {
            if (blind is Polygon polygon1)
            {
                m_blinds.Add(polygon1.Shell.ToDbPolyline());
                return;
            }

            if (blind is GeometryCollection geom)
            {
                var geometryCollection = new List<Polygon>();
                foreach (var geo in geom)
                    if (geo is Polygon polygon)
                        m_blinds.Add(polygon.Shell.ToDbPolyline());
            }
        }
    }
}
