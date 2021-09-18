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
    public class ThStairBuilderEngine : ThBuildingElementBuilder, IDisposable
    {
        public ThStairBuilderEngine() { }
        public void Dispose()
        {
        }

        public override List<ThRawIfcBuildingElementData> Extract(Database db)
        {
            var res = new List<ThRawIfcBuildingElementData>();
            var stairExtractor = new ThDB3StairExtractionEngine();
            stairExtractor.Extract(db);
            stairExtractor.Results.ForEach(e => res.Add(new ThRawIfcBuildingElementData()
            {
                Geometry = e.Geometry,
                Source = DataSource.DB3
            }));
            return res;
        }

        public override List<ThIfcBuildingElement> Recognize(List<ThRawIfcBuildingElementData> datas, Point3dCollection pts)
        {
            var res = new List<ThIfcBuildingElement>();
            var stairRecognize = new ThDB3StairRecognitionEngine();
            stairRecognize.Recognize(datas.Where(o => o.Source == DataSource.DB3).ToList(), pts);
            res.AddRange(stairRecognize.Elements);
            return res;
        }

        public override List<ThIfcBuildingElement> Build(Database db, Point3dCollection pts)
        {
            var rawElement = Extract(db);
            var center = pts.Envelope().CenterPoint();
            var transFormer = new ThMEPOriginTransformer(center);
            rawElement.ForEach(o => transFormer.Transform(o.Geometry));
            var newPts = pts.OfType<Point3d>()
                .Select(o => transFormer.Transform(o))
                .ToCollection();
            var stairList = Recognize(rawElement, newPts);
            var stairCollection = stairList.Select(o => o.Outline).ToCollection();
            transFormer.Reset(stairCollection);
            return stairCollection.Cast<Polyline>().Select(e => ThIfcStair.Create(e)).Cast<ThIfcBuildingElement>().ToList();
        }

    }
}
