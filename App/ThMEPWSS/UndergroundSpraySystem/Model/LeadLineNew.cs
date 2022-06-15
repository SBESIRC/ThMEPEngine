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
using ThMEPEngineCore.Algorithm;
using Draw = ThMEPWSS.UndergroundSpraySystem.Method.Draw;


namespace ThMEPWSS.UndergroundSpraySystem.Model
{
    public class LeadLineNew
    {
        public List<Line> DBObjs { get; set; }
        public DBObjectCollection TextDbObjs { get; set; }//存放提取的文字，避免二次操作
        public LeadLineNew()
        {
            DBObjs = new List<Line>();
            TextDbObjs = new DBObjectCollection();
        }
        public void Extract(Database database, Point3dCollection polygon)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                var results1 = acadDatabase.ModelSpace
                   .OfType<Entity>()
                   .Where(e => IsTCHLeadLine(e))//引出标注 或 天正标注
                   .ToCollection();

                var results2 = acadDatabase.ModelSpace
                   .OfType<Entity>()
                   .Where(o => IsTargetLayer(o.Layer))//特定图层直线
                   .Where(e=> e is Line || e is Polyline)
                   .ToCollection();

                var spatialIndex1 = new ThCADCoreNTSSpatialIndex(results1);
                var spatialIndex2 = new ThCADCoreNTSSpatialIndex(results2);

                var dbObjs1 = spatialIndex1.SelectCrossingPolygon(polygon);
                var dbObjs2 = spatialIndex2.SelectCrossingPolygon(polygon);

                dbObjs1.Cast<Entity>().ForEach(e => ExplodeTCHNote(e));
                foreach(var ent in dbObjs2)
                {
                    if(ent is Line l)
                    {
                        DBObjs.Add(l);
                    }
                    else
                    {
                        var pline = ent as Polyline;
                        var lines = pline.Pline2Lines();
                        foreach(var line in lines)
                        {
                            DBObjs.Add(line);
                        }
                    }
                }
                dbObjs2.Cast<Entity>().ForEach(e => DBObjs.Add(e as Line));
            }
        }

        public List<Line> GetLines()
        {
            var leadLines = new List<Line>();
            foreach(var obj in DBObjs)
            {
                if(!(obj is Line))
                {
                    continue;
                }
                var line = obj as Line;
                leadLines.Add(line);
            }
            leadLines = PipeLineList.CleanLaneLines3(leadLines);
            Draw.LeadLines(leadLines);
            return leadLines;
        }

        private bool IsTargetLayer(string layer)
        {
            return layer.Contains("W-FRPT-HYDT-DIMS")
                || layer.Contains("W-FRPT-SPRL-DIMS")
                || layer.Contains("W-NOTE");
        }

        private void ExplodeTCHNote(Entity entity)
        {
            var dbObjs = new DBObjectCollection();
            entity.Explode(dbObjs);
            foreach (var obj in dbObjs)
            {
                var ent = obj as Entity;
                if(ent is Line line)
                {
                    DBObjs.Add(line);
                }
                if(ent is Polyline pline)
                {
                    var lines = pline.Pline2Lines();
                    lines.ForEach(l => DBObjs.Add(l));
                }
                if(ent is DBText || ent.IsTCHText())
                {
                    TextDbObjs.Add(ent);
                }
            }
        }

        private bool IsTCHLeadLine(Entity entity)
        {
            var name = entity.GetRXClass().DxfName;
            var rst1 = name.Contains("TCH_VPIPEDIM");//天正标注
            var rst2 = name.Contains("TCH_MULTILEADER");//引出标注

            return rst1 || rst2;
        }
    }
}
