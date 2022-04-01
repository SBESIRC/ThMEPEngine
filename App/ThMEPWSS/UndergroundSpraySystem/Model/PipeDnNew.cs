using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm;
using ThMEPWSS.CADExtensionsNs;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;

namespace ThMEPWSS.UndergroundSpraySystem.Model
{
    public class PipeDnNew//提取跨楼层逻辑的管径 DNXX
    {
        public DBObjectCollection DBObjs { get; private set; }
        public PipeDnNew()
        {
            DBObjs = new DBObjectCollection();
        }

        public DBObjectCollection Extract(Database database, SprayIn sprayIn)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                var Results = acadDatabase
                   .ModelSpace
                   .OfType<Entity>()
                   .Where(o => o.IsTCHText() || o is DBText)//文字 或 天正对象的文字
                   .Where(o => IsTargetLayer(o.Layer))
                   .ToList();

                var spatialIndex = new ThCADCoreNTSSpatialIndex(Results.ToCollection());
                foreach (var polygon in sprayIn.FloorRectDic.Values)
                {
                    var dbObjs = spatialIndex.SelectCrossingPolygon(polygon);
                    foreach (var obj in dbObjs)
                    {
                        var ent = obj as Entity;
                        if (ent is DBText text)
                        {
                            if (text.TextString.StartsWith("DN"))
                            {
                                DBObjs.Add(text);
                            }
                        }
                        else if (ent.IsTCHText())
                        {
                            var dbText = ent.ExplodeTCHText()[0] as DBText;
                            if (dbText.TextString.StartsWith("DN"))
                            {
                                DBObjs.Add(dbText);
                            }
                        }
                    }
                }

                return DBObjs;
            }
        }

        public Dictionary<Point3dEx, string> GetSlashDic(Dictionary<Line, List<Point3d>> leadLineDic, Dictionary<Line, List<Line>> segLineDic)
        {
            var slashDic = new Dictionary<Point3dEx, string>();//存放斜点的标注
            foreach (var lead in leadLineDic.Keys)
            {
                var nums = Math.Min(leadLineDic[lead].Count, segLineDic[lead].Count);

                for (int i = 0; i < nums; i++)
                {
                    var slashPt = leadLineDic[lead][i];//对于每个斜边
                    var line = segLineDic[lead][i];//提取每个短线
                    var rectArea = ThFireHydrantSelectArea.CreateArea(line,200);//创建提取区域

                    var spatialIndex = new ThCADCoreNTSSpatialIndex(DBObjs);
                    var dbObj = spatialIndex.SelectCrossingPolygon(rectArea);
                    if (dbObj.Count == 0)
                    {
                        continue;//跳过空边
                    }
                    var dbText = dbObj[0] as DBText;
                    if (dbText.TextString.Contains("*")) continue;
                    if (dbText.TextString.Contains("DN"))
                    {
                        slashDic.Add(new Point3dEx(slashPt), dbText.TextString);
                        continue;
                    }
                    dynamic acObj = dbObj[0].AcadObject;
                    slashDic.Add(new Point3dEx(slashPt), acObj.TextString);
                }
            }
            return slashDic;
        }

        private bool IsTargetLayer(string layer)
        {
            return layer == "W-FRPT-NOTE";
        }
    }
}
