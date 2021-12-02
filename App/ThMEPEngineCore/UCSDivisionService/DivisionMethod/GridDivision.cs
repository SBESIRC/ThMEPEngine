using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPEngineCore.UCSDivisionService.DivisionMethod
{
    public class GridDivision
    {
        public void Division(List<Curve> girds, Polyline polyline)
        {
            
        }

        private List<Line> ConvertToLine(List<Curve> girds)
        {
            List<Line> resLines = new List<Line>();
            foreach (var grid in girds)
            {
                if (grid is Polyline)
                {
                    var objs = new DBObjectCollection();
                    grid.Explode(objs);
                    resLines.AddRange(objs.Cast<Line>());
                }
            }
            return resLines;
        }
    }
}
