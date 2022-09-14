using Autodesk.AutoCAD.Geometry;
using ThMEPTCH.PropertyServices.PropertyEnums;

namespace ThMEPTCH.TCHArchDataConvert.TCHArchTables
{
    public class TArchWall : TArchEntity
    {
        public Point3d EndPoint { get; set; }
        public Point3d StartPoint { get; set; }
        public double LeftWidth { get; set; }
        public double RightWidth { get; set; }
        public bool IsArc { get; set; }
        public double Bulge { get; set; }
        public double Height { get; set; }
        public double Elevation { get; set; }
        public EnumTCHWallMaterial EnumMaterial { get; set; }

        public override bool IsValid()
        {
            var width = LeftWidth + RightWidth;
            var length = StartPoint.DistanceTo(EndPoint);
            return length > 1.0 && width > 1.0;
        }

        public override void TransformBy(Matrix3d transform)
        {
            EndPoint = EndPoint.TransformBy(transform);
            StartPoint = StartPoint.TransformBy(transform);
        }
    }
}
