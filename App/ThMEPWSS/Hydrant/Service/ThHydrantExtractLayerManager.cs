using ThMEPEngineCore.GeojsonExtractor;

namespace ThMEPWSS.Hydrant.Service
{
    public class ThHydrantExtractLayerManager
    {
        public static ThAILayerManager Config()
        {
            return new ThAILayerManager()
            {
                ArchitectureWallLayer = "墙",
                ShearWallLayer = "墙",
                ColumnLayer = "柱",
                DoorOpeningLayer = "门",
                OuterBoundaryLayer = "AI-OuterBoundary",
            };
        }
    }
}
