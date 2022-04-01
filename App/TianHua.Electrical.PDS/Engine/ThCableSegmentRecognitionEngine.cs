using System;
using System.Linq;
using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;

using ThCADCore.NTS;
using ThMEPEngineCore.Engine;
using TianHua.Electrical.PDS.Model;

namespace TianHua.Electrical.PDS.Engine
{
    public class ThCableSegmentRecognitionEngine : ThFlowSegmentRecognitionEngine
    {
        public ThCableSegmentRecognitionEngine()
        {
            Results = new List<ThPDSEntityInfo>();
        }

        public List<ThPDSEntityInfo> Results { get; protected set; }
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            throw new NotImplementedException();
        }

        public override void RecognizeMS(Database database, Point3dCollection polygon)
        {
            var engine = new ThCableSegmentExtractionEngine();
            engine.ExtractFromMS(database);
            Recognize(engine.Results, polygon);
        }

        public override void Recognize(List<ThRawIfcFlowSegmentData> datas, Point3dCollection polygon)
        {
            var entityInfos = datas.Select(data => data.Data as Curve)
                .Where(o => o.GetLength() > 1.0)
                .Select(data => new ThPDSEntityInfo(data, true)).ToList();
            var curves = entityInfos.Select(e => e.Entity).ToCollection();
            if (polygon.Count > 0)
            {
                var spatialIndex = new ThCADCoreNTSSpatialIndex(curves);
                curves = spatialIndex.SelectCrossingPolygon(polygon);
            }
            entityInfos.ForEach(e =>
            {
                if(curves.Contains(e.Entity))
                {
                    Results.Add(e);
                }
            });
        }

        public override void RecognizeEditor(Point3dCollection polygon)
        {
            throw new NotImplementedException();
        }
    }
}
