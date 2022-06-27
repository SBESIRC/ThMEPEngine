using System;
using System.Linq;
using System.Collections.Generic;

using NFox.Cad;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;

namespace ThMEPLighting.Garage.Service
{
    public static class ThLightingLineCorrectionService
    {
        public static List<Line> DoubleRowCorrect(List<Line> sourceLines, double tolerance, double doubleRowOffsetDis)
        {
            var spatialIndex = new ThCADCoreNTSSpatialIndex(sourceLines.ToCollection());
            for (var i = 0; i < sourceLines.Count; i++)
            {
                var buffer = sourceLines[i].BufferSquare(10.0);
                var crossingLines = spatialIndex.SelectCrossingPolygon(buffer)
                    .OfType<Line>().Except(new List<Line> { sourceLines[i] }).ToList();
                if (crossingLines.Count == 0)
                {
                    continue;
                }

                var filter = new DBObjectCollection();
                var indexs = new List<int>();
                for (var j = 0; j < crossingLines.Count; j++)
                {
                    if (indexs.Contains(j))
                    {
                        continue;
                    }
                    for (var k = j + 1; k < crossingLines.Count; k++)
                    {
                        if (indexs.Contains(k))
                        {
                            continue;
                        }
                        if (crossingLines[j].Distance(crossingLines[k]) < doubleRowOffsetDis + 10.0)
                        {
                            indexs.Add(j);
                            indexs.Add(k);
                        }
                    }
                }
                indexs.ForEach(index => filter.Add(crossingLines[index]));
                var correctLines = EndPointCorrect(filter, sourceLines[i], tolerance);
                spatialIndex.Update(correctLines, filter);
            }
            return spatialIndex.SelectAll().OfType<Line>().ToList();
        }

        public static List<Line> SingleRowCorrect(List<Line> firstEdges, List<Line> secondEdges, double tolerance)
        {
            var results = new List<Line>();
            var indexCollection = new DBObjectCollection();
            firstEdges.ForEach(edge => indexCollection.Add(edge));
            secondEdges.ForEach(edge => indexCollection.Add(edge));
            var spatialIndex = new ThCADCoreNTSSpatialIndex(indexCollection);
            for (var i = 0; i < firstEdges.Count; i++)
            {
                var direction = firstEdges[i].LineDirection();
                var startBuffer = firstEdges[i].StartPoint.CreateSquare(10.0);
                var startCrossingLines = spatialIndex.SelectFence(startBuffer)
                    .OfType<Line>().Except(new List<Line> { firstEdges[i] })
                    .OrderByDescending(l => l.Length).ToList();
                var endBuffer = firstEdges[i].EndPoint.CreateSquare(10.0);
                var endCrossingLines = spatialIndex.SelectFence(endBuffer)
                    .OfType<Line>().Except(new List<Line> { firstEdges[i] })
                    .OrderByDescending(l => l.Length).ToList();

                if (NeedReduce(direction, startCrossingLines, secondEdges)
                    && NeedReduce(direction, endCrossingLines, secondEdges))
                {
                    if (firstEdges[i].Length > 2 * tolerance)
                    {
                        results.Add(new Line(firstEdges[i].StartPoint + direction * tolerance, firstEdges[i].EndPoint - direction * tolerance));
                    }
                }
                else if (NeedReduce( direction, startCrossingLines, secondEdges))
                {
                    if (firstEdges[i].Length > tolerance)
                    {
                        results.Add(new Line(firstEdges[i].StartPoint + direction * tolerance, firstEdges[i].EndPoint));
                    }
                }
                else if (NeedReduce(direction, endCrossingLines, secondEdges))
                {
                    if (firstEdges[i].Length > tolerance)
                    {
                        results.Add(new Line(firstEdges[i].StartPoint, firstEdges[i].EndPoint - direction * tolerance));
                    }
                }
                else
                {
                    results.Add(firstEdges[i]);
                }
            }

            return results;
        }

        private static DBObjectCollection EndPointCorrect(DBObjectCollection filter, Line sourceLine, double tolerance)
        {
            var results = new DBObjectCollection();
            filter.OfType<Line>().Where(x => x.Length > tolerance).ForEach(x =>
            {
                var direction = x.LineDirection();
                if (sourceLine.DistanceTo(x.StartPoint, false) < 10.0)
                {
                    results.Add(new Line(x.StartPoint + direction * tolerance, x.EndPoint));
                }
                else if (sourceLine.DistanceTo(x.EndPoint, false) < 10.0)
                {
                    results.Add(new Line(x.StartPoint, x.EndPoint - direction * tolerance));
                }
            });
            return results;
        }

        private static List<Line> HandleCrossLines(Line line, Vector3d direction, List<Line> crossingLines)
        {
            if (crossingLines.Any(l => Math.Abs(l.LineDirection().DotProduct(direction)) > Math.Cos(45 / 180.0 * Math.PI)))
            {
                var parallelLinesLength = crossingLines.Where(l => Math.Abs(l.LineDirection().DotProduct(direction))
                    > Math.Cos(1.0 / 180.0 * Math.PI)).Select(l => l.Length).Sum();
                var verticalLinesLength = crossingLines.Where(l => Math.Abs(l.LineDirection().DotProduct(direction))
                    < Math.Sin(1.0 / 180.0 * Math.PI)).Select(l => l.Length).Sum();
                if (parallelLinesLength + line.Length > verticalLinesLength)
                {
                    return new List<Line>();
                }
            }
            return crossingLines;
        }

        private static bool NeedReduce(Vector3d direction, List<Line> crossingLines, List<Line> secondEdges)
        {
            return crossingLines.Count > 0
                && (crossingLines.Where(l => Math.Abs(l.LineDirection().DotProduct(direction)) < Math.Sin(1 / 180.0 * Math.PI)).Count() > 1
                    || secondEdges.Contains(crossingLines[0]));
        }
    }
}
