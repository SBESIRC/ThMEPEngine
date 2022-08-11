using System;
using System.Linq;
using System.Collections.Generic;

using NFox.Cad;
using Autodesk.AutoCAD.DatabaseServices;

using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;

namespace ThMEPLighting.Garage.Service
{
    public class ThCenterLineService
    {
        public static List<Line> NormalizeEx(List<Line> lightingLines)
        {
            if (lightingLines.Count == 0)
            {
                return new List<Line>();
            }

            // 存结果
            var results = new List<Line>();
            // 存原始线
            var searchedLines = new List<Line>();
            var index = new ThCADCoreNTSSpatialIndex(lightingLines.ToCollection());

            while (lightingLines.Except(searchedLines).Count() > 0)
            {
                var startLine = lightingLines.Except(searchedLines).OrderByDescending(o => o.Length).First();
                searchedLines.Add(startLine);
                startLine = FindStartLine(startLine, index);
                results.Add(startLine);
                Search(startLine, results, searchedLines, index);
            }

            return results;
        }

        private static void Search(Line startLine, List<Line> results, List<Line> searchedLines, ThCADCoreNTSSpatialIndex index)
        {
            var frame = startLine.BufferSquare(10.0);
            var filter = index.SelectCrossingPolygon(frame).OfType<Line>().Except(searchedLines).ToList();
            filter.ForEach(o =>
            {
                searchedLines.Add(o);
                if (o.LineDirection().DotProduct(startLine.LineDirection()) < -Math.Sin(1 / 180.0 * Math.PI))
                {
                    var newLine = new Line(o.EndPoint, o.StartPoint);
                    results.Add(newLine);
                    Search(newLine, results, searchedLines, index);
                }
                else
                {
                    results.Add(o);
                    Search(o, results, searchedLines, index);
                }
            });
        }

        private static Line FindStartLine(Line line, ThCADCoreNTSSpatialIndex index)
        {
            var startPointFrame = line.StartPoint.CreateSquare(10.0);
            var endPointFrame = line.EndPoint.CreateSquare(10.0);
            var startFilter = index.SelectCrossingPolygon(startPointFrame);
            var endFilter = index.SelectCrossingPolygon(endPointFrame);
            if (startFilter.Count > endFilter.Count)
            {
                return new Line(line.EndPoint, line.StartPoint);
            }
            else
            {
                return line;
            }
        }
    }
}
