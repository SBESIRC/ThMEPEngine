using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Algorithm.AStarAlgorithm.AStarModel;

namespace ThMEPEngineCore.Algorithm.AStarAlgorithm.CostGetterService
{
    public interface ICostGetter
    {
        int GetGCost(AStarNode currentNode, CompassDirections moveDirection);

        int GetHCost(Point cell, AStarEntity endInfo);
    }
}
