using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore;
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
#if DEBUG

                using (AcadDatabase currentDb = AcadDatabase.Active())
                {
                    string layerName = "文字图层";
                    try
                    {
                        ThMEPEngineCoreLayerUtils.CreateAILayer(currentDb.Database, layerName, 30);
                    }
                    catch { }
                    foreach (var line in dbTextCollection)
                    {
                        var dbtext = line as DBText;
                        var rect = dbtext.GetRect();
                        rect.LayerId = DbHelper.GetLayerId(layerName);
                        currentDb.CurrentSpace.Add(rect);
                    }
                }
#endif
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
                   layer.ToUpper() == "W-RAIN-NOTE"||
                   layer.ToUpper() == "W-NOTE" ||
                   layer.ToUpper() == "TWT_TEXT";
        }


        //特别耗时
        private void ExplodeText(Entity ent, DBObjectCollection dBObjects, ref double textWidth, ref string textModel)
        {
            if (ent is BlockReference br)
            {
                try
                {
                    if (br.Name.Contains("SDRFSETEW"))
                    {
                        var objs = new DBObjectCollection();
                        br.Explode(objs);
                        foreach (var obj in objs)
                        {
                            if (obj.GetType().Name.Contains("ImpEntity"))
                            {
                                var objs1 = new DBObjectCollection();
                                (obj as Entity).Explode(objs1);
                                objs1.Cast<Entity>()
                                    .Where(e => e.IsTCHText())
                                    .ForEach(e => dBObjects.Add(e.ExplodeTCHText()[0]));
                                objs1.Cast<Entity>()
                                    .Where(e => e is DBText)
                                    .ForEach(e => dBObjects.Add(e));
                            }
                        }
                        return;
                    }
                }
                catch
                {
                }
            }
            if (ent is DBText dbText)//DBText直接添加
            {
                if (dbText.TextString.Contains("De"))
                {
                    return;
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
            if (ent is AlignedDimension || 
                ent is Arc || 
                ent is Line || 
                ent is Circle || 
                ent is Polyline || 
                ent is DBPoint ||
                ent is Hatch)//炸成线就退出
            {
                return;
            }
            if (ent.IsTCHText())//天正单行文字,先炸后添加
            {
                var texts = ent.ExplodeTCHText();
                var noteText = "";
                var insertPt = new Point3d();//文字插入点
                foreach(var text in texts)
                {
                    if(text is DBText db)
                    {
                        noteText += db.TextString;
                        if(insertPt.Equals(new Point3d()))
                        {
                            insertPt = new Point3d(db.Position.X, db.Position.Y, 0);
                        }
                    }
                }
                var dBText = CreateText(insertPt, noteText);//创建成标准文字
                var tWidth = Math.Abs(dBText.GeometricExtents.MaxPoint.X - dBText.GeometricExtents.MinPoint.X);
                if (tWidth > textWidth && noteText.Trim().Contains("X") && (!noteText.Trim().Contains("/")))
                {
                    textWidth = tWidth;
                    textModel = dBText.TextString;
                }
                dBObjects.Add((DBObject)dBText);
                return;
            }
            if(ent.IsTCHPipe())
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
            catch
            {
            }
        }

        private DBText CreateText(Point3d insertPt, string text)
        {
            string layer = "W-FRPT-SPRL-DIMS";
            double rotation = 0;
            return new DBText
            {
                TextString = text,
                Position = insertPt,
                LayerId = DbHelper.GetLayerId(layer),
                Rotation = rotation,
                TextStyleId = DbHelper.GetTextStyleId("TH-STYLE3"),
                Height = 350,
                WidthFactor = 0.7,
                ColorIndex = (int)ColorIndex.BYLAYER
            };
        }
    }
}
