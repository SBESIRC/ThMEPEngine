using ThCADCore.NTS;
using ThMEPEngineCore.Interface;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.BuildRoom.Service
{
    public class ThNTSBuildAreaService : IBuildArea
    {
        public DBObjectCollection BuildArea(DBObjectCollection objs)
        {
            return objs.Count > 0 ? objs.BuildArea() : new DBObjectCollection();
        }
    }
}
