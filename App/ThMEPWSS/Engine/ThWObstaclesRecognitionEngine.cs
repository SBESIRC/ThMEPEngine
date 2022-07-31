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
            var windowVisitor = new ThDB3WindowExtractionVisitor()
            {
                LayerFilter = ThWindowLayerManager.CurveXrefLayers(database),
            };
            var columnVisitor = new ThColumnExtractionVisitor()
            {
                LayerFilter = ThStructureColumnLayerManager.HatchXrefLayers(database),
            };
            var archWallVisitor = new ThDB3ArchWallExtractionVisitor()
            {
                LayerFilter = ThArchitectureWallLayerManager.CurveXrefLayers(database),
            };
            var shearWallVisitor = new ThShearWallExtractionVisitor()
            {
                LayerFilter = ThStructureShearWallLayerManager.HatchXrefLayers(database),
            };

            extractor.Accept(windowVisitor);
            extractor.Accept(columnVisitor);
            extractor.Accept(archWallVisitor);
            extractor.Accept(shearWallVisitor);
            extractor.Extract(database);

            // 识别
            var doorEngine = new ThDB3DoorRecognitionEngine();
            doorEngine.Recognize(database, polygon);

            var windowEngine = new ThDB3WindowRecognitionEngine();
            windowEngine.Recognize(windowVisitor.Results, polygon);
            var columnEngine = new ThColumnRecognitionEngine();
            columnEngine.Recognize(columnVisitor.Results, polygon);
            var archWallEngine = new ThDB3ArchWallRecognitionEngine();
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

        public override void RecognizeEditor(Point3dCollection polygon)
        {
            throw new NotImplementedException();
        }

        public override void RecognizeMS(Database database, Point3dCollection polygon)
        {
            throw new NotImplementedException();
        }
    }
}
