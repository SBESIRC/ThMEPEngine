using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.Model;

namespace ThMEPTCH.Model
{
    /// <summary>
    /// 栏杆
    /// </summary>
    public class ThTCHRailing : ThIfcRailing
    {
        public double Depth { get; set; }
        public double Thickness { get; set; }
        public Vector3d ExtrudedDirection { get; set; }
    }
}
