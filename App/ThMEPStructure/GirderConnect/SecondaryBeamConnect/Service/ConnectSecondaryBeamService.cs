using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPStructure.GirderConnect.SecondaryBeamConnect.Model;

namespace ThMEPStructure.GirderConnect.SecondaryBeamConnect.Service
{
    public class ConnectSecondaryBeamService
    {
        public static void ConnectSecondaryBeam(List<Line> mainbeam, List<Line> assists)
        {
            List<Line> entitys = mainbeam.Union(assists).Select(o => o.ExtendLine(1)).ToList();
            var space = entitys.ToCollection().PolygonsEx().Cast<Polyline>().Select(o=>o.DPSimplify(10)).Where(o=>o.NumberOfVertices<7 && o.NumberOfVertices>3 && o.Area > 1000).ToList();
            ThBeamTopologyGraph Beamgraph = new ThBeamTopologyGraph();
            Beamgraph.CreatGraph(space, mainbeam, assists);
            Beamgraph.PolygonSecondaryBeamLayout();
            Beamgraph.AdjustmentDirection();
            Beamgraph.TriangleSecondaryBeamLayout();
            Beamgraph.DrawGraph(Matrix3d.Identity, false);
        }
    }
}
