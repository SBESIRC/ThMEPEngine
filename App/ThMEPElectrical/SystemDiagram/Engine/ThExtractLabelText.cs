using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm;

namespace ThMEPElectrical.SystemDiagram.Engine
{
    public class ThExtractLabelText//文字提取
    {
        public List<Entity> Results { get; private set; }
        public DBObjectCollection DBObjs { get; private set; }
        public DBObjectCollection Extract(Database database, Point3dCollection polygon)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                Results = acadDatabase
                   .ModelSpace
                   .OfType<Entity>()
                   .Where(o => IsHYDTPipeLayer(o.Layer)).ToList();

                DBObjs = Results.ToCollection();
                if (polygon.Count > 0)
                {
                    var spatialIndex = new ThCADCoreNTSSpatialIndex(Results.ToCollection());
                    DBObjs = spatialIndex.SelectCrossingPolygon(polygon);
                }

                var dbTextCollection = new DBObjectCollection();

                var bkrCollection = new DBObjectCollection();//筛选BlockRefrence
                DBObjs.Cast<Entity>()
                    .Where(o => o is Entity)
                    .ForEach(o => bkrCollection.Add(o));
                foreach (var bkr in bkrCollection)
                {
                    try
                    {
                        if (bkr is Entity ent)
                        {
                            ExplodeText(ent, dbTextCollection);
                        }
                    }
                    catch
                    {
                        ;
                    }

                }
                return dbTextCollection;
            }
        }
        private bool IsHYDTPipeLayer(string layer)
        {
            return layer.ToUpper() == "E-UNIV-NOTE";
        }

        private void ExplodeText(Entity ent, DBObjectCollection dBObjects)
        {
            if (ent is DBText dbText)//DBText直接添加
            {
                if (dbText.TextString.Contains("De"))
                {
                    return; ;
                }
                if (!dbText.TextString.Contains("DN"))
                {
                    dBObjects.Add(ent);
                }
                return;
            }
            if (ent is AlignedDimension || ent is Arc || ent is Line || ent is Circle || ent is Polyline)//炸成线就退出
            {
                return;
            }
            if (ent.IsTCHText())//天正单行文字,先炸后添加
            {
                var texts = ent.ExplodeTCHText();
                foreach (var text in texts)
                {
                    dBObjects.Add((DBObject)text);
                }
                return;
            }
            if (ent.GetRXClass().DxfName.StartsWith("TCH") && ent.GetRXClass().DxfName.Contains("PIPE"))
            {
                var dbObjs = new DBObjectCollection();
                ent.Explode(dbObjs);
                foreach (var db in dbObjs)
                {
                    if (db is Entity ent1)
                    {
                        ExplodeText(ent1, dBObjects);
                    }

                }
            }
            try
            {
                var dbObjs = new DBObjectCollection();
                ent.Explode(dbObjs);
                foreach (var obj in dbObjs)
                {
                    if (obj is Entity ent1)
                    {
                        ExplodeText(ent1, dBObjects);
                    }
                }
            }
            catch (Exception)
            {

            }
        }
    }
}
