using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPEngineCore.Model;

namespace ThMEPEngineCore.Extension
{
    public static class ThGeometryExtension
    {
        public static void ProjectOntoXYPlane(this List<ThGeometry> geos)
        {
            geos.ForEach(g =>
            {
                if (g.Boundary != null)
                {
                    // Reference:
                    // https://knowledge.autodesk.com/support/autocad/learn-explore/caas/sfdcarticles/sfdcarticles/how-to-flatten-a-drawing-in-autocad.html
                    g.Boundary.TransformBy(Matrix3d.Displacement(new Vector3d(0, 0, 1E99)));
                    g.Boundary.TransformBy(Matrix3d.Displacement(new Vector3d(0, 0, -1E99)));
                }
            });
        }
    }
}
