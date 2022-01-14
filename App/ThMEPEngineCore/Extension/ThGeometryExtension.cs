using System.Collections.Generic;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;

namespace ThMEPEngineCore.Extension
{
    public static class ThGeometryExtension
    {
        public static void ProjectOntoXYPlane(this List<ThGeometry> geos)
        {
            geos.ForEach(g => g.Boundary.ProjectOntoXYPlane());
        }
    }
}
