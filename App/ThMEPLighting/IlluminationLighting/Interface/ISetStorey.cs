using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.GeojsonExtractor.Model;

namespace ThMEPLighting.IlluminationLighting.Interface
{
    interface ISetStorey
    {
        void Set(List<ThStoreyInfo> storeyInfos);
        ThStoreyInfo Query(Entity entity);
    }
}
