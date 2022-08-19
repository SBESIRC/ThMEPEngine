using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.CAD
{
    public static class ThMEPDbUniqueIdService
    {
        public static int UniqueId(ObjectId obj)
        {
            if (obj.GetObject(OpenMode.ForRead) is Entity e)
            {
                return e.Handle.GetHashCode();
            }
            return 0;
        }

        public static int UniqueId(ObjectId obj, int pId)
        {
            return Combine(UniqueId(obj), pId);
        }

        public static int UniqueId(ObjectId obj, int pId, Matrix3d matrix)
        {
            return Combine(UniqueId(obj, pId), matrix.GetHashCode());
        }

        public static int Combine(int uid1, int uid2)
        {
            return uid1 ^ uid2;
        }
    }
}
