using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.FlushPoint.Data
{
    public interface IPrint
    {
        void Print(Database database);
    }
}
