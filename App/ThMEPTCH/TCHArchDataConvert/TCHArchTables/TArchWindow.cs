using Autodesk.AutoCAD.Geometry;
using ThMEPTCH.CAD;

namespace ThMEPTCH.TCHArchDataConvert.TCHArchTables
{
    public enum WindowTypeEnum
    {
        /// <summary>
        /// 普通窗
        /// </summary>
        Window = 0,
        /// <summary>
        /// 百叶窗
        /// </summary>
        Shutter = 1,
        /// <summary>
        /// 偏心窗
        /// </summary>
        Eccentric = 2,
    };

    public class TArchWindow : TArchEntity
    {
        public double Height { get; set; }
        public double Width { get; set; }
        public double Thickness { get; set; }
        public double Rotation { get; set; }
        public Point3d BasePoint { get; set; }
        public WindowTypeEnum WindowType { get; set; }

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
