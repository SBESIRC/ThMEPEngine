using Autodesk.AutoCAD.Geometry;
using ThMEPTCH.CAD;

namespace ThMEPTCH.TCHArchDataConvert.TCHArchTables
{
    public enum DoorTypeOperationEnum
    {
        /// <summary>
        /// 平开门
        /// </summary>
        SWING0001 = 1,
        SWING0002 = 2,
        SWING0003 = 3,
        SWING0004 = 4,
        SWING0009 = 9,
        SWING0010 = 10,
        SWING0011 = 11,
        SWING0012 = 12,
        SWING0021 = 21,
        SWING0114 = 114,
        SWING0116 = 116,
        SWING0222 = 222,
        SWING0223 = 223,
        SWING0224 = 224,
        SWING0225 = 225,
        SWING0226 = 226,
        SWING0228 = 228,
        SWING0231 = 231,

        /// <summary>
        /// 推拉门
        /// </summary>
        SLIDING0127 = 127,
        SLIDING0128 = 128,
        SLIDING0129 = 129,
        SLIDING0130 = 130,
        SLIDING0131 = 131,
        SLIDING0132 = 132,
        SLIDING0134 = 134,
        SLIDING0135 = 135,
        SLIDING0138 = 138,
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
