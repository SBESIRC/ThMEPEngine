using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;

namespace ThMEPWSS.UndergroundSpraySystem.Model
{
    public class AlarmText
    {
        public DBObjectCollection DBObjs { get; set; }
        public AlarmText()
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
                        .Where(e => IsTCHNote(e))
                        .ForEach(e => ExplodeTCHNote(e, DBObjs));
                    dbObjs.Cast<Entity>()
                        .Where(e => e is DBText)
                        .ForEach(e => DBObjs.Add(e));
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
                   layer.Contains("-SHET-PROF"));
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
                    var e = db as Entity;
                    if(e.IsTCHText())
                    {
                        var text = e.ExplodeTCHText()[0] as DBText;
                        var textStr = text.TextString.Trim();
                        if (textStr.Count() < 2)
                        {
                            continue;
                        }
                        if(textStr[0] > 'a' && textStr[0] < 'z')
                        {
                            Regex rex = new Regex(@"^\d+$");//^开始，\d匹配一个数字字符，+出现至少一次，$结尾
                            if (rex.IsMatch(textStr[1].ToString()))
                            {
                                DBObjs.Add(text);
                            }
                        }
                    }
                    if(e is DBText text2)
                    {
                        var textStr = text2.TextString.Trim();
                        if (textStr.Count() < 2)
                        {
                            continue;
                        }
                        if (textStr[0] >= 'a' && textStr[0] <= 'z')
                        {
                            Regex rex = new Regex(@"^\d+$");
                            if (rex.IsMatch(textStr[1].ToString()))
                            {
                                DBObjs.Add(text2);
                            }
                        }
                    }
                }
            }
            catch
            {
                ;
            }
        }

        public void CreateAlarmTextDic(SprayIn sprayIn, List<Point3d> alarmPts)
        {
            var spatialIndex = new ThCADCoreNTSSpatialIndex(DBObjs);
            foreach (var pt1 in alarmPts)
            {
                var pt = new Point3dEx(pt1);
                int tolerance = 200;
                while(true)
                {
                    if(tolerance > 1000)
                    {
                        if (sprayIn.AlarmTextDic.ContainsKey(pt))
                        {
                            sprayIn.AlarmTextDic.Remove(pt);
                        }
                        sprayIn.AlarmTextDic.Add(pt, "");
                        break;
                    }
                    var bounds = CreatePolyline(pt, tolerance);
                    var dbObjs = spatialIndex.SelectCrossingPolygon(bounds);
                    if(dbObjs.Count == 1)//只找到一个目标，直接添加
                    {
                        var text = (dbObjs[0] as DBText).TextString;
                        if (sprayIn.AlarmTextDic.ContainsKey(pt))
                        {
                            sprayIn.AlarmTextDic.Remove(pt);
                        }
                        sprayIn.AlarmTextDic.Add(pt, text);
                        break;
                    }
                    if(dbObjs.Count > 1)//多个时选择距离最近的
                    {
                        double dist = 10000;
                        foreach(var db in dbObjs)
                        {
                            var cenPt = GetMidPt(db as DBText);//中心点
                            var text = (db as DBText).TextString;//文字
                            var curDist = cenPt.DistanceTo(pt._pt);
                            if (curDist < dist)//距离小于最小距离
                            {
                                if (sprayIn.AlarmTextDic.ContainsKey(pt))
                                {
                                    sprayIn.AlarmTextDic.Remove(pt);
                                }
                                sprayIn.AlarmTextDic.Add(pt, text);//替换
                                dist = curDist;
                            }
                        }
                        break;
                    }
                    tolerance += 50;
                }
            }
        }

        private static Polyline CreatePolyline(Point3dEx c, int tolerance = 50)
        {
            var pl = new Polyline();
            var pts = new Point2dCollection();
            pts.Add(new Point2d(c._pt.X - tolerance, c._pt.Y - tolerance)); // low left
            pts.Add(new Point2d(c._pt.X - tolerance, c._pt.Y + tolerance)); // high left
            pts.Add(new Point2d(c._pt.X + tolerance, c._pt.Y + tolerance)); // high right
            pts.Add(new Point2d(c._pt.X + tolerance, c._pt.Y - tolerance)); // low right
            pts.Add(new Point2d(c._pt.X - tolerance, c._pt.Y - tolerance)); // low left
            pl.CreatePolyline(pts);
            return pl;
        }

        private static Point3d GetMidPt(Point3d pt1, Point3d pt2)
        {
            double x = (pt1.X + pt2.X) / 2;
            double y = (pt1.Y + pt2.Y) / 2;
            return new Point3d(x, y, 0);
        }

        private static Point3d GetMidPt(DBText dBText)//获取文字的中心点
        {
            var pt1 = dBText.GeometricExtents.MaxPoint;
            var pt2 = dBText.GeometricExtents.MinPoint;
            return GetMidPt(pt1, pt2);
        }
    }
}
