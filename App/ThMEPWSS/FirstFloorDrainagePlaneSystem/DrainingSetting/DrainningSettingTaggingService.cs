using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.Model;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.Service;

namespace ThMEPWSS.FirstFloorDrainagePlaneSystem.DrainingSetting
{
    public class DrainningSettingTaggingService : DraningSettingService
    {
        double moveLength = 1000;
        public DrainningSettingTaggingService(List<RouteModel> _pipes)
        {
            pipes = _pipes;
        }

        public override void CreateDraningSetting()
        {
            var resPipe = CalTaggingPt();
        }

        private List<RouteModel> CalTaggingPt()
        {
            var line = pipes.First().connecLine;
            foreach (var pipe in pipes)
            {
                var allPts = GeometryUtils.GetAllPolylinePts(pipe.route).OrderBy(x => line.GetClosestPointTo(x, true).DistanceTo(x)).ToList();
                var lastPt = allPts.First();
                if (pipe.route.StartPoint.DistanceTo(lastPt) < pipe.route.EndPoint.DistanceTo(lastPt))
                {
                    pipe.route.ReverseCurve();
                }
                pipe.route = GeometryUtils.ShortenPolyline(pipe.route, moveLength);
            }

            return pipes;
        }
    }
}
