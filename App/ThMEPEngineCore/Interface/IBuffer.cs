using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Interface
{
    public interface IBuffer
    {
        Entity Buffer(Entity entity, double length);
    }
}
