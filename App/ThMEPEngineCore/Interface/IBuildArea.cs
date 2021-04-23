using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Interface
{
    interface IBuildArea
    {
        DBObjectCollection BuildArea(DBObjectCollection objs);
    }
}
