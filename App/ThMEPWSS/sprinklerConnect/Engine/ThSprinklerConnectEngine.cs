using System;
using System.Collections.Generic;
using System.Linq;
using Linq2Acad;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using ThCADExtension;
using ThCADCore.NTS;
using ThMEPWSS.SprinklerConnect.Model;
using NFox.Cad;
using Dreambuild.AutoCAD;
using DotNetARX;
using ThMEPWSS.SprinklerConnect.Service;
using ThMEPEngineCore;
using NetTopologySuite.Operation.Buffer;

namespace ThMEPWSS.SprinklerConnect.Engine
{
    public class ThSprinklerConnectEngine
    {
        private static double DTTol { get; set; }
        private static ThSprinklerParameter SprinklerParameter { get; set; }
        private static List<Point3d> SprinklerSearched { get; set; } = new List<Point3d>();
        //private static List<Point3d> SecondSprinklerSearched { get; set; } = new List<Point3d>();
        private static List<Line> LaneLine { get; set; } = new List<Line>();
        private static List<Polyline> Geometry { get; set; } = new List<Polyline>();

        public ThSprinklerConnectEngine(ThSprinklerParameter sprinklerParameter, List<Polyline> geometry)
        {
            SprinklerParameter = sprinklerParameter;
            Geometry = geometry;
        }

