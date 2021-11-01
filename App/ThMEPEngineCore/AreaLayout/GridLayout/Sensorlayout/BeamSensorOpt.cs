using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
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
        public List<UnderUCSOpt> underUCSs { get; set; } = new List<UnderUCSOpt>();//地下UCS列表
        public List<UpUCSOpt> upUCSs { get; set; } = new List<UpUCSOpt>();//地上UCS列表
        public List<Coordinate> Positions { get; set; } = new List<Coordinate>();//交点位置
        public List<Polygon> Detect { get; set; } = new List<Polygon>();//每个点的探测范围

        private ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex;
        private double bufferDist = 500;
        private double bufferArea = 500000;
        private double innerBufferArea = 30000;//内部盲区
        private double bufferAreaToMove = 20000;
        private NetTopologySuite.Geometries.Geometry blind;
        public bool IsUpOrUnder;//地上图纸还是地下图纸

        public BeamSensorOpt(InputArea inputArea, EquipmentParameter parameter)
            : base(inputArea, parameter)
        {
            DBObjectCollection dBObjectCollection = new DBObjectCollection();
            foreach (var layout in layouts)
                dBObjectCollection.Add(layout.ToDbMPolygon());
            thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(dBObjectCollection);//建立关于可布置区域的索引

            IsUpOrUnder = CheckUpOrUnder();
            if (IsUpOrUnder)
            {
                foreach (var record in UCS)
                    underUCSs.Add(new UnderUCSOpt(record.Key, record.Value, room, layouts, columnCenters, MinGap, MaxGap, Radius, AdjustGap, bufferDist));
            }
            else
            {
                foreach (var record in UCS)
                    upUCSs.Add(new UpUCSOpt(record.Key, record.Value, room, layouts, MinGap, MaxGap, Radius, AdjustGap, bufferDist));
            }
        }

        public override void Calculate()
        {
            if (layouts.Count == 0)
                return;
            if (IsUpOrUnder)
            {
                //每个UCS分别布点
                foreach (var UCS in underUCSs)
                {
                    UCS.CalculatePlace();
                    foreach (var p in UCS.PlacePoints)
                        Positions.Add(p);
                }
            }
            else
            {
                //每个UCS分别布点
                foreach (var UCS in upUCSs)
                {
                    UCS.CalculatePlace();
                    foreach (var p in UCS.PlacePoints)
                        Positions.Add(p);
                }
            }

            //计算每个点的覆盖区域
            CalDetectArea();
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
                Detect.Add(DetectCalculator.CalculateDetect(p, room, Radius, IsDetectVisible));
        }
        //计算盲区
        public void CalBlindArea()
        {
            var poly = OverlayNGRobust.Union(Detect.ToArray());
            blind = OverlayNGRobust.Overlay(room, poly, SpatialFunction.Difference);
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
                            if ((!room.Contains(polygon.Buffer(10)) && polygon.Area < bufferArea) || (polygon.Area < innerBufferArea))
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
            for (int i = Positions.Count - 1; i >= 0; i--)
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
                var layout = thCADCoreNTSSpatialIndex.SelectCrossingWindow(min, max).Cast<MPolygon>().First().ToNTSPolygon();
                //计算该点单独负责的区域
                var resp = Detect[i].Copy();
                var nears = FindNearPoints(i);
                if (nears.Count > 0)
                {
                    var nearest = nears.OrderBy(o => Positions[o].Distance(Positions[i])).First();
                    if (Positions[nearest].Distance(Positions[i]) < 800)
                    {
                        var midPoint = new Coordinate((Positions[nearest].X + Positions[i].X) / 2, (Positions[nearest].Y + Positions[i].Y) / 2);
                        if (layout.Contains(new Point(midPoint)))
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
                //resp = resp.Difference(OverlayNGRobust.Union(polylist.ToArray())).Buffer(-10);

                var smaller = layout.Buffer(-400);
                Coordinate adjustPoint = null;
                if (smaller.IsEmpty)
                {
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
                if (Detecti.Contains(resp))
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
            //能探测到中心点的布置区域
            var vis = DetectCalculator.CalculateDetect(center, room, Radius, IsDetectVisible);
            //附近的可布置区域
            var minRect = vis.Envelope as Polygon;
            double minX = minRect.Coordinates[0].X;
            double minY = minRect.Coordinates[0].Y;
            double maxX = minRect.Coordinates[2].X;
            double maxY = minRect.Coordinates[2].Y;
            var min = new Point3d(minX, minY, 0);
            var max = new Point3d(maxX, maxY, 0);
            var dblayouts = thCADCoreNTSSpatialIndex.SelectCrossingWindow(min, max).Cast<MPolygon>().ToList();
            var polygon_layouts = new List<Polygon>();
            foreach (var layout in dblayouts)
                polygon_layouts.Add(layout.ToNTSPolygon());
            //存在无解的盲区
            if (polygon_layouts.Count == 0)
            {
                this.blind = this.blind.Difference(targetToMove);
                return;
            }
            //优先找可布置区域的中心点
            var target = new Coordinate(center.X + Radius, center.Y + Radius);
            foreach (var poly in polygon_layouts)
            {
                Coordinate temp = FireAlarmUtils.AdjustedCenterPoint(poly);
                if (FireAlarmUtils.PolygonContainPoint(vis, temp) && temp.Distance(center) < target.Distance(center))
                    target = temp;
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
            Positions.Add(target);
            var det = DetectCalculator.CalculateDetect(target, room, Radius, IsDetectVisible);
            Detect.Add(det);
            this.blind = OverlayNGRobust.Overlay(this.blind, det, SpatialFunction.Difference);
            //if (this.blind.Area == oldArea)
            //    this.blind = this.blind.Difference(targetToMove);
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

            return resp.Area < bufferAreaToMove;
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
