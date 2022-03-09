using Autodesk.AutoCAD.Geometry;

namespace ThMEPElectrical.Model
{
    public class ThBConvertFanPoints
    {
        public Point3d Inlet { get; set; }
        public Point3d Outlet { get; set; }
        public bool OutletExist { get; set; }

        public ThBConvertFanPoints()
        {
            Inlet = new Point3d();
            Outlet = new Point3d();
            OutletExist = false;
        }
    }
}
