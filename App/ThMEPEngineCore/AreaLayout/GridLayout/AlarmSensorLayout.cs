using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Overlay;
using NetTopologySuite.Operation.OverlayNG;
using ThCADCore.NTS;
using ThMEPEngineCore.AreaLayout.GridLayout.Data;

namespace ThMEPEngineCore.AreaLayout.GridLayout
{
    public abstract class AlarmSensorLayout
    {
        protected List<Point3d> m_placePoints; //布置点集
        protected List<Polyline> m_blinds; //盲区

        //输入区域参数
        public Polygon room { get; private set; }//房间区域
        public List<Polygon> layouts { get; private set; } = new List<Polygon>();//可布置区域
        public List<Polygon> detects { get; private set; } = new List<Polygon>();//探测区域

        public List<Coordinate> columnCenters { get; private set; } = new List<Coordinate>();//柱子中心
        protected Dictionary<Polyline, double> UCS { get; private set; } = new Dictionary<Polyline, double>();//UCS，弧度
        //输入设施参数
        protected double Radius;
        protected double MinGap;
        protected double MaxGap;
        protected double AdjustGap;
        protected bool IsDetectVisible;

        public List<Point3d> PlacePoints
        {
            get { return m_placePoints; }
        }
        public List<Polyline> Blinds
        {
            get { return m_blinds; }
        }
        public abstract void Calculate();
        public AlarmSensorLayout(InputArea inputArea, EquipmentParameter inputParameter)
        {
            m_placePoints = new List<Point3d>();
            m_blinds = new List<Polyline>();
            //生成房间区域
            room = inputArea.room.ToNTSPolygon();
            foreach (var hole in inputArea.holes)
            {
                if (hole.Area / room.Area > 0.9) continue;
                var geo = OverlayNGRobust.Overlay(room, hole.ToNTSPolygon(), SpatialFunction.Difference);
                if (geo is Polygon polygon)
                    room = polygon;
                else if (geo is GeometryCollection collection)
                {
                    Polygon tmpPoly = Polygon.Empty;
                    foreach (var poly in collection)
                    {
                        if (poly is Polygon && poly.Area > tmpPoly.Area)
                            tmpPoly = poly as Polygon;
                    }
                    room = tmpPoly;
                }
            }

            foreach (var wall in inputArea.walls)
            {
                var geo = OverlayNGRobust.Overlay(room, wall.ToNTSPolygon(), SpatialFunction.Difference);
                if (geo is Polygon polygon)
                    room = polygon;
                else if (geo is GeometryCollection collection)
                {
                    Polygon tmpPoly = Polygon.Empty;
                    foreach (var poly in collection)
                    {
                        if (poly is Polygon && poly.Area > tmpPoly.Area)
                            tmpPoly = poly as Polygon;
                    }
                    room = tmpPoly;
                }
            }
            foreach (var column in inputArea.columns)
            {
                var geo = OverlayNGRobust.Overlay(room, column.ToNTSPolygon(), SpatialFunction.Difference);
                if (geo is Polygon polygon)
                    room = polygon;
                else if (geo is GeometryCollection collection)
                {
                    Polygon tmpPoly = Polygon.Empty;
                    foreach (var poly in collection)
                    {
                        if (poly is Polygon && poly.Area > tmpPoly.Area)
                            tmpPoly = poly as Polygon;
                    }
                    room = tmpPoly;
                }
            }

            //生成可布置区域
            foreach (var layout in inputArea.layout_area)
            {
                var NTSlayout = layout.ToNTSPolygon();
                var holesInLayout = new List<Polygon>();
                foreach (var priority in inputArea.prioritys)
                    holesInLayout.Add(priority.ToNTSPolygon());
                foreach (var column in inputArea.columns)
                    holesInLayout.Add(column.ToNTSPolygon());
                var layoutRegion = NTSlayout.Difference(OverlayNGRobust.Union(holesInLayout.ToArray()));
                if (layout.Area < 200)
                    continue;
                if (layoutRegion is Polygon polygon)
                    layouts.Add(polygon);
                else if (layoutRegion is MultiPolygon multi)
                {
                    foreach (Polygon poly in multi)
                        layouts.Add(poly);
                }
            }
            //生成柱子中点
            foreach (var column in inputArea.columns)
            {
                var centerPoint = column.GetCentroidPoint().ToNTSCoordinate();
                columnCenters.Add(centerPoint);
            }

            //生成探测区域
            foreach (var detect in inputArea.detect_area)
                detects.Add(detect.ToNTSPolygon());
            detects = detects.OrderByDescending(o => o.Area).ToList();

            //生成UCS
            foreach (var record in inputArea.UCS)
            {
                double angle = Math.Atan2(record.Value.Y, record.Value.X);
                if (angle > Math.PI / 4)
                    angle = Math.PI / 2 - angle;
                else if (angle < -Math.PI / 4)
                    angle = -Math.PI / 2 - angle;
                UCS.Add(record.Key, angle);
            }
            //设置参数
            Radius = inputParameter.ProtectRadius;
            MinGap = inputParameter.MinGap;
            MaxGap = inputParameter.MaxGap;
            AdjustGap = inputParameter.AdjustGap;
            IsDetectVisible = inputParameter.blindType == BlindType.VisibleArea;
        }
    }
}
