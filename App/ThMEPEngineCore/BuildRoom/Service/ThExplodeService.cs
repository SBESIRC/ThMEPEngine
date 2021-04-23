using ThCADExtension;
using ThMEPEngineCore.Interface;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.BuildRoom.Service
{
    public class ThExplodeService : IExplode
    {
        public DBObjectCollection Explode(DBObjectCollection objs)
        {
            return objs.Count > 0 ? objs.ExplodeCurves() : new DBObjectCollection();
        }
    }
}
