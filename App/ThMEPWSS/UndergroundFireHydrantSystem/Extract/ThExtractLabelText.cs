using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Extract
{
    public class ThExtractLabelText//文字提取
    {
        public DBObjectCollection Extract(Database database, Point3dCollection polygon)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                var results = acadDatabase
                   .ModelSpace
                   .OfType<Entity>()
                   .Where(o => IsTargetType(o));

                var spatialIndex = new ThCADCoreNTSSpatialIndex(results.ToCollection());
                var dBObjs = spatialIndex.SelectCrossingPolygon(polygon);
                var dbTextCollection = new DBObjectCollection();

                foreach (var obj in dBObjs)
                {
                    if (obj is Entity ent)
                    {
                        try
                        {
                            if (ent.IsTCHMULTILEADER())//引出标注
                            {
                                ent.AddText(database, dbTextCollection);
                            }
                            else if(ent.IsTCHText())//天正文字
                            {
                                ent.AddTchText(database, dbTextCollection);
                            }
                            else if(ent.GetType().Name== "ImpEntity")
                            {
                                ent.AddImpEntity(database, dbTextCollection);
                            }
                            else
                            {
                                dbTextCollection.Add(ent);
                            }
                            ;
                        }
                        catch { }
                    }
                }
#if DEBUG
                Dreambuild.AutoCAD.DbHelper.EnsureLayerOn("文字框");
                foreach (var obj in dbTextCollection)
                {
                    var text = obj as DBText;
                    var rect = text.GetRect();
                    rect.Layer = "文字框";
                    var text2 = TCHDeal.CreateText(text.Position,text.TextString);
                    acadDatabase.CurrentSpace.Add(rect);
                    acadDatabase.CurrentSpace.Add(text2);
                }
#endif
                return dbTextCollection;
            }
        }

        private bool IsTargetType(Entity ent)
        {
            return ent is DBText || ent.IsTCHMULTILEADER() || ent.IsTCHText() || ent.GetType().Name.Contains("ImpEntity");
        }

        private void ExplodeText(Entity ent, DBObjectCollection dBObjects)
        {
            if (ent is DBText dbText)//DBText直接添加
            {
                var str = dbText.TextString;
                if (str.Contains("DN")) return;
                dBObjects.Add(ent);
                return;
            }
            if (ent.IsTCHElement())//天正单行文字,先炸后添加
            {
                var texts = new DBObjectCollection();//ent.ExplodeTCHText();
                ent.Explode(texts);
                var str = "";
                var insertPt = new Point3d();//文字插入点
                foreach (var text in texts)
                {
                    if (text is DBText db)
                    {
                        str += db.TextString;
                        if (insertPt.Equals(new Point3d()))
                        {
                            insertPt = new Point3d(db.Position.X, db.Position.Y, 0);
                        }
                    }
                    if ((text as Entity).IsTCHText())
                    {
                        ExplodeText(text as Entity, dBObjects);
                    }
                }
                if (str.Contains("DN") || str.Equals("")) return;

                var dBText = CreateText(insertPt, str);//创建成标准文字
                dBObjects.Add(dBText);
                ThMEPWSS.UndergroundSpraySystem.Method.Draw.Rect(insertPt.GetRect(), "文字test");
                return;
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


    public static class TCHDeal
    {
        /// <summary>
        /// 处理引出标注
        /// </summary>
        public static void AddText(this Entity ent, Database database, DBObjectCollection dbTextCollection)
        {
            var objID = ent.ObjectId;
            dbTextCollection.Add(LoadTextFromDb(database,objID));
        }

        /// <summary>
        /// 处理天正文字
        /// </summary>
        public static void AddTchText(this Entity ent, Database database, DBObjectCollection dbTextCollection)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                dynamic acadObject = ent.AcadObject;
                var text = acadObject;
                //return CreateText(pts.FirstOrDefault(), text);
            }
        }
        public static void AddImpEntity(this Entity ent, Database database, DBObjectCollection dbTextCollection)
        {
            var objs = new DBObjectCollection();
            ent.Explode(objs);
            foreach(var obj in objs)
            {
                if((obj as Entity).GetType().Name== "ImpEntity")
                {
                    var objs2 = new DBObjectCollection();
                    (obj as Entity).Explode(objs2);
                    foreach(var obj2 in objs2)
                    {
                        if(obj2 is DBText)
                        {
                            dbTextCollection.Add((DBObject)obj2);
                        }
                    }
                }
            }
        }

        public static DBText LoadTextFromDb(Database database, ObjectId tch)
        {
            var dxfData = GetDXFData(tch);
            var pts = new List<Point3d>();
            foreach (TypedValue tv in dxfData.AsArray())
            {
                switch ((DxfCode)tv.TypeCode)
                {
                    case (DxfCode)11:
                        {
                            var pt = (Point3d)tv.Value;
                            pts.Add(new Point3d(pt.X,pt.Y,0));

                        }
                        break;
                }
            }
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                var ent = acadDatabase.Element<Entity>(tch);
                dynamic acadObject = ent.AcadObject;
                var text = acadObject.UpText;
                return CreateText(pts.FirstOrDefault(),text);
            }
                

        }
        private static ResultBuffer GetDXFData(ObjectId tch)
        {
            InvokeTool.ads_name name = new InvokeTool.ads_name();
            InvokeTool.acdbGetAdsName(ref name, tch);

            ResultBuffer rb = new ResultBuffer();
            Interop.AttachUnmanagedObject(rb, InvokeTool.acdbEntGet(ref name), true);

            return rb;
        }

        public static DBText CreateText(Point3d insertPt, string text)
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
