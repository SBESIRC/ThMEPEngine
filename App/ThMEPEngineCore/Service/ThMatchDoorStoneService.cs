using System;
using ThCADCore.NTS;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using System.Text.RegularExpressions;
using System.Linq;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPEngineCore.Service
{
    public class ThMatchDoorStoneService
    {
        /// <summary>
        /// 返回门的轮廓
        /// </summary>
        private List<Polyline> Outlines { get; set; }
        private double FindRatio { get; set; }
        private ThCADCoreNTSSpatialIndex DoorStoneSpatialIndex { get; set; }
        private ThCADCoreNTSSpatialIndex DoorMarkSpatialIndex { get; set; }
        private DBObjectCollection DoorStones { get; set; }
        private DBObjectCollection DoorMarks { get; set; }
        private ThMatchDoorStoneService(
            DBObjectCollection doorStones, 
            DBObjectCollection texts,
            double findRatio=1.0)
        {
            DoorStones = doorStones;
            DoorMarks = texts;
            FindRatio = findRatio;
            DoorStoneSpatialIndex = new ThCADCoreNTSSpatialIndex(doorStones);
            DoorMarkSpatialIndex = new ThCADCoreNTSSpatialIndex(texts);
            Outlines = new List<Polyline>();
        }
        public static List<Polyline> Match(
            DBObjectCollection doorStones, 
            DBObjectCollection texts, 
            double findRatio = 1.0)
        {
            var instance = new ThMatchDoorStoneService(doorStones, texts, findRatio);
            instance.Match();
            return instance.Outlines;
        }
        private void Match()
        {
            DoorMarks.Cast<Entity>().ForEach(o =>
                {
                    var content = GetTextString(o);
                    var strList = Parse(content);
                    double length = 0.0;
                    if(strList.Count>=2 && strList.Count <= 3)
                    {
                       length = GetLength(strList);
                    }
                    if (length > 0)
                    {
                        var info = GetTextInfo(o);
                        if (info != null)
                        {
                            double height = info.Item1;
                            Line center= info.Item2;
                            var envelope = CreateEnvelope(center, height, length);
                            var stones = FindStones(envelope);
                            var vec = center.StartPoint.GetVectorTo(center.EndPoint);
                            //通过文字周围的门垛找到匹配的一对，创建其轮廓
                            var outline = ThBuildDoorService.Build(stones, vec, length);
                            if(outline!=null)
                            {
                                Outlines.Add(outline);
                            }
                        }
                    }
                });
        }
        private List<Polyline> FindStones(Polyline envelope)
        {
           return DoorStoneSpatialIndex.SelectCrossingPolygon(envelope).Cast<Polyline>().ToList();
        }
        private Polyline CreateEnvelope(Line center,double height,double length)
        {
            var vec = center.StartPoint.GetVectorTo(center.EndPoint).GetNormal();
            var midPt = ThGeometryTool.GetMidPt(center.StartPoint, center.EndPoint);
            var sp = midPt - vec.MultiplyBy(length / 2.0);
            var ep = midPt + vec.MultiplyBy(length / 2.0);
            return ThDrawTool.ToOutline(sp, ep, (FindRatio + 0.5) * height);
        }
        private string GetTextString(Entity ent)
        {
            if(ent is DBText dbText)
            {
                return dbText.TextString;
            }
            else if(ent is MText mText)
            {
                return mText.Contents;
            }
            else
            {
                throw new NotSupportedException();
            }
        }
        private double GetLength(List<string> values)
        {
            string lengthStr = values[1];
            double length = double.Parse(lengthStr.Substring(0, 2))*100.0;
            if(values.Count==3)
            {
                switch(values[2])
                {
                    case "a":
                        length += 50.0;
                        break;
                    default:
                        length += 0.0;
                        break;
                }
            }
            return length;
        }
        private List<string> Parse(string content)
        {
            var results = new List<string>();
            string pattern1 = @"[M]{1}\d+[a-z*]?";
            var regex = new Regex(pattern1);
            var matches = regex.Matches(content);
            if(matches.Count==1)
            {
                if(matches[0].Value.Length==content.Length)
                {
                    results.Add("M");
                    string pattern2 = @"\d+";
                    var regex1 = new Regex(pattern2);
                    results.Add(regex1.Match(matches[0].Value).Value);
                    string pattern3 = @"[a-z]{1}$";
                    var regex2 = new Regex(pattern3);
                    var matches1 = regex2.Matches(matches[0].Value);
                    if(matches1.Count==1)
                    {
                        results.Add(matches1[0].Value);
                    }
                    return results;
                }
                else
                {
                    return results;
                }
            }
            else
            {
                return results;
            }
        }
        /// <summary>
        /// 获取文字的高度和中心线
        /// </summary>
        /// <param name="ent"></param>
        /// <returns></returns>
        private Tuple<double,Line> GetTextInfo(Entity ent)
        {
            var boundary = new Polyline();
            if (ent is DBText dbText)
            {
                boundary= dbText.TextOBB();
            }
            else if (ent is MText mText)
            {
                boundary = mText.TextOBB();
            }
            else
            {
                throw new NotSupportedException();
            }
            var lines=boundary.ToLines();
            lines=lines.OrderBy(o => o.Length).ToList();
            if(lines.Count==4)
            {
                var sp = ThGeometryTool.GetMidPt(lines[0].StartPoint, lines[0].EndPoint);
                var ep = ThGeometryTool.GetMidPt(lines[1].StartPoint, lines[1].EndPoint);
                return Tuple.Create(Math.Max(lines[0].Length, lines[2].Length),new Line(sp, ep));
            }
            else
            {
                return null;
            }
        }
    }
}
