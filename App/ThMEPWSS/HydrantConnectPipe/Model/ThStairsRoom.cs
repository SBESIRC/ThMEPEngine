using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Model;

namespace ThMEPWSS.HydrantConnectPipe.Model
{
    public class ThStairsRoom : ThIfcBuildingElement
    {
        public Polyline ElementObb { get; set; }
    }
}
