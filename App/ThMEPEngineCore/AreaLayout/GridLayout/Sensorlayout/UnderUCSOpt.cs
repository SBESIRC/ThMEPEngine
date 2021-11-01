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
    public class UnderUCSOpt
    {
        private Polygon area;//区域
        private double angle;//区域方向
        private Point3d center;//旋转中心
        private List<Polygon> layouts { get; set; } = new List<Polygon>();//可布置区域
        private List<Coordinate> columnCenters { get; set; } = new List<Coordinate>();//柱子点位
        private List<LineSegment> hLines { get; set; } = new List<LineSegment>();//横线，自上向下排序
        private List<LineSegment> vLines { get; set; } = new List<LineSegment>();//竖线，自左向右排序
        private List<List<bool>> validPoints { get; set; } = new List<List<bool>>();//交点是否有效
        private List<List<Coordinate>> Positions { get; set; } = new List<List<Coordinate>>();//交点位置
        private List<Coordinate> boundsPoints { get; set; } = new List<Coordinate>();//边界补点
        
        public List<Coordinate> PlacePoints { get; set; } = new List<Coordinate>();//布点位置

        private ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex;
        private double minGap = 5800;
        private double maxGap = 8201;
        private double adjustGap = 5800 * Math.Sqrt(2);
        private double beamGap = 1300;
        private double bufferDist = 500;
        private double radius;

        private double minX;
        private double minY;
        private double maxX;
        private double maxY;

        public UnderUCSOpt(Polyline boundary, double angle, Polygon room, List<Polygon> layouts, List<Coordinate> columnCenters,
            double min, double max, double radius, double adjust, double buffer)
        {
            //生成区域
            boundary = boundary.Buffer(300).Cast<Polyline>().First();
            var geom = OverlayNGRobust.Overlay(boundary.ToNTSPolygon(), room, SpatialFunction.Intersection);
            //var geom = boundary.ToNTSPolygon().Intersection(room);
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
                    var inLayout = layout.Difference(area).Buffer(-1) as Polygon;
                    var dblayout = inLayout.ToDbMPolygon();
                    dblayout.Rotate(center, -angle);
                    tarlayout = dblayout.ToNTSPolygon();
                }
                if (tarlayout != null)
                    this.layouts.Add(tarlayout);
            }
            var region = area.Shell.ToDbPolyline().Buffer(beamGap).ToNTSMultiPolygon().OrderByDescending(o => o.Area).First() as Polygon;
            foreach (var columnCenter in columnCenters)
            {
                if (FireAlarmUtils.PolygonContainPoint(region, columnCenter))
                {
                    var dbcenter = columnCenter.ToAcGePoint3d();
                    dbcenter.RotateBy(-angle, Vector3d.ZAxis, center);
                    this.columnCenters.Add(dbcenter.ToNTSCoordinate());
                }
            }
            //旋转区域
            var dbarea = area.ToDbMPolygon();
            dbarea.Rotate(center, -angle);
            area = dbarea.ToNTSPolygon();
            //建立关于可布置区域的索引
            DBObjectCollection dBObjectCollection = new DBObjectCollection();
            foreach (var layout in this.layouts)
                dBObjectCollection.Add(layout.ToDbMPolygon());
            thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(dBObjectCollection);

            //设置参数
            minGap = min;
            maxGap = max;
            adjustGap = adjust;
            bufferDist = buffer;
            this.radius = radius;
        }
        public void CalculatePlace()
        {
            if (layouts.Count == 0 || columnCenters.Count == 0)
                return;
            InitXYSegments();
            InitPoints();
            AdjustLines();
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
            foreach (var boundPoint in boundsPoints)
            {
                var p = boundPoint.ToAcGePoint3d();
                p = p.RotateBy(angle, Vector3d.ZAxis, center);
                var NTSp = new Coordinate(p.X, p.Y);
                PlacePoints.Add(NTSp);
            }
        }
        //初步生成布置线
        private void InitXYSegments()
        {
            //计算UCS区域的边缘
            var minRect = area.Envelope as Polygon;
            minX = minRect.Coordinates[0].X;
            minY = minRect.Coordinates[0].Y;
            maxX = minRect.Coordinates[2].X;
            maxY = minRect.Coordinates[2].Y;
            //根据柱子的分布对x和y方向进行区间划分
            List<double> column_xs = new List<double>();
            List<double> column_ys = new List<double>();
            foreach (var coor in columnCenters)
            {
                column_xs.Add(coor.X);
                column_ys.Add(coor.Y);
            }
            column_xs.Sort();
            column_ys.Sort();

            int num = 1;
            double sum = column_xs[0];
            List<double> x_mark = new List<double>();
            for (int index = 1; index < column_xs.Count; index++)
            {
                if (column_xs[index] - column_xs[index - 1] > 500)
                {
                    if (num > 1)
                        x_mark.Add(sum / num);
                    num = 1;
                    sum = column_xs[index] + 1;
                }
                else
                {
                    num++;
                    sum += column_xs[index];
                }
            }
            if (num > 1)
                x_mark.Add(sum / num);
            while (x_mark.First() > minX)
                x_mark.Insert(0, x_mark[0] - (x_mark[1] - x_mark[0]));
            while (maxX > x_mark.Last())
                x_mark.Add(x_mark.Last() + (x_mark.Last() - x_mark[x_mark.Count - 2]));

            List<double> y_mark = new List<double>();
            num = 1;
            sum = column_ys[0];
            for (int index = 1; index < column_ys.Count; index++)
            {
                if (column_ys[index] - column_ys[index - 1] > 500)
                {
                    if (num > 1)
                        y_mark.Add(sum / num);
                    num = 1;
                    sum = column_ys[index] + 1;
                }
                else
                {
                    num++;
                    sum += column_ys[index];
                }
            }
            if (num > 1)
                y_mark.Add(sum / num);
            while (y_mark.First() > minY)
                y_mark.Insert(0, y_mark[0] - (y_mark[1] - y_mark[0]));
            while (maxY > y_mark.Last())
                y_mark.Add(y_mark.Last() + (y_mark.Last() - y_mark[y_mark.Count - 2]));

            var vLineXset = new List<double>();
            double x0 = minX + maxGap / 2;
            for (var index = 0; index < x_mark.Count - 1;)
            {
                var left = x_mark[index];
                var right = x_mark[index + 1];
                var left1_4 = left + (right - left) * 0.25;
                var right1_4 = left + (right - left) * 0.75;

                if (x0 < left1_4)
                {
                    if (index > 0)
                        x0 = x_mark[index - 1] + (x_mark[index] - x_mark[index - 1]) * 0.75;
                }
                else if (x0 > right1_4 && x0 < right)
                    x0 = right1_4;
                else if (x0 > right)
                {
                    index++;
                    continue;
                }
                vLineXset.Add(x0);
                if (maxX - x0 > 1.5 * maxGap)
                    x0 += maxGap;
                else x0 += 0.9 * maxGap;
            }
            var hLineYset = new List<double>();
            double y0 = maxY - maxGap / 2;
            for (var index = y_mark.Count - 1; index > 0; index--)
            {
                var top = y_mark[index];
                var bottom = y_mark[index - 1];
                var top1_4 = top - (top - bottom) * 0.25;
                var bottom1_4 = top - (top - bottom) * 0.75;
                if (y0 > top1_4)
                {
                    if (index < y_mark.Count - 1)
                        y0 = y_mark[index + 1] - (y_mark[index + 1] - y_mark[index]) * 0.75;
                }
                else if (y0 < bottom1_4 && y0 > bottom)
                    y0 = bottom1_4;
                else if (y0 < bottom)
                    continue;
                hLineYset.Add(y0);
                y0 -= maxGap;
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
                    double Ty = vLines[h_index - 1].P0.Y;
                    double By = vLines[h_index + 1].P0.Y;
                    if (Ty - BBy > maxGap * 1.5 && TBy - By > maxGap * 1.5)
                    {
                        double nCy1 = Math.Max(0.67 * Ty + 0.33 * By, TBy - 0.5 * maxGap);
                        double nCy2 = Math.Min(0.33 * Ty + 0.67 * By, BBy + 0.5 * maxGap);
                        //修改current、left、right
                        UpdateHLine(h_index, nCy2);
                        if (h_index - 2 > 0)
                            UpdateVLine(h_index - 1, (hLines[h_index - 2].P0.Y + nCy1) / 2);
                        if (h_index + 2 < hLines.Count)
                            UpdateVLine(h_index + 1, (vLines[h_index + 2].P0.X + nCy2) / 2);
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
                    Positions[h_index][v_index] = FindNearestPointWithBuffer(current, bufferDist);

                    //处理边界
                    Coordinate newPoint = null;//新增点
                    Coordinate newPosition = null;//移动点

                    bool hasLeft = HasLeft(h_index, v_index);
                    bool hasRight = HasRight(h_index, v_index);
                    bool hasTop = HasTop(h_index, v_index);
                    bool hasBottom = HasBottom(h_index, v_index);
                    var left = GetLeftPoint(h_index, v_index);
                    var right = GetRightPoint(h_index, v_index);
                    var top = GetTopPoint(h_index, v_index);
                    var bottom = GetBottomPoint(h_index, v_index);

                    int movedir = -1;//移动方向：1234->左右上下
                    //距离左边界太远
                    if (!hasLeft && current.X - left.X > maxGap / 2)
                    {
                        if (right.X - left.X < 1.5 * maxGap)
                        {
                            if (hasRight)
                            {
                                newPosition = new Coordinate(left.X + maxGap / 2, current.Y);
                                movedir = 1;
                            }
                            else
                                newPosition = new Coordinate((left.X + right.X) / 2, current.Y);
                        }
                        else
                            newPoint = new Coordinate(0.5 * (right.X + left.X) - maxGap * 0.75, current.Y);
                    }
                    //距离右边界太远
                    else if (!hasRight && right.X - current.X > maxGap / 2)
                    {
                        if (hasLeft)
                        {
                            if (right.X - left.X < 1.5 * maxGap)
                            {
                                newPosition = new Coordinate(right.X - maxGap / 2, current.Y);
                                movedir = 2;
                            }
                            else
                                newPoint = new Coordinate(0.5 * (right.X + left.X) + maxGap * 0.75, current.Y);
                        }
                    }
                    //距离上边界太远
                    else if (!hasTop && top.Y - current.Y > maxGap / 2)
                    {
                        if (top.Y - bottom.Y < 1.5 * maxGap)
                        {
                            if (hasBottom)
                            {
                                newPosition = new Coordinate(current.X, top.Y - maxGap / 2);
                                movedir = 3;
                            }
                            else
                            {
                                newPosition = new Coordinate(current.X, top.Y - maxGap / 2);
                                newPoint = new Coordinate(current.X, bottom.Y + maxGap / 2);
                            }
                        }
                        else
                            newPoint = new Coordinate(current.X, 0.5 * (top.Y + bottom.Y) + 0.75 * maxGap);
                    }
                    //距离下边界太远
                    else if (!hasBottom && current.Y - bottom.Y > maxGap / 2)
                    {
                        if (hasTop)
                        {
                            if (top.Y - bottom.Y < 1.5 * maxGap)
                            {
                                newPosition = new Coordinate(current.X, bottom.Y + maxGap / 2);
                                movedir = 4;
                            }
                            else
                                newPoint = new Coordinate(current.X, 0.5 * (top.Y + bottom.Y) - 0.75 * maxGap);
                        }
                    }
                    //如果需要移动点，那么保证新位置在可布置区域内
                    if (newPosition != null)
                    {
                        if (movedir == -1)
                        {
                            newPosition = FindNearestPointWithBuffer(newPosition, bufferDist);
                            Positions[h_index][v_index] = newPosition;
                        }
                        if (movedir == 1 || movedir == 2)
                        {
                            for (int i = h_index; i < hLines.Count; i++)
                            {
                                var updatePoint = new Coordinate(newPosition.X, Positions[i][v_index].Y);
                                if (FireAlarmUtils.PolygonContainPoint(area, updatePoint))
                                    Positions[i][v_index] = FindNearestPointWithBuffer(updatePoint, bufferDist);
                            }

                        }
                        if (movedir == 3 || movedir == 4)
                        {
                            for (int j = v_index; j < vLines.Count; j++)
                            {
                                var updatePoint = new Coordinate(Positions[h_index][j].X, newPosition.Y);
                                if (FireAlarmUtils.PolygonContainPoint(area, updatePoint))
                                    Positions[h_index][j] = FindNearestPointWithBuffer(updatePoint, bufferDist);
                            }
                        }
                    }
                    //如果需要添加点，那么保证该点在可布置区域内
                    if (newPoint != null)
                    {
                        newPoint = FindNearestPointWithBuffer(newPoint, bufferDist);
                        boundsPoints.Add(newPoint);
                    }
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
        //计算距离point最近的可布置区域内的点，带buffer
        private Coordinate FindNearestPointWithBuffer(Coordinate point, double buffer)
        {
            var nearLayouts = GetNearLayouts(point);
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

        private bool HasLeft(int i, int j)
        {
            return j > 0 && validPoints[i][j - 1] == true;
        }
        private bool HasTop(int i, int j)
        {
            return i > 0 && validPoints[i - 1][j] == true;
        }
        private bool HasRight(int i, int j)
        {
            return j < vLines.Count - 1 && validPoints[i][j + 1] == true;
        }
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
    }
}