using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.GeojsonExtractor.Interface
{
    public interface IPrint
    {
        void Print(Database database);
    }
}
