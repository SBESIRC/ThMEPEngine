using ThMEPEngineCore.Algorithm.AStarRoutingEngine.AStarModel;

namespace ThMEPEngineCore.Algorithm.AStarRoutingEngine.CostGetterService
{
    public interface ICostGetter
    {
        double GetGCost(AStarBaseNode currentNode, AStarEntity nextNode);

        double GetHCost(AStarEntity cell, AStarEntity endInfo);
    }
}
