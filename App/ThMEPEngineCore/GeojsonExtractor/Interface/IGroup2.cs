using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.GeojsonExtractor.Interface
{
    public interface IGroup2
    {
        void Group2(Dictionary<Entity,string> groupId);
    }
}
