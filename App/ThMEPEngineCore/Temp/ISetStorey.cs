using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPEngineCore.Temp
{
    interface ISetStorey
    {
        void Set(List<StoreyInfo> storeyInfos);
        StoreyInfo Query(Entity entity);
    }
}
