using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
