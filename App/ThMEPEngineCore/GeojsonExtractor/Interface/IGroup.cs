using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.GeojsonExtractor.Interface
{
    public interface IGroup
    {
        void Group(Dictionary<Entity,string> groupId);
    }
}
