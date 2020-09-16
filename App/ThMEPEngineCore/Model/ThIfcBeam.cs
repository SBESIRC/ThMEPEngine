using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Newtonsoft.Json;
using ThMEPEngineCore.CAD;

namespace ThMEPEngineCore.Model
{
    public abstract class ThIfcBeam : ThIfcBuildingElement
    {
        public ThIfcBeam()
        {
        }
        [JsonConverter(typeof(Point3dConverter))]
        public Point3d StartPoint { get; set; }
        [JsonConverter(typeof(Point3dConverter))]
        public Point3d EndPoint { get; set; }
        [JsonProperty]
        public double Width { get; set; }
        [JsonProperty]
        public double Height { get; set; }
        [JsonConverter(typeof(Vector3dConverter))]
        public Vector3d Normal { get; set; }
        [JsonConverter(typeof(BeamTypeConverter))]
        public BeamComponentType ComponentType { get; set; } = BeamComponentType.Undefined;
        [JsonIgnore]
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
        public abstract Polyline Extend(double length,double width);
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
