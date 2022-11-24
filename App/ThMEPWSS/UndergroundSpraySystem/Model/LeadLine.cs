using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;
using ThMEPWSS.UndergroundSpraySystem.General;


namespace ThMEPWSS.UndergroundSpraySystem.Model
{
    public class LeadLine
    {
        public DBObjectCollection DBObjs { get; set; }
        public DBObjectCollection TextDbObjs { get; set; }//存放提取的文字，避免二次操作
        public LeadLine()
        {
            DBObjs = new DBObjectCollection();
            TextDbObjs = new DBObjectCollection();
        }
        public void Extract(Database database, SprayIn sprayIn)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                var results = acadDatabase.ModelSpace.OfType<Entity>()
                   .Where(o => IsTargetLayer(o.Layer.ToUpper()))
                   .ToList();

                var spatialIndex = new ThCADCoreNTSSpatialIndex(results.ToCollection());

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
                   layer.Contains("TWT_TEXT") ||
                   layer.Contains("TWT_LEAD");
        }
        private bool IsTCHNote(Entity entity)
        {
            var name = entity.GetRXClass().DxfName;
            var rst1 = name.Contains("TCH_VPIPEDIM");//天正标注
            var rst2 = name.Contains("TCH_MULTILEADER");//引出标注

            return rst1 || rst2;
        }

        private void ExplodeTCHNote(Entity entity)
        {
            var dbObjs = new DBObjectCollection();
            entity.Explode(dbObjs);
            foreach (var obj in dbObjs)
            {
                var ent = obj as Entity;
                if (ent is Line line)
                {
                    DBObjs.Add(line);
                }
                if (ent is Polyline pline)
                {
                    DBObjs.AddList(pline.Pline2Lines());
                }
                if (ent is DBText || ent.IsTCHText())
                {
                    TextDbObjs.Add(ent);
                }
            }
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
