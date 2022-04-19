using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPWSS.FirstFloorDrainagePlaneSystem.Model
{
    public class RouteModel
    {
        public RouteModel(Polyline _route, VerticalPipeType _type, Point3d point, bool _isEquipment)
        {
            route = _route;
            verticalPipeType = _type;
            startPosition = point;
            IsEquimentPipe = _isEquipment;
        }

        public Point3d startPosition { get; set; }

        public Circle printCircle { get; set; }

        public Polyline route { get; set; }

        public VerticalPipeType verticalPipeType { get; set; }

        public Line connecLine { get; set; }

        /// <summary>
        /// 是否是洁具点位
        /// </summary>
        public bool IsEquimentPipe = false;
    }
}
