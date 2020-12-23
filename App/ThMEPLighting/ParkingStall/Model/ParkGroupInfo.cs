using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPLighting.ParkingStall.Model
{
    public class ParkGroupInfo
    {
        public Polyline BigPolyline;
        public Polyline SmallPolyline;

        public ParkGroupInfo(Polyline bigPoly, Polyline smallPoly)
        {
            BigPolyline = bigPoly;
            SmallPolyline = smallPoly;
        }
    }
}
