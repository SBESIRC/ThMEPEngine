using Autodesk.AutoCAD.Geometry;
using ThMEPTCH.TCHArchDataConvert.TCHArchTables;

namespace ThMEPTCH.TCHArchDataConvert.THArchEntity
{
    class WindowEntity : THArchEntityBase
    {
        public Point3d BasePoint { get; set; }
        public double Rotation { get; set; }
        public double Thickness { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public WindowTypeEnum WindowType { get; set; }

        public WindowEntity(TArchEntity archEntity) : base(archEntity)
        {
        }

        public override void TransformBy(Matrix3d transform)
        {
            base.TransformBy(transform);
            if (DBArchEntity is TArchWindow window)
            {
                Rotation = window.Rotation;
                BasePoint = window.BasePoint;
            }
        }
    }
}
