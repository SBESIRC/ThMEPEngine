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
    public class PumpTextNew
    {
        public DBObjectCollection DBObjs { get; set; }
        public DBObjectCollection TextDbObjs { get; set; }

        public PumpTextNew(DBObjectCollection textDbObjs)
        {
            DBObjs = new DBObjectCollection();
            TextDbObjs = new DBObjectCollection();
            foreach (var obj in textDbObjs)
            {
                TextDbObjs.Add((DBObject)obj);
            }
        }

        public void Extract(Database database, Point3dCollection polygon)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                var Results = acadDatabase.ModelSpace.OfType<Entity>()
                   .Where(e => e is DBText || e.IsTCHText())//文字 或 天正文字
                   .ToCollection();

                var spatialIndex = new ThCADCoreNTSSpatialIndex(Results);
                var dbObjs = spatialIndex.SelectCrossingPolygon(polygon);

                foreach(var obj in dbObjs)
                {
                    var entity = obj as Entity;
                    AddObj(entity);
                }
                foreach(var obj in TextDbObjs)
                {
                    var entity = obj as Entity;
                    AddObj(entity);
                }
            }
        }

        private void AddObj(Entity entity)
        {
            if (entity is DBText dBText)
            {

                AddDbText(dBText);
            }
            if (entity.IsTCHText())
            {
                AddTchText(entity);
            }
        }

        public List<DBText> GetTexts()
        {
            var dbTexts = new List<DBText>();
            DBObjs.Cast<Entity>()
                .ForEach(e => dbTexts.Add((DBText)e));
            return dbTexts;
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
            if (!textStr.Contains("水泵接合器") && textStr.Contains("防火分区"))
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
                var dbText = new Block.Text(st, dBText.Position, "W-NOTE").DbText;
                DBObjs.Add(dbText);
            }
        }
    }
}
