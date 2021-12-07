using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Service
{
    public class ThRoomOutlineSimplifier:ThPolygonalElementSimplifier
    {
        private double ClOSED_DISTANC_TOLERANCE;

        public ThRoomOutlineSimplifier()
        {
            OFFSETDISTANCE = 20.0;
            DISTANCETOLERANCE = 1.0;
            TESSELLATEARCLENGTH = 100.0;
            ClOSED_DISTANC_TOLERANCE = 1000.0; // 待定
            AREATOLERANCE = 100.0; //过滤房间面积
        }

        public List<Polyline> Close(List<Polyline> polys)
        {
            var results = new List<Polyline>();
            polys.ForEach(p =>
            {
                if(IsExactClosed(p))
                {
                    results.Add(p);                    
                }
                else if(IsApproximateClosed(p))
                {
                    var clone = p.Clone() as Polyline;
                    clone.Closed = true;
                    results.Add(clone);
                }
            });
            return results;
        }

        private bool IsExactClosed(Polyline polyline)
        {
            return polyline.Closed;
        }
        private bool IsApproximateClosed(Polyline polyline)
        {
            return polyline.StartPoint.DistanceTo(polyline.EndPoint) <= ClOSED_DISTANC_TOLERANCE;
        }
    }
}
