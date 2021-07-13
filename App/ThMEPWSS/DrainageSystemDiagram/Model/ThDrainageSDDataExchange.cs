using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.DrainageSystemDiagram
{
    public class ThDrainageSDDataExchange
    {
        public List<ThToilateRoom > roomList { get; set; }
        public List<ThIfcSanitaryTerminalToilate> TerminalList { get; set; }
        public Dictionary <string,List<ThIfcSanitaryTerminalToilate>> GroupList { get; set; }
        public Dictionary<string, (string, string)> IslandPair { get; set; }

        public Point3d SupplyCoolStart { get; set; }

        public string AreaID { get; set; }

        public List<Line> Pipes { get; set; }

        public ThDrainageSDTreeNode PipeTreeRoot { get; set; }
    }
}
