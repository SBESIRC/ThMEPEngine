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
    public class LargeDetectUCSOpt
    {
        private Polygon area;//区域
        public double angle;//区域方向
        public Point3d center;//旋转中心
        private List<Polygon> layouts { get; set; } = new List<Polygon>();//可布置区域
        private List<Polygon> detects { get; set; } = new List<Polygon>();//探测区域
        public List<LineSegment> hLines { get; set; } = new List<LineSegment>();//横线，自上向下排序
        public List<LineSegment> vLines { get; set; } = new List<LineSegment>();//竖线，自左向右排序
        private List<List<bool>> validPoints { get; set; } = new List<List<bool>>();//交点是否有效
        private List<List<Coordinate>> Positions { get; set; } = new List<List<Coordinate>>();//交点位置
        private List<Coordinate> boundsPoints { get; set; } = new List<Coordinate>();//边界补点
        private List<bool> IsMovable { get; set; } = new List<bool>();
        private List<Coordinate> detectPoints { get; set; } = new List<Coordinate>();//探测区域补点

        public List<Coordinate> PlacePoints { get; set; } = new List<Coordinate>();//布点位置
        public List<Coordinate> PlaceBoundPoints { get; set; } = new List<Coordinate>();//布点位置

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

        public LargeDetectUCSOpt(Polyline boundary, double angle, Polygon room, List<Polygon> layouts, List<Polygon> detects, double max, double radius, double buffer)
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
                    var inLayout = OverlayNGRobust.Overlay(area, detect, SpatialFunction.Intersection).Buffer(-1);
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
            ////旋转房间
            //var dbroom = room.ToDbMPolygon();
            //dbroom.Rotate(center, -angle);
            //this.room = dbroom.ToNTSPolygon();
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
            if (layouts.Count == 0)
                return;
            InitXYSegments();
            InitPoints();
            AdjustLines();
            AddPoints();
            for (int h_index = 0; h_index < hLines.Count; h_index++)
            {
                for (int v_index = 0; v_index < vLines.Count; v_index++)
                {
                    if (validPoints[h_index][v_index])
                    {
                        var p = Positions[h_index][v_index].ToAcGePoint3d();
                        p = p.RotateBy(angle, Vector3d.ZAxis, center);
                        var NTSp = new Coordinate(p.X, p.Y);
                        PlacePoints.Add(NTSp);
                    }
                }
            }
            for (int i = 0; i < boundsPoints.Count; i++)
            {
                var p = boundsPoints[i].ToAcGePoint3d();
                p = p.RotateBy(angle, Vector3d.ZAxis, center);
                var NTSp = new Coordinate(p.X, p.Y);
                if (IsMovable[i] == true)
                    PlacePoints.Add(NTSp);
                else PlaceBoundPoints.Add(NTSp);
            }
            foreach (var detectPoint in detectPoints)
            {
                var p = detectPoint.ToAcGePoint3d();
                p = p.RotateBy(angle, Vector3d.ZAxis, center);
                var NTSp = new Coordinate(p.X, p.Y);
                PlacePoints.Add(NTSp);
            }
            //GetPoints();
            //foreach(var point in Positions)
            //{
            //    var p = point.ToAcGePoint3d();
            //    p = p.RotateBy(angle, Vector3d.ZAxis, center);
            //    var NTSp = new Coordinate(p.X, p.Y);
            //    PlacePoints.Add(NTSp);
            //}
        }
        //初步生成布置线
        private void InitXYSegments()
        {
            //计算UCS区域的边缘
            var minRect = area.EnvelopeInternal;
            minX = minRect.MinX;
            minY = minRect.MinY;
            maxX = minRect.MaxX;
            maxY = minRect.MaxY;
            if (layouts.Count() < 10)
            {
                var xNum = Math.Ceiling((maxX - minX) / maxGap);
                var yNum = Math.Ceiling((maxY - minY) / maxGap);
                var dx = (maxX - minX) / xNum;
                var dy = (maxY - minY) / yNum;
                for (double i = 0.5; i < yNum; i++)
                    hLines.Add(new LineSegment(minX, minY + dy * i, maxX, minY + dy * i));
                for (double i = 0.5; i < xNum; i++)
                    vLines.Add(new LineSegment(minX + dx * i, minY, minX + dx * i, maxY));
                hLines.Reverse();
                return;
            }
            //根据layouts获取x，y集合
            List<double> column_xs = new List<double>();
            List<double> column_ys = new List<double>();
            int hLs = 0, vLs = 0;
            double minLy = double.MaxValue, minLx = double.MaxValue, maxLy = double.MinValue, maxLx = double.MinValue;
            foreach (var layout in layouts)
            {
                minRect = layout.EnvelopeInternal;
                column_xs.Add(minRect.Centre.X);
                if (minRect.Width >= 3500)
                {
                    column_xs.Add(minRect.Centre.X - minRect.Width * 0.3);
                    column_xs.Add(minRect.Centre.X - minRect.Width * 0.1);
                    column_xs.Add(minRect.Centre.X + minRect.Width * 0.1);
                    column_xs.Add(minRect.Centre.X + minRect.Width * 0.3);
                }
                column_ys.Add(minRect.Centre.Y);
                if (minRect.Height >= 3500)
                {
                    column_ys.Add(minRect.Centre.Y - minRect.Height * 0.3);
                    column_ys.Add(minRect.Centre.Y - minRect.Height * 0.1);
                    column_ys.Add(minRect.Centre.Y + minRect.Height * 0.3);
                    column_ys.Add(minRect.Centre.Y + minRect.Height * 0.3);
                }
                if (minRect.Height > minRect.Width)
                    vLs++;
                else hLs++;

                minLy = Math.Min(minLy, minRect.MinY);
                minLx = Math.Min(minLx, minRect.MinX);
                maxLy = Math.Max(maxLy, minRect.MaxY);
                maxLx = Math.Max(maxLx, minRect.MaxX);
            }
            column_xs.Sort();
            column_ys.Sort();
            //获取归类的x，y坐标
            x_mark = FireAlarmUtils.GetMask(column_xs);
            if (x_mark.Count <= 1)
            {
                x_mark.Clear();
                var xNum = Math.Ceiling((maxX - minX) / maxGap);
                var dx = (maxX - minX) / xNum;
                for (double i = 0.5; i < xNum; i++)
                    x_mark.Add(minX + dx * i);
            }
            while (x_mark.First() > minLx)
                x_mark.Insert(0, x_mark[0] - (x_mark[1] - x_mark[0]));
            while (maxLx > x_mark.Last())
                x_mark.Add(x_mark.Last() + (x_mark.Last() - x_mark[x_mark.Count - 2]));

            y_mark = FireAlarmUtils.GetMask(column_ys);
            if (y_mark.Count <= 1)
            {
                y_mark.Clear();
                var yNum = Math.Ceiling((maxY - minY) / maxGap);
                var dy = (maxY - minY) / yNum;
                for (double i = 0.5; i < yNum; i++)
                    y_mark.Add(minY + dy * i);
            }
            while (y_mark.First() > minLy && y_mark.Count > 1)
                y_mark.Insert(0, y_mark[0] - (y_mark[1] - y_mark[0]));
            while (maxLy > y_mark.Last() && y_mark.Count > 1)
                y_mark.Add(y_mark.Last() + (y_mark.Last() - y_mark[y_mark.Count - 2]));

            //获取初始线的x,y坐标
            var hLineYset = new List<double>();
            var vLineXset = new List<double>();
            if (vLs > hLs)//可布置区域竖向排列
            {
                double x0 = minX + maxGap / 2;
                for (var index = 0; index < x_mark.Count - 1;)
                {
                    if (x_mark[index] < x0)
                    {
                        index++;
                        continue;
                    }
                    if (vLineXset.Count > 0 && x_mark[index] - vLineXset.Last() < maxGap * 1.2)
                        vLineXset.Add(x_mark[index]);
                    else if (vLineXset.Count == 0 || (vLineXset.Count > 0 && x_mark[index - 1] > vLineXset.Last()))
                    {
                        if (x_mark[index] - x0 < 700)
                            vLineXset.Add(x0);
                        else vLineXset.Add(x_mark[index - 1]);
                    }
                    else vLineXset.Add(x0);
                    x0 = vLineXset.Last() + maxGap;
                    if (maxX - x0 < 0.5 * maxGap)
                        x0 -= 0.1 * maxGap;
                }
                vLineXset.Add(x0);

                double x_maxGap = -1;
                for (int i = 0; i < vLineXset.Count - 1; i++)
                    x_maxGap = Math.Max(x_maxGap, vLineXset[i + 1] - vLineXset[i]);
                double y_maxGap = Math.Sqrt(radius * radius * 4 - x_maxGap * x_maxGap);

                double y0 = maxY - y_maxGap / 2;
                for (var index = y_mark.Count - 1; index >= 0;)
                {
                    if (y_mark[index] > y0)
                    {
                        index--;
                        continue;
                    }
                    if ((hLineYset.Count == 0 && index < y_mark.Count - 1) || (hLineYset.Count > 0 && y_mark[index + 1] < hLineYset.Last()))
                    {
                        if (y0 - y_mark[index] < 700)
                            hLineYset.Add(y0);
                        else hLineYset.Add(y_mark[index + 1]);
                    }
                    else hLineYset.Add(y0);
                    y0 = hLineYset.Last() - y_maxGap;
                    if (y0 - minY < 0.5 * y_maxGap)
                        y0 += 0.1 * y_maxGap;
                }
                hLineYset.Add(y0);
            }
            else//可布置区域横向排列
            {
                double y0 = maxY - maxGap / 2;
                for (var index = y_mark.Count - 1; index >= 0;)
                {
                    if (y_mark[index] > y0)
                    {
                        index--;
                        continue;
                    }
                    if (hLineYset.Count > 0 && hLineYset.Last() - y_mark[index] < maxGap * 1.2)
                        hLineYset.Add(y_mark[index]);
                    else if (hLineYset.Count == 0 || (hLineYset.Count > 0 && y_mark[index + 1] < hLineYset.Last()))
                    {
                        if (y0 - y_mark[index] < 700)
                            hLineYset.Add(y0);
                        else hLineYset.Add(y_mark[index + 1]);
                    }
                    else hLineYset.Add(y0);
                    y0 = hLineYset.Last() - maxGap;
                    if (y0 - minY < 0.5 * maxGap)
                        y0 += 0.1 * maxGap;
                }
                hLineYset.Add(y0);

                double y_maxGap = -1;
                for (int i = 0; i < hLineYset.Count - 2; i++)
                    y_maxGap = Math.Max(y_maxGap, hLineYset[i] - hLineYset[i + 1]);
                double x_maxGap = Math.Sqrt(radius * radius * 4 - y_maxGap * y_maxGap);

                double x0 = minX + x_maxGap / 2;
                for (var index = 0; index < x_mark.Count - 1;)
                {
                    if (x_mark[index] < x0)
                    {
                        index++;
                        continue;
                    }
                    if ((vLineXset.Count == 0 && index > 0) || (vLineXset.Count > 0 && x_mark[index - 1] > vLineXset.Last()))
                    {
                        if (x_mark[index] - x0 < 700)
                            vLineXset.Add(x0);
                        else vLineXset.Add(x_mark[index - 1]);
                    }
                    else vLineXset.Add(x0);
                    x0 = vLineXset.Last() + x_maxGap;
                    if (maxX - x0 < 0.5 * x_maxGap)
                        x0 -= 0.1 * x_maxGap;
                }
                vLineXset.Add(x0);
            }


            foreach (var x in vLineXset)
            {
                if (x > minX && x < maxX)
                    vLines.Add(new LineSegment(x, minY, x, maxY));
            }
            foreach (var y in hLineYset)
            {
                if (y > minY && y < maxY)
                    hLines.Add(new LineSegment(minX, y, maxX, y));
            }
        }
        //生成点位
        private void InitPoints()
        {
            if (hLines.Count == 1 && vLines.Count == 1)
            {
                var currentPoint = hLines[0].Intersection(vLines[0]);
                if (!FireAlarmUtils.PolygonContainPoint(area, currentPoint))
                {
                    var center = Centroid.GetCentroid(layouts[0]);
                    hLines[0].P0.Y = hLines[0].P1.Y = center.Y;
                    vLines[0].P0.X = vLines[0].P1.X = center.X;
                    Positions.Add(new List<Coordinate>());
                    Positions[0].Add(center);
                    validPoints.Add(new List<bool>());
                    validPoints[0].Add(true);
                    return;
                }
            }
            for (int i = 0; i < hLines.Count; i++)
            {
                validPoints.Add(new List<bool>());
                Positions.Add(new List<Coordinate>());
                for (int j = 0; j < vLines.Count; j++)
                {
                    var currentPoint = hLines[i].Intersection(vLines[j]);
                    validPoints[i].Add(FireAlarmUtils.PolygonContainPoint(area, currentPoint));
                    Positions[i].Add(currentPoint);
                }
            }
        }
        //调整线
        private void AdjustLines()
        {
            for (int v_index = 0; v_index < vLines.Count; v_index++)
            {
                //有左,无左,有右,无右
                var boundFlag = new List<bool>() { false, false, false, false };

                double LBx = double.MaxValue, RBx = double.MinValue;
                for (int h_index = 0; h_index < hLines.Count; h_index++)
                {
                    if (validPoints[h_index][v_index])
                    {
                        if (!HasLeft(h_index, v_index))
                        {
                            boundFlag[1] = true;
                            LBx = Math.Min(LBx, GetLeftPoint(h_index, v_index).X);
                        }
                        else boundFlag[0] = true;

                        if (!HasRight(h_index, v_index))
                        {
                            boundFlag[3] = true;
                            RBx = Math.Max(RBx, GetRightPoint(h_index, v_index).X);
                        }
                        else boundFlag[2] = true;
                    }
                }
                if (boundFlag[0] && boundFlag[1] && boundFlag[2] && boundFlag[3])
                {
                    double Lx = vLines[v_index - 1].P0.X;
                    double Rx = vLines[v_index + 1].P0.X;
                    if (Rx - LBx > maxGap * 1.5 && RBx - Lx > maxGap * 1.5)
                    {
                        double nCx1 = Math.Min(0.67 * Lx + 0.33 * Rx, LBx + 0.5 * maxGap);
                        double nCx2 = Math.Max(0.33 * Lx + 0.67 * Rx, RBx - 0.5 * maxGap);
                        //修改current、left、right
                        UpdateVLine(v_index, nCx2);
                        if (v_index - 2 > 0)
                            UpdateVLine(v_index - 1, (vLines[v_index - 2].P0.X + nCx1) / 2);
                        if (v_index + 2 < vLines.Count)
                            UpdateVLine(v_index + 1, (vLines[v_index + 2].P0.X + nCx2) / 2);
                        //新增一条线
                        vLines.Insert(v_index, new LineSegment(nCx1, minY, nCx1, maxY));
                        for (int i = 0; i < hLines.Count; i++)
                        {
                            var currentPoint = hLines[i].Intersection(vLines[v_index]);
                            validPoints[i].Insert(v_index, FireAlarmUtils.PolygonContainPoint(area, currentPoint));
                            Positions[i].Insert(v_index, currentPoint);
                        }
                        v_index++;
                    }
                }
            }
            for (int h_index = 0; h_index < hLines.Count; h_index++)
            {
                //有上,无上,有下,无下
                var boundFlag = new List<bool>() { false, false, false, false };

                double TBy = double.MinValue, BBy = double.MaxValue;
                for (int v_index = 0; v_index < vLines.Count; v_index++)
                {
                    if (validPoints[h_index][v_index])
                    {
                        if (!HasTop(h_index, v_index))
                        {
                            boundFlag[1] = true;
                            TBy = Math.Max(TBy, GetTopPoint(h_index, v_index).Y);
                        }
                        else boundFlag[0] = true;

                        if (!HasBottom(h_index, v_index))
                        {
                            boundFlag[3] = true;
                            BBy = Math.Min(BBy, GetBottomPoint(h_index, v_index).Y);
                        }
                        else boundFlag[2] = true;
                    }
                }
                if (boundFlag[0] && boundFlag[1] && boundFlag[2] && boundFlag[3])
                {
                    double Ty = hLines[h_index - 1].P0.Y;
                    double By = hLines[h_index + 1].P0.Y;
                    if (Ty - BBy > maxGap * 1.5 && TBy - By > maxGap * 1.5)
                    {
                        double nCy1 = Math.Max(0.67 * Ty + 0.33 * By, TBy - 0.5 * maxGap);
                        double nCy2 = Math.Min(0.33 * Ty + 0.67 * By, BBy + 0.5 * maxGap);
                        //修改current、left、right
                        UpdateHLine(h_index, nCy2);
                        if (h_index - 2 > 0)
                            UpdateHLine(h_index - 1, (hLines[h_index - 2].P0.Y + nCy1) / 2);
                        if (h_index + 2 < hLines.Count)
                            UpdateHLine(h_index + 1, (hLines[h_index + 2].P0.Y + nCy2) / 2);
                        //新增一条线
                        hLines.Insert(h_index, new LineSegment(minX, nCy1, maxX, nCy1));
                        validPoints.Insert(h_index, new List<bool>());
                        Positions.Insert(h_index, new List<Coordinate>());
                        for (int j = 0; j < vLines.Count; j++)
                        {
                            var currentPoint = hLines[h_index].Intersection(vLines[j]);
                            validPoints[h_index].Add(FireAlarmUtils.PolygonContainPoint(area, currentPoint));
                            Positions[h_index].Add(currentPoint);
                        }
                        h_index++;
                    }
                }
            }
            for (int h_index = 0; h_index < hLines.Count; h_index++)
            {
                for (int v_index = 0; v_index < vLines.Count; v_index++)
                {
                    //当前点不在房间内
                    if (!validPoints[h_index][v_index])
                        continue;
                    var current = Positions[h_index][v_index];

                    //移动到最近的可布置区域
                    var target = FindTargetPoint(current, bufferDist);
                    if (target.X != current.X)
                    {
                        bool flag = true;
                        for (int i = 0; i < hLines.Count; i++)
                        {
                            var tmp = GetNearLayouts(Positions[i][v_index]);
                            var tmpPoint = new Coordinate(target.X, Positions[i][v_index].Y);
                            if (FireAlarmUtils.MultiPolygonContainPoint(tmp, Positions[i][v_index]))
                            {
                                if (!FireAlarmUtils.MultiPolygonContainPoint(tmp, tmpPoint))
                                {
                                    flag = false;
                                    break;
                                }
                            }
                        }
                        if (flag)
                        {
                            for (int i = 0; i < hLines.Count; i++)
                                Positions[i][v_index] = new Coordinate(target.X, Positions[i][v_index].Y);
                            vLines[v_index] = new LineSegment(target.X, minY, target.X, maxY);
                        }
                    }
                    if (target.Y != current.Y && HasRight(h_index, v_index))
                    {
                        bool flag = true;
                        for (int i = 0; i < vLines.Count; i++)
                        {
                            var tmp = GetNearLayouts(Positions[h_index][i]);
                            var tmpPoint = new Coordinate(Positions[h_index][i].X, target.Y);
                            if (FireAlarmUtils.MultiPolygonContainPoint(tmp, Positions[h_index][i]))
                            {
                                if (!FireAlarmUtils.MultiPolygonContainPoint(tmp, tmpPoint))
                                {
                                    flag = false;
                                    break;
                                }
                            }
                        }
                        if (flag)
                        {
                            for (int i = 0; i < vLines.Count; i++)
                                Positions[h_index][i] = new Coordinate(Positions[h_index][i].X, target.Y);
                            hLines[h_index] = new LineSegment(minX, target.Y, maxX, target.Y);
                        }
                    }
                    Positions[h_index][v_index] = target;
                }
            }
            for (int h_index = 0; h_index < hLines.Count; h_index++)
            {
                for (int v_index = 0; v_index < vLines.Count; v_index++)
                {
                    //当前点不在房间内
                    if (!validPoints[h_index][v_index])
                        continue;
                    var current = Positions[h_index][v_index];
                    //处理边界
                    List<Coordinate> NewPoints = new List<Coordinate>();

                    bool hasLeft = HasLeft(h_index, v_index);
                    bool hasRight = HasRight(h_index, v_index);
                    bool hasTop = HasTop(h_index, v_index);
                    bool hasBottom = HasBottom(h_index, v_index);
                    var left = GetLeftPoint(h_index, v_index);
                    var right = GetRightPoint(h_index, v_index);
                    var top = GetTopPoint(h_index, v_index);
                    var bottom = GetBottomPoint(h_index, v_index);

                    var x_maxGap = Math.Sqrt(radius * radius * 4 - Math.Pow(Math.Max(top.Y - current.Y, current.Y - bottom.Y), 2));
                    var y_maxGap = Math.Sqrt(radius * radius * 4 - Math.Pow(Math.Max(right.X - current.X, current.X - left.X), 2));

                    //距离左边界太远
                    if (!hasLeft && current.X - left.X > x_maxGap / 2) NewPoints.Add(new Coordinate(0.5 * (right.X + left.X) - x_maxGap * 0.75, current.Y));
                    //距离右边界太远
                    if (!hasRight && right.X - current.X > x_maxGap / 2) NewPoints.Add(new Coordinate(0.5 * (right.X + left.X) + x_maxGap * 0.75, current.Y));
                    //距离上边界太远
                    if (!hasTop && top.Y - current.Y > y_maxGap / 2) NewPoints.Add(new Coordinate(current.X, 0.5 * (top.Y + bottom.Y) + 0.75 * y_maxGap));
                    //距离下边界太远
                    if (!hasBottom && current.Y - bottom.Y > y_maxGap / 2)
                        NewPoints.Add(new Coordinate(current.X, 0.5 * (top.Y + bottom.Y) - 0.75 * y_maxGap));
                    //如果需要添加点，那么保证该点在可布置区域内
                    foreach (var newPoint in NewPoints)
                    {
                        var tmp = FindTargetPoint(newPoint, bufferDist);
                        if (tmp.Distance(current) > 1500)
                            boundsPoints.Add(tmp);
                    }
                }
            }
        }
        //探测区域补点
        private void AddPoints()
        {
            List<bool> hasPoint = new List<bool>(detects.Count);
            for (int i = 0; i < detects.Count; i++)
                hasPoint.Add(false);
            for (int h_index = 0; h_index < hLines.Count; h_index++)
            {
                for (int v_index = 0; v_index < vLines.Count; v_index++)
                {
                    if (!validPoints[h_index][v_index])
                        continue;
                    for (int i = 0; i < detects.Count; i++)
                    {
                        if (hasPoint[i] == true)
                            continue;
                        if (FireAlarmUtils.PolygonContainPoint(detects[i], Positions[h_index][v_index]))
                        {
                            hasPoint[i] = true;
                            break;
                        }
                    }
                }
            }
            foreach (var boundPoint in boundsPoints)
            {
                bool flag = false;
                for (int i = 0; i < detects.Count; i++)
                {
                    if (hasPoint[i] == true)
                        continue;
                    if (FireAlarmUtils.PolygonContainPoint(detects[i], boundPoint))
                    {
                        hasPoint[i] = true;
                        flag = true;
                        break;
                    }
                }
                IsMovable.Add(flag);
            }
            for (int i = 0; i < hasPoint.Count; i++)
            {
                if (hasPoint[i] == true)
                    continue;
                var minRect = detects[i].EnvelopeInternal;
                var innerLayouts = GetInnerLayouts(detects[i]);
                if (innerLayouts.Count == 0)
                    continue;
                for (int j = 0; j < hLines.Count; j++)
                {
                    if (hLines[j].P0.Y > minRect.MinY && hLines[j].P0.Y < minRect.MaxY)
                    {
                        var intersectLines = FireAlarmUtils.LineIntersectWithMutiPolygon(hLines[j], innerLayouts);
                        if (intersectLines.Count == 0)
                            continue;
                        else
                        {
                            detectPoints.Add(intersectLines.First().MidPoint);
                            hasPoint[i] = true;
                            break;
                        }
                    }
                }
                if (!hasPoint[i])
                {
                    for (int j = 0; j < vLines.Count; j++)
                    {
                        if (vLines[j].P0.X > minRect.MinX && vLines[j].P0.X < minRect.MaxX)
                        {
                            var intersectLines = FireAlarmUtils.LineIntersectWithMutiPolygon(vLines[j], innerLayouts);
                            if (intersectLines.Count == 0)
                                continue;
                            else
                            {
                                detectPoints.Add(intersectLines.First().MidPoint);
                                hasPoint[i] = true;
                                break;
                            }
                        }
                    }
                }
                if (!hasPoint[i])
                {
                    detectPoints.Add(FireAlarmUtils.AdjustedCenterPoint(innerLayouts.First()));
                    hasPoint[i] = true;
                }
            }
        }

        //寻找point附近的可布置区域
        private List<Polygon> GetNearLayouts(Coordinate point)
        {
            var min = new Point3d(point.X - radius, point.Y - radius, 0);
            var max = new Point3d(point.X + radius, point.Y + radius, 0);
            var dblayouts = thCADCoreNTSSpatialIndex.SelectCrossingWindow(min, max).Cast<MPolygon>().ToList();
            var polygon_layouts = new List<Polygon>();
            foreach (var layout in dblayouts)
                polygon_layouts.Add(layout.ToNTSPolygon());
            return polygon_layouts;
        }
        //计算目标点
        private Coordinate FindTargetPoint(Coordinate point, double buffer)
        {
            var nearLayouts = GetNearLayouts(point);
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

        //是否有左点
        private bool HasLeft(int i, int j)
        {
            return j > 0 && validPoints[i][j - 1] == true;
        }
        //是否有下点
        private bool HasTop(int i, int j)
        {
            return i > 0 && validPoints[i - 1][j] == true;
        }
        //是否有右点
        private bool HasRight(int i, int j)
        {
            return j < vLines.Count - 1 && validPoints[i][j + 1] == true;
        }
        //是否有下点
        private bool HasBottom(int i, int j)
        {
            return i < hLines.Count - 1 && validPoints[i + 1][j] == true;
        }
        //寻找左侧点
        private Coordinate GetLeftPoint(int i, int j)
        {
            if (HasLeft(i, j))
                return Positions[i][j - 1];
            //作一条长为maxGap,右端点为Positions[i][j]的横线
            var p0 = new Coordinate(Positions[i][j].X - maxGap, Positions[i][j].Y);
            var line = new LineSegment(p0, Positions[i][j]);
            var coods = FireAlarmUtils.LineInteresectWithPolygon(line, area);
            coods.Remove(Positions[i][j]);
            return coods.OrderBy(o => o.Distance(Positions[i][j])).First();
        }
        //寻找右侧点
        private Coordinate GetRightPoint(int i, int j)
        {
            if (HasRight(i, j))
                return Positions[i][j + 1];
            //作一条长为maxGap,左端点为Positions[i][j]的横线
            var p0 = new Coordinate(Positions[i][j].X + maxGap, Positions[i][j].Y);
            var line = new LineSegment(Positions[i][j], p0);
            var coods = FireAlarmUtils.LineInteresectWithPolygon(line, area);
            coods.Remove(Positions[i][j]);
            return coods.OrderBy(o => o.Distance(Positions[i][j])).First();
        }
        //寻找上方点
        private Coordinate GetTopPoint(int i, int j)
        {
            if (HasTop(i, j))
                return Positions[i - 1][j];
            //作一条长为maxGap,下端点为Positions[i][j]的横线
            var p0 = new Coordinate(Positions[i][j].X, Positions[i][j].Y + maxGap);
            var line = new LineSegment(Positions[i][j], p0);
            var coods = FireAlarmUtils.LineInteresectWithPolygon(line, area);
            coods.Remove(Positions[i][j]);
            return coods.OrderBy(o => o.Distance(Positions[i][j])).First();
        }
        //寻找下方点
        private Coordinate GetBottomPoint(int i, int j)
        {
            if (HasBottom(i, j))
                return Positions[i + 1][j];
            //作一条长为maxGap,上端点为Positions[i][j]的横线
            var p0 = new Coordinate(Positions[i][j].X, Positions[i][j].Y - maxGap);
            var line = new LineSegment(p0, Positions[i][j]);
            var coods = FireAlarmUtils.LineInteresectWithPolygon(line, area);
            coods.Remove(Positions[i][j]);
            return coods.OrderBy(o => o.Distance(Positions[i][j])).First();
        }
        //更新竖线
        private void UpdateVLine(int v_index, double new_X)
        {
            vLines[v_index] = new LineSegment(new_X, minY, new_X, maxY);
            for (int i = 0; i < hLines.Count; i++)
            {
                Positions[i][v_index] = new Coordinate(new_X, Positions[i][v_index].Y);
                validPoints[i][v_index] = FireAlarmUtils.PolygonContainPoint(area, Positions[i][v_index]);
            }
        }
        //更新横线
        private void UpdateHLine(int h_index, double new_Y)
        {
            hLines[h_index] = new LineSegment(minX, new_Y, maxX, new_Y);
            for (int j = 0; j < vLines.Count; j++)
            {
                Positions[h_index][j] = new Coordinate(Positions[h_index][j].X, new_Y);
                validPoints[h_index][j] = FireAlarmUtils.PolygonContainPoint(area, Positions[h_index][j]);
            }
        }
        private void GetPoints()
        {
            detects.OrderByDescending(o => o.Area).ToList();
            foreach (var detect in detects)
            {
                if (detect.IsEmpty)
                    continue;
                var nearLayouts = GetInnerLayouts(detect);
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

                //if (nearLayouts.Count == 0)
                //    continue;
                //else if (nearLayouts.Count == 1)
                //{
                //    var minRectLayout = nearLayouts[0].EnvelopeInternal;
                //    if (Math.Pow(minRectLayout.Width, 2) + Math.Pow(minRectLayout.Height, 2) < 4 * radius * radius)
                //    {
                //        Positions.Add(FireAlarmUtils.AdjustedCenterPoint(nearLayouts[0]));
                //        continue;
                //    }
                //    //else continue;
                //}
                //foreach (var xpos in x)
                //{
                //    foreach (var ypos in y)
                //    {
                //        var current = new Coordinate(xpos, ypos);
                //        if (FireAlarmUtils.PolygonContainPoint(detect, current))
                //        {
                //            var target = FindTargetPoint(current, nearLayouts, bufferDist);
                //            Positions.Add(target);
                //        }
                //    }
                //}
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
        ////计算目标点
        //private Coordinate FindTargetPoint(Coordinate point, List<Polygon> nearLayouts, double buffer)
        //{
        //    var res = new List<Coordinate>();
        //    res.Add(FindNearestPointWithBuffer(point, buffer, nearLayouts));
        //    res.Add(FindNearestPointOnHLineWithBuffer(point, buffer, nearLayouts));
        //    res.Add(FindNearestPointOnVLineWithBuffer(point, buffer, nearLayouts));
        //    return res.OrderBy(o => o.Distance(point)).First();
        //}
        ////计算距离point最近的可布置区域内的点，带buffer
        //private Coordinate FindNearestPointWithBuffer(Coordinate point, double buffer, List<Polygon> nearLayouts)
        //{
        //    if (FireAlarmUtils.MultiPolygonContainPoint(nearLayouts, point))
        //        return point;
        //    Coordinate target = new Coordinate(point.X + radius, point.Y + radius);
        //    foreach (var polygon in nearLayouts)
        //    {
        //        Coordinate temp = target;

        //        //多边形内缩buffer
        //        var bufferPoly = polygon.Buffer(-buffer);
        //        //如果缩不了，那么取中心点
        //        if (bufferPoly.IsEmpty)
        //            temp = Centroid.GetCentroid(polygon);
        //        else if (bufferPoly is Polygon polygon1)
        //            temp = FireAlarmUtils.GetClosePointOnPolygon(polygon1, point);
        //        else if (bufferPoly is MultiPolygon multiPolygon)
        //            temp = FireAlarmUtils.GetClosePointOnMultiPolygon(multiPolygon, point);

        //        if (temp.Distance(point) < target.Distance(point))
        //            target = temp;
        //    }
        //    return target;
        //}
        ////计算横线上距离point最近的可布置区域内的点，带buffer
        //private Coordinate FindNearestPointOnHLineWithBuffer(Coordinate point, double buffer, List<Polygon> nearLayouts)
        //{
        //    List<Coordinate> possible_points = new List<Coordinate>();
        //    //点所在横线
        //    var left = new Coordinate(point.X - radius, point.Y);
        //    var right = new Coordinate(point.X + radius, point.Y);
        //    var hline = new LineSegment(left, right);
        //    //与可布置区域的交集
        //    var intersectLine = FireAlarmUtils.LineIntersectWithMutiPolygon(hline, nearLayouts);
        //    //没有交集，返回失败
        //    if (intersectLine.Count == 0)
        //        return new Coordinate(point.X + radius, point.Y + radius);
        //    //有交集，找目标点
        //    foreach (var seg in intersectLine)
        //    {
        //        Coordinate pos = new Coordinate();
        //        if (seg.Length > buffer * 2)//线长大于2*buffer，两端内缩buffer取较近点
        //        {
        //            var p0 = seg.P0.Distance(point) < seg.P1.Distance(point) ? seg.P0 : seg.P1;
        //            var p1 = p0 == seg.P0 ? seg.P1 : seg.P0;
        //            pos.Y = point.Y;
        //            pos.X = p1.X > p0.X ? p0.X + buffer : p0.X - buffer;
        //        }
        //        else pos = seg.MidPoint;//线长小于2*buffer，取中点
        //        possible_points.Add(pos);
        //    }
        //    if (possible_points.Count == 0) return new Coordinate(point.X + radius, point.Y + radius);
        //    return possible_points.OrderBy(o => o.Distance(point)).First();
        //}
        ////计算竖线上距离point最近的可布置区域内的点，带buffer
        //private Coordinate FindNearestPointOnVLineWithBuffer(Coordinate point, double buffer, List<Polygon> nearLayouts)
        //{
        //    List<Coordinate> possible_points = new List<Coordinate>();
        //    //点所在竖线
        //    var top = new Coordinate(point.X, point.Y + radius);
        //    var bottom = new Coordinate(point.X, point.Y - radius);
        //    var vline = new LineSegment(bottom, top);
        //    //与可布置区域的交集
        //    var intersectLine = FireAlarmUtils.LineIntersectWithMutiPolygon(vline, nearLayouts);
        //    //没有交集，返回失败
        //    if (intersectLine.Count == 0)
        //        return new Coordinate(point.X + radius, point.Y + radius);
        //    //有交集，先找目标点
        //    foreach (var seg in intersectLine)
        //    {
        //        Coordinate pos = new Coordinate();
        //        if (seg.Length > 2 * buffer) //线长大于2*buffer，两端内缩buffer取较近点
        //        {
        //            var p0 = seg.P0.Distance(point) < seg.P1.Distance(point) ? seg.P0 : seg.P1;
        //            var p1 = p0 == seg.P0 ? seg.P1 : seg.P0;
        //            pos.X = point.X;
        //            pos.Y = p1.Y > p0.Y ? p0.Y + buffer : p0.Y - buffer;
        //        }
        //        else pos = seg.MidPoint;//线长小于2*buffer，取中点
        //        possible_points.Add(pos);
        //    }
        //    if (possible_points.Count == 0) return new Coordinate(point.X + radius, point.Y + radius);
        //    return possible_points.OrderBy(o => o.Distance(point)).First();
        //}

    }
}