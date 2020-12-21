using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPLighting.ParkingStall.Model
{
    public class NearParks
    {
        public List<Polyline> Polylines;
        public NearParks(List<Polyline> polylines)
        {
            Polylines = polylines;
        }
    }
}
