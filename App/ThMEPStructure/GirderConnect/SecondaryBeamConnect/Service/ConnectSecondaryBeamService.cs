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
                    newLine.Layer = SecondaryBeamLayoutConfig.SecondaryBeamLayerName;
                    acad.ModelSpace.Add(newLine);
                }
            }
        }

        public static void CreateSecondaryBeamLineLayer(Database database)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                database.AddLayer(SecondaryBeamLayoutConfig.SecondaryBeamLayerName);
                database.SetLayerColor(SecondaryBeamLayoutConfig.SecondaryBeamLayerName, SecondaryBeamLayoutConfig.SecondaryBeamLayerColorIndex);
                acadDatabase.Database.UnLockLayer(SecondaryBeamLayoutConfig.SecondaryBeamLayerName);
                acadDatabase.Database.UnOffLayer(SecondaryBeamLayoutConfig.SecondaryBeamLayerName);
                acadDatabase.Database.UnFrozenLayer(SecondaryBeamLayoutConfig.SecondaryBeamLayerName);
            }
        }

        public static void CreateSecondaryBeamTextLayer(Database database)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                database.AddLayer(SecondaryBeamLayoutConfig.SecondaryBeamTextLayerName);
                database.SetLayerColor(SecondaryBeamLayoutConfig.SecondaryBeamTextLayerName, SecondaryBeamLayoutConfig.SecondaryBeamTextLayerColorIndex);
                acadDatabase.Database.UnLockLayer(SecondaryBeamLayoutConfig.SecondaryBeamTextLayerName);
                acadDatabase.Database.UnOffLayer(SecondaryBeamLayoutConfig.SecondaryBeamTextLayerName);
                acadDatabase.Database.UnFrozenLayer(SecondaryBeamLayoutConfig.SecondaryBeamTextLayerName);
            }
        }

        public static void CreateMainBeamTextLayer(Database database)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                database.AddLayer(SecondaryBeamLayoutConfig.MainBeamTextLayerName);
                database.SetLayerColor(SecondaryBeamLayoutConfig.MainBeamTextLayerName, SecondaryBeamLayoutConfig.MainBeamTextLayerColorIndex);
                acadDatabase.Database.UnLockLayer(SecondaryBeamLayoutConfig.MainBeamTextLayerName);
                acadDatabase.Database.UnOffLayer(SecondaryBeamLayoutConfig.MainBeamTextLayerName);
                acadDatabase.Database.UnFrozenLayer(SecondaryBeamLayoutConfig.MainBeamTextLayerName);
            }
        }

        public static ObjectId AddToModelSpace(Entity obj)
        {
            using (AcadDatabase acad = AcadDatabase.Active())
            {
                return acad.ModelSpace.Add(obj);
            }
        }
        public static ObjectIdList InsertEntity(List<Entity> ents)
        {
            ObjectIdList objectIds = new ObjectIdList();
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                foreach (var item in ents)
                {
                    var objId = acadDatabase.ModelSpace.Add(item);
                    objectIds.Add(objId);
                }
            }
            return objectIds;
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
