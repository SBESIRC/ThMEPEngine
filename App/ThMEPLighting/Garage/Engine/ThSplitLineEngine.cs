using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPLighting.Garage.Service;

namespace ThMEPLighting.Garage.Engine
{
    public class ThSplitLineEngine : IDisposable
    {
        public Dictionary<Line,List<Line>> Results { get; private set; }
        private List<Line> Lines { get; set; }
        public ThSplitLineEngine(List<Line> lines)
        {
            Lines = new List<Line>();
            lines.ForEach(o => Lines.Add(new Line(o.StartPoint, o.EndPoint)));
            Results = new Dictionary<Line, List<Line>>();
        }
        public void Split()
        {
            Lines.ForEach(o =>
            {
               var splitLines = ThSplitLineService.Split(Lines, o);
                if(splitLines.Count==0)
                {
                    var lines = new List<Line>();
                    lines.Add(new Line(o.StartPoint, o.EndPoint));
                    Results.Add(o, lines); 
                }
                else
                {
                    Results.Add(o, splitLines);
                }
            });
        }
        public void Dispose()
        {            
        }
    }
}
