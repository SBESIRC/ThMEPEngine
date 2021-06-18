using NFox.Cad;
using System.Linq;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThCADExtension
{
    public static class ThDbObjectCollectionExtension
    {
        public static DBObjectCollection Except(this DBObjectCollection coll, DBObjectCollection others)
        {
            return coll.Cast<Entity>().Except(others.Cast<Entity>()).ToCollection();
        }

        public static Extents3d GeometricExtents(this DBObjectCollection coll)
        {
            var extents = new Extents3d();
            coll.Cast<Entity>().ForEach(e => extents.AddExtents(e.GeometricExtents));
            return extents;
        }
    }
}
