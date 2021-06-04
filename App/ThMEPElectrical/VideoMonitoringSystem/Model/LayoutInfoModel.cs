using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.VideoMonitoringSystem.Model
{
    public class LayoutInfoModel
    {
        /// <summary>
        /// 所属房间
        /// </summary>
        public Polyline room { get; set; }

        /// <summary>
        /// 门中心点
        /// </summary>
        public Point3d doorCenterPoint { get; set; }

        /// <summary>
        /// 门朝房间内正方向
        /// </summary>
        public Vector3d doorDir { get; set; }
 
        /// <summary>
        /// 可布置柱
        /// </summary>
        public List<Polyline> colums = new List<Polyline>();

        /// <summary>
        /// 可布置墙
        /// </summary>
        public List<Polyline> walls = new List<Polyline>();
    }
}
