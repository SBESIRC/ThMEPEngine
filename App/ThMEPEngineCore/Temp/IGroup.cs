using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Temp
{
    public interface IGroup
    {
        void Group(List<Polyline> groups);
    }
}
