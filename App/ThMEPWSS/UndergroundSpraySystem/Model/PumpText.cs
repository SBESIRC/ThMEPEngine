using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm;
using ThMEPWSS.Uitl.ExtensionsNs;

namespace ThMEPWSS.UndergroundSpraySystem.Model
{
    public class PumpText
    {
        public DBObjectCollection DBObjs { get; set; }
        public PumpText()
        {
            DBObjs = new DBObjectCollection();
        }
        public void Extract(Database database, SprayIn sprayIn)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                var Results = acadDatabase
                   .ModelSpace
                   .OfType<Entity>()
                   .Where(o => o is not DBPoint)
                   .Where(o => IsTargetLayer(o.Layer.ToUpper()))
                   .ToList();

                var spatialIndex = new ThCADCoreNTSSpatialIndex(Results.ToCollection());
                foreach (var polygon in sprayIn.FloorRectDic.Values)
                {
                    var dbObjs = spatialIndex.SelectCrossingPolygon(polygon);
                    dbObjs.Cast<Entity>()
                        .Where(e => e.IsTCHText())
                        .ForEach(e => ExplodeTCHNote(e, DBObjs));
                    dbObjs.Cast<Entity>()
                        .Where(e => e is DBText)
                        .ForEach(e => DBObjs.Add(e));
                    dbObjs.Cast<Entity>()
                        .Where(e => e is BlockReference)
                        .ForEach(e => ExplodeBlockNote(e, DBObjs));
                }
            }
        }
        public List<DBText> GetTexts()
        {
            var dbTexts = new List<DBText>();
            DBObjs.Cast<Entity>()
                .ForEach(e => dbTexts.Add((DBText)e));
            return dbTexts;
        }
        private bool IsTargetLayer(string layer)
        {
            return layer.StartsWith("W-") &&
                  (layer.Contains("-DIMS") ||
                   layer.Contains("-NOTE") ||
                   layer.Contains("-FRPT-HYDT-DIMS") ||
                   layer.Contains("-FRPT-SPRL-DIMS") ||
                   layer.Contains("-SHET-PROF")) || 
                   layer.Contains("TWT_TEXT");
        }
        private bool IsTCHNote(Entity entity)
        {
            try
            {
                return entity.GetType().Name.Equals("ImpEntity");
            }
            catch
            {
                ;
            }
            return false;
        }
        private void ExplodeTCHNote(Entity entity, DBObjectCollection DBObjs)
        {
            try
            {
                var dbObjs = new DBObjectCollection();
                entity.Explode(dbObjs);
                foreach(var db in dbObjs)
                {
                    if((db as Entity).IsTCHText())
                    {
                        AddTchText(db as Entity);
                    }

                    if(db is DBText dBText)
                    {
                        AddDbText(dBText);
                    }
                }
            }
            catch
            {
                ;
            }
        }

        private void ExplodeBlockNote(Entity entity, DBObjectCollection DBObjs)
        {
            try
            {
                var dbObjs = new DBObjectCollection();
                entity.Explode(dbObjs);
                foreach (var db in dbObjs)
                {
                    var ent = db as Entity;
                    if(ent.IsTCHText())
                    {
                        ExplodeTCHNote(ent, DBObjs);
                    }
                    if (ent.IsTCHText())
                    {
                        AddTchText(ent);
                    }

                    if (db is DBText text2)
                    {
                        AddDbText(text2);
                    }
                }
            }
            catch
            {
                ;
            }
        }

        private void AddTchText(Entity ent)
        {
            var textStr = "";//保存当前标注
            var location = new Point3d();//保存标注的位置
            var first = true;
            foreach (var text in ent.ExplodeTCHText())
            {
                var st = (text as DBText).TextString;
                if (!st.StartsWith("DN"))
                {
                    textStr += (text as DBText).TextString;
                }
                if (first)
                {
                    location = (text as DBText).Position.OffsetXY(-50, -50);
                    first = false;
                }
            }
            if (!textStr.Contains("水泵接合器"))
            {
                textStr = textStr.Split('喷')[0];
            }
            var dbText = new Block.Text(textStr, location, "W-NOTE").DbText;
            DBObjs.Add(dbText);
        }

        private void AddDbText(DBText dBText)
        {
            var st = dBText.TextString;
            if (!st.StartsWith("DN"))
            {
                if (!st.Contains("水泵接合器"))
                {
                    st = st.Split('喷')[0];
                }
                var dbText = new Block.Text(st.Split('喷')[0], dBText.Position, "W-NOTE").DbText;
                DBObjs.Add(dbText);
            }
        }
    }
}
