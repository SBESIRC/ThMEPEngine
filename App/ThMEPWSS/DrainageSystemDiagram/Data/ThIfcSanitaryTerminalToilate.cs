using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using ThCADExtension;
using ThMEPEngineCore.Model;
using ThCADCore.NTS;


namespace ThMEPWSS.DrainageSystemDiagram
{
    public class ThIfcSanitaryTerminalToilate : ThIfcSanitaryTerminal
    {
        public string Type { get; set; }
        public ThIfcSanitaryTerminalToilate(Entity geometry, string blkName)
        {
            Outline = geometry;
            Type = blkName;
        }
    }
}
