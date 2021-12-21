using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADCore.NTS;
using System.Linq;
using NFox.Cad;

namespace THMEPWSS.SmallAreaPipeline
{
    public class arrangePipelineService
    {
        private HashSet<Point3d> ptInSmallRoom { get; set; } = new HashSet<Point3d>();

    }
}