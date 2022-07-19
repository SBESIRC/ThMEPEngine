using Autodesk.AutoCAD.Geometry;

namespace ThMEPWSS.WaterSupplyPipeSystem.Method
{
    public static class PtTools
    {
        public static Point3d Clone(this Point3d pt)
        {
            return new Point3d(pt.X, pt.Y, 0);
        }
    }
}
