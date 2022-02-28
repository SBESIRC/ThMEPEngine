using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.Model;

namespace ThMEPWSS.FirstFloorDrainagePlaneSystem.PipeRoute
{
    public class ReprocessingPipe
    {
        List<RouteModel> routes;
        List<Polyline> outFrame;
        public ReprocessingPipe(List<RouteModel> routeModels, List<Polyline> _outFrame)
        {
            routes = routeModels;
            outFrame = _outFrame;
        }

        public void Reprocessing()
        {
            //foreach (var item in collection)
            //{

            //}
        }


    }
}
