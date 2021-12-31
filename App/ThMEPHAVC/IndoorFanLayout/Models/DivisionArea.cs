using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace ThMEPHVAC.IndoorFanLayout.Models
{
    class DivisionArea
    {
        public string Uid { get; }
        public DivisionArea(bool isArc,Polyline polyline) 
        {
            this.IsArc = isArc;
            this.Uid = System.Guid.NewGuid().ToString();
            this.AreaPolyline = polyline;
            this.AreaCurves = IndoorFanCommon.GetPolylineCurves(polyline);
            this.CenterPoint = IndoorFanCommon.PolylinCenterPoint(polyline);
            //矩形区域为区域中心，弧形区域为弧的圆心
            this.ArcCenterPoint = CenterPoint;
            if (isArc) 
            {
                var arc = this.AreaCurves.OfType<Arc>().FirstOrDefault();
                if (null != arc)
                    this.ArcCenterPoint = arc.Center;
            }
        }
        public bool IsArc { get; }
        public Point3d CenterPoint { get;}
        public Point3d ArcCenterPoint { get; set; }
        public Vector3d XVector { get; set; }
        public List<Curve> AreaCurves { get; }
        public Polyline AreaPolyline { get;}
        
    }
}
