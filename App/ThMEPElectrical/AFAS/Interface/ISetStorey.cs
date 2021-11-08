using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.GeojsonExtractor.Model;

namespace ThMEPElectrical.AFAS.Interface
{
    interface ISetStorey
    {
        void Set(List<ThStoreyInfo> storeyInfos);
        ThStoreyInfo Query(Entity entity);
    }
}
