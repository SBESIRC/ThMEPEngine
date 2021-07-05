using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.SecurityPlaneSystem.AccessControlSystem.Model
{
    public class AccessControlModel
    {
        /// <summary>
        /// 排布点位
        /// </summary>
        public Point3d layoutPt { get; set; }

        // <summary>
        /// 排布方向
        /// </summary>
        public Vector3d layoutDir { get; set; }
    }

    public class Buttun : AccessControlModel
    { }

    public class CardReader : AccessControlModel
    { }

    public class ElectricLock : AccessControlModel
    { }

    public class Intercom : AccessControlModel
    { }
}
