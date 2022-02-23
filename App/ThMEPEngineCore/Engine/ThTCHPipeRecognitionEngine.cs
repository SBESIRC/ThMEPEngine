using System;
using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model.Hvac;

namespace ThMEPEngineCore.Engine
{
    public class ThTCHPipeRecognitionEngine : ThFlowSegmentRecognitionEngine
    {
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            throw new NotSupportedException();
        }

        public override void Recognize(List<ThRawIfcFlowSegmentData> datas, Point3dCollection polygon)
        {
            var collection = datas.Select(o => o.Geometry).ToCollection();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(collection);
            var pipes = spatialIndex.SelectCrossingPolygon(polygon);
            datas.Where(o => pipes.Contains(o.Geometry)).ForEach(o =>
            {
                Elements.Add(new ThIfcPipeSegment());
            });
        }

        public override void RecognizeMS(Database database, Point3dCollection polygon)
        {
            throw new NotSupportedException();
        }

        public override void RecognizeEditor(Point3dCollection polygon)
        {
            var engine = new ThTCHPipeExtractionEngine();
            engine.ExtractFromEditor(polygon);
            Recognize(engine.Results, polygon);
        }
    }
}