        // 喷头连管
        public void SprinklerConnectEngine(List<Polyline> doubleStall, List<Polyline> smallRooms, List<Polyline> obstacle, bool isVertical = true)
        {
            var rowConnection = new List<ThSprinklerRowConnect>();
            //var secRowConnection = new List<ThSprinklerRowConnect>();
            SprinklerSearched = new List<Point3d>();
            //SecondSprinklerSearched = new List<Point3d>();
            var pipeScatters = new List<Point3d>();

            var netList = ThSprinklerPtNetworkEngine.GetSprinklerPtNetwork(SprinklerParameter, Geometry, out double dtTol);
            DTTol = dtTol;

            if (doubleStall.Count > 0)
            {
                LaneLine = GetLaneLine(doubleStall);
            }
            else
            {
                LaneLine = new List<Line>();
            }

            // < netList.Count
            for (int i = 0; i < netList.Count; i++)
            {
                // < netList[i].ptsGraph.Count
                for (int j = 0; j < netList[i].ptsGraph.Count; j++)
                {
                    rowConnection.AddRange(GraphPtsConnect(netList[i], j, pipeScatters, isVertical));
                }
            }

            // 处理环
            HandleLoopRow(rowConnection);
            // 散点处理
            HandleScatter(rowConnection, pipeScatters);
            // 列分割
            rowConnection = RowSeparation(rowConnection, isVertical);

            // 散点直接连管
            ConnScatterToPipe(rowConnection, pipeScatters);

            // < netList.Count
            for (int i = 0; i < netList.Count; i++)
            {
                // < netList[i].ptsGraph.Count
                for (int j = 0; j < netList[i].ptsGraph.Count; j++)
                {
                    HandleConsequentScatter(netList[i], j, rowConnection, pipeScatters, smallRooms, obstacle);
                }
            }

            var wallIndex = new ThCADCoreNTSSpatialIndex(Geometry.ToCollection());
            var obstacleIndex = new ThCADCoreNTSSpatialIndex(obstacle.ToCollection());

            var connTolerance = 300.0;
            var lines = new List<Line>();
            rowConnection.ForEach(row =>
            {
                var count = row.OrderDict.Keys.Where(key => key >= 0).Count();
                var ptsTemp = new List<Tuple<Point3d, bool>>();
                ptsTemp.Add(Tuple.Create(row.OrderDict[0][0], false));
                for (int i = 1; i < count; i++)
                {
                    // true表示为喷头点
                    ptsTemp.Add(Tuple.Create(row.OrderDict[i][0], true));
                }
                if (count > 1)
                {
                    var baseLine = new Line(ptsTemp[0].Item1, ptsTemp[1].Item1);
                    for (int i = 1; i < count; i++)
                    {
                        for (int j = 1; j < row.OrderDict[i].Count; j++)
                        {
                            var closePt = baseLine.GetClosestPointTo(row.OrderDict[i][j], true);
                            int num = 1;
                            for (; num < ptsTemp.Count; num++)
                            {
                                var newLine = new Line(ptsTemp[num - 1].Item1, ptsTemp[num].Item1);
                                if (newLine.DistanceTo(closePt, false) < 1.0)
                                {
                                    if (ptsTemp[num - 1].Item2 && closePt.DistanceTo(ptsTemp[num - 1].Item1) < connTolerance / 2)
                                    {
                                        var closePtTemp = ptsTemp[num - 1].Item1 + connTolerance * baseLine.LineDirection();
                                        var extendPt = closePtTemp + (row.OrderDict[i][j] - closePt);
                                        var firstLine = new Line(closePtTemp, extendPt);
                                        var secondLine = new Line(extendPt, row.OrderDict[i][j]);
                                        lines.Add(firstLine);
                                        lines.Add(secondLine);
                                        ptsTemp.Insert(num, Tuple.Create(closePtTemp, false));
                                    }
                                    else if (ptsTemp[num].Item2 && closePt.DistanceTo(ptsTemp[num].Item1) < connTolerance / 2)
                                    {
                                        var closePtTemp = ptsTemp[num].Item1 - connTolerance * baseLine.LineDirection();
                                        var extendPt = closePtTemp + (row.OrderDict[i][j] - closePt);
                                        var firstLine = new Line(closePtTemp, extendPt);
                                        var secondLine = new Line(extendPt, row.OrderDict[i][j]);
                                        lines.Add(firstLine);
                                        lines.Add(secondLine);
                                        ptsTemp.Insert(num, Tuple.Create(closePtTemp, false));
                                    }
                                    else
                                    {
                                        var firstLine = new Line(closePt, row.OrderDict[i][j]);
                                        lines.Add(firstLine);
                                        ptsTemp.Insert(num, Tuple.Create(closePt, false));
                                    }
                                    break;
                                }
                            }
                            if (num == ptsTemp.Count)
                            {
                                if (ptsTemp[num - 1].Item2 && closePt.DistanceTo(ptsTemp[num - 1].Item1) < connTolerance)
                                {
                                    var closePtTemp = ptsTemp[num - 1].Item1 + connTolerance * baseLine.LineDirection();
                                    var firstLine = new Line(closePtTemp, closePtTemp + (row.OrderDict[i][j] - closePt));
                                    var secondLine = new Line(closePtTemp + (row.OrderDict[i][j] - closePt), row.OrderDict[i][j]);
                                    lines.Add(firstLine);
                                    lines.Add(secondLine);
                                    ptsTemp.Insert(num, Tuple.Create(closePtTemp, false));
                                }
                                else
                                {
                                    var firstLine = new Line(closePt, row.OrderDict[i][j]);
                                    lines.Add(firstLine);
                                    ptsTemp.Insert(num, Tuple.Create(closePt, false));
                                }
                            }
                        }
                    }
                    for (int i = 1; i < ptsTemp.Count; i++)
                    {
                        lines.Add(new Line(ptsTemp[i - 1].Item1, ptsTemp[i].Item1));
                    }
                }

                if (row.OrderDict.ContainsKey(-2))
                {
                    for (int j = 1; j < row.OrderDict[-2].Count; j++)
                    {
                        var line = new Line(row.OrderDict[-2][j - 1], row.OrderDict[-2][j]);
                        if (j > 1)
                        {
                            var removeLine = lines.Where(l => l.EndPoint == line.StartPoint).FirstOrDefault();
                            if (removeLine != null)
                            {
                                lines.Remove(removeLine);
                                lines.Add(new Line(removeLine.StartPoint, line.EndPoint));
                            }
                            lines.Add(line);
                        }
                        else
                        {
                            // 直线上距离点位最近的点
                            var closePt = line.GetClosestPointTo(ptsTemp[ptsTemp.Count - 1].Item1, true);
                            if (ptsTemp.Count == 1)
                            {
                                if (closePt.DistanceTo(line.StartPoint) > connTolerance && closePt.DistanceTo(line.EndPoint) > connTolerance)
                                {
                                    var closePtTemp = line.GetClosestPointTo(ptsTemp[ptsTemp.Count - 1].Item1, false);
                                    lines.Add(new Line(ptsTemp[ptsTemp.Count - 1].Item1, closePt));
                                    if (closePt.DistanceTo(closePtTemp) > 10.0)
                                    {
                                        lines.Add(line);
                                        if (row.OrderDict[-2][j - 1].DistanceTo(closePt) + 1.0 < row.OrderDict[-2][j].DistanceTo(closePt))
                                        {
                                            lines.Add(new Line(closePt, row.OrderDict[-2][j - 1]));
                                        }
                                        else
                                        {
                                            lines.Add(new Line(closePt, row.OrderDict[-2][j]));
                                        }
                                    }
                                    else
                                    {
                                        lines.Add(new Line(closePt, row.OrderDict[-2][j - 1]));
                                        lines.Add(new Line(closePt, row.OrderDict[-2][j]));
                                    }
                                }
                                else
                                {
                                    var closePtTemp = GetCenterPoint(line);
                                    var extendPt = ptsTemp[ptsTemp.Count - 1].Item1 + (closePtTemp - closePt);
                                    lines.Add(new Line(ptsTemp[ptsTemp.Count - 1].Item1, extendPt));
                                    lines.Add(new Line(extendPt, closePtTemp));
                                    lines.Add(new Line(closePtTemp, row.OrderDict[-2][j - 1]));
                                    lines.Add(new Line(closePtTemp, row.OrderDict[-2][j]));
                                }
                                continue;
                            }
                            var scrLine = new Line(ptsTemp[ptsTemp.Count - 2].Item1, ptsTemp[ptsTemp.Count - 1].Item1);
                            // 标准L字型连线
                            if (ptsTemp[ptsTemp.Count - 1].Item2
                                && Math.Abs(line.LineDirection().DotProduct(scrLine.LineDirection())) < 0.01
                                && closePt.DistanceTo(ptsTemp[ptsTemp.Count - 1].Item1) < connTolerance)
                            {
                                var closePtTemp = ptsTemp[ptsTemp.Count - 1].Item1
                                    + (ptsTemp[ptsTemp.Count - 2].Item1 - ptsTemp[ptsTemp.Count - 1].Item1).GetNormal() * connTolerance;
                                lines.RemoveAll(l => l.StartPoint == ptsTemp[ptsTemp.Count - 2].Item1 && l.EndPoint == ptsTemp[ptsTemp.Count - 1].Item1);
                                lines.Add(new Line(ptsTemp[ptsTemp.Count - 2].Item1, closePtTemp));
                                lines.Add(new Line(closePtTemp, ptsTemp[ptsTemp.Count - 1].Item1));
                                lines.Add(new Line(closePtTemp, closePtTemp + (GetCenterPoint(line) - closePt)));
                                lines.Add(new Line(closePtTemp + (GetCenterPoint(line) - closePt), GetCenterPoint(line)));
                                lines.Add(new Line(GetCenterPoint(line), row.OrderDict[-2][j - 1]));
                                lines.Add(new Line(GetCenterPoint(line), row.OrderDict[-2][j]));
                            }
                            else if(ptsTemp[ptsTemp.Count - 1].Item2
                                && Math.Abs(line.LineDirection().DotProduct(scrLine.LineDirection())) < 0.01
                                && closePt.DistanceTo(ptsTemp[ptsTemp.Count - 2].Item1) > scrLine.Length
                                && line.GetDistToPoint(closePt, false) > connTolerance / 2)
                            {
                                lines.Add(new Line(ptsTemp[ptsTemp.Count - 1].Item1, closePt));
                                lines.Add(line);
                                if (closePt.DistanceTo(row.OrderDict[-2][j - 1]) < closePt.DistanceTo(row.OrderDict[-2][j]))
                                {
                                    lines.Add(new Line(closePt, row.OrderDict[-2][j - 1]));
                                }
                                else
                                {
                                    lines.Add(new Line(row.OrderDict[-2][j], closePt));
                                }
                            }
                            else if(ptsTemp[ptsTemp.Count - 1].Item2
                                && Math.Abs(line.LineDirection().DotProduct(scrLine.LineDirection())) < 0.01
                                && closePt.DistanceTo(ptsTemp[ptsTemp.Count - 2].Item1) > scrLine.Length
                                && closePt.DistanceTo(row.OrderDict[-2][j - 1]) > connTolerance / 2
                                && closePt.DistanceTo(row.OrderDict[-2][j]) > connTolerance / 2)
                            {
                                lines.Add(new Line(ptsTemp[ptsTemp.Count - 1].Item1, closePt));
                                lines.Add(new Line(closePt, row.OrderDict[-2][j - 1]));
                                lines.Add(new Line(closePt, row.OrderDict[-2][j]));
                            }
                            else if (ptsTemp[ptsTemp.Count - 1].Item2
                                && Math.Abs(line.LineDirection().DotProduct(scrLine.LineDirection())) < 0.01)
                            {
                                lines.RemoveAll(l => l.StartPoint == ptsTemp[ptsTemp.Count - 2].Item1 && l.EndPoint == ptsTemp[ptsTemp.Count - 1].Item1);
                                lines.Add(new Line(ptsTemp[ptsTemp.Count - 2].Item1, closePt));
                                lines.Add(new Line(closePt, ptsTemp[ptsTemp.Count - 1].Item1));
                                lines.Add(line);
                                if(closePt.DistanceTo(row.OrderDict[-2][j - 1]) < closePt.DistanceTo(row.OrderDict[-2][j]))
                                {
                                    lines.Add(new Line(closePt, row.OrderDict[-2][j - 1]));
                                }
                                else
                                {
                                    lines.Add(new Line(row.OrderDict[-2][j], closePt));
                                }
                            }
                            else if(ptsTemp[ptsTemp.Count - 1].Item2 
                                && Math.Abs(line.LineDirection().DotProduct(scrLine.LineDirection())) > 0.998)
                            {
                                var goingOn = true;
                                var corssLine = new Line(ptsTemp[ptsTemp.Count - 1].Item1, closePt);
                                if (!row.IsSmallRoom)
                                {
                                    var filter = wallIndex.SelectCrossingPolygon(corssLine.Buffer(1.0));
                                    if (filter.Count == 0)
                                    {
                                        goingOn = false;
                                    }
                                }
                                else
                                {
                                    var filter = obstacleIndex.SelectCrossingPolygon(corssLine.Buffer(1.0));
                                    if (filter.Count == 0)
                                    {
                                        goingOn = false;
                                    }
                                }
                                // 两线平行且中心连线
                                if (line.DistanceTo(closePt, false) < connTolerance)
                                {
                                    if (closePt.DistanceTo(row.OrderDict[-2][j - 1]) > connTolerance
                                        && closePt.DistanceTo(row.OrderDict[-2][j]) > connTolerance)
                                    {
                                        if (!goingOn)
                                        {
                                            lines.Add(corssLine);
                                            lines.Add(new Line(closePt, row.OrderDict[-2][j - 1]));
                                            lines.Add(new Line(closePt, row.OrderDict[-2][j]));
                                        }
                                    }
                                    if (goingOn)
                                    {
                                        var closePtTemp = new Point3d();
                                        var ptOnScrLine = new Point3d();
                                        for (int exp = 1; exp < 3 && goingOn; exp++)
                                        {
                                            for (int num = 1; num < Math.Pow(2, exp) && goingOn; num++)
                                            {
                                                closePtTemp = row.OrderDict[-2][j - 1] + num / Math.Pow(2, exp) * line.Length * line.LineDirection();
                                                ptOnScrLine = closePtTemp + (ptsTemp[ptsTemp.Count - 1].Item1 - closePt);
                                                corssLine = new Line(closePtTemp, ptOnScrLine);
                                                if (!row.IsSmallRoom)
                                                {
                                                    var filter = wallIndex.SelectCrossingPolygon(corssLine.Buffer(1.0));
                                                    if (filter.Count == 0)
                                                    {
                                                        goingOn = false;
                                                    }
                                                }
                                                else
                                                {
                                                    var filter = obstacleIndex.SelectCrossingPolygon(corssLine.Buffer(1.0));
                                                    if (filter.Count == 0)
                                                    {
                                                        goingOn = false;
                                                    }
                                                }
                                            }
                                        }
                                        lines.RemoveAll(l => l.StartPoint == ptsTemp[ptsTemp.Count - 2].Item1 && l.EndPoint == ptsTemp[ptsTemp.Count - 1].Item1);
                                        lines.Add(new Line(ptsTemp[ptsTemp.Count - 2].Item1, ptOnScrLine));
                                        lines.Add(new Line(ptOnScrLine, ptsTemp[ptsTemp.Count - 1].Item1));
                                        lines.Add(new Line(ptOnScrLine, closePtTemp));
                                        lines.Add(new Line(closePtTemp, row.OrderDict[-2][j - 1]));
                                        lines.Add(new Line(closePtTemp, row.OrderDict[-2][j]));
                                    }
                                }
                                else
                                {
                                    if (!goingOn)
                                    {
                                        var ptOnScrLine = ptsTemp[ptsTemp.Count - 2].Item1 + scrLine.LineDirection() * scrLine.Length / 2;
                                        var closePtTemp = closePt - scrLine.LineDirection() * scrLine.Length / 2;
                                        lines.RemoveAll(l => l.StartPoint == ptsTemp[ptsTemp.Count - 2].Item1 && l.EndPoint == ptsTemp[ptsTemp.Count - 1].Item1);
                                        lines.Add(new Line(ptsTemp[ptsTemp.Count - 2].Item1, ptOnScrLine));
                                        lines.Add(new Line(ptOnScrLine, ptsTemp[ptsTemp.Count - 1].Item1));
                                        lines.Add(new Line(ptOnScrLine, closePtTemp));
                                        if (closePtTemp.DistanceTo(row.OrderDict[-2][j - 1]) < closePtTemp.DistanceTo(row.OrderDict[-2][j]))
                                        {
                                            lines.Add(new Line(closePtTemp, row.OrderDict[-2][j - 1]));
                                        }
                                        else
                                        {
                                            lines.Add(new Line(closePtTemp, row.OrderDict[-2][j]));
                                        }
                                        lines.Add(line);
                                    }
                                    if (goingOn)
                                    {
                                        var closePtTemp = new Point3d();
                                        var ptOnScrLine = new Point3d();
                                        var scrPtTemp = ptsTemp[ptsTemp.Count - 1].Item1 + scrLine.LineDirection() * connTolerance;
                                        for (int exp = 1; exp < 3 && goingOn; exp++)
                                        {
                                            for (int num = 1; num < Math.Pow(2, exp) && goingOn; num++)
                                            {
                                                closePtTemp = row.OrderDict[-2][j - 1] + num / Math.Pow(2, exp) * line.Length * line.LineDirection();
                                                ptOnScrLine = closePtTemp + (scrPtTemp - closePt);
                                                corssLine = new Line(closePtTemp, ptOnScrLine);
                                                if (!row.IsSmallRoom)
                                                {
                                                    var filter = wallIndex.SelectCrossingPolygon(corssLine.Buffer(1.0));
                                                    if (filter.Count == 0)
                                                    {
                                                        goingOn = false;
                                                    }
                                                }
                                                else
                                                {
                                                    var filter = obstacleIndex.SelectCrossingPolygon(corssLine.Buffer(1.0));
                                                    if (filter.Count == 0)
                                                    {
                                                        goingOn = false;
                                                    }
                                                }
                                            }
                                        }
                                        lines.Add(new Line(ptsTemp[ptsTemp.Count - 1].Item1, scrPtTemp));
                                        lines.Add(new Line(scrPtTemp, ptOnScrLine));
                                        lines.Add(new Line(ptOnScrLine, closePtTemp));
                                        lines.Add(new Line(closePtTemp, row.OrderDict[-2][j - 1]));
                                        lines.Add(new Line(closePtTemp, row.OrderDict[-2][j]));
                                    }
                                }
                                
                            }
                            else
                            {
                                if (line.GetDistToPoint(closePt, false) < 1.0)
                                {
                                    var closePtTemp = new Point3d();
                                    var ptOnScrLine = new Point3d();
                                    var goingOn = true;
                                    for (int exp = 1; exp < 3 && goingOn; exp++)
                                    {
                                        for (int num = 1; num < Math.Pow(2, exp) && goingOn; num++)
                                        {
                                            closePtTemp = row.OrderDict[-2][j - 1] + num / Math.Pow(2, exp) * line.Length * line.LineDirection();
                                            ptOnScrLine = closePtTemp + (ptsTemp[ptsTemp.Count - 1].Item1 - closePt);
                                            if (ptsTemp[ptsTemp.Count - 1].Item2)
                                            {
                                                if (ptOnScrLine.DistanceTo(ptsTemp[ptsTemp.Count - 1].Item1) < connTolerance)
                                                {
                                                    continue;
                                                }
                                            }
                                            var corssLine = new Line(closePtTemp, ptOnScrLine);
                                            if (!row.IsSmallRoom)
                                            {
                                                var filter = wallIndex.SelectCrossingPolygon(corssLine.Buffer(1.0));
                                                if (filter.Count == 0)
                                                {
                                                    goingOn = false;
                                                }
                                            }
                                            else
                                            {
                                                var filter = obstacleIndex.SelectCrossingPolygon(corssLine.Buffer(1.0));
                                                if (filter.Count == 0)
                                                {
                                                    goingOn = false;
                                                }
                                            }
                                        }
                                    }
                                    lines.RemoveAll(l => l.StartPoint == ptsTemp[ptsTemp.Count - 2].Item1 && l.EndPoint == ptsTemp[ptsTemp.Count - 1].Item1);
                                    lines.Add(new Line(closePtTemp, row.OrderDict[-2][j - 1]));
                                    lines.Add(new Line(closePtTemp, row.OrderDict[-2][j]));
                                    lines.Add(new Line(closePtTemp, ptOnScrLine));
                                    lines.Add(new Line(ptsTemp[ptsTemp.Count - 2].Item1, ptOnScrLine));
                                    lines.Add(new Line(ptOnScrLine, ptsTemp[ptsTemp.Count - 1].Item1));
                                }
                                else
                                {
                                    if (line.GetDistToPoint(closePt, false) > connTolerance && !ptsTemp[ptsTemp.Count - 1].Item2)
                                    {
                                        lines.Add(new Line(ptsTemp[ptsTemp.Count - 1].Item1, closePt));
                                        lines.Add(line);
                                        if (closePt.DistanceTo(row.OrderDict[-2][j - 1]) < closePt.DistanceTo(row.OrderDict[-2][j]))
                                        {
                                            lines.Add(new Line(row.OrderDict[-2][j - 1], closePt));
                                        }
                                        else
                                        {
                                            lines.Add(new Line(row.OrderDict[-2][j], closePt));
                                        }
                                    }
                                    else
                                    {
                                        var extendDirection = true;
                                        if (closePt.DistanceTo(row.OrderDict[-2][j - 1]) < closePt.DistanceTo(row.OrderDict[-2][j]))
                                        {
                                            extendDirection = false;
                                        }
                                        var closePtTemp = new Point3d();
                                        var ptOnScrLine = new Point3d();
                                        var crossLine = new Line();
                                        var extendLine = new Vector3d();
                                        var goingOn = true;
                                        for (int coefficient = 1; coefficient < 5 && goingOn; coefficient++)
                                        {
                                            if (extendDirection)
                                            {
                                                extendLine = line.LineDirection() * connTolerance * coefficient;
                                            }
                                            else
                                            {
                                                extendLine = -line.LineDirection() * connTolerance * coefficient;
                                            }
                                            closePtTemp = closePt + extendLine;
                                            ptOnScrLine = ptsTemp[ptsTemp.Count - 1].Item1 + extendLine;
                                            crossLine = new Line(closePtTemp, ptOnScrLine);
                                            if (!row.IsSmallRoom)
                                            {
                                                var filter = wallIndex.SelectCrossingPolygon(crossLine.Buffer(1.0));
                                                if (filter.Count == 0)
                                                {
                                                    goingOn = false;
                                                }
                                            }
                                            else
                                            {
                                                var filter = obstacleIndex.SelectCrossingPolygon(crossLine.Buffer(1.0));
                                                if (filter.Count == 0)
                                                {
                                                    goingOn = false;
                                                }
                                            }
                                        }
                                        if (goingOn)
                                        {
                                            continue;
                                        }
                                        lines.RemoveAll(l => l.StartPoint == ptsTemp[ptsTemp.Count - 2].Item1 && l.EndPoint == ptsTemp[ptsTemp.Count - 1].Item1);
                                        lines.Add(new Line(ptsTemp[ptsTemp.Count - 2].Item1, ptOnScrLine));
                                        lines.Add(new Line(ptOnScrLine, ptsTemp[ptsTemp.Count - 1].Item1));
                                        lines.Add(crossLine);
                                        lines.Add(line);

                                        if (extendDirection)
                                        {
                                            lines.Add(new Line(closePtTemp, row.OrderDict[-2][j]));
                                        }
                                        else
                                        {
                                            lines.Add(new Line(closePtTemp, row.OrderDict[-2][j - 1]));
                                        }
                                    }


                                }
                            }
                        }
                    }
                }
            });
            Present(lines);
        }

