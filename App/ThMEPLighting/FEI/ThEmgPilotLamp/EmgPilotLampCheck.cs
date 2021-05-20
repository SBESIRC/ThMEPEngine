using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPLighting.FEI.ThEmgPilotLamp
{
    class EmgPilotLampCheck
    {
        public static bool LineNodeNeedHostLight(List<LineGraphNode> wallGraphNodes,List<GraphRoute> allRoutes, GraphNode graphNode, Vector3d inDir)
        {
            bool isAdd = false;
            Vector3d? vector = null;
            Vector3d? lineDir = null;
            foreach (var lineNodes in wallGraphNodes)
            {
                if (vector != null)
                    break;
                if (null == lineNodes.nodeDirections || lineNodes.nodeDirections.Count < 1)
                    continue;
                foreach (var item in lineNodes.nodeDirections)
                {
                    if (vector != null)
                        break;
                    if (item.graphNode.nodePoint.IsEqualTo(graphNode.nodePoint, new Tolerance(1, 1)))
                    {
                        vector = lineNodes.layoutLineSide;
                        lineDir = lineNodes.lineDir;
                    }
                }
            }
            if (vector != null)
            {
                List<GraphRoute> routes = GraphUtils.GetGraphNodeRoutes(allRoutes, graphNode, false);
                if (null != routes && routes.Count > 0)
                {
                    GraphNode pNode = null;
                    foreach (var route in routes)
                    {
                        if (isAdd)
                            break;
                        var tempRoute = route;
                        pNode = tempRoute.node;
                        if (pNode.nodePoint.IsEqualTo(graphNode.nodePoint, new Tolerance(1, 1)))
                            continue;
                        while (tempRoute.nextRoute != null)
                        {
                            var nextNode = tempRoute.nextRoute.node;
                            if (nextNode.nodePoint.IsEqualTo(graphNode.nodePoint, new Tolerance(1, 1)))
                            {
                                var exitDir = nextNode.nodePoint - pNode.nodePoint;
                                var dot = exitDir.DotProduct(vector.Value);
                                isAdd = dot < -0.1;
                                break;
                            }
                            tempRoute = tempRoute.nextRoute;
                            pNode = tempRoute.node;
                        }
                    }
                }
            }
            return isAdd;
        }
    }
}
