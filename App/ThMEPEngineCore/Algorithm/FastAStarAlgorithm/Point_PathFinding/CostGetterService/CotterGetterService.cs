using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Algorithm.FastAStarAlgorithm.AStarModel;
using ThMEPEngineCore.Algorithm.FastAStarAlgorithm.Point_PathFinding.Model;

namespace ThMEPEngineCore.Algorithm.FastAStarAlgorithm.Point_PathFinding.CostGetterService
{
    public class CotterGetterService
    {
        public double GetGCost(NodeModel currentNode, Point3d nextNode)
        {
            double parentG = currentNode != null ? currentNode.CostG : 0;
            return parentG + currentNode.Location.DistanceTo(nextNode);
        }

        public double GetHCost(Point3d currentNode, Point3d endNode)
        {
            return currentNode.DistanceTo(endNode);
        }
    }
}
