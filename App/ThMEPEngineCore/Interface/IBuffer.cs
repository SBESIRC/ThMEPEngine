using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Interface
{
    interface IBuffer
    {
        Entity Buffer(Entity entity, double length);
    }
}
