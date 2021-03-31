using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Temp
{
    public interface IGroup
    {
        void Group(Dictionary<Entity,string> groupId);
    }
}
