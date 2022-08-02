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
        public RouteModel(Polyline _route, VerticalPipeType _type, Point3d point, bool _isEquipment, bool _isFloorDrainPipe)
        {
            route = _route;
            verticalPipeType = _type;
            startPosition = point;
            IsEquimentPipe = _isEquipment;
            IsFloorDrainPipe = _isFloorDrainPipe;
        }

        public Point3d startPosition { get; set; }

        public Circle printCircle { get; set; }

        public Circle originCircle { get; set; }

        public BlockReference block { get; set; }

        public Polyline route { get; set; }

        public VerticalPipeType verticalPipeType { get; set; }

        public Line connecLine { get; set; }

        /// <summary>
        /// 是否是洁具点位
        /// </summary>
        public bool IsEquimentPipe = false;

        /// <summary>
        /// 是否是地漏点位
        /// </summary>
        public bool IsFloorDrainPipe = false;

        /// <summary>
        /// 是否是连接支管（污废合流接上去的支管）
        /// </summary>
        public bool IsBranchPipe = false;

        /// <summary>
        /// 有堵头
        /// </summary>
        public bool HasReservedPlug = false;
    }
}
