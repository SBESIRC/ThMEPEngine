using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPStructure.GirderConnect.SecondaryBeamConnect.Model;

namespace ThMEPStructure.GirderConnect.SecondaryBeamConnect.Service
{
    public class ConnectSecondaryBeamService
    {
        public static List<Line> ConnectSecondaryBeam(List<Line> mainbeam, List<Line> assists)
        {
            List<Line> entitys = mainbeam.Union(assists).Select(o => o.ExtendLine(1)).ToList();
            var space = entitys.ToCollection().PolygonsEx().Cast<Entity>().Where(o => o is Polyline).Cast<Polyline>().Select(o => o.DPSimplify(10)).Where(o => o.NumberOfVertices<7 && o.NumberOfVertices>3 && o.Area > 1000).ToList();
            if (mainbeam.Count == 0 || space.Count == 0)
            {
                return new List<Line>();
            }
            ThBeamTopologyGraph Beamgraph = new ThBeamTopologyGraph();
            Beamgraph.CreatGraph(space, mainbeam, assists);
            Beamgraph.PolygonSecondaryBeamLayout();
            Beamgraph.AdjustmentDirection();
            Beamgraph.TriangleSecondaryBeamLayout();
            Beamgraph.AdjustSingleBeam();
            //Beamgraph.DrawGraph(Matrix3d.Identity, false);
            return Beamgraph.Nodes.SelectMany(o => o.LayoutLines.SecondaryBeamLines).ToList();
        }

        public static void DrawGraph(List<Line> secondaryBeamLines)
        {
            using (AcadDatabase acad = AcadDatabase.Active())
            {
                foreach (var line in secondaryBeamLines)
                {
                    var newLine = line.Clone() as Line;
                    newLine.Layer = "TH_AICL_BEAM";
                    acad.ModelSpace.Add(newLine);
                }
            }
        }

        public static void CreateSecondaryBeamLineLayer(Database database)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                database.AddLayer(SecondaryBeamLayoutConfig.LayerName);
                database.SetLayerColor(SecondaryBeamLayoutConfig.LayerName, SecondaryBeamLayoutConfig.ColorIndex);
                acadDatabase.Database.UnLockLayer(SecondaryBeamLayoutConfig.LayerName);
                acadDatabase.Database.UnOffLayer(SecondaryBeamLayoutConfig.LayerName);
                acadDatabase.Database.UnFrozenLayer(SecondaryBeamLayoutConfig.LayerName);
            }
        }
    }
}
