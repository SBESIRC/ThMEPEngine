using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPLighting.FEI.Service;

namespace ThMEPLighting.FEI.EvacuationPath
{
    public static class OptimizeStartExtendLineService
    {
        public static Polyline CreateMapFrame(Line lane, Point3d startPt, List<Polyline> holes, double expandLength)
        {
            Vector3d xDir = (lane.EndPoint - lane.StartPoint).GetNormal();
            List<Point3d> pts = new List<Point3d>() { startPt, lane.StartPoint, lane.EndPoint };
            var polyline = GeUtils.GetBoungdingBox(pts, xDir);
            var intersectHoles = SelectService.SelelctCrossing(holes, polyline);
            foreach (var iHoles in intersectHoles)
            {
                pts.AddRange(GeUtils.GetAllPolylinePts(iHoles));
            }
            var resPolyline = GeUtils.GetBoungdingBox(pts, xDir).Buffer(expandLength)[0] as Polyline;

            return resPolyline;
        }
    }
}
