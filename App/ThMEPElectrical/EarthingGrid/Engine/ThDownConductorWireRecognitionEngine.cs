using System;
using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Engine;
using ThMEPElectrical.EarthingGrid.Model;

namespace ThMEPElectrical.EarthingGrid.Engine
{
    public class ThDownConductorWireRecognitionEngine : ThFlowSegmentRecognitionEngine
    {
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            throw new NotImplementedException();
        }

        public override void RecognizeEditor(Point3dCollection polygon)
        {
            var engine = new ThDownConductorWireExtractionEngine();
            engine.ExtractFromEditor(polygon);
            Recognize(engine.Results, polygon);
        }

        public override void RecognizeMS(Database database, Point3dCollection polygon)
        {
            throw new NotImplementedException();
        }

        public override void Recognize(List<ThRawIfcFlowSegmentData> datas, Point3dCollection polygon)
        {
            var curves = new DBObjectCollection();
            var objs = datas.Select(o => o.Geometry).ToCollection();
            if (polygon.Count > 0)
            {
                var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                curves = spatialIndex.SelectCrossingPolygon(polygon);
            }
            else
            {
                curves = objs;
            }
            curves.OfType<Entity>().ForEach(e => Elements.Add(new ThDownConductorWire()
            {
                Outline = e,
            }));
        }
    }
}
