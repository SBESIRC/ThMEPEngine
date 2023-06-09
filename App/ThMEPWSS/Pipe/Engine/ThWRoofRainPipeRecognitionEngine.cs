﻿using System;
using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Engine;
using ThMEPWSS.Pipe.Model;

namespace ThMEPWSS.Pipe.Engine
{
    public class ThWRoofRainPipeExtractionEngine : ThDistributionElementExtractionEngine
    {
        public override void Extract(Database database)
        {
            var visitor = new ThWRoofRainPipeExtractionVisitor();
            var extractor = new ThDistributionElementExtractor();
            extractor.Accept(visitor);
            extractor.Extract(database);
            Results = visitor.Results;
        }

        public override void ExtractFromMS(Database database)
        {
            throw new NotSupportedException();
        }

        public override void ExtractFromEditor(Point3dCollection frame)
        {
            throw new NotSupportedException();
        }
    }
    public class ThWRoofRainPipeRecognitionEngine : ThDistributionElementRecognitionEngine
    {
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            var engine = new ThWRoofRainPipeExtractionEngine();
            engine.Extract(database);
            var dbObjs = engine.Results.Select(o => o.Geometry).ToCollection();
            if (polygon.Count > 0)
            {
                ThCADCoreNTSSpatialIndex spatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
                dbObjs = spatialIndex.SelectCrossingPolygon(polygon);
            }
            Elements.AddRange(dbObjs.Cast<Entity>().Select(o => ThWRoofRainPipe.Create(o.GeometricExtents.ToRectangle())));
        }

        public override void RecognizeMS(Database database, Point3dCollection polygon)
        {
            throw new NotSupportedException();
        }

        public override void RecognizeEditor(Point3dCollection polygon)
        {
            throw new NotSupportedException();
        }
    }
}
