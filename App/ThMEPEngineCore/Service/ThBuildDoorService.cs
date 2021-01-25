using System;
using ThCADCore.NTS;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using System.Text.RegularExpressions;
using System.Linq;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.Model;

namespace ThMEPEngineCore.Service
{
    public class ThBuildDoorService
    {
        /// <summary>
        /// 返回门的轮廓
        /// </summary>
        private List<Polyline> Outlines { get; set; }
        private ThDoorMatchParameter DoorMatchParameter { get; set; }

        private ThBuildDoorService(
            ThDoorMatchParameter doorMatchParameter)
        {
            DoorMatchParameter = doorMatchParameter;
            Outlines = new List<Polyline>();
        }
        public static List<Polyline> Build(
            ThDoorMatchParameter doorMatchParameter)
        {
            var instance = new ThBuildDoorService(doorMatchParameter);
            instance.Build();
            return instance.Outlines;
        }
        private void Build()
        {
            DoorMatchParameter.DoorMarks.Cast<Entity>().ForEach(o =>
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
                            var stones = DoorMatchParameter.FindStones(envelope);
                            var vec = center.StartPoint.GetVectorTo(center.EndPoint);
                            //通过文字周围的门垛找到匹配的一对，创建其轮廓
                            var pair = ThMatchDoorStoneService.Match(stones, vec, length);
                            if(pair != null)
                            {
                                Outlines.Add(outline);
                            }
                        }
                    }
                });
        }
        private Polyline CreateEnvelope(Line center,double height,double length)
        {
            var vec = center.StartPoint.GetVectorTo(center.EndPoint).GetNormal();
            var midPt = ThGeometryTool.GetMidPt(center.StartPoint, center.EndPoint);
            var sp = midPt - vec.MultiplyBy(length / 2.0);
            var ep = midPt + vec.MultiplyBy(length / 2.0);
            return ThDrawTool.ToOutline(sp, ep, (DoorMatchParameter.FindRatio + 0.5) * height);
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
        private double GetDoorWidth(Tuple<Line,Line> pair)
        {
            var firstMidPt = pair.Item1.StartPoint.GetMidPt(pair.Item1.EndPoint);
            var secondMidPt = pair.Item2.StartPoint.GetMidPt(pair.Item2.EndPoint);
            var envelope = ThDrawTool.ToOutline(firstMidPt, secondMidPt, pair.Item1.Length / 2.0);
            var columns = DoorMatchParameter.ObstacleSpatialIndexService.FindColumns(envelope);
            var architectureWalls = DoorMatchParameter.ObstacleSpatialIndexService.FindArchitectureWalls(envelope); 
            var shearWalls = DoorMatchParameter.ObstacleSpatialIndexService.FindShearWalls(envelope);

        }
        private Polyline CreateOutlie(Tuple<Line, Line> pair)
        {
            var width = GetDoorWidth(pair);
            var firstMidPt = first.StartPoint.GetMidPt(first.EndPoint);
            var secondMidPt = second.StartPoint.GetMidPt(second.EndPoint);
            return ThDrawTool.ToRectangle(firstMidPt, secondMidPt, first.Length);
        }
        private 
        private Dictionary<double,List<Line>> Analyze(Polyline polyline,double lengthTolerance)
        {
            var results = new Dictionary<double, List<Line>>();
            var lines = polyline.ToLines();
            foreach(var line in lines)
            {
               var keys = results
                    .Where(o => Math.Abs(o.Key - line.Length) <= lengthTolerance)
                    .Select(o => o.Key)
                    .ToList();
                if(keys.Count==1)
                {
                    results[keys[0]].Add(line);
                }
                else
                {
                    results.Add(line.Length,new List<Line> { line});
                }
            }
            return results.Where(o=>o.Value.Count==2).OrderBy(o=>o.Key).ToDictionary(k=>);
        }
    }
    public class ThDoorMatchParameter
    {
        public ThCADCoreNTSSpatialIndex DoorStoneSpatialIndex { get; private set; }
        public ThCADCoreNTSSpatialIndex DoorMarkSpatialIndex { get; private set; }
        public ThObstacleSpatialIndexService ObstacleSpatialIndexService { get; private set; }

        public double FindRatio { get; set; } = 1.0;
        public DBObjectCollection DoorStones { get; set; }
        public DBObjectCollection DoorMarks { get; set; }
        public double LengthTolerance { get; set; } = 5.0;

        public double DoorMaximumThick { get; set; } = 300.0;
        public double DoorMinimumThick { get; set; } = 20.0;

        public ThDoorMatchParameter(
            DBObjectCollection doorStones, 
            DBObjectCollection doorMarks,
            ThObstacleSpatialIndexService obstructSpatialIndexService)
        {         
            DoorStones = doorStones;
            DoorMarks = doorMarks;
            ObstacleSpatialIndexService = obstructSpatialIndexService;
            DoorStoneSpatialIndex = new ThCADCoreNTSSpatialIndex(DoorStones);
            DoorMarkSpatialIndex = new ThCADCoreNTSSpatialIndex(DoorMarks);
        }
        public List<Polyline> FindStones(Polyline envelope)
        {
            return DoorStoneSpatialIndex.SelectCrossingPolygon(envelope).Cast<Polyline>().ToList();
        }
    }
}
