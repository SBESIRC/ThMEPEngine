using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPLighting.FEI.AStarAlgorithm.AStarModel;

namespace ThMEPLighting.FEI.AStarAlgorithm.CostGetterService
{
    public interface ICostGetter
    {
        int GetGCost(AStarNode currentNode, CompassDirections moveDirection);

        int GetHCost(Point cell, EndModel endInfo);
    }
}
