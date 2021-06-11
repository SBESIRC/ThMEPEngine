using ThCADCore.NTS;
using ThMEPEngineCore.Interface;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Service
{
    public class ThNTSPolygonizeService : IPolygonize
    {
        public DBObjectCollection Polygonize(DBObjectCollection objs)
        {
            return objs.Count>0 ? objs.Polygons():new DBObjectCollection();
        }
    }
}
