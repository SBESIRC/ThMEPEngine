using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using Dreambuild.AutoCAD;
using NFox.Cad;
using ThCADExtension;
using ThCADCore.NTS;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Algorithm;

namespace ThMEPWSS.HydrantLayout.Data
{
    public class ThHydrantExtractionEngine : ThDistributionElementExtractionEngine
    {
        private ThDistributionElementExtractionVisitor Visitor { get; set; }

        public ThHydrantExtractionEngine(ThDistributionElementExtractionVisitor visitor)
        {
            Visitor = visitor;
        }
        public override void Extract(Database database)
        {
            throw new System.NotImplementedException();
        }
        public override void ExtractFromEditor(Point3dCollection frame)
        {
            throw new System.NotImplementedException();
        }
        public override void ExtractFromMS(Database database)
        {
            var extractor = new ThDistributionElementExtractor();
            extractor.Accept(Visitor);
            extractor.ExtractFromMS(database);
            Results.AddRange(Visitor.Results);
        }
    }
    public class ThHydrantRecognitionEngine : ThDistributionElementRecognitionEngine
    {
        private ThDistributionElementExtractionVisitor Visitor;
        public ThHydrantRecognitionEngine(ThDistributionElementExtractionVisitor visitor)
        {
            Visitor = visitor;
        }
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            throw new NotImplementedException();
        }
        public override void Recognize(List<ThRawIfcDistributionElementData> datas, Point3dCollection polygon)
        {
            var collection = datas.Select(o => o.Geometry).ToCollection();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(collection);
            var pipes = spatialIndex.SelectCrossingPolygon(polygon);
            datas.Where(o => pipes.Contains(o.Geometry)).ForEach(o =>
            {
                Elements.Add(new ThIfcDistributionFlowElement()
                {
                    Outline = (BlockReference)o.Data,
                });
            });
        }
        public override void RecognizeEditor(Point3dCollection polygon)
        {
            throw new NotImplementedException();
        }
        public override void RecognizeMS(Database database, Point3dCollection polygon)
        {
            //---提取
            var extractEngine = new ThHydrantExtractionEngine(Visitor);
            extractEngine.ExtractFromMS(database);
            //--转回原点
            var centerPt = polygon.Envelope().CenterPoint();
            var transformer = new ThMEPOriginTransformer(centerPt);
            var newFrame = transformer.Transform(polygon);
            extractEngine.Results.ForEach(x => transformer.Transform(x.Geometry));
            //--识别框内
            Recognize(extractEngine.Results, newFrame);
            //--转回原位置
            //Elements.ForEach(x => transformer.Reset(x.Outline));
        }
    }
}
