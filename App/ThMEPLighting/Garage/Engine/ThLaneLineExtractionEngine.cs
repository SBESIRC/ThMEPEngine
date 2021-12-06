using System;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Service;

namespace ThMEPLighting.Garage.Engine
{
    public class ThLaneLineExtractionEngine : ThSpatialElementExtractionEngine
    {
        public Func<Entity, bool> CheckQualifiedLayer { get; set; }
        public override void Extract(Database database)
        {
            var visitor = new ThLaneLineExtractionVisitor()
            {
                LayerFilter = ThLaneLineLayerManager.GeometryXrefLayers(database),
            };
            if (CheckQualifiedLayer != null)
            {
                visitor.CheckQualifiedLayer = this.CheckQualifiedLayer;
            }           
            var extractor = new ThSpatialElementExtractor();
            extractor.Accept(visitor);
            extractor.Extract(database);
            Results.AddRange(visitor.Results);
        }

        public override void ExtractFromMS(Database database)
        {
            var visitor = new ThLaneLineExtractionVisitor()
            {
                LayerFilter = ThLaneLineLayerManager.GeometryXrefLayers(database),
            };
            if (CheckQualifiedLayer != null)
            {
                visitor.CheckQualifiedLayer = this.CheckQualifiedLayer;
            }
            var extractor = new ThSpatialElementExtractor();
            extractor.Accept(visitor);
            extractor.ExtractFromMS(database);
            Results.AddRange(visitor.Results);
        }

        public override void ExtractFromMS(Database database, ObjectIdCollection dbObjs)
        {
            throw new NotImplementedException();
        }
    }
}
