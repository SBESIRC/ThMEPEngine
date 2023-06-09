﻿using System;
using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.Algorithm;

namespace ThMEPEngineCore.Engine
{
    public class ThParkingStallExtractionEngine : ThSpatialElementExtractionEngine
    {
        public Func<Entity, bool> CheckQualifiedLayer { get; set; }
        public Func<Entity, bool> CheckQualifiedBlockName { get; set; }

        public override void Extract(Database database)
        {
            var visitor = new ThParkingStallExtractionVisitor()
            {
                LayerFilter = ThParkingStallLayerManager.XrefLayers(database),
            };
            if (CheckQualifiedLayer != null)
            {
                visitor.CheckQualifiedLayer = this.CheckQualifiedLayer;
            }
            if (CheckQualifiedBlockName != null)
            {
                visitor.CheckQualifiedBlockName = this.CheckQualifiedBlockName;
            }
            var extractor = new ThSpatialElementExtractor();
            extractor.Accept(visitor);
            extractor.Extract(database);
            Results.AddRange(visitor.Results);
        }

        public override void ExtractFromMS(Database database)
        {
            var visitor = new ThParkingStallExtractionVisitor()
            {
                LayerFilter = ThParkingStallLayerManager.XrefLayers(database),
            };
            if (CheckQualifiedLayer != null)
            {
                visitor.CheckQualifiedLayer = this.CheckQualifiedLayer;
            }
            if (CheckQualifiedBlockName != null)
            {
                visitor.CheckQualifiedBlockName = this.CheckQualifiedBlockName;
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
    public class ThParkingStallRecognitionEngine : ThSpatialElementRecognitionEngine
    {
        public Func<Entity, bool> CheckQualifiedLayer { get; set; }
        public Func<Entity, bool> CheckQualifiedBlockName { get; set; }

        public override void Recognize(Database database, Point3dCollection polygon)
        {
            var engine = new ThParkingStallExtractionEngine()
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
            var center = polygon.Envelope().CenterPoint();
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
                    Elements.Add(ThIfcParkingStall.Create(polyline));
                }
            });
        }
    }
}
