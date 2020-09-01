using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;

namespace ThMEPEngineCore.Service
{
    public class ThSplitLinearBeamSevice: ThSplitBeamSevice, IDisposable
    {
        private ThIfcLineBeam LineBeam { get; set; }       
        private Line CenterLine { get; set; }
        public ThSplitLinearBeamSevice(ThIfcLineBeam thIfcLineBeam,DBObjectCollection components):base(components)
        {
            LineBeam = thIfcLineBeam;
            CenterLine = new Line(LineBeam.StartPoint, LineBeam.EndPoint);
        }
        public override void Split()
        {
            IntersectAreas = CreateIntersectAreas();
            if (IntersectAreas.Count==0)
            {
                return;
            }
            var breakPoints = CreateBreakPoints();
            if (breakPoints.Count == 0)
            {
                return;
            }
            var linePoints = BreakBeamCenterLine(LineBeam.StartPoint, LineBeam.EndPoint, breakPoints);
            if(linePoints.Count>1)
            {
                linePoints.ForEach(o=>
                {
                    if(o.Item1.DistanceTo(o.Item2)>0.0)
                    {
                        SplitBeams.Add(CreateLineBeam(LineBeam, o.Item1, o.Item2));
                    }
                });
            }
        }
        private List<Tuple<Point3d, Point3d>> CreateBreakPoints()
        {
            List<Tuple<Point3d,Point3d>> breakPoints=new List<Tuple<Point3d, Point3d>>();
            for(int i=0;i< IntersectAreas.Count;i++)
            {
                int m = i;
                Point3d startPt = IntersectAreas[i].Item2;
                for (int j =i+1;j< IntersectAreas.Count; j++)
                {
                    if(!CheckTwoLineUnIntersect(IntersectAreas[m].Item2, IntersectAreas[m].Item3,
                        IntersectAreas[j].Item2, IntersectAreas[j].Item3))
                    {
                        m = j;
                    }
                }
                Point3d endPt = IntersectAreas[m].Item3;
                i = m;
                breakPoints.Add(Tuple.Create(startPt, endPt));
            }
            return breakPoints;
        }
        private List<Tuple<Polyline, Point3d, Point3d>> CreateIntersectAreas()
        {
            List<Tuple<Polyline,Point3d,Point3d>> intersectAreas = new List<Tuple<Polyline, Point3d, Point3d>>();
            Polyline beamOutline = LineBeam.Outline as Polyline;
            foreach (Polyline columnOutline in Components)
            {
                Point3dCollection intersPts = IntersectWithEx(beamOutline, columnOutline);
                if(intersPts.Count!=4)
                {
                    continue;
                }
                intersPts = OrderbyPts(intersPts);
                if (!ValidateIntersectPts(intersPts, LineBeam))
                {
                    continue;
                }
                Polyline intersectArea = intersPts.CreatePolyline();
                Point3dCollection centerLineIntersPts = IntersectWithEx(CenterLine, intersectArea);
                if (centerLineIntersPts.Count == 2)
                {
                    if(LineBeam.StartPoint.DistanceTo(centerLineIntersPts[0])< 
                        LineBeam.StartPoint.DistanceTo(centerLineIntersPts[1]))
                    {
                        intersectAreas.Add(Tuple.Create(intersectArea, centerLineIntersPts[0], centerLineIntersPts[1]));
                    }
                    else
                    {
                        intersectAreas.Add(Tuple.Create(intersectArea, centerLineIntersPts[1], centerLineIntersPts[0]));
                    }
                }
                else
                {
                    intersectArea.Dispose();
                }
            }
            return intersectAreas.OrderBy(o => LineBeam.StartPoint.DistanceTo(o.Item2)).ToList();
        }
        public void Dispose()
        {
            CenterLine.Dispose();
            IntersectAreas.ForEach(o => o.Item1.Dispose());
        }
    }
}
