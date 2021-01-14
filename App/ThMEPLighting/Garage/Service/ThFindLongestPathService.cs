using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Common;

namespace ThMEPLighting.Garage.Service
{
    public class ThFindLongestPathService
    {
        private ThLightGraphService Result { get; set; }
        private List<Point3d> Ports { get; set; }
        private ThLightGraphService LightGraph { get; set; }
        /// <summary>
        /// 记录LightGraph所有的边
        /// </summary>

        private List<ThLightEdge> LightEdges { get; set; }

        private ThFindLongestPathService(List<Point3d> ports, ThLightGraphService lightGraph)
        {           
            LightGraph = lightGraph;
            LightEdges = new List<ThLightEdge>();
            LightGraph.Links.ForEach(l => LightEdges.AddRange(l.Path));
            Ports = ThGarageLightUtils.PtOnLines(ports, LightEdges.Select(o => o.Edge).ToList());
        }
        public static ThLightGraphService Find(List<Point3d> ports,ThLightGraphService lightGraph)
        {
            var instance = new ThFindLongestPathService(ports, lightGraph);
            instance.Find();
            return instance.Result;
        }
        private void Find()
        {
            if (Ports.Count == 0 || LightEdges.Count==0)
            {
                return;
            }
            Remove();
            var graphs = new List<ThLightGraphService>();
            graphs.Add(LightGraph);
            Ports.ForEach(p =>
            {
                var edges = CreateNewEdges();
                var graph = ThLightGraphService.Build(edges, p);
                graphs.Add(graph);
            });
            LightEdges.ForEach(l => l.IsTraversed = true);
            Result = graphs.Where(o => o.Links.Count > 0).OrderByDescending(o => o.Links[0].Length).First();
        }
        private List<ThLightEdge> CreateNewEdges()
        {
            var results = new List<ThLightEdge>();
            LightEdges.ForEach(o =>
            {
                results.Add(new ThLightEdge(new Line(o.Edge.StartPoint,o.Edge.EndPoint)));
            });
            return results;
        }
        private void Remove()
        {
            for(int i=0;i<Ports.Count;i++)
            {
                if(Ports[i].DistanceTo(LightGraph.StartPoint)<=1.0)
                {
                    Ports.RemoveAt(i);
                    break;
                }
            }
        }
    }
}
