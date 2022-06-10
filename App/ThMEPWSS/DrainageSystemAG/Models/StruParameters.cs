using Autodesk.AutoCAD.DatabaseServices;
using System.Collections.Generic;

namespace ThMEPWSS.DrainageSystemAG.Models
{
    class StruParameters
    {
        public StruParameters() 
        {
            Walls = new List<Polyline>();
            Columns = new List<Polyline>();
            Beams = new List<Polyline>();
        }
        public List<Polyline> Walls { get; }
        public List<Polyline> Columns { get; }
        public List<Polyline> Beams { get; }
    }
}
