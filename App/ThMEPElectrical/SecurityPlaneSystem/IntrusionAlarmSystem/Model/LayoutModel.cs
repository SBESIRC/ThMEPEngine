using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.SecurityPlaneSystem.IntrusionAlarmSystem.Model
{
    public class LayoutModel
    {
        public ControllerModel controller { get; set; }

        public DetectorModel detector { get; set; }
    }

    public class ControllerModel
    {
        public Point3d LayoutPoint { get; set; }

        public Vector3d LayoutDir { get; set; }
    }

    public class DetectorModel
    {
        public Point3d LayoutPoint { get; set; }

        public Vector3d LayoutDir { get; set; }

    }
}
