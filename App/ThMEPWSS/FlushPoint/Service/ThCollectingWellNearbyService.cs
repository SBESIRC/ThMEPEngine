using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.FlushPoint.Service
{
    public class ThCollectingWellNearbyService
    {
        public double NearbyDistance { get; set; } //排水沟的中心线外扩4000范围内视为“附近”
        private ThCADCoreNTSSpatialIndex SpatialIndex { get; set; }
        public ThCollectingWellNearbyService(List<Polyline> wells)
        {
            NearbyDistance = 3000;
            SpatialIndex = new ThCADCoreNTSSpatialIndex(wells.ToCollection());
        }
        public bool Find(Point3d pt)
        {
            var circle = new Circle(pt, Vector3d.ZAxis, NearbyDistance);
            var poly = circle.Tessellate(5.0);
            var objs = SpatialIndex.SelectCrossingPolygon(poly);
            if(objs.Count==0)
            {
                return false;
            }
            return objs.Cast<Polyline>().ToList().Where(o =>
            {
                if (o.IsRectangle())
                {
                    var center = o.GetPoint3dAt(0).GetMidPt(o.GetPoint3dAt(2));
                    return pt.DistanceTo(center) <= NearbyDistance;
                }
                else
                {
                    var obb = o.GetMinimumRectangle();
                    var center = obb.GetPoint3dAt(0).GetMidPt(obb.GetPoint3dAt(2));
                    return pt.DistanceTo(center) <= NearbyDistance;
                }
            }).Any();               
        }
    }
}
