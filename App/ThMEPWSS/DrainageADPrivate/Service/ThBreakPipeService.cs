using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AcHelper;
using NFox.Cad;
using Linq2Acad;
using Dreambuild.AutoCAD;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using ThCADExtension;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Diagnostics;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.LaneLine;
using NetTopologySuite.Geometries;

using ThMEPWSS.DrainageADPrivate.Model;

namespace ThMEPWSS.DrainageADPrivate.Service
{
    internal class ThBreakPipeService
    {
        public static void BreakPipe(List<ThDrainageTreeNode> rootList, out List<Line> coolPipe, out List<Line> hotPipe)
        {
            var allNode = rootList.SelectMany(x => x.GetDescendant()).ToList();
            allNode.AddRange(rootList);

            var transLineNodeDict = ThCreateLineService.TurnNodeToTransLineDict(allNode);
            var allLine = transLineNodeDict.Select(x => x.Key).ToList();

            var intersectPt = GetIntersectionPt(allLine);

            var lineIntersectPtDict = GetNeedBreakLine(intersectPt, transLineNodeDict);

            var breakLine = BreakLine(lineIntersectPtDict);

            //修改最终结果里的断线
            coolPipe = transLineNodeDict.Where(x => x.Value.IsCool == true).Select(x => x.Key).ToList();
            var removeCool = coolPipe.Where(x => breakLine.ContainsKey(x) && breakLine[x].Count > 0).ToList();
            coolPipe.RemoveAll(x => removeCool.Contains(x));
            coolPipe.AddRange(breakLine.Where(x => removeCool.Contains(x.Key)).SelectMany(x => x.Value));

            hotPipe = transLineNodeDict.Where(x => x.Value.IsCool == false).Select(x => x.Key).ToList();
            var removeHot = hotPipe.Where(x => breakLine.ContainsKey(x) && breakLine[x].Count > 0).ToList();
            hotPipe.RemoveAll(x => removeHot.Contains(x));
            hotPipe.AddRange(breakLine.Where(x => removeHot.Contains(x.Key)).SelectMany(x => x.Value));

        }

        private static Dictionary<Point3d, KeyValuePair<Line, Line>> GetIntersectionPt(List<Line> allLine)
        {
            var intersectPt = new Dictionary<Point3d, KeyValuePair<Line, Line>>();

            var tol = new Tolerance(1, 1);
            for (int i = 0; i < allLine.Count; i++)
            {
                for (int j = i + 1; j < allLine.Count; j++)
                {
                    var ptList = allLine[i].IntersectWithEx(allLine[j]).OfType<Point3d>().ToList();
                    if (ptList.Count > 0)
                    {
                        var truePt = ptList.Where(x => x.IsEqualTo(allLine[i].StartPoint, tol) == false &&
                                                       x.IsEqualTo(allLine[i].EndPoint, tol) == false &&
                                                       x.IsEqualTo(allLine[j].StartPoint, tol) == false &&
                                                       x.IsEqualTo(allLine[i].EndPoint, tol) == false).ToList();
                        if (truePt.Count > 0)
                        {
                            intersectPt.Add(truePt.First(), new KeyValuePair<Line, Line>(allLine[i], allLine[j]));
                        }
                    }
                }
            }

            return intersectPt;
        }

        private static Dictionary<Line, List<Point3d>> GetNeedBreakLine(Dictionary<Point3d, KeyValuePair<Line, Line>> intersectPt, Dictionary<Line, ThDrainageTreeNode> transLineNodeDict)
        {
            var lineIntersectPtDict = new Dictionary<Line, List<Point3d>>();

            foreach (var intersectPtDict in intersectPt)
            {
                //相交点在原始线上的点
                var lineA = intersectPtDict.Value.Key;
                var lineB = intersectPtDict.Value.Value;

                var ptA = GetIntersectPtOnOri(intersectPtDict.Key, lineA, transLineNodeDict);
                var ptB = GetIntersectPtOnOri(intersectPtDict.Key, lineB, transLineNodeDict);

                var dir = ptB - ptA;
                if (dir.Z < 0)
                {
                    AddToLineIntersectPtDict(lineIntersectPtDict, lineB, intersectPtDict.Key);
                }
                else
                {
                    AddToLineIntersectPtDict(lineIntersectPtDict, lineA, intersectPtDict.Key);
                }
            }

            return lineIntersectPtDict;
        }

        private static Point3d GetIntersectPtOnOri(Point3d interPt, Line l, Dictionary<Line, ThDrainageTreeNode> transLineNodeDict)
        {
            var node = transLineNodeDict[l];
            var multiple = GetLineMultiple(interPt, l);
            var ptOnOri = CalculateIntersectPtOnOri(node, multiple);

            if (node.IsCool == false)
            {
                //热水管z轴+20
                ptOnOri = ptOnOri + 20 * new Vector3d(0, 0, 1);
            }

            return ptOnOri;
        }

        private static void AddToLineIntersectPtDict(Dictionary<Line, List<Point3d>> lineIntersectPtDict, Line l, Point3d pt)
        {
            if (lineIntersectPtDict.ContainsKey(l) == false)
            {
                lineIntersectPtDict.Add(l, new List<Point3d>());
            }
            lineIntersectPtDict[l].Add(pt);
        }

        private static double GetLineMultiple(Point3d pt, Line line)
        {
            double multiple = 0.0;
            if (line != null)
            {
                var vs = pt - line.StartPoint;
                var es = line.EndPoint - line.StartPoint;

                multiple = (vs.Length / es.Length);
            }

            return multiple;
        }

        private static Point3d CalculateIntersectPtOnOri(ThDrainageTreeNode node, double multiple)
        {
            var es = node.Pt - node.Parent.Pt;
            var insertVect = es * multiple;
            var interPtOri = node.Parent.Pt + insertVect;
            return interPtOri;
        }

        private static Dictionary<Line, List<Line>> BreakLine(Dictionary<Line, List<Point3d>> lineIntersectPtDict)
        {
            var breakLine = new Dictionary<Line, List<Line>>();//写入原line 以防线太短没做
            var break_length = ThDrainageADCommon.BreakLineLength;

            //break line in dictionary A to list<line> B
            foreach (var lineIntersectPt in lineIntersectPtDict)
            {
                var incPts = lineIntersectPt.Value;
                var line = lineIntersectPt.Key;
                var breakLineTemp = new List<Line>();
                var dir = (line.EndPoint - line.StartPoint).GetNormal();

                incPts = incPts.OrderBy(x => x.DistanceTo(line.StartPoint)).ToList();
                incPts.Insert(0, line.StartPoint - dir * break_length);
                incPts.Add(line.EndPoint + dir * break_length);

                for (int i = 1; i < incPts.Count; i++)
                {
                    if (incPts[i].DistanceTo(incPts[i - 1]) >= break_length * 2)
                    {
                        var l = new Line(incPts[i - 1] + dir * break_length, incPts[i] - dir * break_length);
                        breakLineTemp.Add(l);
                    }
                }
                breakLine.Add(line, breakLineTemp);
            }

            return breakLine;
        }
    }
}
