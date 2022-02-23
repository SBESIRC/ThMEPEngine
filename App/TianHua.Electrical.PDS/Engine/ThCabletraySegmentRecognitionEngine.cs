using System;
using System.Linq;
using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;

using ThCADCore.NTS;
using ThMEPEngineCore.Engine;

namespace TianHua.Electrical.PDS.Engine
{
    public class ThCabletraySegmentRecognitionEngine : ThFlowSegmentRecognitionEngine
    {
        public List<Line> Results { get; protected set; }

        public override void Recognize(Database database, Point3dCollection polygon)
        {
            throw new NotImplementedException();
        }

        public override void RecognizeMS(Database database, Point3dCollection polygon)
        {
            var engine = new ThCabletraySegmentExtractionEngine();
            engine.ExtractFromMS(database);
            Recognize(engine.Results, polygon);
        }

        public override void Recognize(List<ThRawIfcFlowSegmentData> datas, Point3dCollection polygon)
        {
            var curves = datas.Select(data => data.Data as Curve).ToCollection();
            if (polygon.Count > 0)
            {
                var spatialIndex = new ThCADCoreNTSSpatialIndex(curves);
                curves = spatialIndex.SelectCrossingPolygon(polygon);
            }
            Results = curves.OfType<Line>().Where(o => o.GetLength() > 1.0).ToList();
        }

        public override void RecognizeEditor(Point3dCollection polygon)
        {
            throw new NotImplementedException();
        }
    }
}
