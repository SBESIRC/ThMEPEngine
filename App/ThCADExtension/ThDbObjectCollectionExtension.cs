using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using NFox.Cad;
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
            var extents = new Extents3d(Point3d.Origin, Point3d.Origin);
            coll.Cast<Entity>().ForEach(e => extents.AddExtents(e.GeometricExtents));
            return extents;
        }

        public static DBObjectCollection Union(this DBObjectCollection first, DBObjectCollection second)
        {
            var results = new DBObjectCollection();
            foreach (DBObject dbObj in first)
            {
                if (!results.Contains(dbObj))
                {
                    results.Add(dbObj);
                }
            }
            foreach (DBObject dbObj in second)
            {
                if (!results.Contains(dbObj))
                {
                    results.Add(dbObj);
                }
            }
            return results;
        }

        public static DBObjectCollection Difference(this DBObjectCollection first, DBObjectCollection second)
        {
            var results = new DBObjectCollection();
            results = Union(results, first);
            foreach (DBObject dbObj in second)
            {
                results.Remove(dbObj);
            }
            return results;
        }

        public static DBObjectCollection Distinct(this DBObjectCollection objs)
        {
            var results = new DBObjectCollection();
            foreach (DBObject dbObj in objs)
            {
                if (!results.Contains(dbObj))
                {
                    results.Add(dbObj);
                }
            }
            return results;
        }
    }
}
