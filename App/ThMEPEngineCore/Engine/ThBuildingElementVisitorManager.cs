using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public class ThBuildingElementVisitorManager
    {
        public ThDB3ArchWallExtractionVisitor DB3ArchWallVisitor { get; private set; }
        public ThDB3ArchWallExtractionVisitor DB3PcArchWallVisitor { get; private set; }
        public ThShearWallExtractionVisitor ShearWallVisitor { get; private set; }
        public ThDB3ShearWallExtractionVisitor DB3ShearWallVisitor { get; private set; }
        public ThDB3ColumnExtractionVisitor DB3ColumnVisitor { get;private set; }
        public ThDB3WindowExtractionVisitor DB3WindowVisitor { get; private set; }
        public ThDB3BeamExtractionVisitor DB3BeamVisitor { get; private set; }
        public ThDB3DoorMarkExtractionVisitor DB3DoorMarkVisitor { get; private set; }
        public ThDB3DoorStoneExtractionVisitor DB3DoorStoneVisitor { get; private set; }
        public ThDB3RailingExtractionVisitor DB3RailingVisitor { get; private set; }
        public ThAXISLineExtractionVisitor AXISLineVisitor { get; private set; }
        public ThColumnExtractionVisitor ColumnVisitor { get; private set; }
        public ThDB3CorniceExtractionVisitor DB3CorniceVisitor { get; private set; }
        public ThDB3CurtainWallExtractionVisitor DB3CurtainWallVisitor { get; private set; }
        public ThDB3SlabExtractionVisitor DB3SlabVisitor { get; private set; }
        public ThDB3StairExtractionVisitor DB3StairVisitor { get; private set; }
        public ThDrainageWellExtractionVisitor DrainageWellVisitor { get; private set; }
        public ThVStructuralElementExtractionVisitor VStructuralElementVisitor { get; private set; }
        public ThBuildingElementVisitorManager(Database database)
        {
            #region ----------非DB3-----------
            ColumnVisitor = new ThColumnExtractionVisitor()
            {
                LayerFilter = ThStructureColumnLayerManager.HatchXrefLayers(database),
            };
            ShearWallVisitor = new ThShearWallExtractionVisitor()
            {
                LayerFilter = ThStructureShearWallLayerManager.HatchXrefLayers(database),
            };
            DB3BeamVisitor = new ThDB3BeamExtractionVisitor()
            {
                LayerFilter = ThBeamLayerManager.AnnotationXrefLayers(database),
            };
            AXISLineVisitor = new ThAXISLineExtractionVisitor();
            DrainageWellVisitor = new ThDrainageWellExtractionVisitor()
            {
                LayerFilter = ThDrainageWellLayerManager.CurveXrefLayers(database),
            };
            VStructuralElementVisitor = new ThVStructuralElementExtractionVisitor();
            #endregion
            #region ----------DB3-----------
            DB3ArchWallVisitor = new ThDB3ArchWallExtractionVisitor()
            {
                LayerFilter = ThArchitectureWallLayerManager.CurveXrefLayers(database),
            };
            DB3PcArchWallVisitor = new ThDB3ArchWallExtractionVisitor()
            {
                LayerFilter = ThPCArchitectureWallLayerManager.CurveXrefLayers(database),
            };            
            DB3ShearWallVisitor = new ThDB3ShearWallExtractionVisitor();
            DB3ColumnVisitor = new ThDB3ColumnExtractionVisitor();
            DB3WindowVisitor = new ThDB3WindowExtractionVisitor()
            {
                LayerFilter = ThWindowLayerManager.CurveXrefLayers(database),
            };            
            DB3DoorMarkVisitor = new ThDB3DoorMarkExtractionVisitor()
            {
                LayerFilter = ThDoorMarkLayerManager.XrefLayers(database),
            };
            DB3DoorStoneVisitor = new ThDB3DoorStoneExtractionVisitor()
            {
                LayerFilter = ThDoorStoneLayerManager.XrefLayers(database),
            };
            DB3RailingVisitor = new ThDB3RailingExtractionVisitor()
            {
                LayerFilter = ThRailingLayerManager.CurveXrefLayers(database),
            };           
            DB3CorniceVisitor = new ThDB3CorniceExtractionVisitor()
            {
                LayerFilter = ThCorniceLayerManager.CurveXrefLayers(database),
            };
            DB3CurtainWallVisitor = new ThDB3CurtainWallExtractionVisitor()
            {
                LayerFilter = ThCurtainWallLayerManager.CurveXrefLayers(database),
            };
            DB3SlabVisitor = new ThDB3SlabExtractionVisitor()
            {
                LayerFilter = ThSlabLayerManager.CurveXrefLayers(database),
            };
            DB3StairVisitor = new ThDB3StairExtractionVisitor();
            #endregion
        }
    }
}
