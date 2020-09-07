using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPEngineCore.Model
{
    public class ThIfcBeam : ThIfcBuildingElement
    {
        public ThIfcBeam()
        {
        }
        public Point3d StartPoint { get; set; }
        public Point3d EndPoint { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public Vector3d Normal { get; set; }
        public BeamComponentType ComponentType { get; set; } = BeamComponentType.Undefined;
        public double ActualWidth
        {
            get
            {
                if (Outline != null)
                {
                    Polyline beamGeometry = Outline as Polyline;
                    return beamGeometry.GetPoint3dAt(0).DistanceTo(beamGeometry.GetPoint3dAt(1));
                }
                return 0.0;
            }
        }
    }
    public enum BeamComponentType
    {
        Undefined,
        /// <summary>
        /// 主梁
        /// 两端均为竖向构件
        /// </summary>
        PrimaryBeam,
        /// <summary>
        /// 半主梁
        /// 一端为竖向构件，另一端为主梁
        /// </summary>
        HalfPrimaryBeam,
        /// <summary>
        /// 悬挑主梁
        /// 一端为竖向构件,另一端无主梁或竖向构件，且无延续构件
        /// </summary>
        OverhangingPrimaryBeam,
        /// <summary>
        /// 次梁
        /// 两端均为主梁或半主梁
        /// </summary>
        SecondaryBeam
    }
}
