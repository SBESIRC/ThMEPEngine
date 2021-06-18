using NFox.Cad;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThCADExtension
{
    public static class ThDbObjectCollectionExtension
    {
        public static DBObjectCollection Except(this DBObjectCollection coll, DBObjectCollection others)
        {
            return coll.Cast<Entity>().Except(others.Cast<Entity>()).ToCollection();
        }
    }
}
