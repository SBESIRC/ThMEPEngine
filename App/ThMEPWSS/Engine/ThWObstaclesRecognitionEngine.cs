using System;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Service;

namespace ThMEPWSS.Engine
{
    public class ThWObstaclesRecognitionEngine : ThBuildingElementRecognitionEngine
    {
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            // 提取
            var extractor = new ThBuildingElementExtractor();
            var doorVisitor = new ThDoorExtractionVisitor();
            var windowVisitor = new ThWindowExtractionVisitor()
            {
                LayerFilter = ThWindowLayerManager.CurveXrefLayers(database),
            };
            var columnVisitor = new ThColumnExtractionVisitor()
            {
                LayerFilter = ThStructureColumnLayerManager.HatchXrefLayers(database),
            };
            var archWallVisitor = new ThArchitectureWallExtractionVisitor()
            {
                LayerFilter = ThArchitectureWallLayerManager.CurveXrefLayers(database),
            };
            var shearWallVisitor = new ThShearWallExtractionVisitor()
            {
                LayerFilter = ThStructureShearWallLayerManager.HatchXrefLayers(database),
            };
            extractor.Accept(doorVisitor);
            extractor.Accept(windowVisitor);
            extractor.Accept(columnVisitor);
            extractor.Accept(archWallVisitor);
            extractor.Accept(shearWallVisitor);
            extractor.Extract(database);

            // 识别
            var doorEngine = new ThDoorRecognitionEngine();
            doorEngine.Recognize(doorVisitor.Results, polygon);
            var windowEngine = new ThWindowRecognitionEngine();
            windowEngine.Recognize(windowVisitor.Results, polygon);
            var columnEngine = new ThColumnRecognitionEngine();
            columnEngine.Recognize(columnVisitor.Results, polygon);
            var archWallEngine = new ThArchitectureWallRecognitionEngine();
            archWallEngine.Recognize(archWallVisitor.Results, polygon);
            var shearWallEngine = new ThShearWallRecognitionEngine();
            shearWallEngine.Recognize(shearWallVisitor.Results, polygon);

            // 获取
            Elements.AddRange(doorEngine.Elements);
            Elements.AddRange(windowEngine.Elements);
            Elements.AddRange(columnEngine.Elements);
            Elements.AddRange(archWallEngine.Elements);
            Elements.AddRange(shearWallEngine.Elements);
        }

        public override void Recognize(List<ThRawIfcBuildingElementData> objs, Point3dCollection polygon)
        {
            throw new NotSupportedException();
        }
    }
}
