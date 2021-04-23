using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Interface
{
    public interface IPolygonize
    {
        DBObjectCollection Polygonize(DBObjectCollection objs);
    }
}
