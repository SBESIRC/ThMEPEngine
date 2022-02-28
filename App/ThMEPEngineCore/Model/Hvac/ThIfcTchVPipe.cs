using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Model.Hvac
{
    public class ThIfcTchVPipe : ThIfcPipeSegment
    {
        public Entity Data { get; set; }
    }
}
