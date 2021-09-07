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
        public List<Coordinate> AddList { get; set; }  = new List<Coordinate>();//补盲区新增点位
        public ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex;
        public double bufferDist =500;
        public double bufferArea = 500000;
        public NetTopologySuite.Geometries.Geometry blind;

        public BeamSensorOpt(InputArea inputArea, EquipmentParameter parameter)
            : base(inputArea, parameter)
        {
            IsLayoutByBeams = true;
            DBObjectCollection dBObjectCollection = new DBObjectCollection();
            foreach (var layout in layouts)
                dBObjectCollection.Add(layout.ToDbMPolygon());
            thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(dBObjectCollection);//建立关于可布置区域的索引
            foreach (var record in UCS)
                UCSs.Add(new UCSOpt(record.Key, record.Value, room, layouts, MinGap, MaxGap, Radius, AdjustGap, bufferDist));
        }

        public override List<Point3d> CalculatePlace()
        {
            if (layouts.Count==0)
                return null;

            foreach(var UCS in UCSs)
            {
                UCS.CalculatePlace();
                foreach(var p in UCS.PlacePoints)
                    Positions.Add(p);
            }
            CalDetectArea();
            CalBlindArea();
            AddPoints();
            DeletePoints();
            AdjustPoints();
            CalBlindArea();
            return PlacePoints;
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
            //for (int h_index = 0; h_index < hLines.Count; h_index++)
            //{
            //    for (int v_index = 0; v_index < vLines.Count; v_index++)
            //    {
            //        if(IsMovable(h_index, v_index))
            //        {
            //            validPoints[h_index][v_index] = false;
            //            for (int row = v_index; row >= 0; row -= 2) 
            //            {
            //                for (int column = h_index - 2; column >= 0; column -= 2)
            //                    if (IsMovable(column, row))
            //                        validPoints[column][row] = false;
            //                for (int column = h_index + 2; column < hLines.Count; column += 2) 
            //                    if (IsMovable(column, row))
            //                        validPoints[column][row] = false;
            //                for (int column = h_index - 1; column >= 0; column -= 2)
            //                    if (IsMovable(column, row))
            //                        validPoints[column][row] = false;
            //                for (int column = h_index + 1; column < hLines.Count; column += 2) 
            //                    if (IsMovable(column, row))
            //                        validPoints[column][row] = false;
            //            }
            //            for (int row = v_index + 2; row < vLines.Count; row += 2) 
            //            {
            //                for (int column = h_index - 2; column >= 0; column -= 2)
            //                    if (IsMovable(column, row))
            //                        validPoints[column][row] = false;
            //                for (int column = h_index + 2; column < hLines.Count; column += 2) 
            //                    if (IsMovable(column, row))
            //                        validPoints[column][row] = false;
            //                for (int column = h_index - 1; column >= 0; column -= 2)
            //                    if (IsMovable(column, row))
            //                        validPoints[column][row] = false;
            //                for (int column = h_index + 1; column < hLines.Count; column += 2) 
            //                    if (IsMovable(column, row))
            //                        validPoints[column][row] = false;
            //            }
            //        }
            //    }
            //}
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
                    var adjustPoint = Methods.AdjustedCenterPoint(layout);
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
                    var adjustPoint = Methods.GetClosePointOnPolygon(polygon, Positions[i]);
                    if (VisiblePolygon.ComputeWithRadius(adjustPoint, room, Radius).Contains(resp))
                        Positions[i] = adjustPoint;
                    continue;
                }
                else if (smaller is MultiPolygon multiPolygon)
                {
                    var adjustPoint = Methods.GetClosePointOnMultiPolygon(multiPolygon, Positions[i]);
                    if (VisiblePolygon.ComputeWithRadius(adjustPoint, room, Radius).Contains(resp))
                        Positions[i] = adjustPoint;
                    continue;
                }
            }
        }
        //寻找附近的点
        public List<int> FindNearPoints(int index)
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
        public void RemoveBlind(NetTopologySuite.Geometries.Geometry blind)
        {
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
            var center = Methods.AdjustedCenterPoint(targetToMove);
            //能探测到中心点的布置区域
            var vis = VisiblePolygon.ComputeWithRadius(center, room, 5800);
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
            var target = new Coordinate(center.X + 5800, center.Y + 5800);
            foreach (var poly in polygon_layouts)
            {
                Coordinate temp = Centroid.GetCentroid(poly);
                if (Methods.PolygonContainPoint(vis, temp) && temp.Distance(center) < target.Distance(center))
                    target = temp;
            }
            //找不到就找交集的中心点
            if (target.Distance(center) > 5800)
            {
                foreach (var poly in polygon_layouts)
                {
                    var intersect = poly.Intersection(vis);
                    Coordinate inter_center = null;
                    if (intersect.Area == 0) continue;
                    if (intersect is Polygon polygon1)
                        inter_center = Methods.AdjustedCenterPoint(polygon1);
                    else if (intersect is MultiPolygon multi)
                        inter_center = Methods.AdjustedCenterPoint(multi.First() as Polygon);
                    if (inter_center.Distance(center) < target.Distance(center))
                        target = inter_center;
                }
            }
            Positions.Add(target);
            var det = VisiblePolygon.ComputeWithRadius(target, room, Radius);
            Detect.Add(det);
            this.blind = this.blind.Difference(det);
        }
        //是否可以删除
        public bool IsMovable(int index)
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
    }
}
