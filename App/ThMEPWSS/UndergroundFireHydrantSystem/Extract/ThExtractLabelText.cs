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
using ThMEPWSS.CADExtensionsNs;
using ThMEPWSS.Pipe.Service;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Extract
{
    public class ThExtractLabelText//文字提取
    {
        public List<Entity> Results { get; private set; }
        public DBObjectCollection DBObjs { get; private set; }
        public DBObjectCollection Extract(Database database, Point3dCollection polygon, ref double textWidth, ref string textModel)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                Results = acadDatabase
                   .ModelSpace
                   .OfType<Entity>()
                   .Where(o => IsHYDTPipeLayer(o.Layer)).ToList();

                var spatialIndex = new ThCADCoreNTSSpatialIndex(Results.ToCollection());
                DBObjs = spatialIndex.SelectCrossingPolygon(polygon);
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
                            ExplodeText(ent, dbTextCollection, ref textWidth, ref textModel);
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
            return layer.ToUpper() == "W-RAIN-DIMS" ||
                   layer.ToUpper() == "W-FRPT-HYDT-DIMS" ||
                   layer.ToUpper() == "W-FRPT-HYDT-NOTE" ||
                   layer.ToUpper() == "W-FRPT-HYDT-EQPM" ||
                   layer.ToUpper() == "W-WSUP-DIMS" ||
                   layer.ToUpper() == "W-DRAI-DIMS" ||
                   layer.ToUpper() == "W-FRPT-NOTE" ||
                   layer.ToUpper() == "W-FRPT-HYDT-NOTE" ||
                   layer.ToUpper() == "0" ||
                   layer.ToUpper() == "W-RAIN-NOTE";
        }

        private void ExplodeText(Entity ent, DBObjectCollection dBObjects, ref double textWidth, ref string textModel)
        {
            if (ent is DBText dbText)//DBText直接添加
            {
                if (dbText.TextString.Contains("De"))
                {
                    return; ;
                }
                if (!dbText.TextString.Contains("DN"))
                {
                    var tWidth = Math.Abs((ent as Entity).GeometricExtents.MaxPoint.X - (ent as Entity).GeometricExtents.MinPoint.X);
                    if (tWidth > textWidth && (ent as DBText).TextString.Contains("X") && !((ent as DBText).TextString.Contains("/")))
                    {
                        textWidth = tWidth;
                        textModel = (ent as DBText).TextString;
                    }
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
                    var tWidth = Math.Abs((text as Entity).GeometricExtents.MaxPoint.X - (text as Entity).GeometricExtents.MinPoint.X);
                    if (tWidth > textWidth && (text as DBText).TextString.Trim().Contains("X") && (!(text as DBText).TextString.Trim().Contains("/")))
                    {
                        textWidth = tWidth;

                        textModel = (text as DBText).TextString;
                    }
                    dBObjects.Add((DBObject)text);
                }
                return;
            }
            if(ent.GetRXClass().DxfName.StartsWith("TCH") && ent.GetRXClass().DxfName.Contains("PIPE"))
            {
                var dbObjs = new DBObjectCollection();
                ent.Explode(dbObjs);
                foreach(var db  in dbObjs)
                {
                    if(db is Entity ent1)
                    {
                        ExplodeText(ent1, dBObjects, ref textWidth, ref textModel);
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
                        ExplodeText(ent1, dBObjects, ref textWidth, ref textModel);
                    }
                }
            }
            catch (Exception)
            {

            }
        }
    }
}
