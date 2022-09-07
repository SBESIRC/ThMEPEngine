using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPTCH.TCHArchDataConvert.TCHArchTables;

namespace ThMEPTCH.TCHArchDataConvert.THArchEntity
{
    public class WallEntity : THArchEntityBase
    {
        public Point3d EndPoint { get; set; }
        public Point3d StartPoint { get; set; }
        public Curve LeftCurve { get; set; }
        public Curve RightCurve { get; set; }
        public Curve CenterCurve { get; set; }
        public double Elevation { get; set; }
        public double LeftWidth { get; set; }
        public double RightWidth { get; set; }
        public double Height { get; set; }

        public WallEntity(TArchEntity dbWall):base(dbWall)
        {
            //
        }

        public override void TransformBy(Matrix3d transform)
        {
            base.TransformBy(transform);
            EndPoint = EndPoint.TransformBy(transform);
            StartPoint = StartPoint.TransformBy(transform);
            LeftCurve.TransformBy(transform);
            RightCurve.TransformBy(transform);
            CenterCurve.TransformBy(transform);
        }
    }
}
