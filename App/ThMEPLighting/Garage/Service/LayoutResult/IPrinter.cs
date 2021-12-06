using DotNetARX;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.Garage.Service.LayoutResult
{
    interface IPrinter
    {
        ObjectIdList ObjIds {get;}
        void Print(Database db);
    }
}
