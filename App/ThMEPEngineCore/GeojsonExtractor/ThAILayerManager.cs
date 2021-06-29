namespace ThMEPEngineCore.GeojsonExtractor
{
    public class ThAILayerManager
    {
        public string ShearWallLayer { get; set; }
        public string ArchitectureWallLayer { get; set; }
        public string ColumnLayer { get; set; }
        public string WindowLayer { get; set; }
        public string BeamLayer { get; set; }
        public string DoorLayer { get; set; }
        public string DoorOpeningLayer { get; set; }
        public string OuterBoundaryLayer { get; set; }
        public string ArchtectureOutlineLayer { get; set; }
        public ThAILayerManager()
        {
            ShearWallLayer = "AI-Wall";
            ArchitectureWallLayer = "AI-Wall";
            ColumnLayer = "AI-Column";
            WindowLayer = "AI-Window";
            BeamLayer = "AI-Beam";
            DoorLayer = "AI-Door";
            DoorOpeningLayer = "AI-DoorOpening";
            OuterBoundaryLayer = "AI-OuterBoundary";
            ArchtectureOutlineLayer = "AI-ArchtectureOutline";
        }
    }
}
