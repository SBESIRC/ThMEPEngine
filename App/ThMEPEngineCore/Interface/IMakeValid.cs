using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Interface
{
    internal interface IMakeValid
    {
        Polyline MakeValid(Polyline polyline);
        DBObjectCollection MakeValid(DBObjectCollection polylines);
    }
}
