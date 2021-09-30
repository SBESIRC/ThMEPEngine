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
    public class ThWindowBuilderEngine : ThBuildingElementBuilder, IDisposable
    {
        public ThWindowBuilderEngine() { }
        public void Dispose()
        {
        }

        public override List<ThRawIfcBuildingElementData> Extract(Database db)
        {
            var res = new List<ThRawIfcBuildingElementData>();
            var windowExtractor = new ThDB3WindowExtractionEngine();
            windowExtractor.Extract(db);
            windowExtractor.Results.ForEach(e => res.Add(new ThRawIfcBuildingElementData()
            {
                Geometry = e.Geometry,
                Source = DataSource.DB3
            }));
            return res;
        }

        public override List<ThIfcBuildingElement> Recognize(List<ThRawIfcBuildingElementData> datas, Point3dCollection pts)
        {
            var res = new List<ThIfcBuildingElement>();
            var windowRecognize = new ThDB3WindowRecognitionEngine();
            windowRecognize.Recognize(datas.Where(o => o.Source == DataSource.DB3).ToList(), pts);
            res.AddRange(windowRecognize.Elements);
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
            var windowlist = Recognize(rawelement, newPts);
            var windowcollection = windowlist.Select(o => o.Outline).ToCollection();
            transformer.Reset(windowcollection);
            Elements = windowcollection.OfType<Polyline>()
                .Select(e => ThIfcWindow.Create(e))
                .OfType<ThIfcBuildingElement>()
                .ToList();
        }
    }
}
