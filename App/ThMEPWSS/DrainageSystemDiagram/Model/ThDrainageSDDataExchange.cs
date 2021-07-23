using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.DrainageSystemDiagram
{
    public class ThDrainageSDDataExchange
    {
        public ThDrainageSDCoolSupplyStart SupplyStart { get; set; }

        public ThDrainageSDRegion Region { get; set; }

        public List<ThToiletRoom > roomList { get; set; }

        public List<ThTerminalToilet> TerminalList { get; set; }

        public Dictionary <string,List<ThTerminalToilet>> GroupList { get; set; }

        public Dictionary<string, (string, string)> IslandPair { get; set; }

        public string AreaID { get; set; }

        public List<Line> Pipes { get; set; }

        public ThDrainageSDTreeNode PipeTreeRoot { get; set; }
    }
}
