using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm;
using ThMEPWSS.Uitl.ExtensionsNs;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;

namespace ThMEPWSS.UndergroundSpraySystem.Model
{
    public class AlarmTchText
    {
        public DBObjectCollection DBObjs { get; set; }
        public AlarmTchText()
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
                   .Where(o => o.IsTCHText())
                   .Where(o => IsTargetLayer(o.Layer))
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
            return layer.Contains("TWT_TEXT");
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
                foreach (var db in dbObjs)
                {
                    var e = db as Entity;
                    if (e.IsTCHText())
                    {
                        var text = e.ExplodeTCHText()[0] as DBText;
                        var textStr = text.TextString.Trim();
                        if (textStr.Count() < 2)
                        {
                            continue;
                        }
                        if (IsAlphabet(textStr[0]))
                        {
                            Regex rex = new Regex(@"^\d+$");//^开始，\d匹配一个数字字符，+出现至少一次，$结尾
                            if (rex.IsMatch(textStr[1].ToString()))
                            {
                                DBObjs.Add(text);
                            }
                        }
                    }
                    if (e is DBText text2)
                    {
                        var textStr = text2.TextString.Trim();
                        if (textStr.Count() < 2)
                        {
                            continue;
                        }
                        if (IsAlphabet(textStr[0]))
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
            catch(Exception ex)
            {
                ;
            }
        }

        private bool IsAlphabet(char ch)
        {
            if(ch >= 'a' && ch <= 'z')
            {
                return true;
            }
            if(ch >= 'A' && ch <= 'Z')
            {
                return true;
            }
            return false;
        }

        public void CreateAlarmTextDic(SprayIn sprayIn, List<Point3d> alarmPts, ThCADCoreNTSSpatialIndex textSpatialIndex)
        {
            var spatialIndex = new ThCADCoreNTSSpatialIndex(DBObjs);
            foreach (var pt1 in alarmPts)
            {
                var pt = new Point3dEx(pt1);
                int tolerance = 200;
                while (true)
                {
                    if (tolerance > 1000)
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
                    if (dbObjs.Count == 1)//只找到一个目标，直接添加
                    {
                        var text = (dbObjs[0] as DBText).TextString;
                        if (sprayIn.AlarmTextDic.ContainsKey(pt))
                        {
                            sprayIn.AlarmTextDic.Remove(pt);
                        }
                        sprayIn.AlarmTextDic.Add(pt, text);
                        break;
                    }
                    if (dbObjs.Count > 1)//多个时选择距离最近的
                    {
                        double dist = 10000;
                        foreach (var db in dbObjs)
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
                if(pt1.DistanceTo(new Point3d(1445761.1, 2760364.6,0))<100)
                {
                    ;
                }
                if(sprayIn.AlarmTextDic[pt].Equals(""))
                {
                    Line startLine = new Line();
                    Line textLine = new Line();
                    foreach (var l in sprayIn.LeadLines)
                    {
                        var spt = l.StartPoint;
                        var ept = l.EndPoint;
                        if (pt._pt.DistanceTo(spt) < 100 || pt._pt.DistanceTo(ept) < 100)
                        {
                            startLine = l;
                        }
                    }
                    if(!sprayIn.LeadLineDic.ContainsKey(startLine))
                    {
                        string str = ExtractText(textSpatialIndex, CreateLineHalfBuffer(startLine, 300));
                        sprayIn.AlarmTextDic[pt] = str;
                    }
                    else
                    {
                        var adjs = sprayIn.LeadLineDic[startLine];
                        if (adjs.Count > 0)
                        {
                            string str = ExtractText(textSpatialIndex, CreateLineHalfBuffer(adjs[0], 300));
                            sprayIn.AlarmTextDic[pt] = str;
                        }
                    }

                }
            }
        }

        private static Polyline CreateLineHalfBuffer(Line line, int tolerance = 50)
        {
            var pl = new Polyline();
            var pts = new Point2dCollection();

            var spt = line.StartPoint;
            var ept = line.EndPoint;
            pts.Add(spt.ToPoint2D()); // low left
            pts.Add(spt.OffsetY(tolerance).ToPoint2D()); // high left
            pts.Add(ept.OffsetY(tolerance).ToPoint2D()); // low right
            pts.Add(ept.ToPoint2D()); // high right
            pts.Add(spt.ToPoint2D()); // low left
            pl.CreatePolyline(pts);
            using (AcadDatabase currentDb = AcadDatabase.Active())
            {
                currentDb.CurrentSpace.Add(pl);
            }
            return pl;
        }


        private string ExtractText(ThCADCoreNTSSpatialIndex spatialIndex, Polyline selectArea)
        {
            var DBObjs = spatialIndex.SelectCrossingPolygon(selectArea);
            var pipeNumber = "";
            double dist = 1000;
            if (DBObjs.Count == 1)
            {
                pipeNumber = (DBObjs[0] as DBText).TextString;
            }
            foreach (var obj in DBObjs)
            {
                if (obj is DBText br)
                {
                    var curDist = Math.Min(br.Position.DistanceTo(selectArea.StartPoint),
                                           br.Position.DistanceTo(selectArea.GetPoint3dAt(3)));
                    if (curDist < dist)
                    {
                        pipeNumber = br.TextString;
                        dist = curDist;
                    }
                }
            }
            return pipeNumber;
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
