using System.Linq;
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
        public ThRawBeamExtractionVisitor RawBeamVisitor { get; private set; }
        public ThBuildingElementVisitorManager(Database database)
        {
            #region ----------非DB3-----------
            ColumnVisitor = new ThColumnExtractionVisitor()
            {
                LayerFilter = ThStructureColumnLayerManager.HatchXrefLayers(database).ToHashSet(),
            };
            ShearWallVisitor = new ThShearWallExtractionVisitor()
            {
                LayerFilter = ThStructureShearWallLayerManager.HatchXrefLayers(database).ToHashSet(),
            };            
            AXISLineVisitor = new ThAXISLineExtractionVisitor();
            DrainageWellVisitor = new ThDrainageWellExtractionVisitor()
            {
                LayerFilter = ThDrainageWellLayerManager.CurveXrefLayers(database).ToHashSet(),
            };
            VStructuralElementVisitor = new ThVStructuralElementExtractionVisitor();
            RawBeamVisitor = CreateRawBeamVisitor(database);
            #endregion
            #region ----------DB3-----------
            DB3ArchWallVisitor = new ThDB3ArchWallExtractionVisitor()
            {
                LayerFilter = ThArchitectureWallLayerManager.CurveXrefLayers(database).ToHashSet(),
            };
            DB3PcArchWallVisitor = new ThDB3ArchWallExtractionVisitor()
            {
                LayerFilter = ThPCArchitectureWallLayerManager.CurveXrefLayers(database).ToHashSet(),
            };
            DB3BeamVisitor = new ThDB3BeamExtractionVisitor()
            {
                LayerFilter = ThBeamLayerManager.AnnotationXrefLayers(database).ToHashSet(),
            };
            DB3ShearWallVisitor = new ThDB3ShearWallExtractionVisitor()
            {
                LayerFilter = ThDbLayerManager.Layers(database).ToHashSet(),
            };
            DB3ColumnVisitor = new ThDB3ColumnExtractionVisitor()
            {
                LayerFilter = ThDbLayerManager.Layers(database).ToHashSet(),
            };
            DB3WindowVisitor = new ThDB3WindowExtractionVisitor()
            {
                LayerFilter = ThWindowLayerManager.CurveXrefLayers(database).ToHashSet(),
            };            
            DB3DoorMarkVisitor = new ThDB3DoorMarkExtractionVisitor()
            {
                LayerFilter = ThDoorMarkLayerManager.XrefLayers(database).ToHashSet(),
            };
            DB3DoorStoneVisitor = new ThDB3DoorStoneExtractionVisitor()
            {
                LayerFilter = ThDoorStoneLayerManager.XrefLayers(database).ToHashSet(),
            };
            DB3RailingVisitor = new ThDB3RailingExtractionVisitor()
            {
                LayerFilter = ThRailingLayerManager.CurveXrefLayers(database).ToHashSet(),
            };           
            DB3CorniceVisitor = new ThDB3CorniceExtractionVisitor()
            {
                LayerFilter = ThCorniceLayerManager.CurveXrefLayers(database).ToHashSet(),
            };
            DB3CurtainWallVisitor = new ThDB3CurtainWallExtractionVisitor()
            {
                LayerFilter = ThCurtainWallLayerManager.CurveXrefLayers(database).ToHashSet(),
            };
            DB3SlabVisitor = new ThDB3SlabExtractionVisitor()
            {
                LayerFilter = ThSlabLayerManager.CurveXrefLayers(database).ToHashSet(),
            };
            DB3StairVisitor = new ThDB3StairExtractionVisitor();
            #endregion
        }
        private ThRawBeamExtractionVisitor CreateRawBeamVisitor(Database database)
        {
            return new ThRawBeamExtractionVisitor()
            {
                LayerFilter = ThBeamLayerManager.GeometryXrefLayers(database).ToHashSet(),
            };
        }
    }
}
