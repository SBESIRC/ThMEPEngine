using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using NetTopologySuite.Algorithm;
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
    public class SmallDetectUCSOpt
    {
        private Polygon area;//区域
        public double angle;//区域方向
        public Point3d center;//旋转中心
        private Polygon room;//房间框线
        private List<Polygon> layouts { get; set; } = new List<Polygon>();//可布置区域
        private List<Polygon> detects { get; set; } = new List<Polygon>();//探测区域
        public List<LineSegment> hLines { get; set; } = new List<LineSegment>();//横线，自上向下排序
        public List<LineSegment> vLines { get; set; } = new List<LineSegment>();//竖线，自左向右排序
        private List<Coordinate> Positions { get; set; } = new List<Coordinate>();//交点位置
        private List<Coordinate> boundsPoints { get; set; } = new List<Coordinate>();//边界补点
        public List<Coordinate> PlacePoints { get; set; } = new List<Coordinate>();//布点位置
        public List<Coordinate> PlaceBoundPoints { get; set; } = new List<Coordinate>();//布点位置

        private List<double> column_xs { get; set; } = new List<double>();
        private List<double> column_ys { get; set; } = new List<double>();
        private List<double> x_mark { get; set; } = new List<double>();
        private List<double> y_mark { get; set; } = new List<double>();

        private ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex;
        private double maxGap = 8201;
        private double bufferDist = 500;
        private double radius;

        private double minX;
        private double minY;
        private double maxX;
        private double maxY;

        public SmallDetectUCSOpt(Polyline boundary, double angle, Polygon room, List<Polygon> layouts, List<Polygon> detects, double max, double radius, double buffer)
        {
            //生成区域
            var geom = OverlayNGRobust.Overlay(boundary.ToNTSPolygon(), room, SpatialFunction.Intersection);
            if (geom is Polygon polygon)
                area = polygon;
            else if (geom is GeometryCollection geometrycollection)
            {
                Polygon tmpPoly = Polygon.Empty;
                foreach (var poly in geometrycollection)
                {
                    if (poly is Polygon && poly.Area > tmpPoly.Area)
                        tmpPoly = poly as Polygon;
                }
                area = tmpPoly;
            }
            this.angle = angle;
            center = Centroid.GetCentroid(area).ToAcGePoint3d();
            //提取旋转后的可布置区域
            foreach (var layout in layouts)
            {
                Polygon tarlayout = null;
                if (area.Contains(layout))
                {
                    var dblayout = layout.ToDbMPolygon();
                    dblayout.Rotate(center, -angle);
                    tarlayout = dblayout.ToNTSPolygon();
                }
                else if (area.Intersects(layout))
                {
                    var inLayout = layout.Intersection(area).Buffer(-1) as Polygon;
                    var dblayout = inLayout.ToDbMPolygon();
                    dblayout.Rotate(center, -angle);
                    tarlayout = dblayout.ToNTSPolygon();
                }
                if (tarlayout != null)
                    this.layouts.Add(tarlayout);
            }
            //提取旋转后的探测区域
            foreach (var detect in detects)
            {
                Polygon tardetect = null;
                if (area.Contains(detect))
                {
                    var dblayout = detect.ToDbMPolygon();
                    dblayout.Rotate(center, -angle);
                    tardetect = dblayout.ToNTSPolygon();
                }
                else if (area.Intersects(detect))
                {
                    var inLayout = detect.Intersection(area).Buffer(-1);
                    if (inLayout.Area < 1) continue;
                    if (inLayout is Polygon)
                    {
                        var dblayout = (inLayout as Polygon).ToDbMPolygon();
                        dblayout.Rotate(center, -angle);
                        tardetect = dblayout.ToNTSPolygon();
                    }
                    else if (inLayout is MultiPolygon multi)
                    {
                        var dblayout = (multi.OrderByDescending(o => o.Area).First() as Polygon).ToDbMPolygon();
                        dblayout.Rotate(center, -angle);
                        tardetect = dblayout.ToNTSPolygon();
                    }
                }
                if (tardetect != null)
                    this.detects.Add(tardetect);
            }
            //旋转区域
            var dbarea = area.ToDbMPolygon();
            dbarea.Rotate(center, -angle);
            area = dbarea.ToNTSPolygon();
            //旋转房间
            var dbroom = room.ToDbMPolygon();
            dbroom.Rotate(center, -angle);
            this.room = dbroom.ToNTSPolygon();
            //建立关于可布置区域的索引
            DBObjectCollection dBObjectCollection = new DBObjectCollection();
            foreach (var layout in this.layouts)
                dBObjectCollection.Add(layout.ToDbMPolygon());
            thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(dBObjectCollection);
            //设置参数
            maxGap = max;
            bufferDist = buffer;
            this.radius = radius;
        }
        public void Calculate()
        {
            InitPoints();
            InitXYs();
            InitXYSegments();
            AdjustPoints();
            foreach (var point in Positions)
            {
                var p = point.ToAcGePoint3d();
                p = p.RotateBy(angle, Vector3d.ZAxis, center);
                var NTSp = new Coordinate(p.X, p.Y);
                PlacePoints.Add(NTSp);
            }
            foreach (var boundPoint in boundsPoints)
            {
                var p = boundPoint.ToAcGePoint3d();
                p = p.RotateBy(angle, Vector3d.ZAxis, center);
                var NTSp = new Coordinate(p.X, p.Y);
                PlaceBoundPoints.Add(NTSp);
            }
        }
        //生成初始点
        private void InitPoints()
        {
            detects.OrderByDescending(o => o.Area).ToList();
            foreach (var detect in detects)
            {
                if (detect.IsEmpty)
                    continue;
                var nearLayouts = GetInnerLayouts(detect);
                if (nearLayouts.Count == 0)
                    continue;
                //如果探测区域太大，单独作为一个区域处理
                if (detect.Area / room.Area > 0.8 || nearLayouts.Count > 10)
                {
                    NormalUCSOpt testUCS = new NormalUCSOpt(detect.Shell.ToDbPolyline(), 0, room, layouts, maxGap, radius, bufferDist);
                    testUCS.Calculate();
                    foreach (var p in testUCS.PlacePoints)
                        Positions.Add(p);
                    foreach (var p in testUCS.PlaceBoundPoints)
                        boundsPoints.Add(p);
                }
                //小的探测区域
                var minRect = detect.EnvelopeInternal;
                List<double> x = new List<double>();
                List<double> y = new List<double>();
                if (minRect.Width < maxGap + 500)
                    x.Add(minRect.Centre.X);
                else
                {
                    x.Add(minRect.Centre.X - 0.3 * minRect.Width);
                    x.Add(minRect.Centre.X + 0.3 * minRect.Width);
                }
                double y_maxGap = Math.Sqrt(4 * radius * radius - Math.Pow(minRect.Width, 2));
                if (x.Count > 1)
                    y_maxGap = Math.Sqrt(4 * radius * radius - Math.Pow(x[1] - x[0], 2));
                if (minRect.Height < y_maxGap + 500)
                    y.Add(minRect.Centre.Y);
                else
                {
                    y.Add(minRect.Centre.Y - 0.3 * minRect.Height);
                    y.Add(minRect.Centre.Y + 0.3 * minRect.Height);
                }

                if (nearLayouts.Count == 1)
                {
                    var minRectLayout = nearLayouts[0].EnvelopeInternal;
                    if (Math.Pow(minRectLayout.Width, 2) + Math.Pow(minRectLayout.Height, 2) < 4 * radius * radius)
                    {
                        Positions.Add(FireAlarmUtils.AdjustedCenterPoint(nearLayouts[0]));
                        continue;
                    }
                }
                foreach (var xpos in x)
                {
                    foreach (var ypos in y)
                    {
                        var current = new Coordinate(xpos, ypos);
                        if (FireAlarmUtils.PolygonContainPoint(detect, current))
                        {
                            var target = FindTargetPoint(current, nearLayouts, bufferDist);
                            Positions.Add(target);
                        }
                    }
                }
            }
        }
        //获取点的xy信息
        private void InitXYs()
        {
            foreach (var point in Positions)
            {
                column_xs.Add(point.X);
                column_ys.Add(point.Y);
            }
        }
        //获取网格线
        private void InitXYSegments()
        {
            //计算UCS区域的边缘
            var minRect = area.EnvelopeInternal;
            minX = minRect.MinX;
            minY = minRect.MinY;
            maxX = minRect.MaxX;
            maxY = minRect.MaxY;

            column_xs.Sort();
            column_ys.Sort();

            x_mark = FireAlarmUtils.GetMask(column_xs, 500);
            y_mark = FireAlarmUtils.GetMask(column_ys, 500);

            foreach (var x in x_mark)
            {
                if (x > minX && x < maxX)
                    vLines.Add(new LineSegment(x, minY, x, maxY));
            }
            foreach (var y in y_mark)
            {
                if (y > minY && y < maxY)
                    hLines.Add(new LineSegment(minX, y, maxX, y));
            }
        }
        //调整点位
        private void AdjustPoints()
        {
            for (int i = 0; i < Positions.Count; i++)
            {
                var cur = Positions[i];
                //计算包含该点的可布置区域
                var min = new Point3d(cur.X - 50, cur.Y - 50, 0);
                var max = new Point3d(cur.X + 50, cur.Y + 50, 0);
                var layout = thCADCoreNTSSpatialIndex.SelectCrossingWindow(min, max).Cast<MPolygon>().First().ToNTSPolygon();
                //获取附近的目标点
                double nearX = 0, nearY = 0;
                if (x_mark.Count > 0)
                    nearX = x_mark.OrderBy(o => Math.Abs(o - cur.X)).First();
                if (y_mark.Count > 0)
                    nearY = y_mark.OrderBy(o => Math.Abs(o - cur.Y)).First();
                var res = new List<Coordinate>();
                if (Math.Abs(nearX - cur.X) < 1000 && Math.Abs(nearY - cur.Y) < 1000)
                    res.Add(new Coordinate(nearX, nearY));
                if (Math.Abs(nearX - cur.X) < 1000)
                    res.Add(new Coordinate(nearX, cur.Y));
                if (Math.Abs(nearY - cur.Y) < 1000)
                    res.Add(new Coordinate(cur.X, nearY));
                //如果某个目标点在可布置区域内，那么移至该目标点
                foreach (var targetPoint in res)
                {
                    if (FireAlarmUtils.PolygonContainPoint(layout, targetPoint))
                    {
                        Positions[i] = targetPoint;
                        break;
                    }
                }
            }
        }

        //寻找detect内部的可布置区域
        private List<Polygon> GetInnerLayouts(Polygon detect)
        {
            var minRect = detect.EnvelopeInternal;
            var min = new Point3d(minRect.MinX, minRect.MinY, 0);
            var max = new Point3d(minRect.MaxX, minRect.MaxY, 0);
            var dblayouts = thCADCoreNTSSpatialIndex.SelectCrossingWindow(min, max).Cast<MPolygon>().ToList();
            var polygon_layouts = new List<Polygon>();
            foreach (var layout in dblayouts)
            {
                var polygon_layout = layout.ToNTSPolygon();
                if (detect.Contains(polygon_layout))
                    polygon_layouts.Add(polygon_layout);
            }
            return polygon_layouts;
        }
        //计算目标点
        private Coordinate FindTargetPoint(Coordinate point, List<Polygon> nearLayouts, double buffer)
        {
            var res = new List<Coordinate>();
            res.Add(FindNearestPointWithBuffer(point, buffer, nearLayouts));
            res.Add(FindNearestPointOnHLineWithBuffer(point, buffer, nearLayouts));
            res.Add(FindNearestPointOnVLineWithBuffer(point, buffer, nearLayouts));
            return res.OrderBy(o => o.Distance(point)).First();
        }
        //计算距离point最近的可布置区域内的点，带buffer
        private Coordinate FindNearestPointWithBuffer(Coordinate point, double buffer, List<Polygon> nearLayouts)
        {
            if (FireAlarmUtils.MultiPolygonContainPoint(nearLayouts, point))
                return point;
            Coordinate target = new Coordinate(point.X + radius, point.Y + radius);
            foreach (var polygon in nearLayouts)
            {
                Coordinate temp = target;

                //多边形内缩buffer
                var bufferPoly = polygon.Buffer(-buffer);
                //如果缩不了，那么取中心点
                if (bufferPoly.IsEmpty)
                    temp = Centroid.GetCentroid(polygon);
                else if (bufferPoly is Polygon polygon1)
                    temp = FireAlarmUtils.GetClosePointOnPolygon(polygon1, point);
                else if (bufferPoly is MultiPolygon multiPolygon)
                    temp = FireAlarmUtils.GetClosePointOnMultiPolygon(multiPolygon, point);

                if (temp.Distance(point) < target.Distance(point))
                    target = temp;
            }
            return target;
        }
        //计算横线上距离point最近的可布置区域内的点，带buffer
        private Coordinate FindNearestPointOnHLineWithBuffer(Coordinate point, double buffer, List<Polygon> nearLayouts)
        {
            List<Coordinate> possible_points = new List<Coordinate>();
            //点所在横线
            var left = new Coordinate(point.X - radius, point.Y);
            var right = new Coordinate(point.X + radius, point.Y);
            var hline = new LineSegment(left, right);
            //与可布置区域的交集
            var intersectLine = FireAlarmUtils.LineIntersectWithMutiPolygon(hline, nearLayouts);
            //没有交集，返回失败
            if (intersectLine.Count == 0)
                return new Coordinate(point.X + radius, point.Y + radius);
            //有交集，找目标点
            foreach (var seg in intersectLine)
            {
                Coordinate pos = new Coordinate();
                if (seg.Length > buffer * 2)//线长大于2*buffer，两端内缩buffer取较近点
                {
                    var p0 = seg.P0.Distance(point) < seg.P1.Distance(point) ? seg.P0 : seg.P1;
                    var p1 = p0 == seg.P0 ? seg.P1 : seg.P0;
                    pos.Y = point.Y;
                    pos.X = p1.X > p0.X ? p0.X + buffer : p0.X - buffer;
                }
                else pos = seg.MidPoint;//线长小于2*buffer，取中点
                possible_points.Add(pos);
            }
            if (possible_points.Count == 0) return new Coordinate(point.X + radius, point.Y + radius);
            return possible_points.OrderBy(o => o.Distance(point)).First();
        }
        //计算竖线上距离point最近的可布置区域内的点，带buffer
        private Coordinate FindNearestPointOnVLineWithBuffer(Coordinate point, double buffer, List<Polygon> nearLayouts)
        {
            List<Coordinate> possible_points = new List<Coordinate>();
            //点所在竖线
            var top = new Coordinate(point.X, point.Y + radius);
            var bottom = new Coordinate(point.X, point.Y - radius);
            var vline = new LineSegment(bottom, top);
            //与可布置区域的交集
            var intersectLine = FireAlarmUtils.LineIntersectWithMutiPolygon(vline, nearLayouts);
            //没有交集，返回失败
            if (intersectLine.Count == 0)
                return new Coordinate(point.X + radius, point.Y + radius);
            //有交集，先找目标点
            foreach (var seg in intersectLine)
            {
                Coordinate pos = new Coordinate();
                if (seg.Length > 2 * buffer) //线长大于2*buffer，两端内缩buffer取较近点
                {
                    var p0 = seg.P0.Distance(point) < seg.P1.Distance(point) ? seg.P0 : seg.P1;
                    var p1 = p0 == seg.P0 ? seg.P1 : seg.P0;
                    pos.X = point.X;
                    pos.Y = p1.Y > p0.Y ? p0.Y + buffer : p0.Y - buffer;
                }
                else pos = seg.MidPoint;//线长小于2*buffer，取中点
                possible_points.Add(pos);
            }
            if (possible_points.Count == 0) return new Coordinate(point.X + radius, point.Y + radius);
            return possible_points.OrderBy(o => o.Distance(point)).First();
        }


    }
}