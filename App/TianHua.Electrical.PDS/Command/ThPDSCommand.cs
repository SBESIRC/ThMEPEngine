using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.ApplicationServices;
using Dreambuild.AutoCAD;
using Linq2Acad;
using QuikGraph;

using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Command;
using TianHua.Electrical.PDS.Engine;
using TianHua.Electrical.PDS.Model;
using TianHua.Electrical.PDS.Service;
using TianHua.Electrical.PDS.Project;
using ThMEPEngineCore.Engine;

namespace TianHua.Electrical.PDS.Command
{
    public class ThPDSCommand : ThMEPBaseCommand, IDisposable
    {
        readonly static string LoadConfigUrl = Path.Combine(ThCADCommon.SupportPath(), "平面关注对象.xlsx");

        public override void SubExecute()
        {
            // 记录所有图纸中的图
            var graphList = new List<AdjacencyGraph<ThPDSCircuitGraphNode, ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>>>();
            var cableTrayNode = new ThPDSCircuitGraphNode
            {
                NodeType = PDSNodeType.CableCarrier,
            };

            //加载所有已打开的文件
            var dm = Application.DocumentManager;
            foreach (Document doc in dm)
            {
                //var fileName = doc.Name.Split('\\').Last();
                //if (FireCompartmentParameter.ChoiseFileNames.Count(file => string.Equals(fileName, file)) != 1)
                //{
                //    continue;
                //}

                using (DocumentLock docLock = doc.LockDocument())
                using (var acad = AcadDatabase.Use(doc.Database))
                {
                    // 读取配置表信息
                    var fileService = new ThConfigurationFileService();
                    var tableInfo = fileService.Acquire(LoadConfigUrl);
                    var distBoxKey = new List<string>();
                    var nameFilter = new List<string>();
                    var propertyFilter = new List<string>();
                    var tableAnalysis = new ThPDSTableAnalysisService();
                    tableAnalysis.Analysis(tableInfo, ref nameFilter, ref propertyFilter, ref distBoxKey);

                    var storeysEngine = new ThEStoreysRecognitionEngine();
                    storeysEngine.Recognize(acad.Database, new Point3dCollection());
                    if (storeysEngine.Elements.Count == 0)
                    {
                        //continue;
                    }

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

                    // 提取配电箱框线
                    var distBoxFrame = ThPDSDistBoxFrameExtraction.GetDistBoxFrame(acad.Database);

                    //做一个标注的Service
                    var markService = new ThMarkService(markExtractor.Results, loadExtractService.MarkBlocks, tchWireDimExtractor.Results);

                    ThPDSGraphService.DistBoxBlocks = loadExtractService.DistBoxBlocks;
                    ThPDSGraphService.LoadBlocks = loadExtractService.LoadBlocks;
                    var graphEngine = new ThPDSLoopGraphEngine(acad.Database, loadExtractService.DistBoxBlocks.Keys.ToList(),
                        loadExtractService.LoadBlocks.Keys.ToList(), cableTrayEngine.Results, cableEngine.Results, markService,
                        distBoxKey, cableTrayNode);

                    graphEngine.MultiDistBoxAnalysis(distBoxFrame);
                    graphEngine.CreatGraph();
                    graphEngine.CopyAttributes();

                    var graph = graphEngine.GetGraph();
                    graphList.Add(graph);

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
                }
            }

            var unionEngine = new ThPDSGraphUnionEngine();
            var unionGraph = unionEngine.GraphUnion(graphList, cableTrayNode);
            PDSProject.Instance.PushGraphData(unionGraph);
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
