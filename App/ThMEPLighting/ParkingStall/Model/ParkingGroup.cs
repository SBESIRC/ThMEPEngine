using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPLighting.ParkingStall.Model
{
    /// <summary>
    /// 成对的车位parks
    /// </summary>
    public class ParkingRelatedGroup
    {
        public List<Polyline> RelatedParks
        {
            get;
            set;
        } = new List<Polyline>();

        public ParkingRelatedGroup(List<Polyline> polylines)
        {
            RelatedParks = polylines;
        }
    }
}
