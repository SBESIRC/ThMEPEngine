using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using NFox.Cad;
using System.Collections.Generic;
using System.Linq;

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

        public static DBObjectCollection Union(this DBObjectCollection first, DBObjectCollection second)
        {
            var firstHash = first.OfType<DBObject>().ToHashSet();
            var secondHash = second.OfType<DBObject>().ToHashSet();
            return firstHash.Union(secondHash).ToCollection();
        }

        public static DBObjectCollection Difference(this DBObjectCollection first, DBObjectCollection second)
        {
            var firstHash = first.OfType<DBObject>().ToHashSet();
            var secondHash = second.OfType<DBObject>().ToHashSet();
            return firstHash.Except(secondHash).ToCollection();
        }

        public static DBObjectCollection Intersect(this DBObjectCollection first, DBObjectCollection second)
        {
            var firstHash = first.OfType<DBObject>().ToHashSet();
            var secondHash = second.OfType<DBObject>().ToHashSet();
            return firstHash.Intersect(secondHash).ToCollection();
        }

        public static DBObjectCollection Distinct(this DBObjectCollection objs)
        {
            return objs.OfType<DBObject>().ToHashSet().ToCollection();
        }
    }
}
