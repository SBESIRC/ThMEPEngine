using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.OverlayNG;
using ThCADCore.NTS;
using ThMEPElectrical.AlarmSensorLayout.Data;


namespace ThMEPElectrical.AlarmSensorLayout
{
    public abstract class AlarmSensorLayout
    {
        protected List<Point3d> m_placePoints; //布置点集
        protected List<Polyline> m_blinds; //盲区

        //输入区域参数
        public Polygon room { get; private set; }//房间区域
        protected List<Polygon> layouts { get; private set; } = new List<Polygon>();//可布置区域
        public List<Polyline> detects { get; private set; }//探测区域
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
            foreach(var hole in inputArea.holes)
                room = room.Difference(hole.ToNTSPolygon()) as Polygon;
            foreach(var wall in inputArea.walls)
                room = room.Difference(wall.ToNTSPolygon()) as Polygon;
            foreach (var column in inputArea.columns)
                room = room.Difference(column.ToNTSPolygon()) as Polygon;

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
                if (layoutRegion is Polygon polygon)
                    layouts.Add(polygon);
                else if(layoutRegion is MultiPolygon multi)
                {
                    foreach (Polygon poly in multi)
                        layouts.Add(poly);
                }
            }
            //生成UCS
            foreach(var record in inputArea.UCS)
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
