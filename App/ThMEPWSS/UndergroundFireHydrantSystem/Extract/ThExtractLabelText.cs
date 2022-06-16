using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
                   layer.ToUpper() == "W-RAIN-NOTE"||
                   layer.ToUpper() == "W-NOTE" ||
                   layer.ToUpper() == "W-SHET-PROF" ||
                   layer.ToUpper() == "TWT_TEXT";
        }


        /// <summary>
        /// 判断字符串中是否包含中文
        /// </summary>
        /// <param name="str">需要判断的字符串</param>
        /// <returns>判断结果</returns>
        public bool HasChinese(string str)
        {
            return Regex.IsMatch(str, @"[\u4e00-\u9fa5]");
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
                var str = dbText.TextString;
                if (str.Contains("De") || str.Contains("DN"))
                {
                    return;
                }
                if(HasChinese(str))
                {
                    return;
                }
                var tWidth = Math.Abs(ent.GeometricExtents.MaxPoint.X - ent.GeometricExtents.MinPoint.X);
                if (tWidth > textWidth && str.Contains("X") && !(str.Contains("/")))
                {
                    textWidth = tWidth;
                    textModel = str;
                }

                dBObjects.Add(ent);
                
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
                var str = "";
                var insertPt = new Point3d();//文字插入点
                foreach(var text in texts)
                {
                    if(text is DBText db)
                    {
                        str += db.TextString;
                        if(insertPt.Equals(new Point3d()))
                        {
                            insertPt = new Point3d(db.Position.X, db.Position.Y, 0);
                        }
                    }
                }
                if (str.Contains("De") || str.Contains("DN"))
                {
                    return;
                }
                if (HasChinese(str))
                {
                    return;
                }
                var dBText = CreateText(insertPt, str);//创建成标准文字
                var tWidth = Math.Abs(dBText.GeometricExtents.MaxPoint.X - dBText.GeometricExtents.MinPoint.X);
                if (tWidth > textWidth && str.Trim().Contains("X") && (!str.Trim().Contains("/")))
                {
                    textWidth = tWidth;
                    textModel = dBText.TextString;
                }
                dBObjects.Add(dBText);
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
