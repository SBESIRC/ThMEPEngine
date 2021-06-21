using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.GeojsonExtractor.Interface
{
    public interface IClean
    {
        DBObjectCollection Clean(DBObjectCollection objs);
    }
}
