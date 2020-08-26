using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.CAD;

namespace ThMEPEngineCore.Model
{
    public class ThBeamLink
    {
        public List<ThIfcBuildingElement> Start { get; set; } = new List<ThIfcBuildingElement>();
        public List<ThIfcBuildingElement> End { get; set; } = new List<ThIfcBuildingElement>();
        public List<ThIfcBeam> Beams { get; set; } = new List<ThIfcBeam>();
        public Polyline CreateExtendBeamOutline(double extendDis)
        {
            Polyline polyline=new Polyline();
            if(Beams.Count==0 || Beams.Where(o => o is ThIfcArcBeam).Any())
            {
                return polyline;
            }
            double maxWidth = Beams.Select(o => o.ActualWidth).OrderByDescending(o => o).FirstOrDefault();
            Point3d firstBeamSpt = Beams[0].StartPoint;
            Point3d firstBeamEpt = Beams[0].EndPoint;
            List<Point3d> ptList = new List<Point3d>();
            Beams.ForEach(o =>
            {
                ptList.Add(o.StartPoint.GetProjectPtOnLine(firstBeamSpt, firstBeamEpt));
                ptList.Add(o.EndPoint.GetProjectPtOnLine(firstBeamSpt, firstBeamEpt));
            });
            Vector3d vec = ptList[0].GetVectorTo(ptList[ptList.Count - 1]);
            Vector3d perpendVec = vec.GetPerpendicularVector();
            Point3d newSpt = ptList[0] - vec.GetNormal().MultiplyBy(extendDis);
            Point3d newEpt = ptList[ptList.Count - 1] + vec.GetNormal().MultiplyBy(extendDis);
            Point3d pt1 = newSpt - perpendVec.GetNormal().MultiplyBy(maxWidth/2.0);
            Point3d pt2 = newEpt - perpendVec.GetNormal().MultiplyBy(maxWidth / 2.0);
            Point3d pt4 = newSpt + perpendVec.GetNormal().MultiplyBy(maxWidth / 2.0);
            Point3d pt3 = newEpt + perpendVec.GetNormal().MultiplyBy(maxWidth / 2.0);
            Point3dCollection pts = new Point3dCollection() { pt1, pt2, pt3, pt4 };
            return pts.CreatePolyline();
        }
    }
}
