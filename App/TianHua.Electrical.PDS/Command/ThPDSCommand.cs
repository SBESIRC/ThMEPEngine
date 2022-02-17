using System;
using System.Linq;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;

using ThMEPEngineCore.Command;
using TianHua.Electrical.PDS.Engine;
using TianHua.Electrical.PDS.Model;
using TianHua.Electrical.PDS.Service;

namespace TianHua.Electrical.PDS.Command
{
    public class ThPDSCommand : ThMEPBaseCommand, IDisposable
    {
        public override void SubExecute()
        {
            using (var acad = AcadDatabase.Active())
            {
                // 提取标注
                var markExtractor = new ThCircuitMarkExtractionEngine();
                markExtractor.ExtractFromMS(acad.Database);
                // 根据块名提取负载及标注块
                var loadExtractService = new ThPDSLoadExtractService();
                loadExtractService.Extract(acad.Database);

                // 提取配电箱
                //var distributionExtractService = new ThPDSDistributionExtractService();
                //distributionExtractService.Extract(acad.Database);
                //var distributionLayer = ThPDSLayerService.CreateAITestDistributionLayer(acad.Database);
                //distributionExtractService.Results.ForEach(o =>
                //{
                //    var blockReference = acad.Element<BlockReference>(o.ObjId, true);
                //    var rectangle = blockReference.GeometricExtents.ToRectangle();
                //    acad.ModelSpace.Add(rectangle);
                //    rectangle.LayerId = distributionLayer;
                //});


                // 提取回路
                var cableEngine = new ThCableSegmentRecognitionEngine();
                cableEngine.RecognizeMS(acad.Database, new Point3dCollection());
                //var cableLayer = ThPDSLayerService.CreateAITestCableLayer(acad.Database);
                //cableEngine.Results.OfType<Curve>().ForEach(o =>
                //{
                //    acad.ModelSpace.Add(o);
                //    o.LayerId = cableLayer;
                //});

                // 提取桥架
                var cabletrayEngine = new ThCabletraySegmentRecognitionEngine();
                cabletrayEngine.RecognizeMS(acad.Database, new Point3dCollection());
                //var cabletrayLayer = ThPDSLayerService.CreateAITestCabletrayLayer(acad.Database);
                //cabletrayEngine.Results.OfType<Curve>().ForEach(o =>
                //{
                //    acad.ModelSpace.Add(o);
                //    o.LayerId = cabletrayLayer;
                //});

                //做一个标注的Service

                ThPDSLoopGraphEngine graphEngine = new ThPDSLoopGraphEngine(acad.Database, loadExtractService.DistBoxBlocks,
                    loadExtractService.LoadBlocks, cabletrayEngine.Results.OfType<Line>().ToList(), cableEngine.Results.OfType<Curve>().ToList());
                graphEngine.CreatGraph();

                var graph = graphEngine.GetGraph();
            }
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
