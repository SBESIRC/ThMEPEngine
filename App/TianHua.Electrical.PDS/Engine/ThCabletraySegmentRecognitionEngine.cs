using System;
using System.Linq;
using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using NFox.Cad;

using ThCADCore.NTS;
using ThMEPEngineCore.Engine;

namespace TianHua.Electrical.PDS.Engine
{
    public class ThCabletraySegmentRecognitionEngine : ThFlowSegmentRecognitionEngine
    {
        public ThCabletraySegmentRecognitionEngine()
        {
            Results = new DBObjectCollection();
        }

        public DBObjectCollection Results { get; protected set; }

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
            curves.OfType<Curve>().Where(o => o.Bounds.HasValue && o.GetLength() > 1.0).ForEach(o =>
            {
                if (o is Line || o is Arc || o is Polyline)
                {
                    Results.Add(o);
                }
                else
                {
                    var objs = new DBObjectCollection();
                    o.Explode(objs);
                    objs.OfType<Curve>().ForEach(e => Results.Add(e));
                }
            });
        }

        public override void RecognizeEditor(Point3dCollection polygon)
        {
            throw new NotImplementedException();
        }
    }
}
