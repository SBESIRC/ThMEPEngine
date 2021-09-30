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

        public override void Build(Database db, Point3dCollection pts)
        {
            var rawElement = Extract(db);
            var center = pts.Envelope().CenterPoint();
            var transFormer = new ThMEPOriginTransformer(center);
            rawElement.ForEach(o => transFormer.Transform(o.Geometry));
            var newPts = pts
                .OfType<Point3d>()
                .Select(o => transFormer.Transform(o))
                .ToCollection();
            var stairList = Recognize(rawElement, newPts);
            var stairCollection = stairList.Select(o => o.Outline).ToCollection();
            transFormer.Reset(stairCollection);
            Elements = stairCollection
                .OfType<Polyline>()
                .Select(e => ThIfcStair.Create(e))
                .OfType<ThIfcBuildingElement>()
                .ToList();
        }
    }
}