        private static List<ThSprinklerRowConnect> RowSeparation(List<ThSprinklerRowConnect> connection, bool isVertical = true)
        {
            var results = new List<ThSprinklerRowConnect>();
            connection.ForEach(o =>
            {
                if (!o.OrderDict.ContainsKey(-1))
                {
                    var row = new Dictionary<int, List<Point3d>>();
                    var num = 0;
                    for (int i = 0; i < o.OrderDict.Count; i++)
                    {
                        var ptList = new List<Point3d>();
                        ptList.Add(o.OrderDict[i][0]);
                        num++;
                        if (num <= 9)
                        {
                            row.Add(num - 1, ptList);
                        }
                        else
                        {
                            SprinklerSearched.Remove(o.OrderDict[i][0]);
                        }
                    }

                    for (int i = o.OrderDict.Count - 1; i >= 0; i--)
                    {
                        for (int n = 1; n < o.OrderDict[i].Count; n++)
                        {
                            if (num >= 9)
                            {
                                SprinklerSearched.Remove(o.OrderDict[i][n]);
                            }
                            else
                            {
                                row[i].Add(o.OrderDict[i][n]);
                                num++;
                            }
                        }
                    }

                    var rowConn = new ThSprinklerRowConnect
                    {
                        OrderDict = row,
                        Count = num - 1,
                        IsStallArea = o.IsStallArea,
                        StartPoint = o.OrderDict[0][0],
                        EndPoint = o.OrderDict[o.OrderDict.Count - 1][0],
                    };
                    results.Add(rowConn);
                }
                else
                {
                    var index = 1;
                    var num = 0;
                    for (int i = 1; i < o.OrderDict.Count - 1; i++)
                    {
                        num += o.OrderDict[i].Count;
                        if (num > o.Count / 2)
                        {
                            index = i;
                            break;
                        }
                    }
                    //if (o.Count <= 2 || LaneLine.Count == 0)
                    //{
                    //    o.OrderDict = OrderChange(o.OrderDict, true);
                    //    index = 1;
                    //}
                    if (o.IsStallArea && isVertical)
                    {
                        var newLine = new Line(o.OrderDict[-1][0], o.OrderDict[o.OrderDict.Count - 2][0]);
                        if (o.Count <= 8 && IsIntersection(newLine, LaneLine) && newLine.Length > DTTol)
                        {
                            o.OrderDict = OrderChange(o.OrderDict, true);
                            index = 1;
                        }
                        else
                        {
                            var newNum = 0;
                            var minDelta = o.Count;
                            for (int i = 1; i < o.OrderDict.Count - 1; i++)
                            {
                                var edge = new Line(o.OrderDict[i - 1][0], o.OrderDict[i][0]);
                                if (IsIntersection(edge, LaneLine))
                                {
                                    var delta = Math.Abs(o.Count - 2 * newNum);
                                    if (minDelta > delta)
                                    {
                                        index = i;
                                        minDelta = delta;
                                    }
                                }
                                newNum += o.OrderDict[i].Count;
                            }
                        }
                    }

                    var first = new Dictionary<int, List<Point3d>>
                    {
                        { 0, o.OrderDict[0] },
                    };
                    var firstNum = 1;
                    var second = new Dictionary<int, List<Point3d>>
                    {
                        {0, o.OrderDict[-1] },
                    };
                    var secondNum = 1;

                    for (int j = 1; j < index; j++)
                    {
                        var ptList = new List<Point3d>();
                        ptList.Add(o.OrderDict[j][0]);
                        firstNum++;
                        if (firstNum <= 9)
                        {
                            first.Add(firstNum - 1, ptList);
                        }
                        else
                        {
                            SprinklerSearched.Remove(o.OrderDict[j][0]);
                        }
                    }

                    for (int j = index - 1; j >= 1; j--)
                    {
                        for (int n = 1; n < o.OrderDict[j].Count; n++)
                        {
                            if (firstNum >= 9)
                            {
                                SprinklerSearched.Remove(o.OrderDict[j][n]);
                            }
                            else
                            {
                                first[j].Add(o.OrderDict[j][n]);
                                firstNum++;
                            }
                        }
                    }

                    for (int j = o.OrderDict.Count - 2; j >= index; j--)
                    {
                        var ptList = new List<Point3d>();
                        ptList.Add(o.OrderDict[j][0]);
                        secondNum++;
                        if (secondNum <= 9)
                        {
                            second.Add(secondNum - 1, ptList);
                        }
                        else
                        {
                            SprinklerSearched.Remove(o.OrderDict[j][0]);
                        }
                    }

                    for (int j = index; j < o.OrderDict.Count - 1; j++)
                    {
                        for (int n = 1; n < o.OrderDict[j].Count; n++)
                        {
                            if (secondNum >= 9)
                            {
                                SprinklerSearched.Remove(o.OrderDict[j][n]);
                            }
                            else
                            {
                                second[o.OrderDict.Count - 1 - j].Add(o.OrderDict[j][n]);
                                secondNum++;
                            }
                        }
                    }

                    if (first.Count > 1)
                    {
                        var rowConn = new ThSprinklerRowConnect
                        {
                            OrderDict = first,
                            Count = firstNum - 1,
                            IsStallArea = o.IsStallArea,
                            StartPoint = first[0][0],
                            EndPoint = first[first.Count - 1][0],
                        };
                        results.Add(rowConn);
                    }
                    if (second.Count > 1)
                    {
                        var rowConn = new ThSprinklerRowConnect
                        {
                            OrderDict = second,
                            Count = secondNum - 1,
                            IsStallArea = o.IsStallArea,
                            StartPoint = second[0][0],
                            EndPoint = second[second.Count - 1][0],
                        };

                        results.Add(rowConn);
                    }
                }
            });

            return results;
        }

