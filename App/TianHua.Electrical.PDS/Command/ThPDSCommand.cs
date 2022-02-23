using System;
using System.IO;
using System.Collections.Generic;

using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using QuickGraph;

using ThCADExtension;
using ThMEPEngineCore.Command;
using TianHua.Electrical.PDS.Engine;
using TianHua.Electrical.PDS.Model;
using TianHua.Electrical.PDS.Service;

namespace TianHua.Electrical.PDS.Command
{
    public class ThPDSCommand : ThMEPBaseCommand, IDisposable
    {
        readonly static string LoadConfigUrl = Path.Combine(ThCADCommon.SupportPath(), "平面关注对象.xlsx");

        /// <summary>
        /// 配电箱序列
        /// </summary>
        public List<int> DistBoxFilter = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8 };

        public override void SubExecute()
        {
            using (var acad = AcadDatabase.Active())
            {
                // 读取配置表信息
                var fileService = new ThConfigurationFileService();
                var tableInfo = fileService.Acquire(LoadConfigUrl);
                var distBoxKey = new List<string>();
                var nameFilter = new List<string>();
                var propertyFilter = new List<string>();
                var tableAnalysis = new ThPDSTableAnalysisService();
                tableAnalysis.Analysis(tableInfo, DistBoxFilter, ref nameFilter, ref propertyFilter, ref distBoxKey);

                // 提取标注
                var markExtractor = new ThCircuitMarkExtractionEngine();
                markExtractor.ExtractFromMS(acad.Database);
                // 根据块名提取负载及标注块
                var loadExtractService = new ThPDSBlockExtractService();
                loadExtractService.Extract(acad.Database, nameFilter, propertyFilter, DistBoxFilter);

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
                var markService = new ThMarkService(markExtractor.Results, loadExtractService.MarkBlocks);
                var graphEngine = new ThPDSLoopGraphEngine(acad.Database, loadExtractService.DistBoxBlocks,
                    loadExtractService.LoadBlocks, cabletrayEngine.Results, cableEngine.Results, markService, distBoxKey);
                graphEngine.CreatGraph();
                var graph = graphEngine.GetGraph();

                var graphList = new List<AdjacencyGraph<ThPDSCircuitGraphNode, ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>>>
                {
                    graph,
                };
                var unionEngine = new ThPDSGraphUnionEngine(distBoxKey);
                var unionGrapg = unionEngine.GraphUnion(graphList, graphEngine.CabletrayNode);
            }
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
