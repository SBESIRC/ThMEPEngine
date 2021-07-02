using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.SecurityPlaneSystem.VideoMonitoringSystem.Model
{
    public class LayoutModel
    {
        /// <summary>
        /// 布置点位
        /// </summary>
        public Point3d layoutPt { get; set; }

        /// <summary>
        /// 布置方向s
        /// </summary>
        public Vector3d layoutDir { get; set; }
    }

    public class GunCameraModel : LayoutModel
    { }

    public class PanTiltCameraModel : LayoutModel
    { }

    public class DomeCameraModel : LayoutModel
    { }

    public class GunCameraWithShieldModel : LayoutModel
    { }

    public class FaceRecognitionCameraModel : LayoutModel
    { }
}
