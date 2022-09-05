using Autodesk.AutoCAD.Geometry;
using ThMEPTCH.TCHArchDataConvert.TCHArchTables;

namespace ThMEPTCH.TCHArchDataConvert.THArchEntity
{
    public class DoorEntity : THArchEntityBase
    {
        public Point3d BasePoint { get; set; }
        public double Rotation { get; set; }
        public double Thickness { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public DoorEntity(TArchEntity dbDoor) : base(dbDoor)
        {
        }

        public override void TransformBy(Matrix3d transform)
        {
            base.TransformBy(transform);
            if (DBArchEntity is TArchDoor door)
            {
                Rotation = door.Rotation;
                BasePoint = door.BasePoint;
            }
        }
    }
}
