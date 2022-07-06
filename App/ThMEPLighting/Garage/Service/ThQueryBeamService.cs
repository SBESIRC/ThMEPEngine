using NFox.Cad;
using System;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Algorithm;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.Garage.Service
{
    public class ThQueryBeamService:IDisposable
    {
        private List<ThIfcBeam> Beams { get; set; }
        private ThMEPOriginTransformer Transformer { get; set; }
        public ThQueryBeamService(Database database)
        {
            Extract(database);
            GetTransformer();
        }
        public void Dispose()
        {
            Beams
                .Where(o => o.Outline != null)
                .Select(o => o.Outline)
                .ToCollection()
                .ThDispose();
        }
        private void Extract(Database database)
        {
            Beams = new List<ThIfcBeam>();
            var db3Extractor = new ThDB3BeamExtractionEngine();
            db3Extractor.Extract(database);
            db3Extractor.Results.ForEach(o => Beams.Add(ThIfcLineBeam.Create(o.Data as ThIfcBeamAnnotation)));
        }
        private void GetTransformer()
        {
            Transformer = new ThMEPOriginTransformer(Beams.Select(o => o.Outline).ToCollection());
        }
        public List<ThIfcBeam> SelectCrossPolygon(Entity polygon)
        {
            var results = new List<ThIfcBeam>();
            var clone = polygon.Clone() as Entity;
            Transformer.Transform(clone);
            Beams.ForEach(b => Transformer.Transform(b.Outline));
            var spatialIndex = new ThCADCoreNTSSpatialIndex(Beams.Select(o=>o.Outline).ToCollection());
            var objs = spatialIndex.SelectCrossingPolygon(clone);
            results = Beams.Where(o => objs.Contains(o.Outline)).ToList();
            Beams.ForEach(b => Transformer.Reset(b.Outline));
            return results;
        }
    }
}
