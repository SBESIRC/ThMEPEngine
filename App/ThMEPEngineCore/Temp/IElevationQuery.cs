using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Temp
{
    public interface IElevationQuery
    {
        List<double> Query(Entity ent);
    }
}
