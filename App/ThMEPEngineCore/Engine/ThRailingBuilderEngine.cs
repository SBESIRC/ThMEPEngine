using System;
using NFox.Cad;
using System.Linq;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Algorithm;

namespace ThMEPEngineCore.Engine
{
    public class ThRailingBuilderEngine : ThBuildingElementBuilder, IDisposable
    {
        public ThRailingBuilderEngine() { }
        public void Dispose()
        {
        }

        public override List<ThRawIfcBuildingElementData> Extract(Database db)
        {
            var res = new List<ThRawIfcBuildingElementData>();
            var railingExtractor = new ThDB3RailingExtractionEngine();
            railingExtractor.Extract(db);
            railingExtractor.Results.ForEach(e => res.Add(new ThRawIfcBuildingElementData()
            {
                Geometry = e.Geometry,
                Source = DataSource.DB3
            }));
            return res;
        }

        public override List<ThIfcBuildingElement> Recognize(List<ThRawIfcBuildingElementData> datas, Point3dCollection pts)
        {
            var res = new List<ThIfcBuildingElement>();
            var railingRecognize = new ThDB3RailingRecognitionEngine();
            railingRecognize.Recognize(datas.Where(o => o.Source == DataSource.DB3).ToList(), pts);
            res.AddRange(railingRecognize.Elements);
            return res;
        }
        public override void Build(Database db, Point3dCollection pts)
        {
            var rawelement = Extract(db);
            var center = pts.Envelope().CenterPoint();
            var transformer = new ThMEPOriginTransformer(center);
            rawelement.ForEach(o => transformer.Transform(o.Geometry));
            var newPts = pts
                .OfType<Point3d>()
                .Select(o => transformer.Transform(o))
                .ToCollection();
            var railingllist = Recognize(rawelement, newPts);
            var railingcollection = railingllist.Select(o => o.Outline).ToCollection();
            transformer.Reset(railingcollection);
            Elements = railingcollection
                .OfType<Polyline>()
                .Select(e => ThIfcRailing.Create(e))
                .OfType<ThIfcBuildingElement>()
                .ToList();
        }
    }
}
