using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using Dreambuild.AutoCAD;
using NFox.Cad;
using ThCADExtension;
using ThCADCore.NTS;
using ThMEPEngineCore.Model.Hvac;
using ThMEPEngineCore.Algorithm;

namespace ThMEPEngineCore.Engine
{
    public class ThBlockVPipeRecognitionEngine : ThFlowSegmentRecognitionEngine
    {
        public List<string> LayerFilter { get; set; } = new List<string>();

        public override void Recognize(Database database, Point3dCollection polygon)
        {
            throw new NotImplementedException();
        }

        public override void Recognize(List<ThRawIfcFlowSegmentData> datas, Point3dCollection polygon)
        {
            var collection = datas.Select(o => o.Geometry).ToCollection();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(collection);
            var pipes = spatialIndex.SelectCrossingPolygon(polygon);
            datas.Where(o => pipes.Contains(o.Geometry)).ForEach(o =>
            {
                Elements.Add(new ThIfcVirticalPipe()
                {
                    Data = (Entity)o.Data,
                    Outline = o.Geometry,
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
            var pipeExtractEngine = new ThBlockVPipeExtractionEngine()
            {
                LayerFilter = LayerFilter,
            };

            pipeExtractEngine.ExtractFromMS(database);
            //--转回原点
            var centerPt = polygon.Envelope().CenterPoint();
            var transformer = new ThMEPOriginTransformer(centerPt);
            var newFrame = transformer.Transform(polygon);
            pipeExtractEngine.Results.ForEach(x => transformer.Transform(x.Geometry));
            //--识别框内
            Recognize(pipeExtractEngine.Results, newFrame);
            //--转回原位置
            Elements.ForEach(x => transformer.Reset(x.Outline));
        }
    }
}
