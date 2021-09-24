using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Interface
{
    interface IThMEPIPersistable
    {
        void Persist(Database database);
    }
}
