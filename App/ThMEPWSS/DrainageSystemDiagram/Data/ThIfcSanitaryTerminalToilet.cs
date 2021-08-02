using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;

namespace ThMEPWSS.DrainageSystemDiagram
{
    public class ThIfcSanitaryTerminalToilet : ThIfcSanitaryTerminal
    {
        public string Type { get; set; }
        public ThIfcSanitaryTerminalToilet(Entity geometry, string blkName)
        {
            Outline = geometry;
            Type = blkName;
        }
    }
}
