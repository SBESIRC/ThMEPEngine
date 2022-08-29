using Autodesk.AutoCAD.Geometry;
using ThMEPTCH.CAD;

namespace ThMEPTCH.TCHArchDataConvert.TCHArchTables
{
    public enum DoorTypeOperationEnum
    {
        /// <summary>
        /// 平开门
        /// </summary>
        SWING,
        /// <summary>
        /// 推拉门
        /// </summary>
        SLIDING,
    }

    public enum SwingEnum
    {
        SWING_RIGHT_IN = 0,
        SWING_LEFT_IN = 1,
        SWING_LEFT_OUT = 2,
        SWING_RIGHT_OUT = 3,
    }

    public class TArchDoor : TArchEntity
    {
        public double Height { get; set; }
        public double Width { get; set; }
        public double Thickness { get; set; }
        public double Rotation { get; set; }
        public Point3d BasePoint { get; set; }
        public SwingEnum Swing { get; set; }
        public DoorTypeOperationEnum OperationType { get; set; }

        public override bool IsValid()
        {
            return Width > 1.0 && Thickness > 1.0;
        }

        public override void TransformBy(Matrix3d transform)
        {
            var profile = this.Profile();
            profile.TransformBy(transform);
            this.SyncWithProfile(profile);
        }
    }
}