        private static List<ThSprinklerRowConnect> GraphPtsConnect(ThSprinklerNetGroup net, int graphIdx, List<Point3d> pipeScatters, bool isVertical = true)
        {
            // 给定索引所对应的图
            var graph = net.ptsGraph[graphIdx];
            // 虚拟点
            var virtualPts = net.GetVirtualPts(graphIdx).OrderBy(pt => pt.X).ThenBy(pt => pt.Y).ToList();
            // 虚拟点在pts中的索引集
            var virtualPtsIndex = net.GetVirtualPtsIndex(graphIdx);
            // 图中的所有点位，包括虚拟点
            var graphPts = net.GetGraphPts(graphIdx);
            // 图中的所有喷淋点位
            var realPts = graphPts.Where(pt => !virtualPts.Contains(pt)).ToList();

            // 返回图中的连接关系
            var connection = new List<Tuple<List<ThSprinklerRowConnect>, List<Point3d>, List<Point3d>>>();

            // 计算主方向（车道方向）
            if (graphPts.Count < 3)
            {
                return new List<ThSprinklerRowConnect>();
            }
            Vector3d mainDirction;
            if (LaneLine.Count > 0)
            {
                mainDirction = MainDirction(GetConvexHull(graphPts), LaneLine, 2 * DTTol);
                if (!isVertical)
                {
                    mainDirction = GetVerticalDirction(mainDirction);
                }
            }
            else
            {
                mainDirction = MainDirction(GetConvexHull(graphPts), LaneLine, 10.0);
            }

            for (int time = 0; time < 2; time++)
            {
                var sprinklerSearched = new List<Point3d>();
                var connectionTemp = new List<ThSprinklerRowConnect>();
                var pipeScattersTemp = new List<Point3d>();

                // 已检索的虚拟点位
                var virtualPtsSearched = new List<int>();
                // 沿主方向检索
                for (int i = 0; i < virtualPtsIndex.Count; i++)
                {
                    // 找出图中的虚拟点对应的节点索引
                    var idx = graph.SearchNodeIndex(virtualPtsIndex[i]);
                    if (idx != -1)
                    {
                        // 虚拟点对应的节点
                        var virtualNode = graph.SprinklerVertexNodeList[idx];
                        var edgeNode = virtualNode.FirstEdge;
                        while (edgeNode != null)
                        {
                            if (SprinklerSearched.Contains(net.pts[graph.SprinklerVertexNodeList[edgeNode.EdgeIndex].NodeIndex])
                                || sprinklerSearched.Contains(net.pts[graph.SprinklerVertexNodeList[edgeNode.EdgeIndex].NodeIndex])
                                || pipeScatters.Contains(net.pts[graph.SprinklerVertexNodeList[edgeNode.EdgeIndex].NodeIndex])
                                || pipeScattersTemp.Contains(net.pts[graph.SprinklerVertexNodeList[edgeNode.EdgeIndex].NodeIndex]))
                            {
                                edgeNode = edgeNode.Next;
                                continue;
                            }

                            // 图中虚拟点所在的线段
                            var edge = new Line(net.pts[virtualNode.NodeIndex],
                                                net.pts[graph.SprinklerVertexNodeList[edgeNode.EdgeIndex].NodeIndex]);
                            if (!IsOrthogonal(edge))
                            {
                                edgeNode = edgeNode.Next;
                                continue;
                            }

                            var dirction = edge.LineDirection();
                            // 判断是否需要往该方向延伸
                            if (ContinueConnect(mainDirction, dirction))
                            {
                                // 单次循环内已检索的喷淋点位
                                var realPtsSearchedTemp = new List<Point3d>();

                                // 点位顺序
                                realPtsSearchedTemp.Add(net.pts[graph.SprinklerVertexNodeList[edgeNode.EdgeIndex].NodeIndex]);
                                // 记录点位及其对应的顺序
                                var order = 0;
                                var virtualPt = net.pts[virtualNode.NodeIndex];
                                var rowConnect = new ThSprinklerRowConnect();
                                rowConnect.OrderDict.Add(order++, new List<Point3d> { virtualPt });
                                rowConnect.OrderDict.Add(order++, new List<Point3d> { net.pts[graph.SprinklerVertexNodeList[edgeNode.EdgeIndex].NodeIndex] });
                                rowConnect.Count++;
                                rowConnect.StartPoint = virtualPt;
                                rowConnect.EndPoint = net.pts[graph.SprinklerVertexNodeList[edgeNode.EdgeIndex].NodeIndex];
                                if (LaneLine.Count > 0 && GetCloseLaneLine(edge, LaneLine).Item1 < 5000.0)
                                {
                                    rowConnect.IsStallArea = true;
                                }
                                else
                                {
                                    rowConnect.IsStallArea = false;
                                }

                                var edgeIndex = edgeNode.EdgeIndex;
                                while (KeepSearching(graph, net, edgeIndex, realPts, out var newIdx, realPtsSearchedTemp, sprinklerSearched,
                                    dirction, virtualPts, virtualPt, rowConnect, order))
                                {
                                    order++;
                                    edgeIndex = newIdx;
                                }

                                if (!rowConnect.OrderDict.ContainsKey(-1) && rowConnect.Count > 8)
                                {
                                    for (int m = 9; m <= rowConnect.Count; m++)
                                    {
                                        realPtsSearchedTemp.Remove(rowConnect.OrderDict[m][0]);
                                        rowConnect.OrderDict.Remove(m);
                                    }
                                    rowConnect.Count = 8;
                                    rowConnect.EndPoint = rowConnect.OrderDict[8][0];
                                }
                                virtualPtsSearched.Add(i);
                                if(rowConnect.Count == 1)
                                {
                                    pipeScattersTemp.AddRange(realPtsSearchedTemp);
                                }
                                else
                                {
                                    sprinklerSearched.AddRange(realPtsSearchedTemp);
                                }
                                connectionTemp.Add(rowConnect);
                            }

                            edgeNode = edgeNode.Next;
                        }
                    }
                }

                if ((time == 0 || connectionTemp.Count == 0) && realPts.Count > sprinklerSearched.Count + pipeScattersTemp.Count)
                {
                    // 对剩余点位进行检索
                    for (int i = 0; i < realPts.Count; i++)
                    {
                        if (SprinklerSearched.Contains(realPts[i]) || sprinklerSearched.Contains(realPts[i])
                            || pipeScatters.Contains(realPts[i]) || pipeScattersTemp.Contains(realPts[i]))
                        {
                            continue;
                        }

                        // 找出距离点位较近的支干管虚拟点
                        var virtualPtList = SearchVirtualPt(realPts[i], 2.0 * DTTol);
                        if (virtualPtList.Count == 0)
                        {
                            continue;
                        }

                        virtualPtList.ForEach(virtualPt =>
                        {
                            var edge = new Line(virtualPt, realPts[i]);
                            if (VaildLine(SprinklerParameter.SprinklerPt, Geometry, edge))
                            {
                                return;
                            }

                            var dirction = edge.LineDirection();
                            if (ContinueConnect(mainDirction, dirction))
                            {
                                // 单次循环内已检索的喷淋点位
                                var realPtsSearchedTemp = new List<Point3d>();
                                // 点位顺序
                                realPtsSearchedTemp.Add(realPts[i]);
                                // 记录点位及其对应的顺序
                                var order = 0;
                                var rowConnect = new ThSprinklerRowConnect();
                                rowConnect.OrderDict.Add(order++, new List<Point3d> { virtualPt });
                                rowConnect.OrderDict.Add(order++, new List<Point3d> { realPts[i] });
                                rowConnect.StartPoint = virtualPt;
                                rowConnect.EndPoint = realPts[i];
                                rowConnect.Count++;
                                if (LaneLine.Count > 0 && GetCloseLaneLine(edge, LaneLine).Item1 < 5000.0)
                                {
                                    rowConnect.IsStallArea = true;
                                }
                                else
                                {
                                    rowConnect.IsStallArea = false;
                                }

                                var edgeIndex = graph.SearchNodeIndex(net.pts.IndexOf(realPts[i]));
                                while (KeepSearching(graph, net, edgeIndex, graphPts, out var newIdx, realPtsSearchedTemp, sprinklerSearched,
                                        dirction, virtualPts, virtualPt, rowConnect, order))
                                {
                                    order++;
                                    edgeIndex = newIdx;
                                }

                                if (!rowConnect.OrderDict.ContainsKey(-1) && rowConnect.Count > 8)
                                {
                                    for (int m = 9; m < rowConnect.Count; m++)
                                    {
                                        realPtsSearchedTemp.Remove(rowConnect.OrderDict[m][0]);
                                        rowConnect.OrderDict.Remove(m);
                                    }
                                    rowConnect.Count = 8;
                                    rowConnect.EndPoint = rowConnect.OrderDict[8][0];
                                }
                                if (rowConnect.Count > 1)
                                {
                                    sprinklerSearched.AddRange(realPtsSearchedTemp);
                                    connectionTemp.Add(rowConnect);
                                }
                            }
                        });
                    }
                }

                var secChecked = false;
                // 沿次方向检索
                if((sprinklerSearched.Count + pipeScattersTemp.Count) / (realPts.Count * 1.0) < 0.95 
                    || realPts.Count - sprinklerSearched.Count - pipeScattersTemp.Count > 3)
                {
                    var connectionTempClone = connectionTemp.Select(row => row.Clone() as ThSprinklerRowConnect).ToList();
                    var sprinklerSearchedClone = new List<Point3d>();
                    sprinklerSearched.ForEach(pt => sprinklerSearchedClone.Add(pt));
                    for (int cycle = 0;cycle<2;cycle++)
                    {
                        for (int i = 0; i < virtualPtsIndex.Count; i++)
                        {
                            if (virtualPtsSearched.Contains(i))
                            {
                                continue;
                            }

                            // 找出图中的虚拟点对应的节点索引
                            var idx = graph.SearchNodeIndex(virtualPtsIndex[i]);
                            if (idx != -1)
                            {
                                // 虚拟点对应的节点
                                var virtualNode = graph.SprinklerVertexNodeList[idx];
                                var edgeNode = virtualNode.FirstEdge;
                                while (edgeNode != null)
                                {
                                    // 图中虚拟点所在的线段
                                    var edge = new Line(net.pts[virtualNode.NodeIndex],
                                                        net.pts[graph.SprinklerVertexNodeList[edgeNode.EdgeIndex].NodeIndex]);
                                    if (!IsOrthogonal(edge))
                                    {
                                        edgeNode = edgeNode.Next;
                                        continue;
                                    }

                                    var hasScatter = false;
                                    IsNoisePoint(sprinklerSearchedClone, realPts, net.pts[graph.SprinklerVertexNodeList[edgeNode.EdgeIndex].NodeIndex], ref hasScatter);

                                    var rowConnect = new ThSprinklerRowConnect();
                                    if (LaneLine.Count > 0 && GetCloseLaneLine(edge, LaneLine).Item1 < 5000.0)
                                    {
                                        rowConnect.IsStallArea = true;
                                    }
                                    else
                                    {
                                        rowConnect.IsStallArea = false;
                                    }

                                    var dirction = edge.LineDirection();
                                    // 单次循环内已检索的喷淋点位
                                    var realPtsSearchedTemp = new List<Point3d>();
                                    // 点位顺序
                                    realPtsSearchedTemp.Add(net.pts[graph.SprinklerVertexNodeList[edgeNode.EdgeIndex].NodeIndex]);
                                    // 记录点位及其对应的顺序
                                    var order = 0;
                                    var virtualPt = net.pts[virtualNode.NodeIndex];
                                    rowConnect.OrderDict.Add(order++, new List<Point3d> { virtualPt });
                                    rowConnect.OrderDict.Add(order++, new List<Point3d> { net.pts[graph.SprinklerVertexNodeList[edgeNode.EdgeIndex].NodeIndex] });
                                    rowConnect.Count++;
                                    rowConnect.StartPoint = net.pts[virtualNode.NodeIndex];
                                    rowConnect.EndPoint = net.pts[graph.SprinklerVertexNodeList[edgeNode.EdgeIndex].NodeIndex];

                                    var edgeIndex = edgeNode.EdgeIndex;
                                    while (KeepSearching(graph, net, edgeIndex, realPts, out var newIdx, ref hasScatter, realPtsSearchedTemp,
                                        sprinklerSearched, dirction, virtualPts, virtualPt, rowConnect, order))
                                    {
                                        order++;
                                        edgeIndex = newIdx;
                                    }

                                    if (hasScatter && rowConnect.Count > 1)
                                    {
                                        virtualPtsSearched.Add(i);
                                        HandleSecondRow(connectionTempClone, rowConnect, sprinklerSearchedClone, realPtsSearchedTemp);
                                    }

                                    edgeNode = edgeNode.Next;
                                }
                            }
                        }
                    }

                    if(sprinklerSearchedClone.Count > sprinklerSearched.Count)
                    {
                        sprinklerSearched = sprinklerSearchedClone;
                        connectionTemp = connectionTempClone;
                        secChecked = true;
                    }
                }

                // 再次检索
                if (secChecked 
                    && (sprinklerSearched.Count + pipeScattersTemp.Count) / (realPts.Count * 1.0) < 0.95
                    || realPts.Count - sprinklerSearched.Count - pipeScattersTemp.Count > 3)
                {
                    var connectionTempClone = connectionTemp.Select(row => row.Clone() as ThSprinklerRowConnect).ToList();
                    var sprinklerSearchedClone = new List<Point3d>();
                    sprinklerSearched.ForEach(pt => sprinklerSearchedClone.Add(pt));
                    for (int cycle = 0; cycle < 2; cycle++)
                    {
                        for (int i = 0; i < virtualPtsIndex.Count; i++)
                        {
                            
                                // 找出图中的虚拟点对应的节点索引
                                var idx = graph.SearchNodeIndex(virtualPtsIndex[i]);
                            if (idx != -1)
                            {
                                // 虚拟点对应的节点
                                var virtualNode = graph.SprinklerVertexNodeList[idx];
                                var edgeNode = virtualNode.FirstEdge;
                                while (edgeNode != null)
                                {
                                    // 图中虚拟点所在的线段
                                    var edge = new Line(net.pts[virtualNode.NodeIndex],
                                                        net.pts[graph.SprinklerVertexNodeList[edgeNode.EdgeIndex].NodeIndex]);
                                    if (!IsOrthogonal(edge))
                                    {
                                        edgeNode = edgeNode.Next;
                                        continue;
                                    }

                                    var hasScatter = false;
                                    IsNoisePoint(sprinklerSearchedClone, realPts, net.pts[graph.SprinklerVertexNodeList[edgeNode.EdgeIndex].NodeIndex], ref hasScatter);

                                    var rowConnect = new ThSprinklerRowConnect();
                                    if (LaneLine.Count > 0 && GetCloseLaneLine(edge, LaneLine).Item1 < 5000.0)
                                    {
                                        rowConnect.IsStallArea = true;
                                    }
                                    else
                                    {
                                        rowConnect.IsStallArea = false;
                                    }

                                    var dirction = edge.LineDirection();

                                    if (ContinueConnect(mainDirction, dirction))
                                    {
                                        // 单次循环内已检索的喷淋点位
                                        var realPtsSearchedTemp = new List<Point3d>();
                                        // 点位顺序
                                        realPtsSearchedTemp.Add(net.pts[graph.SprinklerVertexNodeList[edgeNode.EdgeIndex].NodeIndex]);
                                        // 记录点位及其对应的顺序
                                        var order = 0;
                                        var virtualPt = net.pts[virtualNode.NodeIndex];
                                        rowConnect.OrderDict.Add(order++, new List<Point3d> { virtualPt });
                                        rowConnect.OrderDict.Add(order++, new List<Point3d> { net.pts[graph.SprinklerVertexNodeList[edgeNode.EdgeIndex].NodeIndex] });
                                        rowConnect.Count++;
                                        rowConnect.StartPoint = net.pts[virtualNode.NodeIndex];
                                        rowConnect.EndPoint = net.pts[graph.SprinklerVertexNodeList[edgeNode.EdgeIndex].NodeIndex];

                                        var edgeIndex = edgeNode.EdgeIndex;
                                        while (KeepSearching(graph, net, edgeIndex, realPts, out var newIdx, ref hasScatter, realPtsSearchedTemp,
                                            sprinklerSearched, dirction, virtualPts, virtualPt, rowConnect, order))
                                        {
                                            order++;
                                            edgeIndex = newIdx;
                                        }

                                        if (hasScatter && rowConnect.Count > 1)
                                        {
                                            virtualPtsSearched.Add(i);
                                            HandleSecondRow(connectionTempClone, rowConnect, sprinklerSearchedClone, realPtsSearchedTemp);
                                        }
                                    }

                                    edgeNode = edgeNode.Next;
                                }
                            }
                        }
                    }

                    if (sprinklerSearchedClone.Count > sprinklerSearched.Count)
                    {
                        sprinklerSearched = sprinklerSearchedClone;
                        connectionTemp = connectionTempClone;
                    }
                }

                if (time == 0 && LaneLine.Count > 0
                    && (sprinklerSearched.Count + pipeScattersTemp.Count) / (realPts.Count * 1.0) > 0.8 
                    && realPts.Count - sprinklerSearched.Count - pipeScattersTemp.Count < 10)
                {
                    SprinklerSearched.AddRange(sprinklerSearched);
                    pipeScatters.AddRange(pipeScattersTemp);
                    return connectionTemp;
                }
                else
                {
                    mainDirction = GetVerticalDirction(mainDirction);
                    var tuple = new Tuple<List<ThSprinklerRowConnect>, List<Point3d>, List<Point3d>>(connectionTemp, sprinklerSearched, pipeScattersTemp);
                    connection.Add(tuple);
                }
            }

            var firstRowCount = 0;
            if (LaneLine.Count > 0)
            {
                connection[0].Item1.ForEach(item =>
                {
                    if (item.OrderDict.ContainsKey(-1))
                    {
                        firstRowCount++;
                    }
                });
            }
            var secondRowCount = 0;
            if (LaneLine.Count > 0)
            {
                connection[1].Item1.ForEach(item =>
                {
                    if (item.OrderDict.ContainsKey(-1))
                    {
                        secondRowCount++;
                    }
                });
            }

            // 比较两次已检索点的数量，多的优先；当已检索点个数相同时，再比较生成支管数量，少的优先
            if (connection[0].Item2.Count > connection[1].Item2.Count
                || (connection[0].Item2.Count== connection[1].Item2.Count
                    && (connection[0].Item1.Count + firstRowCount < connection[1].Item1.Count + secondRowCount
                        || (connection[0].Item1.Count + firstRowCount == connection[1].Item1.Count + secondRowCount
                        && firstRowCount <= secondRowCount))))
            {
                SprinklerSearched.AddRange(connection[0].Item2);
                pipeScatters.AddRange(connection[0].Item3);
                return connection[0].Item1;
            }
            else
            {
                //if (LaneLine.Count > 0)
                //{
                //    return new List<ThSprinklerRowConnect>();
                //}
                //else
                //{
                    
                //}
                SprinklerSearched.AddRange(connection[1].Item2);
                pipeScatters.AddRange(connection[1].Item3);
                return connection[1].Item1;
            }
        }

