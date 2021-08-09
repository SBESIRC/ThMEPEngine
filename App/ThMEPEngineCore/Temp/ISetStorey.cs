using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Temp
{
    public interface ISetStorey
    {
        void Set(List<StoreyInfo> storeyInfos);
        StoreyInfo Query(Entity entity);
    }
}
