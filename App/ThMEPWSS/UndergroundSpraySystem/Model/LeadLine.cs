using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;
using ThMEPWSS.UndergroundSpraySystem.General;

namespace ThMEPWSS.UndergroundSpraySystem.Model
{
    public class LeadLine
    {
        public DBObjectCollection DBObjs { get; set; }
        public LeadLine()
        {
            DBObjs = new DBObjectCollection();
        }
        public void Extract(Database database, SprayIn sprayIn)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                var pipeNumber = acadDatabase
                   .ModelSpace
                   .OfType<Entity>()
                   .Where(o => IsTargetLayer(o.Layer.ToUpper()))
                   .ToList();

                var spatialIndex = new ThCADCoreNTSSpatialIndex(pipeNumber.ToCollection());

                foreach(var polygon in sprayIn.FloorRectDic.Values)
                {
                    var dbObjs = spatialIndex.SelectCrossingPolygon(polygon);
                    dbObjs.Cast<Entity>()
                        .Where(e => IsTCHNote(e))
                        .ForEach(e => ExplodeTCHNote(e));
                    dbObjs.Cast<Entity>()
                        .Where(e => e is BlockReference)
                        .ForEach(e => ExplodeBlockNote(e));
                    dbObjs.Cast<Entity>()
                        .Where(e => e is Line)
                        .ForEach(e => DBObjs.Add(e));
                    dbObjs.Cast<Entity>()
                    .Where(e => e is Polyline)
                    .ForEach(e => DBObjs.AddList((e as Polyline).Pline2Lines()));
                }
            }
        }

        public List<Line> GetLines()
        {
            var leadLines = new List<Line>();
            foreach(var db in DBObjs)
            {
                var line = db as Line;
                if(line is not null)
                {
                    leadLines.Add(line);
                }
            }
            leadLines = PipeLineList.CleanLaneLines3(leadLines);
            return leadLines;
        }
        private bool IsTargetLayer(string layer)
        {
            return layer.StartsWith("W-") &&
                  (layer.Contains("-DIMS") ||
                   layer.Contains("-NOTE") ||
                   layer.Contains("-FRPT-HYDT-DIMS") ||
                   layer.Contains("-SHET-PROF") ||
                   layer.Contains("-FRPT-SPRL-DIMS")) ||
                   layer.Contains("TWT_TEXT");
        }
        private bool IsTCHNote(Entity entity)
        {
            return entity.GetType().Name.Equals("ImpEntity");
        }

        private void ExplodeTCHNote(Entity entity)
        {
            var dbObjs = new DBObjectCollection();
            entity.Explode(dbObjs);
            dbObjs.Cast<Entity>()
                .Where(e => e is Line)
                .ForEach(e => DBObjs.Add(e));
            dbObjs.Cast<Entity>()
                .Where(e => e is Polyline)
                .ForEach(e => DBObjs.AddList((e as Polyline).Pline2Lines()));
        }

        private void ExplodeBlockNote(Entity entity)
        {
            var dbObjs = new DBObjectCollection();
            entity.Explode(dbObjs);
            dbObjs.Cast<Entity>()
                .Where(e => e is Line)
                .ForEach(e => DBObjs.Add(e));
            dbObjs.Cast<Entity>()
                .Where(e => e is Polyline)
                .ForEach(e => DBObjs.AddList((e as Polyline).Pline2Lines()));
            dbObjs.Cast<Entity>()
                .Where(e => IsTCHNote(e))
                .ForEach(e => ExplodeTCHNote(e));
        }
    }
}
