using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace THMEPCore3D.Interface
{
    interface IExtract
    {
        void Extract(Database db, Point3dCollection range);
    }
}
