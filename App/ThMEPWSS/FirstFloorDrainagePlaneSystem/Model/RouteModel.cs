using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPWSS.FirstFloorDrainagePlaneSystem.Model
{
    public class RouteModel
    {
        public RouteModel(Polyline _route, VerticalPipeType _type)
        {
            route = _route;
            verticalPipeType = _type;
        }

        public Circle printCircle { get; set; }

        public Polyline route { get; set; }

        public VerticalPipeType verticalPipeType { get; set; }
    }
}
