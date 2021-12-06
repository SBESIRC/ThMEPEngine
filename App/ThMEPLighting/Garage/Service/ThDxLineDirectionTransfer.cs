using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.Garage.Service
{
    internal class ThDxLineDirectionTransfer
    {
        private List<Line> FirstLines { get; set; }
        Dictionary<Line, Tuple<List<Line>, List<Line>>> CenterSides;
        private Dictionary<Line, Vector3d> CenterDirectionDict { get; set; }
        private ThQueryLineService LineQuery { get; set; }
        public ThDxLineDirectionTransfer(
            Dictionary<Line, Vector3d> centerDirectionDict,
            Dictionary<Line, Tuple<List<Line>, List<Line>>> centerSides)
        {
            CenterSides = centerSides;
            CenterDirectionDict = centerDirectionDict;
            CreateLineQuery();
        }
        private void CreateLineQuery()
        {
            var lines = new List<Line>();
            lines.AddRange(CenterSides.SelectMany(o=>o.Value.Item1));
            lines.AddRange(CenterSides.SelectMany(o => o.Value.Item2));
            LineQuery = ThQueryLineService.Create(lines);
        }
        public Vector3d? Transfer(Line line)
        {
            var lines = LineQuery.QueryCollinearLines(line.StartPoint, line.EndPoint);
            if (lines.Count > 0)
            {
                var center = Query(lines.First());
                if (center != null)
                {
                    return CenterDirectionDict[center];
                }
                else
                {
                    //
                }
            }
            return null;
        }
        public Dictionary<Line,Vector3d> Transfer(List<Line> sideLines)
        {
            var result = new Dictionary<Line,Vector3d>();
            sideLines.ForEach(o =>
            {
                var vec = Transfer(o);
                if(vec.HasValue)
                {
                    result.Add(o, vec.Value);
                }
                else
                {
                    //ToDo
                }
            });
            return result;
        }
        private Line Query(Line sideLine)
        {
            return CenterSides
                 .Where(o => o.Value.Item1.Contains(sideLine) || o.Value.Item2.Contains(sideLine))
                 .Select(o => o.Key)
                 .FirstOrDefault();
        }
    }
}
