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
    public interface ICostGetter
    {
        double GetGCost(AStarNode currentNode, Point3d nextPt);

        double GetHCost(Point3d cell, Line endInfo);
    }
}
