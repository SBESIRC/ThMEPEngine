using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
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
using ThMEPStructure.GirderConnect.Data;
using ThMEPStructure.GirderConnect.SecondaryBeamConnect.Model;

namespace ThMEPStructure.GirderConnect.SecondaryBeamConnect.Service
{
    public class ConnectSecondaryBeamService
    {
        public static List<Line> ConnectSecondaryBeam(List<Line> mainbeam, List<Line> assists)
        {
            List<Line> entitys = mainbeam.Union(assists).Select(o => o.ExtendLine(1)).ToList();
            var space = entitys.ToCollection().PolygonsEx().Cast<Entity>().Where(o => o is Polyline).Cast<Polyline>().Select(o => o.DPSimplify(10)).Where(o => o.NumberOfVertices<7 && o.NumberOfVertices>3 && o.Area > 1000 * 1000).ToList();
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
                    newLine.Layer = BeamConfig.SecondaryBeamLayerName;
                    newLine.ColorIndex = (int)ColorIndex.BYLAYER;
                    newLine.Linetype = "ByLayer";
                    acad.ModelSpace.Add(newLine);
                }
            }
        }

        public static ObjectIdList InsertEntity(List<Entity> ents)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                // 添加到图纸中
                ents.ForEach(o => acadDatabase.ModelSpace.Add(o));

                // 设置文字图层
                var textStyleId = DbHelper.GetTextStyleId("TH-STYLE3");
                ents.OfType<DBText>().ForEach(o => o.TextStyleId = textStyleId);

                // 返回图元Id
                return new ObjectIdList(ents.Select(o => o.ObjectId).ToArray());
            }
        }

        public static void Erase(ObjectIdCollection objs)
        {
            using (AcadDatabase acad = AcadDatabase.Active())
            {
                foreach (ObjectId objId in objs)
                {
                    var entity = acad.Element<Entity>(objId);
                    entity.UpgradeOpen();
                    entity.Erase();
                    entity.DowngradeOpen();
                }
            }
        }
    }
}
