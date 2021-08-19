using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Model;

namespace ThMEPEngineCore.Engine
{
    public class ThCornicesBuilderEngine : ThBuildingElementBuilder, IDisposable
    {
        public ThCornicesBuilderEngine() { }
        public void Dispose()
        {
        }

        public override List<ThRawIfcBuildingElementData> Extract(Database db)
        {
            var res = new List<ThRawIfcBuildingElementData>();
            var cornicesExtractor = new ThDB3CorniceExtractionEngine();
            cornicesExtractor.Extract(db);
            cornicesExtractor.Results.ForEach(e => res.Add(new ThRawIfcBuildingElementData()
            {
                Geometry = e.Geometry,
                Source = DataSource.DB3
            }));
            return res;
        }

        public override List<ThIfcBuildingElement> Recognize(List<ThRawIfcBuildingElementData> datas, Point3dCollection pts)
        {
            var res = new List<ThIfcBuildingElement>();
            var cornicesRecognize = new ThDB3CorniceRecognitionEngine();
            cornicesRecognize.Recognize(datas.Where(o => o.Source == DataSource.DB3).ToList(), pts);
            res.AddRange(cornicesRecognize.Elements);
            return res;
        }
        public override List<ThIfcBuildingElement> Build(Database db, Point3dCollection pts)
        {
            var rawelement = Extract(db);
            var center = pts.Envelope().CenterPoint();
            var transformer = new ThMEPOriginTransformer(center);
            rawelement.ForEach(o => transformer.Transform(o.Geometry));
            var newPts = pts.OfType<Point3d>()
                .Select(o => transformer.Transform(o))
                .ToCollection();
            var cornicesllist = Recognize(rawelement, newPts);
            var cornicescollection = cornicesllist.Select(o => o.Outline).ToCollection();
            transformer.Reset(cornicescollection);
            return cornicescollection.Cast<Polyline>().Select(e => ThIfcCornice.Create(e)).Cast<ThIfcBuildingElement>().ToList();
        }

    }
}
