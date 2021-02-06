using System;
using System.Linq;
using ThMEPLighting.Common;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPLighting.Garage.Model;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.Garage.Engine
{
    public abstract class ThBuildNumberEngine
    {        
        protected List<ThLightEdge> LineEdges { get; set; }
        protected Point3d Start { get; set; }
        protected List<Point3d> Ports { get; set; }
        protected ThLightArrangeParameter ArrangeParameter { get; set; }
        protected ThBuildNumberEngine(
            List<Point3d> ports,
            List<ThLightEdge> lineEdges,
            ThLightArrangeParameter arrangeParameter)
        {
            LineEdges = lineEdges; //保持顺序，不要放在后面
            Ports = ports.PtOnLines(LineEdges.Where(o=>o.IsDX).Select(o=>o.Edge).ToList());
            if (Ports.Count>0)
            {
                Start=Ports.First();
            } 
            else
            {
                var dxLightEdges = LineEdges.Where(o => o.IsDX);
                if (dxLightEdges.Count() > 0)
                {
                    Start = dxLightEdges.First().Edge.StartPoint;
                }
            }
            Ports = Ports.Distinct().ToList();
            ArrangeParameter = arrangeParameter;            
        }
        protected ThBuildNumberEngine(
            List<Point3d> ports,
            List<ThLightEdge> lineEdges,           
            ThLightArrangeParameter arrangeParameter,
            Point3d start):this(ports, lineEdges, arrangeParameter)
        {
            if(Ports.Count > 0)
            {
                //获取与传入的起始点最接近的端点
                Start = Ports.OrderBy(o => start.DistanceTo(o)).First();
            }            
        }
        public abstract void Build();
    }
}
