using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPWSS.CADExtensionsNs;
using ThMEPWSS.Pipe.Service;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Extract
{
    public class ThExtractPipeDN//管径提取
    {
        public List<Entity> Results { get; private set; }
        public DBObjectCollection DBObjs { get; private set; }
        public DBObjectCollection DBObjsResult { get; private set; }

        public DBObjectCollection Extract(Database database, Point3dCollection polygon)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                Results = acadDatabase
                   .ModelSpace
                   .OfType<Entity>()
                   .Where(o => IsPipeDNLayer(o.Layer)).ToList();

                var spatialIndex = new ThCADCoreNTSSpatialIndex(Results.ToCollection());
                DBObjs = spatialIndex.SelectCrossingPolygon(polygon);

                DBObjsResult = new DBObjectCollection();
                foreach (var db in DBObjs)
                {
                    try
                    {
                        if (db is DBPoint)
                        {
                            continue;
                        }
                        if (db is DBText)
                        {
                            if ((db as DBText).TextString.Contains("DN"))
                            {
                                DBObjsResult.Add((DBObject)db);
                            }
                        }
                        else if (db is Line)
                        {
                            continue;
                        }
                        else if (db is BlockReference)
                        {
                            if ((db as BlockReference).GetEffectiveName().Equals("消火栓环管标记"))
                            {
                                continue;
                            }
                            if ((db as BlockReference).GetEffectiveName().Equals("消火栓环管节点标记"))
                            {
                                continue;
                            }
                            var objID = (db as BlockReference).ObjectId;
                            var val = objID.GetDynBlockValue("可见性");
                            ;
                            if (val?.Contains("DN") == true)
                            {
                                var DNtext = new DBText();
                                DNtext.TextString = val;
                                DNtext.Position = (db as BlockReference).Position;
                                DNtext.Rotation = (db as BlockReference).Rotation;
                                DBObjsResult.Add(DNtext);
                            }
                            else
                            {
                                var objs = new DBObjectCollection();
                                (db as BlockReference).Explode(objs);
                                ;
                                foreach (var obj in objs)
                                {
                                    if (obj is DBText)
                                    {
                                        ;
                                    }
                                    else if (obj is BlockReference)
                                    {
                                        var objs1 = new DBObjectCollection();
                                        (obj as BlockReference).Explode(objs1);
                                        foreach (var obj1 in objs1)
                                        {
                                            if (obj1 is DBText)
                                            {
                                                if ((obj1 as DBText)?.TextString.Contains("DN") == true)
                                                {
                                                    ;
                                                }
                                                else
                                                {
                                                    continue;
                                                }
                                            }
                                            else
                                            {
                                                continue;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }
                            }
                        }
                        else
                        {
                            var objs = new DBObjectCollection();
                            (db as Entity).Explode(objs);
                            ;
                            foreach (var obj in objs)
                            {
                                if (obj is DBText)
                                {
                                    if((obj as DBText).TextString.StartsWith("DN"))
                                        DBObjsResult.Add((DBObject)obj);
                                }
                            }
                        }
                    }
                    catch(Exception ex)
                    {
                        ;
                    }
                    

                }
                return DBObjsResult;
            }
        }

        private bool IsPipeDNLayer(string layer)
        {
            return (layer.ToUpper().Equals("W-FRPT-NOTE") ||
                    layer.ToUpper().Equals("W-FRPT-HYDT-DIMS") ||
                    layer.ToUpper().Equals("W-DRAI-DIMS") ||
                    layer.ToUpper().Equals("W-RAIN-DIMS") ||
                    layer.ToUpper().Equals("TWT_TEXT"));
        }

        public Dictionary<Point3dEx, string> GetSlashDic(Dictionary<Line, List<Point3d>> leadLineDic, Dictionary<Line, List<Line>> segLineDic)
        {
            var slashDic = new Dictionary<Point3dEx, string>();//存放斜点的标注
            foreach (var lead in leadLineDic.Keys)
            {
                var nums = 0;

                if (leadLineDic[lead].Count < segLineDic[lead].Count)
                {
                    nums = leadLineDic[lead].Count;
                }
                else
                {
                    nums = segLineDic[lead].Count;
                }

                for (int i = 0; i < nums; i++)
                {
                    var slashPt = leadLineDic[lead][i];//对于每个斜边
                    var line = segLineDic[lead][i];//提取每个短线
                    var rectArea = ThFireHydrantSelectArea.CreateArea(line);//创建提取区域

                    var spatialIndex = new ThCADCoreNTSSpatialIndex(DBObjsResult.ToDBObjectList().ToCollection());
                    var dbObj = spatialIndex.SelectCrossingPolygon(rectArea);
                    if (dbObj.Count == 0)
                    {
                        continue;//跳过空边
                    }

                    if ((dbObj[0] as DBText).TextString.Contains("DN"))
                    {
                        slashDic.Add(new Point3dEx(slashPt), (dbObj[0] as DBText).TextString);
                        continue;
                    }
                    dynamic acObj = dbObj[0].AcadObject;
                    slashDic.Add(new Point3dEx(slashPt), acObj.TextString);
                }
            }
            return slashDic;
        }
    
    }
}
