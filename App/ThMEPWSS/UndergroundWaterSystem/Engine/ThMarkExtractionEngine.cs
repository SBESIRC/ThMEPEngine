using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPWSS.UndergroundWaterSystem.Command;
using ThMEPWSS.UndergroundWaterSystem.Model;
using ThMEPWSS.UndergroundWaterSystem.Service;
using static ThMEPWSS.UndergroundWaterSystem.Utilities.GeoUtils;

namespace ThMEPWSS.UndergroundWaterSystem.Engine
{

    public class ThMarkExtractionEngine
    {
        public List<ThMarkModel> GetMarkListOptimized(Point3dCollection pts, Point3d startPoint,ref string startinfo)
        {
            using (var adb = AcadDatabase.Active())
            {
                var results = new List<ThMarkModel>();
                var entities = adb.ModelSpace.OfType<Entity>();
                DBObjectCollection dbObjs = null;
                if (pts != null)
                {
                    //var spatialIndex = new ThCADCoreNTSSpatialIndex(entities.ToCollection());
                    var pline = new Polyline()
                    {
                        Closed = true,
                    };
                    pline.CreatePolyline(pts);
                    dbObjs = entities.Where(e =>
                    {
                        var ex = e.Bounds;
                        if (ex == null) return true;
                        else return pline.Contains(((Extents3d)ex).CenterPoint());
                    }).ToCollection();
                }
                else
                {
                    dbObjs = entities.ToCollection();
                }
                var textList = new List<DBText>();
                var textLines = new List<Line>();
                foreach (var _object in dbObjs)
                {
                    var entity = _object as Entity;
                    if (!entity.Layer.Contains("W-")) continue;
                    if (!(entity.Layer.Contains("DIMS") || entity.Layer.Contains("NOTE")
                        || entity.Layer.Contains("EQPM"))) continue;
                    if (IsTianZhengElement(entity))
                    {
                        var explodeResult = GetAllEntitiesByExplodingTianZhengElementThoroughly(entity);
                        DBText ts = new DBText();
                        foreach (var obj in explodeResult)
                        {
                            var ent = obj as Entity;
                            if (ent is DBText t)
                            {
                                if (ts.TextString.Length == 0) ts = t;
                                else ts.TextString += t.TextString;
                            }
                            else if (ent is Line l) textLines.Add(l.GetProjectLine());
                            else if (ent is Polyline pl) textLines.AddRange(pl.ToLines().Select(e => e.GetProjectLine()));
                        }
                        if (ts.TextString.Length > 0) textList.Add(ts);
                    }
                    else
                    {
                        if (entity is DBText t) textList.Add(t);
                        else if (entity is Line l) textLines.Add(l.GetProjectLine());
                        else if (entity is Polyline pl) textLines.AddRange(pl.ToLines().Select(e => e.GetProjectLine()));
                    }
                }
                results.AddRange(CombMarkList(textList, textLines));
                startinfo = "";
                foreach (var res in results)
                {
                    if (res.Poistion.DistanceTo(startPoint) < 10)
                    {
                        startinfo = res.MarkText;
                        break;
                    }
                }
                //results = results.Where(e => !TestContainsChineseCharacter(e.MarkText)).ToList();
                //results = results.Where(e => !e.MarkText.Contains("DN")).ToList();
                return results;
            }
        }
        public List<ThMarkModel> GetMarkList(Point3dCollection pts = null)
        {
            using (var database = AcadDatabase.Active())
            {
                var retLines = new List<ThMarkModel>();
                var entities = database.ModelSpace.OfType<Entity>();

                DBObjectCollection dbObjs = null;
                if (pts != null)
                {
                    var spatialIndex = new ThCADCoreNTSSpatialIndex(entities.ToCollection());
                    var pline = new Polyline()
                    {
                        Closed = true,
                    };
                    pline.CreatePolyline(pts);
                    dbObjs = spatialIndex.SelectCrossingPolygon(pline);
                }
                else
                {
                    dbObjs = entities.ToCollection();
                }
                var textList = new List<DBText>();
                var textLines = new List<Line>();
                foreach (var obj in dbObjs)
                {
                    var ent = obj as Entity;
                    if (ThUndergroundWaterSystemUtils.IsTianZhengElement(ent))
                    {
                        var outTexts = new List<DBText>();
                        var outLines = new List<Line>();
                        if (GetTianZhengTextAndLine(ent, ref outTexts, ref outLines))
                        {
                            textList.AddRange(outTexts);
                            textLines.AddRange(outLines);
                        }
                    }
                    else
                    {
                        if (IsLayer(ent.Layer))
                        {
                            if (ent is DBText t)
                            {
                                textList.Add(t);
                                if (t.TextString.Equals("S-JGL-1"))
                                {
                                    string dir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                                    FileStream fs = new FileStream(dir + "\\WaterDebug.txt", FileMode.Append);
                                    StreamWriter sw = new StreamWriter(fs);
                                    sw.WriteLine(t.Position.X);
                                    sw.WriteLine(t.Position.Y);
                                    sw.Close();
                                    fs.Close();
                                }
                            }
                            else if (ent is Line l)
                            {
                                var tmpPt1 = new Point3d(l.StartPoint.X, l.StartPoint.Y, 0.0);
                                var tmpPt2 = new Point3d(l.EndPoint.X, l.EndPoint.Y, 0.0);
                                var tmpline = new Line(tmpPt1, tmpPt2);
                                textLines.Add(tmpline);
                            }
                            else if (ent is Polyline pl)
                            {
                                foreach (var o in pl.ToLines())
                                {
                                    var tmpPt1 = new Point3d(o.StartPoint.X, o.StartPoint.Y, 0.0);
                                    var tmpPt2 = new Point3d(o.EndPoint.X, o.EndPoint.Y, 0.0);
                                    var tmpline = new Line(tmpPt1, tmpPt2);
                                    textLines.Add(tmpline);
                                }
                            }
                        }
                    }
                }
                retLines = CombMarkList(textList, textLines);
                return retLines;
            }
        }
        public bool GetTianZhengTextAndLine(Entity ent, ref List<DBText> texts, ref List<Line> lines)
        {
            var explodeResult = GetAllEntitiesByExplodingTianZhengElementThoroughly(ent);
            foreach (var obj in explodeResult)
            {
                var entity = obj as Entity;
                if (IsLayer(entity.Layer))
                {
                    if (entity is DBText t)
                    {
                        texts.Add(t);
                    }
                    else if (entity is Line l)
                    {
                        var tmpPt1 = new Point3d(l.StartPoint.X, l.StartPoint.Y, 0.0);
                        var tmpPt2 = new Point3d(l.EndPoint.X, l.EndPoint.Y, 0.0);
                        var tmpline = new Line(tmpPt1, tmpPt2);
                        lines.Add(tmpline);
                    }
                    else if (entity is Polyline pl)
                    {
                        foreach (var o in pl.ToLines())
                        {
                            var tmpPt1 = new Point3d(o.StartPoint.X, o.StartPoint.Y, 0.0);
                            var tmpPt2 = new Point3d(o.EndPoint.X, o.EndPoint.Y, 0.0);
                            var tmpline = new Line(tmpPt1, tmpPt2);
                            lines.Add(tmpline);
                        }
                    }
                }
            }
            return true;
        }
        public bool IsLayer(string layer)
        {
            if ((layer.ToUpper().Contains("W-") && layer.ToUpper().Contains("-DIMS"))
                || (layer.ToUpper().Contains("W-") && layer.ToUpper().Contains("-NOTE"))
                || layer.ToUpper().Contains("W-"))
            {
                return true;
            }
            return false;
        }
        public List<ThMarkModel> CombMarkList(List<DBText> texts, List<Line> lines)
        {
            var retList = new List<ThMarkModel>();
            foreach (var t in texts)
            {
                var tplines = lines.OrderBy(e =>
                {
                    var a = e.StartPoint.DistanceTo(t.Position);
                    var b = e.EndPoint.DistanceTo(t.Position);
                    if (a <= b) return a;
                    else return b;
                }).Take(10).ToList();
                var mark = CombMark(t, tplines);
                mark.Layer = t.Layer;
                mark.TextStyle = t.TextStyleName;
                retList.Add(mark);
            }
            return retList;
        }
        public ThMarkModel CombMark(DBText text, List<Line> lines)
        {
            double tol = 1000;
            var retMark = new ThMarkModel();
            var textPt = new Point3d(text.Position.X, text.Position.Y, 0.0);
            Line tmpLine = null;
            var foundlines = new List<Line>();
            foreach (var l in lines)
            {
                if (l.GetClosestPointTo(textPt, false).DistanceTo(textPt) < tol)
                {
                    foundlines.Add(l);
                }
            }
            if (foundlines.Count > 0) tmpLine = foundlines.OrderBy(e => e.GetClosestPointTo(textPt, false).DistanceTo(textPt)).First();
            retMark.MarkText = text.TextString;
            retMark.Poistion = textPt;
            if (tmpLine != null)
            {
                var tmpPts = new List<Point3d>();
                tmpPts.Add(tmpLine.StartPoint);
                tmpPts.Add(tmpLine.EndPoint);
                foreach (var pt in tmpPts)
                {
                    var seriesLines = FindSeriesLine(pt, lines);
                    if (seriesLines.Count == 2)
                    {
                        Line tempLine = null;
                        foreach (var seriesLine in seriesLines)
                        {
                            var lineVector = new Vector3d(Math.Cos(seriesLine.Angle), Math.Sin(Math.Sin(seriesLine.Angle)), 0.0);
                            var textVector = new Vector3d(Math.Cos(text.Rotation), Math.Sin(Math.Sin(text.Rotation)), 0.0);
                            if (lineVector.IsParallelToEx(textVector))
                            {
                                tempLine = seriesLine;
                                break;
                            }

                        }
                        if (tempLine != null)
                        {
                            seriesLines.Remove(tempLine);
                            var line = seriesLines.FirstOrDefault();
                            var distance1 = tempLine.GetClosestPointTo(line.StartPoint, false).DistanceTo(line.StartPoint);
                            var distance2 = tempLine.GetClosestPointTo(line.EndPoint, false).DistanceTo(line.EndPoint);
                            if (distance1 > distance2)
                            {
                                retMark.Poistion = line.StartPoint;
                            }
                            else
                            {
                                retMark.Poistion = line.EndPoint;
                            }
                        }
                        break;
                    }
                }
            }
            return retMark;
        }
        public List<Line> FindSeriesLine(Point3d startPt, List<Line> allLines)
        {
            var tmpLines = new List<Line>();
            foreach (var l in allLines)
            {
                var tmpline = new Line(l.StartPoint, l.EndPoint);
                tmpLines.Add(tmpline);
            }
            //查找到与起点相连的线
            var startLine = ThUndergroundWaterSystemUtils.FindStartLine(startPt, tmpLines);
            if (startLine == null)
            {
                return null;
            }
            //查找到与startLine相连的一系列线
            tmpLines.Remove(startLine);
            var retLines = FindSeriesLine(startLine, ref tmpLines);
            return retLines;
        }
        private List<Line> FindSeriesLine(Line objectLine, ref List<Line> allLines)
        {
            var retLines = new List<Line>();
            retLines.Add(objectLine);
            var conLine = FindConnectLine(objectLine, ref allLines);
            if (conLine != null)
            {
                retLines.AddRange(FindSeriesLine(conLine, ref allLines));
            }
            return retLines;
        }
        private Line FindConnectLine(Line objectLine, ref List<Line> lines)
        {
            Line retLine = null;
            var objectEndPt = objectLine.EndPoint;
            foreach (var l in lines)
            {
                if (l.Length < 10.0)
                {
                    continue;
                }
                var startPt = l.StartPoint;
                var endPt = l.EndPoint;
                if (objectEndPt.DistanceTo(startPt) < 10.0)
                {
                    retLine = l;
                    break;
                }
                else if (objectEndPt.DistanceTo(endPt) < 10.0)
                {
                    var tmpPt = new Point3d(l.StartPoint.X, l.StartPoint.Y, l.StartPoint.Z);
                    l.StartPoint = l.EndPoint;
                    l.EndPoint = tmpPt;
                    retLine = l;
                    break;
                }
            }
            lines.Remove(retLine);
            return retLine;
        }
    }
}
