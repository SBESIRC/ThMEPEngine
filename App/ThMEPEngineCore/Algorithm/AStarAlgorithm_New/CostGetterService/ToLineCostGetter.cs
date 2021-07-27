using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Algorithm.AStarAlgorithm_New.Model;

namespace ThMEPEngineCore.Algorithm.AStarAlgorithm_New.CostGetterService
{
    public class ToLineCostGetter : ICostGetter
    {
        public double GetGCost(AStarNode currentNode, Point3d nextPt)
        {
            double parentG = currentNode != null ? currentNode.CostG : 0;
            return parentG + currentNode.Location.DistanceTo(nextPt);
        }

        public double GetHCost(Point3d cell, Line endInfo)
        {
            return endInfo.GetClosestPointTo(cell, false).DistanceTo(cell);
        }
    }
}
