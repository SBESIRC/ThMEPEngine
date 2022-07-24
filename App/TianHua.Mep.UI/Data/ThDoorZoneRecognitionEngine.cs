using System;
using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.Algorithm;

namespace TianHua.Mep.UI.Data
{
    public class ThDoorZoneExtractionEngine : ThSpatialElementExtractionEngine
    {
        public Func<Entity, bool> CheckQualifiedLayer { get; set; }
        public Func<Entity, bool> CheckQualifiedBlockName { get; set; }
        public override void Extract(Database database)
        {
            var doorZoneVisitor = new ThDoorZoneExtractionVisitor()
            {
                LayerFilter = ThDbLayerManager.Layers(database),
            };
            if (CheckQualifiedLayer != null)
            {
                doorZoneVisitor.CheckQualifiedLayer = this.CheckQualifiedLayer;
            }            
            if (CheckQualifiedBlockName != null)
            {
                doorZoneVisitor.CheckQualifiedBlockName = this.CheckQualifiedBlockName;
            }
            var extractor = new ThSpatialElementExtractor();
            extractor.Accept(doorZoneVisitor);
            extractor.Extract(database);
            Results.AddRange(doorZoneVisitor.Results);
        }

        public override void ExtractFromMS(Database database)
        {
            var doorZoneVisitor = new ThDoorZoneExtractionVisitor();
            if (CheckQualifiedLayer != null)
            {
                doorZoneVisitor.CheckQualifiedLayer = this.CheckQualifiedLayer;
            }
            if (CheckQualifiedBlockName != null)
            {
                doorZoneVisitor.CheckQualifiedBlockName = this.CheckQualifiedBlockName;
            }
            var extractor = new ThSpatialElementExtractor();
            extractor.Accept(doorZoneVisitor);
            extractor.ExtractFromMS(database);
            Results.AddRange(doorZoneVisitor.Results);
        }

        public override void ExtractFromMS(Database database, ObjectIdCollection dbObjs)
        {
            throw new NotImplementedException();
        }
    }
    public class ThDoorZoneRecognitionEngine : ThSpatialElementRecognitionEngine
    {
        public Func<Entity, bool> CheckQualifiedLayer { get; set; }
        public Func<Entity, bool> CheckQualifiedBlockName { get; set; }
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            var engine = new ThDoorZoneExtractionEngine()
            {
                CheckQualifiedLayer = this.CheckQualifiedLayer,
                CheckQualifiedBlockName = this.CheckQualifiedBlockName,
            };
            engine.Extract(database);
            Recognize(engine.Results, polygon);
        }
        public override void RecognizeMS(Database database, Point3dCollection polygon)
        {
            var engine = new ThParkingStallExtractionEngine()
            {
                CheckQualifiedLayer = this.CheckQualifiedLayer,
                CheckQualifiedBlockName = this.CheckQualifiedBlockName,
            };
            engine.ExtractFromMS(database);
            Recognize(engine.Results, polygon);
        }

        public override void RecognizeMS(Database database, ObjectIdCollection dbObjs)
        {
            throw new NotImplementedException();
        }

        public override void Recognize(List<ThRawIfcSpatialElementData> datas, Point3dCollection polygon)
        {
            var results = new List<ThRawIfcSpatialElementData>();
            var objs = datas.Select(o => o.Geometry).ToCollection();
            var center = polygon.Count>0 ? polygon.Envelope().CenterPoint() : objs.GeometricExtents().CenterPoint();
            var transformer = new ThMEPOriginTransformer(center);
            transformer.Transform(objs);
            var newPts = transformer.Transform(polygon);
            if (newPts.Count > 0)
            {
                var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                objs = spatialIndex.SelectCrossingPolygon(newPts);
            }
            transformer.Reset(objs);
            objs.Cast<Entity>().ForEach(o =>
            {
                if (o is Polyline polyline && polyline.Area > 0.0)
                {
                    Elements.Add(new ThIfcSpace { Boundary = polyline });
                }
            });
        }
    }
}
