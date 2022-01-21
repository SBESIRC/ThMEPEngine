using NFox.Cad;
using System.Linq;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThCADExtension
{
    public static class ThPoint3dCollectionExtensions
    {
        public static Extents3d Envelope(this Point3dCollection points)
        {
            var extents = new Extents3d();
            points.Cast<Point3d>().ForEach(p => extents.AddPoint(p));
            return extents;
        }

        public static DBObjectCollection ToDBObjects(this Point3dCollection points)
        {
            return points.OfType<Point3d>().Select(o => new DBPoint(o)).ToCollection();
        }
    }
}
