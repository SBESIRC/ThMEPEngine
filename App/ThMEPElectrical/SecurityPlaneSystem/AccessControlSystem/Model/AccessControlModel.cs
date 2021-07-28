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

    /// <summary>
    /// 电锁按钮
    /// </summary>
    public class Buttun : AccessControlModel
    { }

    /// <summary>
    /// 读卡器
    /// </summary>
    public class CardReader : AccessControlModel
    { }

    /// <summary>
    /// 电锁
    /// </summary>
    public class ElectricLock : AccessControlModel
    { }

    /// <summary>
    /// 出入口对讲门口机
    /// </summary>
    public class Intercom : AccessControlModel
    { }
}
