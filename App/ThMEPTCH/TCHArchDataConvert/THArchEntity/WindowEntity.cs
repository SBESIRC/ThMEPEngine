using Autodesk.AutoCAD.Geometry;
using ThMEPTCH.TCHArchDataConvert.TCHArchTables;

namespace ThMEPTCH.TCHArchDataConvert.THArchEntity
{
    class WindowEntity : THArchEntityBase
    {
        public Point3d MidPoint { get; set; }
        public Point3d TextPoint { get; set; }
        public double Rotation { get; set; }
        public double Thickness { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double MinZ
        {
            get
            {
                return MidPoint.Z;
            }
        }
        public double WallMaxZ
        {
            get
            {
                return MidPoint.Z + Height;
            }
        }
        public WindowEntity(TArchEntity archEntity):base(archEntity)
        {
        }
    }
}
