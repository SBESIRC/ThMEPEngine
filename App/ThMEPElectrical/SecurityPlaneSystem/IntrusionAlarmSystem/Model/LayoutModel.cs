using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Model;

namespace ThMEPElectrical.SecurityPlaneSystem.IntrusionAlarmSystem.Model
{
    public class LayoutModel
    {

        public Point3d LayoutPoint { get; set; }

        public Vector3d LayoutDir { get; set; }

        public ThIfcRoom Room { get; set; }
    }

    /// <summary>
    /// 控制器
    /// </summary>
    public class ControllerModel : LayoutModel
    { }

    /// <summary>
    /// 探测器
    /// </summary>
    public class DetectorModel : LayoutModel
    { }

    /// <summary>
    /// 声光报警按钮
    /// </summary>
    public class SoundLightAlarm : LayoutModel
    { }

    /// <summary>
    /// 残卫报警按钮
    /// </summary>
    public class DisabledAlarmButtun : LayoutModel
    { }

    /// <summary>
    /// 紧急报警按钮
    /// </summary>
    public class EmergencyAlarmButton : LayoutModel
    { }

    /// <summary>
    /// 红外壁装探测器
    /// </summary>
    public class InfraredWallDetectorModel : DetectorModel
    { }

    /// <summary>
    /// 双鉴壁装探测器
    /// </summary>
    public class DoubleWallDetectorModel : DetectorModel
    { }
    
    /// <summary>
    /// 红外吊装探测器
    /// </summary>
    public class InfraredHositingDetectorModel : DetectorModel
    { }

    /// <summary>
    /// 双鉴吊装探测器
    /// </summary>
    public class DoubleHositingDetectorModel : DetectorModel
    { }
}
