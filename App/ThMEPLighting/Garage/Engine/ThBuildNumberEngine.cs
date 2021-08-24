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
        protected List<Point3d> Ports { get; set; }
        protected ThLightArrangeParameter ArrangeParameter { get; set; }
        protected ThBuildNumberEngine(
            List<Point3d> ports,
            List<ThLightEdge> lineEdges,
            ThLightArrangeParameter arrangeParameter)
        {
            LineEdges = lineEdges; //保持顺序，不要放在后面
            Ports = ports.PtOnLines(LineEdges.Where(o=>o.IsDX).Select(o=>o.Edge).ToList());
            Ports = Ports.Distinct().ToList();
            ArrangeParameter = arrangeParameter;            
        }
        public abstract void Build();
    }
}
