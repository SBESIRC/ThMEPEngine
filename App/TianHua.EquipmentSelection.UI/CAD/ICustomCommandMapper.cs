using Autodesk.AutoCAD.DatabaseServices;

namespace TianHua.FanSelection.UI.CAD
{
    public interface ICustomCommandMapper
    {
        string GetMappedCustomCommand(ObjectId entId);
    }
}
