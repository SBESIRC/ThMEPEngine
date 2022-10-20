using DotNetARX;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public class ThDB3BeamExtractionEngine : ThBuildingElementExtractionEngine
    {
        public override void Extract(Database database)
        {
            var visitor = new ThDB3BeamExtractionVisitor()
            {
                LayerFilter = ThBeamLayerManager.AnnotationXrefLayers(database).ToHashSet(),
            };
            var extractor = new ThBuildingElementExtractor();
            extractor.Accept(visitor);
            extractor.Extract(database);
            Results = visitor.Results;
        }

        public override void ExtractFromEditor(Point3dCollection frame)
        {
            throw new System.NotImplementedException();
        }

        public override void ExtractFromMS(Database database)
        {
            throw new System.NotImplementedException();
        }
    }

    public class ThDB3BeamRecognitionEngine : ThBuildingElementRecognitionEngine
    {
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            var engine = new ThDB3BeamExtractionEngine();
            engine.Extract(database);
            Recognize(engine.Results, polygon);
        }

        public override void Recognize(List<ThRawIfcBuildingElementData> datas, Point3dCollection polygon)
        {
            datas.ForEach(o => Elements.Add(ThIfcLineBeam.Create(o.Data as ThIfcBeamAnnotation)));
            Recognize(Elements, polygon);
        }
        public void Recognize(List<ThIfcBuildingElement> datas, Point3dCollection polygon)
        {
            if (polygon.Count > 0)
            {
                var dbObjs = new DBObjectCollection();
                datas.ForEach(o => dbObjs.Add(o.Outline));
                var beamSpatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
                var pline = new Polyline()
                {
                    Closed = true,
                };
                pline.CreatePolyline(polygon);
                var filterObjs = beamSpatialIndex.SelectCrossingPolygon(pline);
                Elements = datas.Where(o => filterObjs.Contains(o.Outline)).ToList();
            }
        }

        public override void RecognizeEditor(Point3dCollection polygon)
        {
            throw new System.NotImplementedException();
        }

        public override void RecognizeMS(Database database, Point3dCollection polygon)
        {
            throw new System.NotImplementedException();
        }
    }
}
