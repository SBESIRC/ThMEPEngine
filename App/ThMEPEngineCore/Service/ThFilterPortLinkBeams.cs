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
    public class ThFilterPortLinkBeams
    {
        private ThIfcBeam CurrentBeam { set; get; }
        private Point3d PortPt { get; set; }
        public List<ThIfcBeam> LinkedBeams { get; set; }        
        private ThFilterPortLinkBeams(ThIfcBeam thIfcBeam,Point3d portPt,List<ThIfcBeam> linkedBeams)
        {
            CurrentBeam = thIfcBeam;
            PortPt = portPt;
            LinkedBeams = linkedBeams;
        }
        public static List<ThIfcBeam> Filter(ThIfcBeam thIfcBeam, Point3d portPt, List<ThIfcBeam> linkedBeams)
        {
            ThFilterPortLinkBeams portFilter = new ThFilterPortLinkBeams(thIfcBeam, portPt, linkedBeams);
            portFilter.Filter();
            return portFilter.LinkedBeams;
        }
        private void Filter()
        {
            LinkedBeams = LinkedBeams.Where(o => o.Uuid != CurrentBeam.Uuid).ToList();
            if(CurrentBeam is ThIfcLineBeam thIfcLineBeam)
            {
                FilterLineBeam(thIfcLineBeam);
            }
            else if(CurrentBeam is ThIfcArcBeam thIfcArcBeam)
            {
                FilterCurveBeam(thIfcArcBeam);
            }
            else
            {
                throw new NotSupportedException();
            }
        }
        private void FilterLineBeam(ThIfcLineBeam thIfcLineBeam)
        {
            var parallelBeams = LinkedBeams.Where(o=> o is ThIfcLineBeam beam && 
            ThGeometryTool.IsCollinearEx(thIfcLineBeam.StartPoint, thIfcLineBeam.EndPoint,
            beam.StartPoint,beam.EndPoint)).ToList();
            if(PortPt.DistanceTo(thIfcLineBeam.StartPoint)<=1.0)
            {
                parallelBeams = parallelBeams.OrderBy(o => o.EndPoint.DistanceTo(PortPt)).ToList();
            }
            else
            {
                parallelBeams = parallelBeams.OrderBy(o => o.StartPoint.DistanceTo(PortPt)).ToList();
            }
            for(int i=1;i<parallelBeams.Count;i++)
            {
                LinkedBeams.Remove(parallelBeams[i]);
            }
        }
        private void FilterCurveBeam(ThIfcArcBeam thIfcArcBeam)
        {            
        }
    }
}
