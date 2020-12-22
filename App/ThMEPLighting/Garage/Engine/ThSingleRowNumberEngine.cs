using System.Linq;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPLighting.Garage.Model;
using ThMEPLighting.Garage.Service;

namespace ThMEPLighting.Garage.Engine
{
    /// <summary>
    /// 单排编号
    /// </summary>
    public class ThSingleRowNumberEngine : ThBuildNumberEngine
    {
        public List<ThLightEdge> DxLightEdges{ get; set; }
        public ThSingleRowNumberEngine(
            List<Point3d> ports,
            List<ThLightEdge> lightEdges,
            ThLightArrangeParameter arrangeParameter)
            :base(ports, lightEdges,arrangeParameter)
        {
            DxLightEdges = new List<ThLightEdge>();
        }
        public ThSingleRowNumberEngine(
            List<Point3d> ports,
            List<ThLightEdge> lightEdges,
            ThLightArrangeParameter arrangeParameter,
            Point3d start) : base(ports, lightEdges, arrangeParameter, start)
        {            
        }
        public override void Build()
        {
            //对传入的灯边界不在进行任何处理
            Point3d findSp = Start;
            var lightEdges = new List<ThLightEdge>();
            LineEdges.ForEach(o => lightEdges.Add(o)); //包括Dx和Fdx边界
            var ports = new List<Point3d>();
            Ports.ForEach(o => ports.Add(o));
            do
            {
                //对灯线边建图
                var lightGraph = ThLightGraphService.Build(lightEdges, findSp);
                ThSingleRowNumberService.Number(lightGraph, ArrangeParameter);
                lightGraph.Links.ForEach(o => DxLightEdges.AddRange(o.Path));
                lightEdges = LineEdges.Where(o => o.IsTraversed == false).ToList();                
                ports = ports.PtOnLines(lightEdges.Where(o => o.IsDX).Select(o => o.Edge).ToList());
                if (ports.Count>0)
                {
                    findSp = ports.First();
                }
                if(lightEdges.Where(o=>o.IsDX).Count()==0)
                {
                    break;
                }
            } while (lightEdges.Count > 0 && ports.Count > 0);
        }
    }
}
