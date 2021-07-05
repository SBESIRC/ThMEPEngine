using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPWSS.HydrantConnectPipe.Model
{
    public class ThHydrantMainLine
    {
        public List<Line> MainLines { set; get; }
        public ThHydrantMainLine()
        {
            MainLines = new List<Line>();
        }

        public static ThHydrantMainLine Create(List<Line> lines)
        {
            var mainLine = new ThHydrantMainLine
            {
                MainLines = lines
            };
            return mainLine;
        }
    }
}
