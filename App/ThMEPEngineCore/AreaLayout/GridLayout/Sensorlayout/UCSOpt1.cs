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
    public class UCSOpt1
    {
        private Polygon area;//区域
        public double angle;//区域方向
        public Point3d center;//旋转中心
        private List<Polygon> layouts { get; set; } = new List<Polygon>();//可布置区域
        private List<LineSegment> hLines { get; set; } = new List<LineSegment>();//横线，自上向下排序
        private List<LineSegment> vLines { get; set; } = new List<LineSegment>();//竖线，自左向右排序
        private List<List<bool>> validPoints { get; set; } = new List<List<bool>>();//交点是否有效
        private List<List<Coordinate>> Positions { get; set; } = new List<List<Coordinate>>();//交点位置
        public List<Coordinate> PlacePoints { get; set; } = new List<Coordinate>();//布点位置

        private ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex;
        private double minGap = 5800;
        private double maxGap = 8201;
        private double adjustGap = 5800 * Math.Sqrt(2);
        private double bufferDist = 500;
        private double radius;

        private double minX;
        private double minY;
        private double maxX;
        private double maxY;

        public UCSOpt1(Polyline boundary, double angle, Polygon room, List<Polygon> layouts,
            double min, double max, double radius, double adjust, double buffer)
        {
            //生成区域
            var geom = OverlayNGRobust.Overlay(boundary.ToNTSPolygon(),room, SpatialFunction.Intersection);
            //var geom = boundary.ToNTSPolygon().Intersection(room);
            if (geom is Polygon polygon)
                area = polygon;
            else if(geom is GeometryCollection geometrycollection)
            {
                Polygon tmpPoly = Polygon.Empty;
                foreach(var poly in geometrycollection)
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
            InitLines();
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
        }
        //初步生成布置线
        private void InitLines()
        {
            var minRect = area.Envelope as Polygon;
            minX = minRect.Coordinates[0].X;
            minY = minRect.Coordinates[0].Y;
            maxX = minRect.Coordinates[2].X;
            maxY = minRect.Coordinates[2].Y;
            var xNum = Math.Ceiling((maxX - minX) / adjustGap);
            var yNum = Math.Ceiling((maxY - minY) / adjustGap);
            var dx = (maxX - minX) / xNum;
            var dy = (maxY - minY) / yNum;
            //if (yNum == 1)
            //    hLines.Add(new LineSegment(minX, (minY + maxY) / 2, maxX, (minY + maxY) / 2));
            for (double i = 0.5; i < yNum; i++)
                hLines.Add(new LineSegment(minX, minY + dy * i, maxX, minY + dy * i));
            //if (xNum == 1)
            //    vLines.Add(new LineSegment((minX + maxX) / 2, minY, (minX + maxX) / 2, maxY));
            for (double i = 0.5; i < xNum; i++)
                vLines.Add(new LineSegment(minX + dx * i, minY, minX + dx * i, maxY));
            hLines.Reverse();
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
                    //var current = new Coordinate(currentPoint.X, currentPoint.Y);
                    validPoints[i].Add(FireAlarmUtils.PolygonContainPoint(area, currentPoint));
                    Positions[i].Add(currentPoint);
                }
            }

            //第1行特殊处理,如果存在某条竖线与房间相交，但两端点都不在房间内的情况，那么新增一条线
            for (int i = 0; i < vLines.Count; i++)
            {
                if (validPoints[0][i] == true)
                    continue;
                var p0 = new Coordinate(Positions[0][i].X, Positions[0][i].Y + radius);
                var line = new LineSegment(p0, Positions[0][i]);
                var intersect = FireAlarmUtils.LineInteresectWithPolygon(line, area);
                if (intersect.Count == 0)
                    continue;
                else
                {
                    double y = (intersect[0].Y + intersect[1].Y) / 2;
                    hLines.Insert(0, new LineSegment(hLines[0].P0.X, y, hLines[0].P1.X, y));
                    validPoints.Insert(0, new List<bool>());
                    Positions.Insert(0, new List<Coordinate>());
                    for (int j = 0; j < vLines.Count; j++)
                    {
                        Positions[0].Add(hLines[0].Intersection(vLines[j]));
                        validPoints[0].Add(FireAlarmUtils.PolygonContainPoint(area, Positions[0][j]));
                    }
                    break;
                }
            }
            //第1列特殊处理,如果存在某条横线与房间相交，但两端点都不在房间内的情况，那么新增一条线
            for (int i = 0; i < hLines.Count; i++)
            {
                if (validPoints[i][0] == true)
                    continue;
                var p0 = new Coordinate(Positions[i][0].X - radius, Positions[i][0].Y);
                var line = new LineSegment(p0, Positions[i][0]);
                var intersect = FireAlarmUtils.LineInteresectWithPolygon(line, area);
                if (intersect.Count == 0)
                    continue;
                else
                {
                    double x = (intersect[0].X + intersect[1].X) / 2;
                    vLines.Insert(0, new LineSegment(x, vLines[0].P0.Y, x, vLines[0].P1.Y));
                    for (int j = 0; j < hLines.Count; j++)
                    {
                        Positions[j].Insert(0, vLines[0].Intersection(hLines[j]));
                        validPoints[j].Insert(0, FireAlarmUtils.PolygonContainPoint(area, Positions[j][0]));
                    }
                    break;
                }
            }
            for (int i = 0; i < hLines.Count; i++)
            {
                for (int j = 0; j < vLines.Count; j++)
                {
                    if (validPoints[i][j] == true) continue;
                    double new_X = Positions[i][j].X;
                    double new_Y = Positions[i][j].Y;
                    var intersect = new List<Coordinate>();
                    //先判断横线
                    if (j < vLines.Count - 1)
                    {
                        if (!validPoints[i][j + 1])
                            intersect = FireAlarmUtils.LineInteresectWithPolygon(new LineSegment(Positions[i][j], Positions[i][j + 1]), area);
                    }
                    else intersect = FireAlarmUtils.LineInteresectWithPolygon(new LineSegment(Positions[i][j], new Coordinate(Positions[i][j].X + radius, Positions[i][j].Y)), area);
                    if (intersect.Count > 1)
                    {
                        new_X = (intersect[0].X + intersect[1].X) / 2;
                        validPoints[i][j] = true;
                        Positions[i][j].X = new_X;
                        continue;
                    }

                    //再判断竖线
                    if (i < hLines.Count - 1)
                    {
                        if (!validPoints[i + 1][j])
                            intersect = FireAlarmUtils.LineInteresectWithPolygon(new LineSegment(Positions[i][j], Positions[i + 1][j]), area);
                    }
                    else intersect = FireAlarmUtils.LineInteresectWithPolygon(new LineSegment(Positions[i][j], new Coordinate(Positions[i][j].X, Positions[i][j].Y - radius)), area);
                    if (intersect.Count > 1)
                    {
                        new_Y = (intersect[0].Y + intersect[1].Y) / 2;
                        validPoints[i][j] = true;
                        Positions[i][j].Y = new_Y;
                        continue;
                    }
                }
            }
        }
        //调整线
        private void AdjustLines()
        {
            for (int h_index = 0; h_index < hLines.Count; h_index++)
            {
                for (int v_index = 0; v_index < vLines.Count; v_index++)
                {
                    //当前点不在房间内
                    if (!validPoints[h_index][v_index])
                        continue;
                    //如果当前横线与上一条横线距离过大或者过小，先调整距离
                    var hgap = GetTopPoint(h_index, v_index).Y - Positions[h_index][v_index].Y;
                    if (HasTop(h_index, v_index) && (hgap > maxGap || (HasBottom(h_index, v_index) && hgap < minGap)))
                        CutHLine(h_index, v_index, GetTopPoint(h_index, v_index).Y - adjustGap);
                    else if (!HasTop(h_index, v_index) && (hgap > adjustGap / 2))
                        CutHLine(h_index, v_index, GetTopPoint(h_index, v_index).Y - adjustGap / 2);
                    //如果最后一条横线移动后距离底部太远，加一条横线
                    if (h_index == hLines.Count - 1 && Positions[h_index][v_index].Y - minY > minGap)
                    {
                        hLines.Add(new LineSegment(new Coordinate(minX, Positions[h_index][v_index].Y - minGap), new Coordinate(maxX, Positions[h_index][v_index].Y - minGap)));
                        Positions.Add(new List<Coordinate>());
                        validPoints.Add(new List<bool>());
                        for (int i = 0; i < vLines.Count; i++)
                        {
                            var tmpPoint = new Coordinate(Positions[h_index][i].X, Positions[h_index][v_index].Y - minGap);
                            Positions[hLines.Count - 1].Add(tmpPoint);
                            if (i < v_index)
                                validPoints[hLines.Count - 1].Add(false);
                            else
                                validPoints[hLines.Count - 1].Add(FireAlarmUtils.PolygonContainPoint(area, tmpPoint));
                        }
                    }

                    //当前点不在房间内
                    if (!validPoints[h_index][v_index])
                        continue;
                    var vgap = Positions[h_index][v_index].X - GetLeftPoint(h_index, v_index).X;
                    //如果当前竖线与上一条竖线距离过大或者过小，先调整距离
                    if (HasLeft(h_index, v_index) && (vgap > maxGap || (HasRight(h_index, v_index) && vgap < minGap)))
                        CutVLine(h_index, v_index, GetLeftPoint(h_index, v_index).X + adjustGap);
                    else if (!HasLeft(h_index, v_index) && (vgap > adjustGap / 2))
                        CutVLine(h_index, v_index, GetLeftPoint(h_index, v_index).X + adjustGap / 2);
                    //如果最后一条竖线移动后距离右边太远，加一条竖线
                    if (v_index == vLines.Count - 1 && maxX - Positions[h_index][v_index].X > minGap)
                    {
                        vLines.Add(new LineSegment(new Coordinate(Positions[h_index][v_index].X + minGap, minY), new Coordinate(Positions[h_index][v_index].X + minGap, maxY)));
                        for (int i = 0; i < hLines.Count; i++)
                        {
                            var tmpPoint = new Coordinate(Positions[h_index][v_index].X + minGap, Positions[i][v_index].Y);
                            Positions[i].Add(tmpPoint);
                            if (i < h_index)
                                validPoints[i].Add(false);
                            else
                                validPoints[i].Add(FireAlarmUtils.PolygonContainPoint(area, tmpPoint));
                        }
                    }
                    //当前点不在房间内
                    if (!validPoints[h_index][v_index])
                        continue;
                    //当前点在可布置区域内
                    if (FireAlarmUtils.MultiPolygonContainPoint(layouts, Positions[h_index][v_index]))
                        continue;
                    //计算周围的可布置区域
                    var old_Line = new List<Coordinate>();
                    var old_valid = new List<bool>();
                    for (int t = 0; t < hLines.Count; t++)
                    {
                        old_Line.Add(Positions[t][v_index].Copy());
                        old_valid.Add(validPoints[t][v_index]);
                    }
                    //寻找带buffer目标点
                    var target = FindNearestPointWithBuffer(h_index, v_index, bufferDist);
                    var MoveSuccess = false;
                    //同时移动横线和竖线
                    if (AdjustVLine(h_index, v_index, target.X))
                    {
                        if (AdjustHLine(h_index, v_index, target.Y))
                            MoveSuccess = true;
                    }
                    //移动不成功，直接切断
                    if (!MoveSuccess)
                    {
                        for (int t = 0; t < hLines.Count; t++)
                        {
                            Positions[t][v_index] = old_Line[t];
                            validPoints[t][v_index] = old_valid[t];
                        }
                        CutHLine(h_index, v_index, target.Y);
                        CutVLine(h_index, v_index, target.X);
                    }
                    ////不是最后一条横线，且移动后距离边界太远
                    //if (h_index != hLines.Count - 1 && validPoints[h_index + 1][v_index] == false && Positions[h_index][v_index].Y - GetBottomPoint(h_index, v_index).Y > minGap) 
                    //{
                    //    Positions[h_index + 1][v_index].Y = Positions[h_index][v_index].Y - minGap;
                    //    validPoints[h_index + 1][v_index] = true;
                    //}
                    ////不是最后一条竖线，且移动后距离边界太远
                    //if (v_index != vLines.Count - 1 && validPoints[h_index][v_index + 1] == false && GetRightPoint(h_index, v_index).X - Positions[h_index][v_index].X > minGap)
                    //{
                    //    Positions[h_index][v_index + 1].X = Positions[h_index][v_index].X + minGap;
                    //    validPoints[h_index][v_index + 1] = true;
                    //}
                }
            }
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
            //作一条长为radius,右端点为Positions[i][j]的横线
            var p0 = new Coordinate(Positions[i][j].X - radius, Positions[i][j].Y);
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
            //作一条长为radius,左端点为Positions[i][j]的横线
            var p0 = new Coordinate(Positions[i][j].X + radius, Positions[i][j].Y);
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
            //作一条长为radius,下端点为Positions[i][j]的横线
            var p0 = new Coordinate(Positions[i][j].X, Positions[i][j].Y + radius);
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
            //作一条长为radius,上端点为Positions[i][j]的横线
            var p0 = new Coordinate(Positions[i][j].X, Positions[i][j].Y - radius);
            var line = new LineSegment(p0, Positions[i][j]);
            var coods = FireAlarmUtils.LineInteresectWithPolygon(line, area);
            coods.Remove(Positions[i][j]);
            return coods.OrderBy(o => o.Distance(Positions[i][j])).First();
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
        private Coordinate GetTargetPoint(int i, int j)
        {
            bool hasLeft = HasLeft(i, j);
            bool hasTop = HasTop(i, j);
            var left = GetLeftPoint(i, j);
            var top = GetTopPoint(i, j);
            if (!hasLeft && !hasTop)
                return new Coordinate(left.X + adjustGap / 2, top.Y - adjustGap / 2);
            if (hasLeft && !hasTop)
                return new Coordinate(left.X + adjustGap, left.Y);
            if (!hasLeft && hasTop)
                return new Coordinate(top.X, top.Y - adjustGap);
            return new Coordinate(top.X, left.Y);
        }
        //更新点所在竖线
        private void UpdateVLine(int h_index, int v_index, double new_X)
        {
            var old_X = Positions[h_index][v_index].X;
            for (int i = 0; i < hLines.Count; i++)
                if (Positions[i][v_index].X == old_X)
                {
                    var newPointInRoom = FireAlarmUtils.PolygonContainPoint(area, new Coordinate(new_X, Positions[i][v_index].Y));
                    Positions[i][v_index].X = new_X;
                    if (i >= h_index)
                        validPoints[i][v_index] = newPointInRoom;
                }
        }
        //更新点所在横线
        private void UpdateHLine(int h_index, int v_index, double new_Y)
        {
            var old_Y = Positions[h_index][v_index].Y;
            for (int i = 0; i < vLines.Count; i++)
                if (Positions[h_index][i].Y == old_Y)
                {
                    var newPointInRoom = FireAlarmUtils.PolygonContainPoint(area, new Coordinate(Positions[h_index][i].X, new_Y));
                    Positions[h_index][i].Y = new_Y;
                    if (i >= v_index)
                        validPoints[h_index][i] = newPointInRoom;
                }
        }
        //从一点处切断横线
        private void CutHLine(int h_index, int v_index, double new_Y)
        {
            var old_Y = Positions[h_index][v_index].Y;
            for (int i = v_index; i < vLines.Count; i++)
            {
                if (Positions[h_index][i].Y == old_Y)
                {
                    var newPointInRoom = FireAlarmUtils.PolygonContainPoint(area, new Coordinate(Positions[h_index][i].X, new_Y));
                    Positions[h_index][i].Y = new_Y;
                    validPoints[h_index][i] = newPointInRoom;
                }
            }
        }
        //从一点处切断竖线
        private void CutVLine(int h_index, int v_index, double new_X)
        {
            var old_X = Positions[h_index][v_index].X;
            for (int i = h_index; i < hLines.Count; i++)
            {
                var newPointInRoom = FireAlarmUtils.PolygonContainPoint(area, new Coordinate(new_X, Positions[i][v_index].Y));
                Positions[i][v_index].X = new_X;
                validPoints[i][v_index] = newPointInRoom;
            }
        }
        //计算距离point最近的可布置区域内的点，带buffer
        private Coordinate FindNearestPointWithBuffer(int i, int j, double buffer)
        {
            //var point = Positions[i][j];
            var point = GetTargetPoint(i, j);
            var nearLayouts = GetNearLayouts(point);
            if (FireAlarmUtils.MultiPolygonContainPoint(nearLayouts, point))
                return point;
            Coordinate target = new Coordinate(point.X + radius, point.Y + radius);
            Coordinate targetWithoutDist = new Coordinate(point.X + radius, point.Y + radius);
            bool hasTarget = false;
            while (buffer >= 100)
            {
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

                    if ((temp.X - GetLeftPoint(i, j).X < maxGap) && (GetTopPoint(i, j).Y - temp.Y < maxGap)
                        && temp.Distance(point) < target.Distance(point))
                    {
                        target = temp;
                        hasTarget = true;
                    }

                    if (temp.Distance(point) < targetWithoutDist.Distance(point))
                        targetWithoutDist = temp;
                }
                buffer -= 100;
            }
            if (hasTarget)
                return target;
            else return targetWithoutDist;
        }
        //计算横线上距离point最近的可布置区域内的点，带buffer
        private Coordinate FindNearestPointOnHLineWithBuffer(int i, int j, List<Polygon> polygons, double buffer)
        {
            var point = Positions[i][j];
            //var point = GetTargetPoint(i, j);
            List<Coordinate> possible_points = new List<Coordinate>();
            //点所在横线
            var left = new Coordinate(GetLeftPoint(i, j).X, Positions[i][j].Y);
            var right = new Coordinate(GetRightPoint(i, j).Y, Positions[i][j].Y);
            var hline = new LineSegment(left, right);
            //与可布置区域的交集
            var intersectLine = FireAlarmUtils.LineIntersectWithMutiPolygon(hline, polygons);
            //没有交集，返回失败
            if (intersectLine.Count == 0)
                return null;
            while (buffer > 100)
            {
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
                    if ((pos.X - GetLeftPoint(i, j).X < maxGap))
                        possible_points.Add(pos);
                }
                buffer -= 100;
            }
            if (possible_points.Count == 0) return null;
            return possible_points.OrderBy(o => o.Distance(point)).First();
        }
        //计算竖线上距离point最近的可布置区域内的点，带buffer
        private Coordinate FindNearestPointOnVLineWithBuffer(int i, int j, List<Polygon> polygons, double buffer)
        {
            var point = Positions[i][j];
            //var point = GetTargetPoint(i, j);
            List<Coordinate> possible_points = new List<Coordinate>();
            //点所在竖线
            var top = new Coordinate(Positions[i][j].X, GetTopPoint(i, j).Y);
            var bottom = new Coordinate(Positions[i][j].X, GetBottomPoint(i, j).Y);
            var vline = new LineSegment(bottom, top);
            //与可布置区域的交集
            var intersectLine = FireAlarmUtils.LineIntersectWithMutiPolygon(vline, polygons);
            //没有交集，返回失败
            if (intersectLine.Count == 0)
                return null;
            while (buffer > 100)
            {
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
                    if (GetTopPoint(i, j).Y - pos.Y < maxGap)
                        possible_points.Add(pos);
                }
                buffer -= 100;
            }
            if (possible_points.Count == 0) return null;
            return possible_points.OrderBy(o => o.Distance(point)).First();
        }
        //移动竖线
        private bool AdjustVLine(int i, int j, double target_X)
        {
            var point = Positions[i][j];
            //距离不对，返回失败
            if (target_X - GetLeftPoint(i, j).X > maxGap//距离过大
                || (HasLeft(i, j) && target_X - GetLeftPoint(i, j).X < minGap)) //距离左边点过小
                return false;
            else//满足距离要求，查找该点上方的共线点
            {
                //先移动该线
                var old_X = Positions[i][j].X;
                UpdateVLine(i, j, target_X);
                var checkFalsePoints = new List<KeyValuePair<int, int>>();//移出可布置区域的点
                for (int h = 0; h < i; h++)
                {
                    //略过不在区域内的点和不共线的点
                    if (validPoints[h][j] == false || Positions[h][j].X != point.X)
                        continue;
                    if(!FireAlarmUtils.PolygonContainPoint(area,Positions[h][j]))
                    {
                        UpdateVLine(i, j, old_X);
                        return false;
                    }
                    if (!FireAlarmUtils.MultiPolygonContainPoint(layouts, Positions[h][j]))
                        checkFalsePoints.Add(new KeyValuePair<int, int>(h, j));
                }
                if (checkFalsePoints.Count == 0)//没有点移出可布置区域
                    return true;
                else if (checkFalsePoints.Count == 1)//有一个点移出可布置区域
                {
                    var point_index = checkFalsePoints[0];
                    var new_nearLayouts = GetNearLayouts(Positions[point_index.Key][point_index.Value]);
                    var new_target = FindNearestPointOnVLineWithBuffer(point_index.Key, point_index.Value, new_nearLayouts, bufferDist);
                    if (new_target != null && StrictAdjustHLine(point_index.Key, point_index.Value, new_target.Y))
                        return true;
                }
                //如果移动失败，要保证能够移动回去
                UpdateVLine(i, j, old_X);
                return false;
            }
        }
        //移动某条竖线后调整受影响的点
        private bool StrictAdjustHLine(int i, int j, double target_Y)
        {
            var point = Positions[i][j];
            //距离不对，返回失败
            if (GetTopPoint(i, j).Y - target_Y > maxGap//距离过大
                || (HasTop(i, j) && GetTopPoint(i, j).Y - target_Y < minGap))  //距离上方点过小
                return false;
            else//满足距离要求，查找该点所在横线上的点
            {
                var checkFalsePoints = new List<Coordinate>();
                //var hline = hLines[ptToLine[point].Key];
                for (int v = 0; v < vLines.Count; v++)
                {
                    if (validPoints[i][v] == false || Positions[i][v].Y != point.Y)
                        continue;
                    var adjustedPoint = new Coordinate(Positions[i][v].X, target_Y);
                    if (!FireAlarmUtils.MultiPolygonContainPoint(layouts, adjustedPoint))
                        checkFalsePoints.Add(adjustedPoint);
                }
                if (checkFalsePoints.Count == 0)//没有点移出可布置区域
                {
                    UpdateHLine(i, j, target_Y);
                    return true;
                }
                else return false;//有点移出可布置区域
            }
        }
        //移动横线
        private bool AdjustHLine(int i, int j, double target_Y)
        {
            var point = Positions[i][j];
            if (validPoints[i][j] == false)
                return false;
            //距离不对，返回失败
            if (GetTopPoint(i, j).Y - target_Y > maxGap//距离过大
                || (HasTop(i, j) && GetTopPoint(i, j).Y - target_Y < minGap))  //距离上方点过小
                return false;
            else//满足距离要求，查找该点左侧的点
            {
                var old_Y = point.Y;
                UpdateHLine(i, j, target_Y);
                var checkFalsePoints = new List<KeyValuePair<int, int>>();
                //var hline = hLines[ptToLine[point].Key];
                for (int v = 0; v < j; v++)
                {
                    if (validPoints[i][v] == false || Positions[i][v].Y != point.Y)
                        continue;
                    if (!FireAlarmUtils.PolygonContainPoint(area, Positions[i][v]))
                    {
                        UpdateHLine(i, j, old_Y);
                        return false;
                    }
                    if (!FireAlarmUtils.MultiPolygonContainPoint(layouts, Positions[i][v]))
                        checkFalsePoints.Add(new KeyValuePair<int, int>(i, v));
                }
                if (checkFalsePoints.Count == 0)//没有点移出可布置区域
                    return true;
                else if (checkFalsePoints.Count == 1)//有一个点移出可布置区域
                {
                    var point_index = checkFalsePoints[0];
                    var new_nearLayouts = GetNearLayouts(Positions[point_index.Key][point_index.Value]);
                    var new_target = FindNearestPointOnHLineWithBuffer(point_index.Key, point_index.Value, new_nearLayouts, bufferDist);
                    if (new_target != null && StrictAdjustVLine(point_index.Key, point_index.Value, new_target.X))
                        return true;
                }
                //如果受到影响的点不能通过移动所在竖线调整，或者影响了超过两个点，说明移动失败
                UpdateHLine(i, j, old_Y);
                return false;
            }
        }
        //移动某条横线后通过移动竖线调整受影响的点
        private bool StrictAdjustVLine(int i, int j, double target_X)
        {
            var point = Positions[i][j];
            //距离不对，返回失败
            if (target_X - GetLeftPoint(i, j).X > maxGap//距离过大
                || (HasLeft(i, j) && target_X - GetLeftPoint(i, j).X < minGap)//距离左边点过小
                || target_X - GetRightPoint(i, j).X > maxGap
                || (HasRight(i, j) && target_X - GetRightPoint(i, j).X < minGap))  //距离右边点过小
                return false;
            else//满足距离要求，查找该点上方的点
            {
                var checkFalsePoints = new List<Coordinate>();
                //var vline = vLines[ptToLine[point].Value];
                for (int h = 0; h < i; h++)
                {
                    if (validPoints[h][j] == false || Positions[h][j].Y != point.Y)
                        continue;
                    var adjustedPoint = new Coordinate(target_X, Positions[h][j].Y);
                    if (!FireAlarmUtils.MultiPolygonContainPoint(layouts, adjustedPoint))
                        checkFalsePoints.Add(adjustedPoint);
                }
                if (checkFalsePoints.Count == 0)//没有点移出可布置区域
                {
                    UpdateVLine(i, j, target_X);
                    return true;
                }
                else return false;//有点移出可布置区域
            }
        }
    }
}