        /// <summary>
        /// 处理连续散点情形
        /// </summary>
        private static void HandleConsequentScatter(ThSprinklerNetGroup net, int graphIdx,
            List<ThSprinklerRowConnect> rowConnection, List<Point3d> pipeScatters, List<Polyline> smallRooms, List<Polyline> obstacle)
        {
            // 给定索引所对应的图
            var graph = net.ptsGraph[graphIdx];
            // 图中的所有点位，包括虚拟点
            var graphPts = net.GetGraphPts(graphIdx);
            // 虚拟点
            var virtualPts = net.GetVirtualPts(graphIdx).OrderBy(pt => pt.X).ThenBy(pt => pt.Y).ToList();
            // 图中的所有喷淋点位
            var realPts = graphPts.Where(pt => !virtualPts.Contains(pt)).ToList();

            if (realPts.Count == 0)
            {
                return;
            }
            // 判断喷头是否位于小房间中
            var isSprinklerInSmallRoom = IsSprinklerInSmallRoom(realPts[0], smallRooms);

            var rowLines = rowConnection.Select(row => row.Base).ToCollection();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(rowLines);
            var pipeIndex = new ThCADCoreNTSSpatialIndex(SprinklerParameter.SubMainPipe.ToCollection());
            var startPts = rowConnection.Select(row => row.Base.StartPoint).ToList();
            var endPts = rowConnection.Select(row => row.Base.EndPoint).ToList();

            // 已搜索点位
            var seachedPts = new List<Point3d>();

            for (int time = 0; time < 2; time++)
            {
                for (int i = 0; i < realPts.Count; i++)
                {
                    if (SprinklerSearched.Contains(realPts[i]) || seachedPts.Contains(realPts[i]) || pipeScatters.Contains(realPts[i]))
                    {
                        continue;
                    }

                    var edgeIndex = graph.SearchNodeIndex(net.pts.IndexOf(realPts[i]));
                    var vertexNode = graph.SprinklerVertexNodeList[edgeIndex];
                    var ptNext = net.pts[graph.SprinklerVertexNodeList[vertexNode.FirstEdge.EdgeIndex].NodeIndex];

                    var node = vertexNode.FirstEdge;

                    var vaildNode = 0;
                    if (!net.ptsVirtual.Contains(ptNext) && !seachedPts.Contains(ptNext) && !SprinklerSearched.Contains(ptNext))
                    {
                        vaildNode++;
                    }

                    while (node.Next != null)
                    {
                        node = node.Next;
                        var ptNextTemp = net.pts[graph.SprinklerVertexNodeList[node.EdgeIndex].NodeIndex];
                        if (!net.ptsVirtual.Contains(ptNextTemp) && !seachedPts.Contains(ptNextTemp) && !SprinklerSearched.Contains(ptNext))
                        {
                            vaildNode++;
                            ptNext = ptNextTemp;
                        }
                    }

                    if (vaildNode != 1)
                    {
                        continue;
                    }

                    //if (SprinklerSearched.Contains(ptNext))
                    //{
                    //    continue;
                    //}

                    var scatterCount = 0;
                    var pipeScattersTemp = new List<Point3d>();
                    if (pipeScatters.Contains(ptNext))
                    {
                        pipeScattersTemp.Add(ptNext);
                        scatterCount++;
                    }

                    var ptList = new List<Point3d>();
                    ptList.Add(realPts[i]);
                    ptList.Add(ptNext);
                    seachedPts.Add(realPts[i]);
                    seachedPts.Add(ptNext);

                    var edge = new Line(realPts[i], ptNext);
                    var startDirection = edge.LineDirection();
                    var ptNextIndex = node.EdgeIndex;
                    while (KeepSearching(graph, net, startDirection, SprinklerSearched, seachedPts, ptList, ref ptNextIndex,
                        pipeScatters, pipeScattersTemp, ref scatterCount))
                    {
                        edge = new Line(realPts[i], net.pts[graph.SprinklerVertexNodeList[ptNextIndex].NodeIndex]);
                    }

                    if (scatterCount > 1)
                    {
                        continue;
                    }

                    ptList.ForEach(pt => rowConnection.RemoveAll(row => row.Base.EndPoint == pt));
                    pipeScattersTemp.ForEach(pt => pipeScatters.RemoveAll(o => o == pt));

                    var center = new Point3d((realPts[i].X + ptNext.X) / 2, (realPts[i].Y + ptNext.Y) / 2, 0);
                    var filter = spatialIndex.SelectCrossingPolygon(CreateSquare(center, 3 * DTTol));
                    var closeRowLines = new List<Line>();
                    if (filter.Count > 0)
                    {
                        filter.OfType<Line>().ForEach(line =>
                        {
                            closeRowLines.Add(line);
                        });
                        closeRowLines = closeRowLines.OrderBy(line => line.Distance(edge)
                            + edge.GetDistToPoint(line.GetClosestPointTo(edge.StartPoint, false), true)).ToList();
                    }

                    var closePipeLines = new List<Line>();
                    var pipefilter = pipeIndex.SelectCrossingPolygon(CreateSquare(center, 3 * DTTol));
                    if (pipefilter.Count > 0)
                    {
                        pipefilter.OfType<Line>().ForEach(line =>
                        {
                            closePipeLines.Add(line);
                        });
                        closePipeLines = closePipeLines.OrderBy(line => line.Distance(edge)
                            + edge.GetDistToPoint(line.GetClosestPointTo(edge.StartPoint, false), true)).ToList();
                    }

                    if (closeRowLines.Count == 0 || IsPipeInSmallRoom(isSprinklerInSmallRoom, closePipeLines))
                    {
                        // 连接到支干管
                        if (closePipeLines.Count == 0 || !isSprinklerInSmallRoom.Item1)
                        {
                            continue;
                        }
                        else
                        {
                            for (int pipeCount = 0; pipeCount < closePipeLines.Count; pipeCount++)
                            {
                                if (ConnectToPipe(edge, closePipeLines[pipeCount], rowConnection, ptList))
                                {
                                    break;
                                }
                            }
                            continue;
                        }
                    }

                    if (closePipeLines.Count == 0)
                    {
                        // 连接到支管
                        if (closeRowLines.Count == 0)
                        {
                            continue;
                        }
                        else
                        {
                            for (int rowCount = 0; rowCount < closeRowLines.Count; rowCount++)
                            {
                                if (ConnectToRow(isSprinklerInSmallRoom.Item1, edge, closeRowLines[rowCount], obstacle, rowConnection, ptList))
                                {
                                    break;
                                }
                            }
                            continue;
                        }
                    }

                    if (closeRowLines.Count > 0 && closePipeLines.Count > 0)
                    {
                        var closeLines = new List<Line>();
                        closeLines.AddRange(closeRowLines);
                        closeLines.AddRange(closePipeLines);
                        closeLines = closeLines.OrderBy(line => line.Distance(edge)
                            + edge.GetDistToPoint(line.GetClosestPointTo(edge.StartPoint, false), true)).ToList();

                        for (int count = 0; count < closeLines.Count; count++)
                        {
                            if (closeRowLines.Contains(closeLines[count]))
                            {
                                if (ConnectToRow(isSprinklerInSmallRoom.Item1, edge, closeLines[count], obstacle, rowConnection, ptList))
                                {
                                    break;
                                }
                            }
                            else
                            {
                                if (ConnectToPipe(edge, closeLines[count], rowConnection, ptList))
                                {
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void HandleSecondRow(List<ThSprinklerRowConnect> rowConnection, List<ThSprinklerRowConnect> secRowConnection)
        {
            var lines = rowConnection.Select(row => row.Base).ToCollection();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(lines);

            var dict = new Dictionary<Point3d, List<ThSprinklerRowConnect>>();
            secRowConnection.ForEach(row =>
            {
                var filter = spatialIndex.SelectCrossingPolygon(row.Base.ExtendLine(-10.0).Buffer(1.0));
                if (filter.Count == 0)
                {
                    rowConnection.Add(row);
                    row.OrderDict.Values.ForEach(o => SprinklerSearched.AddRange(o));

                    // 更新索引
                    spatialIndex.Update(new DBObjectCollection { row.Base }, new DBObjectCollection());
                }
                else if (filter.Count == 1)
                {
                    var startPoint = filter.OfType<Line>().First().StartPoint;
                    if (dict.ContainsKey(startPoint))
                    {
                        dict[startPoint].Add(row);
                    }
                    else
                    {
                        dict.Add(startPoint, new List<ThSprinklerRowConnect> { row });
                    }
                }
            });

            rowConnection.ForEach(row =>
            {
                if (dict.ContainsKey(row.Base.StartPoint))
                {
                    var secRowCount = dict[row.Base.StartPoint].Select(row => row.Count).Sum();
                    if (secRowCount > row.Count)
                    {
                        rowConnection.Remove(row);
                        row.OrderDict.Values.ForEach(o => o.ForEach(pt => SprinklerSearched.Remove(pt)));
                        dict[row.Base.StartPoint].ForEach(row =>
                        {
                            rowConnection.Add(row);
                            row.OrderDict.Values.ForEach(o => SprinklerSearched.AddRange(o));
                        });
                    }
                }
            });
        }

        /// <summary>
        /// 处理环
        /// </summary>
        /// <param name="rowConnection"></param>
        /// <param name="secRowConnection"></param>
        private static void HandleLoopRow(List<ThSprinklerRowConnect> rowConnection)
        {
            var lines = rowConnection.Select(row => row.Base).ToCollection();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(lines);

            rowConnection.ForEach(row =>
            {
                var filter = spatialIndex.SelectCrossingPolygon(row.Base.ExtendLine(-10.0).Buffer(1.0));
                if (filter.Count > 2)
                {
                    // 更新索引
                    spatialIndex.Update(new DBObjectCollection(), new DBObjectCollection { row.Base });
                    row.OrderDict.Values.ForEach(o => o.ForEach(pt => SprinklerSearched.Remove(pt)));
                    rowConnection.Remove(row);
                }
            });
        }

        /// <summary>
        /// 次方向散点处理
        /// </summary>
        /// <param name="rowConnection"></param>
        /// <param name="secRowConnection"></param>
        private static void HandleSecondRow(List<ThSprinklerRowConnect> connectionTempClone, ThSprinklerRowConnect rowConnect, 
            List<Point3d> sprinklerSearchedClone, List<Point3d> realPtsSearchedTemp)
        {
            var lines = connectionTempClone.Select(row => row.Base).ToCollection();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(lines);
            var frame = rowConnect.Base.ExtendLine(10.0).Buffer(1.0);
            var filter = spatialIndex.SelectCrossingPolygon(frame);
            filter.OfType<Line>().ForEach(line =>
            {
                var filterRow = connectionTempClone.Where(row => row.StartPoint == line.StartPoint && row.EndPoint == line.EndPoint).First();
                if(filterRow.OrderDict.ContainsKey(-1))
                {
                    var i = 1;
                    for(; i< filterRow.OrderDict.Count -1;i++)
                    {
                        if(frame.Contains(filterRow.OrderDict[i][0]))
                        {
                            break;
                        }
                    }
                    if(i == 1)
                    {
                        spatialIndex.Update(new DBObjectCollection(), new DBObjectCollection { line });
                        filterRow.OrderDict.Values.ForEach(values => values.ForEach(pt => sprinklerSearchedClone.Remove(pt)));
                        connectionTempClone.Remove(filterRow);
                    }
                    else
                    {
                        for (int j = filterRow.OrderDict.Count - 2; j >= i; j--)
                        {
                            sprinklerSearchedClone.Remove(filterRow.OrderDict[j][0]);
                            filterRow.OrderDict.Remove(j);
                        }
                        filterRow.OrderDict.Remove(-1);
                        filterRow.Count = i - 1;
                        filterRow.EndPoint = filterRow.OrderDict[i - 1][0];
                        spatialIndex.Update(new DBObjectCollection { filterRow.Base }, new DBObjectCollection { line });
                    }
                }
                else
                {
                    var i = 1;
                    for (; i < filterRow.OrderDict.Count; i++)
                    {
                        if (frame.Contains(filterRow.OrderDict[i][0]))
                        {
                            break;
                        }
                    }
                    if (i == 1)
                    {
                        spatialIndex.Update(new DBObjectCollection(), new DBObjectCollection { line });
                        filterRow.OrderDict.Values.ForEach(values => values.ForEach(pt => sprinklerSearchedClone.Remove(pt)));
                        connectionTempClone.Remove(filterRow);
                    }
                    else
                    {
                        for (int j = filterRow.OrderDict.Count - 1; j >= i; j--)
                        {
                            sprinklerSearchedClone.Remove(filterRow.OrderDict[j][0]);
                            filterRow.OrderDict.Remove(j);
                        }
                        filterRow.Count = i - 1;
                        filterRow.EndPoint = filterRow.OrderDict[i - 1][0];
                        spatialIndex.Update(new DBObjectCollection { filterRow.Base }, new DBObjectCollection { line });
                    }
                }
            });

            connectionTempClone.Add(rowConnect);
            sprinklerSearchedClone.AddRange(realPtsSearchedTemp);
        }

        private static void HandleScatter(List<ThSprinklerRowConnect> rowConnection, List<Point3d> pipeScatters)
        {
            var ptList = SprinklerParameter.SprinklerPt.OrderBy(pt => pt.X).ToList();
            for (int time = 0; time < 2; time++)
            {
                var objs = rowConnection.Select(row => row.Base).ToCollection();
                var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                for (int i = 0; i < ptList.Count; i++)
                {
                    if (SprinklerSearched.Contains(ptList[i]) || pipeScatters.Contains(ptList[i]))
                    {
                        continue;
                    }

                    if (rowConnection.Count > 0)
                    {
                        var filter = spatialIndex.SelectCrossingPolygon(CreateSquare(ptList[i], 2 * DTTol)).OfType<Line>().ToList();
                        if (filter.Count == 0)
                        {
                            continue;
                        }

                        var closeDistToRow = 2 * filter[0].DistanceTo(ptList[i], true) + filter[0].DistanceTo(ptList[i], false);
                        var closeIndex = -1;
                        var startPoints = filter.Select(l => l.StartPoint).ToList();
                        var endPoints = filter.Select(l => l.EndPoint).ToList();
                        for (int n = 0; n < rowConnection.Count; n++)
                        {
                            if (!startPoints.Contains(rowConnection[n].Base.StartPoint) || !endPoints.Contains(rowConnection[n].Base.EndPoint))
                            {
                                continue;
                            }

                            var realClosePt = rowConnection[n].Base.GetClosestPointTo(ptList[i], false);
                            var extendClosePt = rowConnection[n].Base.GetClosestPointTo(ptList[i], true);
                            var realLine = new Line(realClosePt, ptList[i]);

                            // 判断线是否与管线相交
                            if (realLine.Length < 1.0 || IsIntersects(realLine, SprinklerParameter.AllPipe))
                            {
                                continue;
                            }

                            // 判断线是否与墙线相交
                            if (IsLineInWall(realClosePt, extendClosePt, ptList[i], Geometry))
                            {
                                continue;
                            }

                            // 避免多余线添加至列
                            if (time == 1)
                            {
                                if (ThSprinklerNetworkService.SearchClosePt(ptList[i], SprinklerParameter.SubMainPipe, 1.2 * DTTol, out var virtualPtList))
                                {
                                    continue;
                                }
                            }

                            var distance = 2 * rowConnection[n].Base.DistanceTo(ptList[i], true) + rowConnection[n].Base.DistanceTo(ptList[i], false);
                            if (closeDistToRow + 1.0 > distance)
                            {
                                closeDistToRow = distance;
                                closeIndex = n;
                            }
                        }

                        if (closeIndex == -1)
                        {
                            continue;
                        }

                        if (closeDistToRow < 1.8 * DTTol)
                        {
                            var rowPts = rowConnection[closeIndex].OrderDict;
                            if (rowPts.TryGetValue(1, out var first))
                            {
                                var closeDistToPoint = ptList[i].DistanceTo(first[0]);
                                var ptIndex = 1;
                                for (int m = 2; m < rowPts.Count - 1; m++)
                                {
                                    var distance = ptList[i].DistanceTo(rowPts[m][0]);
                                    if (closeDistToPoint > distance)
                                    {
                                        closeDistToPoint = distance;
                                        ptIndex = m;
                                    }
                                }
                                if (rowPts.TryGetValue(rowPts.Count - 1, out var end))
                                {
                                    var distance = ptList[i].DistanceTo(end[0]);
                                    if (closeDistToPoint > distance)
                                    {
                                        closeDistToPoint = distance;
                                        ptIndex = rowPts.Count - 1;
                                    }
                                }

                                rowPts[ptIndex].Add(ptList[i]);
                                SprinklerSearched.Add(ptList[i]);
                                rowConnection[closeIndex].Count++;

                                var newLine = GetLongLine(rowConnection[closeIndex].Base, ptList[i]);
                                spatialIndex.Update(new DBObjectCollection { newLine }, new DBObjectCollection { rowConnection[closeIndex].Base });
                                rowConnection[closeIndex].StartPoint = newLine.StartPoint;
                                rowConnection[closeIndex].EndPoint = newLine.EndPoint;
                            }
                        }
                    }
                }
            }
        }

        private static void ConnScatterToPipe(List<ThSprinklerRowConnect> rowSeparation, List<Point3d> pipeScatters)
        {
            var ptList = SprinklerParameter.SprinklerPt.OrderBy(pt => pt.X).ToList();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(ptList.Select(pt => new DBPoint(pt)).ToCollection());
            for (int i = 0; i < ptList.Count; i++)
            {
                if (SprinklerSearched.Contains(ptList[i]) || pipeScatters.Contains(ptList[i]))
                {
                    continue;
                }

                if (ThSprinklerNetworkService.SearchClosePt(ptList[i], SprinklerParameter.SubMainPipe, 1.2 * DTTol, out var virtualPtList, true))
                {
                    foreach (var closePt in virtualPtList)
                    {
                        var newLine = new Line(closePt, ptList[i]).ExtendLine(-10.0);
                        var filter = spatialIndex.SelectCrossingPolygon(newLine.Buffer(10.0));
                        if (filter.Count == 0 && !IsLineInWall(newLine, Geometry))
                        {
                            var row = new Dictionary<int, List<Point3d>>
                            {
                                {0, new List<Point3d> { closePt } },
                                {1, new List<Point3d>{ ptList[i]} },
                            };

                            var rowConn = new ThSprinklerRowConnect();
                            rowConn.StartPoint = closePt;
                            rowConn.EndPoint = ptList[i];
                            rowConn.OrderDict = row;
                            rowConn.Count++;
                            rowSeparation.Add(rowConn);
                            pipeScatters.Add(ptList[i]);
                            break;
                        }
                    }
                }
            }
        }

        private static List<Line> GetLaneLine(List<Polyline> doubleStall)
        {
            var laneLine = new List<Line>();
            doubleStall.ForEach(o =>
            {
                var pts = o.Vertices();
                laneLine.Add(new Line(pts[0], pts[1]));
                laneLine.Add(new Line(pts[2], pts[3]));
            });
            return laneLine;
        }

        private static bool ContinueConnect(Vector3d laneLine, Vector3d dirction)
        {
            if (Math.Abs(dirction.DotProduct(laneLine)) < 0.01)
            {
                return true;
            }
            return false;
        }

        private static Tuple<double, Line> GetCloseLaneLine(Line line, List<Line> laneLine)
        {
            var newLine = line.ExtendLine(3000.0);
            var closeDistance = laneLine[0].ExtendLine(5000.0).Distance(newLine);
            var closeLine = laneLine[0];
            for (int i = 1; i < laneLine.Count; i++)
            {
                var distance = laneLine[i].ExtendLine(5000.0).Distance(newLine);
                if (closeDistance > distance)
                {
                    closeDistance = distance;
                    closeLine = laneLine[i];
                }
            }
            return new Tuple<double, Line>(closeDistance, closeLine);
        }

        /// <summary>
        /// 判断线与车道线是否正交，正交则返回true
        /// </summary>
        /// <param name="line"></param>
        /// <param name="laneLine"></param>
        /// <returns></returns>
        private static bool IsIntersection(Line line, List<Line> laneLine)
        {
            var closeDistance = laneLine[0].ExtendLine(5000.0).Distance(line);
            var isOrthogonal = IsOrthogonal(line, laneLine[0]);
            for (int i = 1; i < laneLine.Count; i++)
            {
                if (!IsOrthogonal(line, laneLine[i]))
                {
                    continue;
                }
                var distance = laneLine[i].ExtendLine(5000.0).Distance(line);
                isOrthogonal = true;
                if (closeDistance > distance)
                {
                    closeDistance = distance;
                }
            }
            return isOrthogonal && closeDistance < 10.0;
        }

        /// <summary>
        /// 获得垂线
        /// </summary>
        /// <param name="point"></param>
        /// <param name="laneLine"></param>
        /// <returns></returns>
        private static Vector3d GetVerticalDirction(Vector3d dirction)
        {
            if (dirction.X != 0)
            {
                return new Vector3d(-dirction.Y / dirction.X, 1, 0).GetNormal();
            }
            else
            {
                return new Vector3d(1, -dirction.X / dirction.Y, 0).GetNormal();
            }
        }

        private static bool KeepSearching(ThSprinklerGraph graph, ThSprinklerNetGroup net, int edgeIndex, List<Point3d> realPts,
            out int newIdx, List<Point3d> realPtsSearchedTemp, List<Point3d> sprinklerSearched, Vector3d dirction,
            List<Point3d> virtualPts, Point3d virtualPt, ThSprinklerRowConnect rowConnect, int order)
        {
            if (edgeIndex == -1)
            {
                newIdx = -1;
                return false;
            }

            // 返回是否退出当前索引的循环
            // 搜索下一个点位
            var vertexNode = graph.SprinklerVertexNodeList[edgeIndex];
            var originalPt = net.pts[vertexNode.NodeIndex];
            var ptNext = net.pts[graph.SprinklerVertexNodeList[vertexNode.FirstEdge.EdgeIndex].NodeIndex];
            var edgeNext = new Line(originalPt, ptNext);
            var node = vertexNode.FirstEdge;
            newIdx = vertexNode.FirstEdge.EdgeIndex;
            // 如果点位已被检索，或之间连线角度偏大，或节点为虚拟点，则进入循环
            while (SprinklerSearched.Contains(ptNext)
                || sprinklerSearched.Contains(ptNext)
                || realPtsSearchedTemp.Contains(ptNext)
                || virtualPt.IsEqualTo(ptNext)
                || edgeNext.LineDirection().DotProduct(dirction) < 0.998)
            {
                if (node.Next != null)
                {
                    node = node.Next;
                    ptNext = net.pts[graph.SprinklerVertexNodeList[node.EdgeIndex].NodeIndex];
                    edgeNext = new Line(originalPt, ptNext);
                    newIdx = node.EdgeIndex;
                }
                else
                {
                    // 继续沿该方向进行搜索
                    var extendLine = new Line(originalPt - dirction, originalPt + 2.5 * dirction * DTTol);
                    var ptSearched = SearchPointByDirction(realPts, originalPt, extendLine, Geometry, out var firstPt);
                    var virtualPtSearched = SearchVirtualPt(extendLine, originalPt, Geometry, out var firstVirtualPt);
                    virtualPtSearched = false;
                    if (ptSearched && !virtualPtSearched)
                    {
                        ptNext = firstPt;
                        newIdx = graph.SearchNodeIndex(net.pts.IndexOf(ptNext));
                        if (newIdx == -1
                            || SprinklerSearched.Contains(ptNext)
                            || sprinklerSearched.Contains(ptNext)
                            || realPtsSearchedTemp.Contains(ptNext))
                        {
                            return false;
                        }
                        else
                        {
                            break;
                        }
                    }
                    else if (!ptSearched && virtualPtSearched)
                    {
                        virtualPts.Add(firstVirtualPt);
                        ptNext = firstVirtualPt;
                        break;
                    }
                    else if (ptSearched && virtualPtSearched)
                    {
                        if (firstPt.DistanceTo(originalPt) < firstVirtualPt.DistanceTo(originalPt))
                        {
                            ptNext = firstPt;
                            newIdx = graph.SearchNodeIndex(net.pts.IndexOf(ptNext));
                            if (SprinklerSearched.Contains(ptNext) || sprinklerSearched.Contains(ptNext) || realPtsSearchedTemp.Contains(ptNext))
                            {
                                return false;
                            }
                            else
                            {
                                break;
                            }
                        }
                        else
                        {
                            virtualPts.Add(firstVirtualPt);
                            ptNext = firstVirtualPt;
                            break;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            if (virtualPts.Contains(ptNext))
            {
                rowConnect.OrderDict.Add(-1, new List<Point3d> { ptNext });
                rowConnect.OrderDict = OrderChange(rowConnect.OrderDict);
                rowConnect.EndPoint = ptNext;
                return false;
            }
            else
            {
                //if (LaneLine.Count == 0 && order >= 8)
                //{
                //    rowConnect.OrderDict.Add(order, new List<Point3d> { ptNext });
                //    rowConnect.EndPoint = ptNext;
                //    rowConnect.Count++;
                //    realPtsSearchedTemp.Add(ptNext);
                //    return false;
                //}
                rowConnect.OrderDict.Add(order, new List<Point3d> { ptNext });
                rowConnect.EndPoint = ptNext;
                rowConnect.Count++;
                realPtsSearchedTemp.Add(ptNext);
                return true;
            }
        }

        /// <summary>
        /// 判断次方向连线
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="net"></param>
        /// <param name="edgeIndex"></param>
        /// <param name="graphPts"></param>
        /// <param name="newIdx"></param>
        /// <param name="realPtsSearchedTemp"></param>
        /// <param name="sprinklerSearched"></param>
        /// <param name="dirction"></param>
        /// <param name="virtualPts"></param>
        /// <param name="virtualPt"></param>
        /// <param name="rowConnect"></param>
        /// <param name="order"></param>
        /// <returns></returns>
        private static bool KeepSearching(ThSprinklerGraph graph, ThSprinklerNetGroup net, int edgeIndex, List<Point3d> realPts,
            out int newIdx, ref bool hasScatter, List<Point3d> realPtsSearchedTemp, List<Point3d> sprinklerSearched, Vector3d dirction,
            List<Point3d> virtualPts, Point3d virtualPt, ThSprinklerRowConnect rowConnect, int order)
        {
            if (edgeIndex == -1)
            {
                newIdx = -1;
                return false;
            }

            // 返回是否退出当前索引的循环
            // 搜索下一个点位
            var vertexNode = graph.SprinklerVertexNodeList[edgeIndex];
            var originalPt = net.pts[vertexNode.NodeIndex];
            var ptNext = net.pts[graph.SprinklerVertexNodeList[vertexNode.FirstEdge.EdgeIndex].NodeIndex];
            var edgeNext = new Line(originalPt, ptNext);
            var node = vertexNode.FirstEdge;
            newIdx = vertexNode.FirstEdge.EdgeIndex;

            // 如果点位已被检索，或之间连线角度偏大，或节点为虚拟点，则进入循环
            while (realPtsSearchedTemp.Contains(ptNext)
                || virtualPt.IsEqualTo(ptNext)
                || edgeNext.LineDirection().DotProduct(dirction) < 0.998)
            {
                if (node.Next != null)
                {
                    node = node.Next;
                    ptNext = net.pts[graph.SprinklerVertexNodeList[node.EdgeIndex].NodeIndex];
                    edgeNext = new Line(originalPt, ptNext);
                    newIdx = node.EdgeIndex;
                }
                else
                {
                    // 继续沿该方向进行搜索
                    var extendLine = new Line(originalPt - dirction, originalPt + 2.5 * dirction * DTTol);
                    var ptSearched = SearchPointByDirction(realPts, originalPt, extendLine, Geometry, out var firstPt);
                    var virtualPtSearched = SearchVirtualPt(extendLine, originalPt, Geometry, out var firstVirtualPt);
                    if (ptSearched && !virtualPtSearched)
                    {
                        ptNext = firstPt;
                        newIdx = graph.SearchNodeIndex(net.pts.IndexOf(ptNext));
                        if (newIdx == -1
                            || realPtsSearchedTemp.Contains(ptNext))
                        {
                            return false;
                        }
                        else
                        {
                            break;
                        }
                    }
                    else if (!ptSearched && virtualPtSearched)
                    {
                        virtualPts.Add(firstVirtualPt);
                        ptNext = firstVirtualPt;
                        break;
                    }
                    else if (ptSearched && virtualPtSearched)
                    {
                        if (firstPt.DistanceTo(originalPt) < firstVirtualPt.DistanceTo(originalPt))
                        {
                            ptNext = firstPt;
                            newIdx = graph.SearchNodeIndex(net.pts.IndexOf(ptNext));
                            if (realPtsSearchedTemp.Contains(ptNext))
                            {
                                return false;
                            }
                            else
                            {
                                break;
                            }
                        }
                        else
                        {
                            virtualPts.Add(firstVirtualPt);
                            ptNext = firstVirtualPt;
                            break;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            if (virtualPts.Contains(ptNext))
            {
                rowConnect.OrderDict.Add(-1, new List<Point3d> { ptNext });
                rowConnect.OrderDict = OrderChange(rowConnect.OrderDict);
                rowConnect.EndPoint = ptNext;
                return false;
            }
            else
            {
                IsNoisePoint(sprinklerSearched, realPts, ptNext, ref hasScatter);

                //if (LaneLine.Count == 0 && order >= 8)
                //{
                //    rowConnect.OrderDict.Add(order, new List<Point3d> { ptNext });
                //    rowConnect.EndPoint = ptNext;
                //    rowConnect.Count++;
                //    realPtsSearchedTemp.Add(ptNext);
                //    return false;
                //}
                rowConnect.OrderDict.Add(order, new List<Point3d> { ptNext });
                rowConnect.EndPoint = ptNext;
                rowConnect.Count++;
                realPtsSearchedTemp.Add(ptNext);
                return true;
            }
        }

        /// <summary>
        /// 对剩余点沿所给方向继续搜索
        /// </summary>
        /// <returns></returns>
        private static bool KeepSearching(ThSprinklerGraph graph, ThSprinklerNetGroup net, Vector3d dirction,
            List<Point3d> sprinklerSearched, List<Point3d> seachedPts, List<Point3d> ptList, ref int ptNextIndex,
            List<Point3d> pipeScatters, List<Point3d> pipeScattersTemp, ref int scatterCount)
        {
            var vertexNode = graph.SprinklerVertexNodeList[ptNextIndex];
            var startPoint = net.pts[vertexNode.NodeIndex];
            var ptNext = net.pts[graph.SprinklerVertexNodeList[vertexNode.FirstEdge.EdgeIndex].NodeIndex];
            var edge = new Line(startPoint, ptNext);
            var node = vertexNode.FirstEdge;

            while (edge.LineDirection().DotProduct(dirction) < 0.998
                || sprinklerSearched.Contains(ptNext)
                || seachedPts.Contains(ptNext))
            {
                if (node.Next != null)
                {
                    node = node.Next;
                    ptNext = net.pts[graph.SprinklerVertexNodeList[node.EdgeIndex].NodeIndex];
                    edge = new Line(startPoint, ptNext);
                }
                else
                {
                    return false;
                }
            }

            if (pipeScatters.Contains(ptNext))
            {
                pipeScattersTemp.Add(ptNext);
                scatterCount++;
                if (scatterCount > 1)
                {
                    return false;
                }
            }

            ptNextIndex = node.EdgeIndex;
            ptList.Add(ptNext);
            seachedPts.Add(ptNext);
            return true;
        }

        /// <summary>
        /// 搜索沿某一方向的最近点
        /// </summary>
        /// <param name="pts"></param>
        /// <param name="originalPt"></param>
        /// <param name="extendLine"></param>
        /// <param name="firstPt"></param>
        /// <returns></returns>
        private static bool SearchPointByDirction(List<Point3d> pts, Point3d originalPt, Line extendLine, List<Polyline> geometry, out Point3d firstPt)
        {
            firstPt = new Point3d();
            var pline = extendLine.Buffer(1.0);

            var dbPoints = pts.Select(o => new DBPoint(o)).ToCollection();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(dbPoints);
            var filter = spatialIndex.SelectCrossingPolygon(pline);
            if (filter.Count > 1)
            {
                firstPt = filter.OfType<DBPoint>().Select(pt => pt.Position).OrderBy(pt => pt.DistanceTo(originalPt)).ToList()[1];

                var newLine = new Line(originalPt, firstPt);
                var wallIndex = new ThCADCoreNTSSpatialIndex(geometry.ToCollection());
                var wallFilter = wallIndex.SelectFence(newLine.Buffer(1.0));
                if (wallFilter.Count > 0)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 在支干管和线的交点中，搜索距起点最近的点
        /// </summary>
        /// <param name="extendLine"></param>
        /// <param name="originalPt"></param>
        /// <param name="firstVirtualPt"></param>
        /// <returns></returns>
        private static bool SearchVirtualPt(Line extendLine, Point3d originalPt, List<Polyline> geometry, out Point3d firstVirtualPt)
        {
            firstVirtualPt = new Point3d();
            var pts = new List<Point3d>();
            SprinklerParameter.SubMainPipe.ForEach(pipe =>
            {
                if (!IsOrthogonal(pipe, extendLine))
                {
                    return;
                }
                var breakPt = new Point3dCollection();
                extendLine.IntersectWith(pipe, Intersect.OnBothOperands, breakPt, (IntPtr)0, (IntPtr)0);
                if (breakPt.Count > 0)
                {
                    pts.AddRange(breakPt.OfType<Point3d>().ToList());
                }
            });

            if (pts.Count > 0)
            {
                firstVirtualPt = pts.OrderBy(pt => pt.DistanceTo(originalPt)).FirstOrDefault();
                var newLine = new Line(originalPt, firstVirtualPt);
                // Exception
                if (newLine.Length < 1.0)
                {
                    return false;
                }
                var wallIndex = new ThCADCoreNTSSpatialIndex(geometry.ToCollection());
                var wallFilter = wallIndex.SelectFence(newLine.Buffer(1.0));
                if (wallFilter.Count > 0)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 对各个支干管，搜索其离起点最近的点位，并返回阈值范围内的点
        /// </summary>
        /// <param name="originalPt"></param>
        /// <param name="virtualPts"></param>
        /// <param name="tol"></param>
        /// <returns></returns>
        private static List<Point3d> SearchVirtualPt(Point3d originalPt, double tol)
        {
            var virtualPts = new List<Point3d>();
            SprinklerParameter.SubMainPipe.ForEach(pipe =>
            {
                var closePt = pipe.GetClosestPointTo(originalPt, false);
                if (Math.Abs((closePt - originalPt).GetNormal().DotProduct(pipe.LineDirection())) > 0.01)
                {
                    return;
                }
                var dist = closePt.DistanceTo(originalPt);
                if (dist < tol)
                {
                    virtualPts.Add(closePt);
                }
            });

            return virtualPts;
        }

        /// <summary>
        /// 判断线上是否存在已检索点或墙线
        /// </summary>
        /// <param name="pts"></param>
        /// <param name="originalPt"></param>
        /// <param name="line"></param>
        /// <param name="firstPt"></param>
        /// <returns></returns>
        private static bool VaildLine(List<Point3d> pts, List<Polyline> geometry, Line line)
        {
            var pline = line.Buffer(1.0);
            // 检测线上是否存在点
            var dbPoints = pts.Select(o => new DBPoint(o)).ToCollection();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(dbPoints);
            var filter = spatialIndex.SelectCrossingPolygon(pline);
            // 检测是否与墙线相交
            var wallIndex = new ThCADCoreNTSSpatialIndex(geometry.ToCollection());
            var wallFilter = wallIndex.SelectCrossingPolygon(pline);

            if (filter.Count + wallFilter.Count > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 调整点位顺序，使起点离支干管最近
        /// </summary>
        /// <param name="dict"></param>
        /// <returns></returns>
        private static Dictionary<int, List<Point3d>> OrderChange(Dictionary<int, List<Point3d>> dict, bool force = false)
        {
            var startDist = dict[0][0].DistanceTo(dict[1][0]);
            var endDist = dict[dict.Count - 2][0].DistanceTo(dict[-1][0]);
            if (startDist > endDist || force)
            {
                var newDict = new Dictionary<int, List<Point3d>>();
                var order = 0;
                newDict.Add(order++, dict[-1]);
                for (int i = dict.Count - 2; i > 0; i--)
                {
                    newDict.Add(order++, dict[i]);
                }
                newDict.Add(-1, dict[0]);
                return newDict;
            }
            else
            {
                return dict;
            }
        }

        /// <summary>
        /// 判断直线与支干管是否正交，若正交则返回true
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private static bool IsOrthogonal(Line line)
        {
            var lineExtend = line.ExtendLine(1.0);
            var spatialIndex = new ThCADCoreNTSSpatialIndex(SprinklerParameter.SubMainPipe.ToCollection());
            var filter = spatialIndex.SelectCrossingPolygon(lineExtend.Buffer(1.0)).OfType<Line>().First();
            return IsOrthogonal(lineExtend, filter);
        }

        private static bool IsOrthogonal(Line first, Line second)
        {
            return Math.Abs(first.LineDirection().DotProduct(second.LineDirection())) < 0.01;
        }

        private static Polyline GetConvexHull(List<Point3d> pts)
        {
            var convexPl = new Polyline();
            var netI2d = pts.Select(x => x.ToPoint2d()).ToList();

            if (netI2d.Select(o => o.X).Distinct().Count() > 1 && netI2d.Select(o => o.Y).Distinct().Count() > 1)
            {
                var convex = netI2d.GetConvexHull();
                for (int j = 0; j < convex.Count; j++)
                {
                    convexPl.AddVertexAt(convexPl.NumberOfVertices, convex.ElementAt(j), 0, 0, 0);
                }
                convexPl.Closed = true;

                if (convexPl.Area > 1.0)
                {
                    return convexPl;
                }
            }

            var newPts = pts.OrderBy(pt => pt.X).ThenBy(pt => pt.Y).ToList();
            var longLine = new Line(newPts.First(), newPts[newPts.Count - 1]);
            return longLine.Buffer(1.0);
        }

        private static Vector3d MainDirction(Polyline convexHull, List<Line> laneLine, double tolerance)
        {
            var temp = convexHull.DPSimplify(10.0);
            Polyline frame;
            if (temp.Area > 1.0)
            {
                frame = temp.Buffer(1.0).OfType<Polyline>().OrderByDescending(o => o.Area).First();
            }
            else
            {
                var objs = new DBObjectCollection();
                temp.Explode(objs);
                var maxLine = objs.OfType<Line>().OrderByDescending(o => o.Length).First();
                frame = maxLine.ToNTSLineString().Buffer(tolerance, EndCapStyle.Square).ToDbObjects()[0] as Polyline;
            }

            var filter = new List<Line>();
            if (laneLine.Count > 0)
            {
                var spatialIndex = new ThCADCoreNTSSpatialIndex(laneLine.ToCollection());
                filter = spatialIndex.SelectCrossingPolygon(frame).OfType<Line>().ToList();
                if (filter.Count == 0)
                {
                    frame = frame.Buffer(tolerance).OfType<Polyline>().OrderByDescending(o => o.Area).First();
                    filter = spatialIndex.SelectCrossingPolygon(frame).OfType<Line>().ToList();
                }
            }
            if (filter.Count == 0)
            {
                var spatialIndex = new ThCADCoreNTSSpatialIndex(SprinklerParameter.SubMainPipe.ToCollection());
                filter = spatialIndex.SelectCrossingPolygon(frame).OfType<Line>().ToList();
            }
            var trim = new List<Line>();
            for (int j = 0; j < filter.Count; j++)
            {
                var objs = new DBObjectCollection();
                var pline = frame.Trim(filter[j]).OfType<Polyline>().FirstOrDefault();
                // Exception
                if (pline == null)
                {
                    return new Vector3d();
                }
                pline.Explode(objs);
                trim.Add(objs.OfType<Line>().OrderByDescending(l => l.Length).First());
            }

            if (trim.Count == 0)
            {
                return new Vector3d();
            }
            var orderList = new List<Tuple<double, double, Vector3d>>();
            for (int i = 0; i < trim.Count; i++)
            {
                var angle = trim[i].Angle > Math.PI ? trim[i].Angle - Math.PI : trim[i].Angle;
                var length = trim[i].Length;

                int j = 0;
                for (; j < orderList.Count; j++)
                {
                    if (Math.Abs(angle - orderList[j].Item1) < Math.PI / 180.0)
                    {
                        var lengthTotal = orderList[j].Item2 + length;
                        var tuple = new Tuple<double, double, Vector3d>(orderList[j].Item1, lengthTotal, orderList[j].Item3);
                        orderList[j] = tuple;
                        break;
                    }
                }
                if (j == orderList.Count)
                {
                    var dirction = trim[i].LineDirection();
                    var tuple = new Tuple<double, double, Vector3d>(angle, length, dirction);
                    orderList.Add(tuple);
                }
            }
            orderList = orderList.OrderByDescending(o => o.Item2).ToList();
            return orderList.First().Item3;
        }

        /// <summary>
        /// 判断线是否与线组相交，若相交则返回true
        /// </summary>
        /// <param name="line"></param>
        /// <param name="laneLine"></param>
        /// <returns></returns>
        private static bool IsIntersects(Line line, List<Line> lineList)
        {
            var lineExtend = line.ExtendLine(1.0);
            for (int i = 0; i < lineList.Count; i++)
            {
                if (lineList[i].LineIsIntersection(lineExtend))
                {
                    return true;
                }
            }
            return false;
        }

        private static Polyline CreateSquare(Point3d center, double length)
        {
            var pline = new Polyline
            {
                Closed = true
            };
            var pts = new Point3dCollection
            {
                center + length * Vector3d.XAxis + length * Vector3d.YAxis,
                center - length * Vector3d.XAxis + length * Vector3d.YAxis,
                center - length * Vector3d.XAxis - length * Vector3d.YAxis,
                center + length * Vector3d.XAxis - length * Vector3d.YAxis,
            };
            pline.CreatePolyline(pts);
            return pline;
        }

        /// <summary>
        /// 判断线是否和墙线相交，若相交则返回true
        /// </summary>
        /// <param name="line"></param>
        /// <param name="geometry"></param>
        /// <returns></returns>
        private static bool IsLineInWall(Point3d first, Point3d second, Point3d third, List<Polyline> geometry)
        {
            var spatialIndex = new ThCADCoreNTSSpatialIndex(geometry.ToCollection());
            var pline = new Polyline();
            var pts = new Point3dCollection
            {
                first,
                second,
            };
            pline.CreatePolyline(pts);
            var filter = spatialIndex.SelectFence(pline);

            var otherpPline = new Polyline();
            var otherPts = new Point3dCollection
            {
                third,
                third + (first - second),
            };
            otherpPline.CreatePolyline(otherPts);
            var otherFilter = spatialIndex.SelectFence(otherpPline);

            if (filter.Count > 0 && otherFilter.Count > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private static bool IsLineInWall(Line line, List<Polyline> geometry)
        {
            var spatialIndex = new ThCADCoreNTSSpatialIndex(geometry.ToCollection());
            var filter = spatialIndex.SelectFence(line.Buffer(1.0));

            if (filter.Count > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 根据新增点，获得最长的线
        /// </summary>
        /// <param name="line"></param>
        /// <param name="pt"></param>
        /// <returns></returns>
        private static Line GetLongLine(Line line, Point3d pt)
        {
            var closePtOnLine = line.GetClosestPointTo(pt, true);
            var first = new Line(line.StartPoint, closePtOnLine);
            var second = new Line(closePtOnLine, line.EndPoint);
            var list = new List<Line>
            {
                first,
                second,
                line
            };
            return list.OrderByDescending(l => l.Length).First();
        }

        private static void Present(List<Line> results)
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var layerId = acadDatabase.Database.CreateAILayer("AI-喷淋连管", 2);
                results.ForEach(o =>
                {
                    acadDatabase.ModelSpace.Add(o);
                    o.LayerId = layerId;
                });
            }
        }

        /// <summary>
        /// 计算线段中点
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private static Point3d GetCenterPoint(Line line)
        {
            return new Point3d((line.StartPoint.X + line.EndPoint.X) / 2, (line.StartPoint.Y + line.EndPoint.Y) / 2, 0);
        }

        /// <summary>
        /// 判断喷头是否在小房间内，若在则返回true
        /// </summary>
        /// <param name="sprinkler"></param>
        /// <param name="smallRooms"></param>
        /// <returns></returns>
        private static Tuple<bool, Polyline> IsSprinklerInSmallRoom(Point3d sprinkler, List<Polyline> smallRooms)
        {
            var isSprinklerInSmallRoom = false;
            var smallroom = new Polyline();
            smallRooms.ForEach(r =>
            {
                if (r.Contains(sprinkler))
                {
                    smallroom = r;
                    isSprinklerInSmallRoom = true;
                }
            });
            return Tuple.Create(isSprinklerInSmallRoom, smallroom);
        }

        /// <summary>
        /// 判断两线之间是否完全被墙隔绝，若隔绝则返回true
        /// </summary>
        /// <returns></returns>
        private static bool CanConnect(Line edge, Line targetLine)
        {
            // 中心线
            var centerPoint = GetCenterPoint(edge);
            var actualDistPt = targetLine.GetClosestPointTo(centerPoint, false);
            var centerLine = new Line(centerPoint, actualDistPt);

            // 起始线
            var startDistPt = targetLine.GetClosestPointTo(edge.StartPoint, false);
            var startLine = new Line(edge.StartPoint, startDistPt);

            // 结尾线
            var endDistPt = targetLine.GetClosestPointTo(edge.EndPoint, false);
            var endLine = new Line(edge.EndPoint, endDistPt);

            var canConnect = true;
            var linesCollection = new List<Line>();
            linesCollection.Add(centerLine);
            linesCollection.Add(startLine);
            linesCollection.Add(endLine);
            var wallIndex = new ThCADCoreNTSSpatialIndex(Geometry.ToCollection());
            for (int lineCount = 0; lineCount < linesCollection.Count; lineCount++)
            {
                var wallFilter = wallIndex.SelectFence(linesCollection[lineCount].Buffer(1.0));
                if (wallFilter.Count == 0)
                {
                    canConnect = false;
                    break;
                }
            }

            return canConnect;
        }

        /// <summary>
        /// 判断两线之间是否完全被不可穿墙隔绝，若隔绝则返回true
        /// </summary>
        /// <param name="edge"></param>
        /// <param name="targetLine"></param>
        /// <param name="obstacle"></param>
        /// <returns></returns>
        private static bool CanConnect(Line edge, Line targetLine, List<Polyline> obstacle)
        {
            // 中心线
            var centerPoint = GetCenterPoint(edge);
            var actualDistPt = targetLine.GetClosestPointTo(centerPoint, false);
            var centerLine = new Line(centerPoint, actualDistPt);

            // 起始线
            var startDistPt = targetLine.GetClosestPointTo(edge.StartPoint, false);
            var startLine = new Line(edge.StartPoint, startDistPt);

            // 结尾线
            var endDistPt = targetLine.GetClosestPointTo(edge.EndPoint, false);
            var endLine = new Line(edge.EndPoint, endDistPt);

            var canConnect = true;
            var linesCollection = new List<Line>();
            linesCollection.Add(centerLine);
            linesCollection.Add(startLine);
            linesCollection.Add(endLine);
            var wallIndex = new ThCADCoreNTSSpatialIndex(obstacle.ToCollection());
            for (int lineCount = 0; lineCount < linesCollection.Count; lineCount++)
            {
                // tag
                var wallFilter = wallIndex.SelectFence(linesCollection[lineCount].Buffer(1.0));
                if (wallFilter.Count == 0)
                {
                    canConnect = false;
                    break;
                }
            }

            return canConnect;
        }

        private static bool ConnectToRow(bool isSprinklerInSmallRoom, Line edge, Line targetLine, List<Polyline> obstacle,
            List<ThSprinklerRowConnect> rowConnection, List<Point3d> ptList)
        {
            if (isSprinklerInSmallRoom)
            {
                var canConnect = CanConnect(edge, targetLine, obstacle);
                if (canConnect)
                {
                    return false;
                }
            }
            else
            {
                var canConnect = CanConnect(edge, targetLine);
                if (canConnect)
                {
                    return false;
                }
            }

            // 判断两线是否正交或平行
            if (Math.Abs(edge.LineDirection().DotProduct(targetLine.LineDirection())) > 0.01
                && Math.Abs(edge.LineDirection().DotProduct(targetLine.LineDirection())) < 0.998)
            {
                return false;
            }

            var extendPt = edge.GetClosestPointTo(targetLine.EndPoint, true);
            if (Math.Abs(edge.LineDirection().DotProduct(targetLine.LineDirection())) < 0.01
                && (extendPt.DistanceTo(edge.StartPoint) < 150.0
                    || extendPt.DistanceTo(edge.EndPoint) < 150.0))
            {
                return false;
            }

            var rowConn = rowConnection
                .Where(row => row.Base.StartPoint == targetLine.StartPoint
                && row.Base.EndPoint == targetLine.EndPoint).FirstOrDefault();
            if (rowConn == null)
            {
                if (edge.Distance(targetLine) < 10.0 && Math.Abs(edge.LineDirection().DotProduct(targetLine.LineDirection())) < 0.01)
                {
                    var rowConnTemp = new ThSprinklerRowConnect();
                    rowConnTemp.OrderDict.Add(0, new List<Point3d> { targetLine.StartPoint });
                    rowConnTemp.Count++;
                    for (int i = 1; i <= ptList.Count; i++)
                    {
                        rowConnTemp.OrderDict.Add(i, new List<Point3d> { ptList[ptList.Count - i] });
                        rowConnTemp.Count++;
                    }
                    rowConnection.Add(rowConnTemp);
                    return true;
                }
                else
                {
                    return false;
                }
            }

            if (rowConn.Count + ptList.Count > 8)
            {
                return false;
            }

            for (int num = 2; ; num++)
            {
                if (!rowConn.OrderDict.ContainsKey(-num))
                {
                    rowConn.OrderDict.Add(-num, ptList);
                    rowConn.Count += ptList.Count;
                    rowConn.IsSmallRoom = isSprinklerInSmallRoom;
                    break;
                }
            }
            return true;
        }

        private static bool ConnectToPipe(Line edge, Line targetLine, List<ThSprinklerRowConnect> rowConnection, List<Point3d> ptList)
        {
            var canConnect = CanConnect(edge, targetLine);
            if (canConnect)
            {
                return false;
            }

            // 判断两线是否正交或平行
            if (Math.Abs(edge.LineDirection().DotProduct(targetLine.LineDirection())) > 0.01
                && Math.Abs(edge.LineDirection().DotProduct(targetLine.LineDirection())) < 0.998)
            {
                return false;
            }

            var rowConn = new ThSprinklerRowConnect();
            var startDist = edge.DistanceTo(targetLine.StartPoint, false);
            var endDist = edge.DistanceTo(targetLine.EndPoint, false);
            var centerPoint = GetCenterPoint(edge);
            var actualDistPt = targetLine.GetClosestPointTo(centerPoint, true);
            var actualDist = actualDistPt.DistanceTo(centerPoint);
            if (startDist < endDist)
            {
                if (startDist - actualDist < 10.0)
                {
                    rowConn.OrderDict.Add(0, new List<Point3d> { targetLine.StartPoint });
                }
                else
                {
                    rowConn.OrderDict.Add(0, new List<Point3d> { actualDistPt });
                }
            }
            else
            {
                if (endDist - actualDist < 10.0)
                {
                    rowConn.OrderDict.Add(0, new List<Point3d> { targetLine.EndPoint });
                }
                else
                {
                    rowConn.OrderDict.Add(0, new List<Point3d> { actualDistPt });
                }
            }
            rowConn.OrderDict.Add(-2, ptList);
            rowConnection.Add(rowConn);
            return true;
        }

        /// <summary>
        /// 判断小房间内是否存在管线
        /// </summary>
        /// <returns></returns>
        private static bool IsPipeInSmallRoom(Tuple<bool, Polyline> isSprinklerInSmallRoom, List<Line> closePipeLines)
        {
            if (!isSprinklerInSmallRoom.Item1)
            {
                return false;
            }
            var spatialIndex = new ThCADCoreNTSSpatialIndex(closePipeLines.ToCollection());
            var filter = spatialIndex.SelectCrossingPolygon(isSprinklerInSmallRoom.Item2);

            if (filter.Count > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 判断散点是否为噪音
        /// </summary>
        private static void IsNoisePoint(List<Point3d> sprinklerSearchedClone, List<Point3d> realPts, Point3d point, ref bool hasScatter)
        {
            var scatterList = realPts.Where(pt => !sprinklerSearchedClone.Contains(pt)).ToList();
            if (!sprinklerSearchedClone.Contains(point))
            {
                var square = CreateSquare(point, 2400.0 * 1.5);
                var scatterCount = 0;
                scatterList.ForEach(pt =>
                {
                    if (square.Contains(pt))
                    {
                        scatterCount++;
                    }
                });
                if (scatterCount > 1)
                {
                    hasScatter = true;
                }
            }
        }
    }
}
