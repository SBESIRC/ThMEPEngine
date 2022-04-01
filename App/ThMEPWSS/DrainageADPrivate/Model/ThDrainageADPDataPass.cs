using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPWSS.DrainageADPrivate.Model
{
    internal class ThDrainageADPDataPass
    {
        //--input
        public List<Line> HotPipeTopView { get; set; }
        public List<Line> CoolPipeTopView { get; set; }
        public List<Line> ADHotPipe { get; set; }
        public List<Line> ADCoolPipe { get; set; }
        public List<Line> VerticalPipe { get; set; }
        public Point3d StartPt { get; set; }
        public List<ThSaniterayTerminal> Terminal { get; set; }
        //--output
    }
}
