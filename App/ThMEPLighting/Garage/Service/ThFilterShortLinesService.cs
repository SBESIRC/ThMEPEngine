using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.Garage.Service
{
    public abstract class ThFilterShortLinesService
    {
        protected List<Line> Lines { get; set; }
        protected List<Line> Results { get; set; }
        protected double LimitDistance { get; set; }
        protected ThQueryLineService QueryInstance { get; set; }
        protected ThFilterShortLinesService(List<Line> lines, double limitDistance)
        {
            Lines = lines;
            LimitDistance = limitDistance;
            Results = new List<Line>();
            QueryInstance = ThQueryLineService.Create(Lines);
        }
        protected abstract void Filter();
    }
}
