using System.Linq;
using ThMEPLighting.Common;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPLighting.Garage.Model;
using ThMEPLighting.Garage.Service;
using Autodesk.AutoCAD.DatabaseServices;
using System;

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
                if (lightEdges.Where(o => o.IsDX).Count() == 0)
                {
                    break;
                }
                //对灯线边建图
                var lightGraph = ThLightGraphService.Build(lightEdges, findSp);
                //找到从ports中的点出发拥有最长边的图
                lightGraph = ThFindLongestPathService.Find(ports, lightGraph);
                ThSingleRowDistributeService.Distribute(lightGraph, ArrangeParameter);
                if(ArrangeParameter.AutoCalculate)
                {
                    int numOfLights = 0;
                    lightGraph.Links.ForEach(l => l.Path.ForEach(p => numOfLights += p.LightNodes.Count));
                    ArrangeParameter.LoopNumber = CalculateLoopNumber(numOfLights);
                }
                ThSingleRowNumberService.Number(lightGraph, ArrangeParameter);
                lightGraph.Links.ForEach(o => DxLightEdges.AddRange(o.Path));
                lightEdges = LineEdges.Where(o => o.IsTraversed == false).ToList();                
                ports = ports.PtOnLines(lightEdges.Where(o => o.IsDX).Select(o => o.Edge).ToList());                
                if (ports.Count > 0)
                {
                    findSp = ports.First();
                }
                else if(lightEdges.Where(o => o.IsDX).Count()>0)
                {
                    findSp = lightEdges.Where(o => o.IsDX).First().Edge.StartPoint;
                }
                else
                {
                    break;
                }
            } while (lightEdges.Count > 0);
            //指定为中心线
            DxLightEdges.ForEach(o => o.Pattern = EdgePattern.Center);
        }
        private int CalculateLoopNumber(int lightNumbers)
        {
            double number = Math.Ceiling(lightNumbers*1.0 / 25);
            if(number < 3)
            {
                number = 3;
            }
            return (int)number;
        }
    }
}
