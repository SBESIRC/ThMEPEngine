using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using QuickGraph;

using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Command;
using TianHua.Electrical.PDS.Engine;
using TianHua.Electrical.PDS.Model;
using TianHua.Electrical.PDS.Service;
using TianHua.Electrical.PDS.Project;

namespace TianHua.Electrical.PDS.Command
{
    public class ThPDSCommand : ThMEPBaseCommand, IDisposable
    {
        readonly static string LoadConfigUrl = Path.Combine(ThCADCommon.SupportPath(), "平面关注对象.xlsx");

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
                tableAnalysis.Analysis(tableInfo, ref nameFilter, ref propertyFilter, ref distBoxKey);

                // 提取回路
                var cableEngine = new ThCableSegmentRecognitionEngine();
                cableEngine.RecognizeMS(acad.Database, new Point3dCollection());

                // 创建移动到原点的类
                var transformerPt = new Point3d();
                if (cableEngine.Results.Count > 0)
                {
                    transformerPt = cableEngine.Results[0].StartPoint;
                }
                var transformer = new ThMEPOriginTransformer(transformerPt);

                cableEngine.Results.ForEach(o =>
                {
                    transformer.Transform(o);
                    ThMEPEntityExtension.ProjectOntoXYPlane(o);
                });

                // 提取桥架
                var cableTrayEngine = new ThCabletraySegmentRecognitionEngine();
                cableTrayEngine.RecognizeMS(acad.Database, new Point3dCollection());
                cableTrayEngine.Results.ForEach(o =>
                {
                    transformer.Transform(o);
                    ThMEPEntityExtension.ProjectOntoXYPlane(o);
                });

                // 提取标注
                var markExtractor = new ThCircuitMarkRecognitionEngine();
                markExtractor.RecognizeMS(acad.Database, new Point3dCollection());
                markExtractor.Results.ForEach(o =>
                {
                    transformer.Transform(o);
                    ThMEPEntityExtension.ProjectOntoXYPlane(o);
                });
                var tchWireDimExtractor = new ThTCHWireDim2RecognitionEngine();
                tchWireDimExtractor.RecognizeMS(acad.Database, new Point3dCollection());
                tchWireDimExtractor.Results.ForEach(o =>
                {
                    transformer.Transform(o);
                    ThMEPEntityExtension.ProjectOntoXYPlane(o);
                });

                // 根据块名提取负载及标注块
                var loadExtractService = new ThPDSBlockExtractService();
                loadExtractService.Extract(acad.Database, tableInfo, nameFilter, propertyFilter, distBoxKey);
                loadExtractService.MarkBlocks.ForEach(o =>
                {
                    var block = acad.Element<BlockReference>(o.Value.ObjId, true);
                    transformer.Transform(block);
                    ThMEPEntityExtension.ProjectOntoXYPlane(block);
                });
                loadExtractService.DistBoxBlocks.ForEach(o =>
                {
                    var block = acad.Element<BlockReference>(o.Value.ObjId, true);
                    transformer.Transform(block);
                    ThMEPEntityExtension.ProjectOntoXYPlane(block);
                });
                loadExtractService.LoadBlocks.ForEach(o =>
                {
                    var block = acad.Element<BlockReference>(o.Value.ObjId, true);
                    transformer.Transform(block);
                    ThMEPEntityExtension.ProjectOntoXYPlane(block);
                });

                //做一个标注的Service
                var markService = new ThMarkService(markExtractor.Results, loadExtractService.MarkBlocks, tchWireDimExtractor.Results);

                ThPDSGraphService.DistBoxBlocks = loadExtractService.DistBoxBlocks;
                ThPDSGraphService.LoadBlocks = loadExtractService.LoadBlocks;
                var graphEngine = new ThPDSLoopGraphEngine(acad.Database, loadExtractService.DistBoxBlocks.Keys.ToList(),
                    loadExtractService.LoadBlocks.Keys.ToList(), cableTrayEngine.Results, cableEngine.Results, markService, distBoxKey);
                graphEngine.CreatGraph();
                graphEngine.CopyAttributes();
                var graph = graphEngine.GetGraph();

                var graphList = new List<AdjacencyGraph<ThPDSCircuitGraphNode, ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>>>
                {
                    graph,
                };
                var unionEngine = new ThPDSGraphUnionEngine(distBoxKey);
                var unionGraph = unionEngine.GraphUnion(graphList, graphEngine.CabletrayNode);

                // 移动回原位
                loadExtractService.MarkBlocks.ForEach(o =>
                {
                    var block = acad.Element<BlockReference>(o.Value.ObjId, true);
                    transformer.Reset(block);
                });
                loadExtractService.DistBoxBlocks.ForEach(o =>
                {
                    var block = acad.Element<BlockReference>(o.Value.ObjId, true);
                    transformer.Reset(block);
                });
                loadExtractService.LoadBlocks.ForEach(o =>
                {
                    var block = acad.Element<BlockReference>(o.Value.ObjId, true);
                    transformer.Reset(block);
                });

                PDSProject.Instance.PushGraphData(unionGraph);
            }
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
