using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Algorithm.Distance;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.OverlayNG;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPElectrical.AlarmSensorLayout.Data;
using ThMEPElectrical.AlarmSensorLayout.Method;

namespace ThMEPElectrical.AlarmSensorLayout.Sensorlayout
{
    class BeamSensorOpt : AlarmSensorLayout
    {
        public List<UCSOpt> UCSs { get; set; } = new List<UCSOpt>();//UCS列表
        public List<Coordinate> Positions { get; set; } = new List<Coordinate>();//交点位置
        public List<Polygon> Detect { get; set; } = new List<Polygon>();//每个点的探测范围
        
        private ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex;
        private double bufferDist =500;
        private double bufferArea = 500000;
        private NetTopologySuite.Geometries.Geometry blind;

        public BeamSensorOpt(InputArea inputArea, EquipmentParameter parameter)
            : base(inputArea, parameter)
        {
            DBObjectCollection dBObjectCollection = new DBObjectCollection();
            foreach (var layout in layouts)
                dBObjectCollection.Add(layout.ToDbMPolygon());
            thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(dBObjectCollection);//建立关于可布置区域的索引
            foreach (var record in UCS)
                UCSs.Add(new UCSOpt(record.Key, record.Value, room, layouts, MinGap, MaxGap, Radius, AdjustGap, bufferDist));
        }

        public override void Calculate()
        {
            if (layouts.Count==0)
                return;
            //每个UCS分别布点
            foreach(var UCS in UCSs)
            {
                UCS.CalculatePlace();
                foreach(var p in UCS.PlacePoints)
                    Positions.Add(p);
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
            CalBlindArea();
            //转化点位
            ConvertPoints();
            //转化盲区
            ConvertBlind();
        }
        //计算探测范围
        public void CalDetectArea()
        {
            foreach (var p in Positions)
                Detect.Add(DetectCalculator.CalculateDetect(p, room, Radius, IsDetectVisible));
        }
        //计算盲区
        public void CalBlindArea()
        {
            var poly = OverlayNGRobust.Union(Detect.ToArray());
            blind = room.Difference(poly);
        }
        //加点
        public void AddPoints()
        {
            while (blind.Area > 100)
            {
                var old_blind = blind.Area;
                //先处理掉非polygon的元素
                if (blind is GeometryCollection geom)
                {
                    var geometryCollection = new List<Polygon>();
                    foreach (var geo in geom)
                        if (geo is Polygon polygon)
                            geometryCollection.Add(polygon);
                    blind = new MultiPolygon(geometryCollection.ToArray());
                }
                //一次只处理一个polygon
                RemoveBlind(blind);
            }
        }
        //删点
        public void DeletePoints()
        {
            for(int i=0;i<Positions.Count;)
            {
                if (IsMovable(i))
                {
                    Positions.RemoveAt(i);
                    Detect.RemoveAt(i);
                }
                else i++;
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
                var polylist = new List<Polygon>();
                foreach (var j in nears)
                    polylist.Add(Detect[j]);
                resp = resp.Difference(OverlayNGRobust.Union(polylist.ToArray())).Buffer(-10);

                var smaller = layout.Buffer(-400);
                if (smaller.IsEmpty)
                {
                    var adjustPoint = FireAlarmUtils.AdjustedCenterPoint(layout);
                    if (DetectCalculator.CalculateDetect(adjustPoint, room, Radius, IsDetectVisible).Contains(resp))
                    {
                        Positions[i] = adjustPoint;
                        continue;
                    }
                    continue;
                }
                //var adjust = Methods.AdjustedCenterPoint(layout);
                //if (DetectCalculator.CalculateDetect(adjust, room, Radius, IsDetectVisible).Contains(resp))
                //{
                //    Positions[i] = adjust;
                //    continue;
                //}
                if (smaller.Contains(new Point(Positions[i]))) continue;
                if (smaller is Polygon polygon)
                {
                    var adjustPoint = FireAlarmUtils.GetClosePointOnPolygon(polygon, Positions[i]);
                    if (DetectCalculator.CalculateDetect(adjustPoint, room, Radius, IsDetectVisible).Contains(resp))
                        Positions[i] = adjustPoint;
                    continue;
                }
                else if (smaller is MultiPolygon multiPolygon)
                {
                    var adjustPoint = FireAlarmUtils.GetClosePointOnMultiPolygon(multiPolygon, Positions[i]);
                    if (DetectCalculator.CalculateDetect(adjustPoint, room, Radius, IsDetectVisible).Contains(resp))
                        Positions[i] = adjustPoint;
                    continue;
                }
            }
        }
        //寻找附近的点
        private List<int> FindNearPoints(int index)
        {
            var nears = new List<int>();
            for(int i=0;i<Positions.Count;i++)
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
            //对于边界上面积小于bufferArea的盲区，直接忽略它
            if(!room.Contains(targetToMove.Buffer(10))&& targetToMove.Area < bufferArea)
            {
                this.blind = this.blind.Difference(targetToMove);
                return;
            }
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
            if(polygon_layouts.Count==0)
            {
                this.blind = this.blind.Difference(targetToMove);
                return;
            }
            //优先找可布置区域的中心点
            var target = new Coordinate(center.X + Radius, center.Y + Radius);
            foreach (var poly in polygon_layouts)
            {
                Coordinate temp = Centroid.GetCentroid(poly);
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
            this.blind = this.blind.Difference(det);
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
                NetTopologySuite.Geometries.Geometry poly = Detect[points[0]];
                for (int i = 1; i < points.Count; i++)
                    poly = poly.Union(Detect[points[i]]);
                resp = resp.Difference(poly);
            }

            return resp.Area < 100;
        }
        //转化布置点集
        private void ConvertPoints()
        {
            foreach(var point in Positions)
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
