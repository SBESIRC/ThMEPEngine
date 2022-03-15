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
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model.Electrical;
using NFox.Cad;
using ThCADCore.NTS;
using TianHua.Electrical.PDS.Project;

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

            // 读取配置表信息
            var fileService = new ThConfigurationFileService();
            var tableInfo = fileService.Acquire(LoadConfigUrl);
            var distBoxKey = new List<string>();
            var nameFilter = new List<string>();
            var propertyFilter = new List<string>();
            var tableAnalysis = new ThPDSTableAnalysisService();
            tableAnalysis.Analysis(tableInfo, ref nameFilter, ref propertyFilter, ref distBoxKey);

            //加载所有已打开的文件
            var dm = Application.DocumentManager;
            foreach (Document doc in dm)
            {
                //var fileName = doc.Name.Split('\\').Last();
                //if (FireCompartmentParameter.ChoiseFileNames.Count(file => string.Equals(fileName, file)) != 1)
                //{
                //    continue;
                //}

                using (var docLock = doc.LockDocument())
                using (var acad = AcadDatabase.Use(doc.Database))
                {
                    var storeysEngine = new ThEStoreysRecognitionEngine();
                    storeysEngine.Recognize(acad.Database, new Point3dCollection());
                    if (storeysEngine.Elements.Count == 0)
                    {
                        continue;
                    }

                    var storeysGeometry = new List<Polyline>();
                    storeysEngine.Elements.ForEach(o =>
                    {
                        var storey = acad.Element<BlockReference>((o as ThEStoreys).ObjectId, true);
                        storeysGeometry.Add(storey.ToOBB(storey.BlockTransform));
                    });

                    // 创建移动到原点的类
                    var transformerPt = new Point3d();
                    //var transformerPt = storeysGeometry[0].StartPoint;
                    var transformer = new ThMEPOriginTransformer(transformerPt);

                    EntitiesTransform(transformer, storeysGeometry.ToCollection());

                    // 提取回路
                    var cableEngine = new ThCableSegmentRecognitionEngine();
                    cableEngine.RecognizeMS(acad.Database, new Point3dCollection());
                    EntitiesTransform(transformer, cableEngine.Results);

                    // 提取桥架
                    var cableTrayEngine = new ThCabletraySegmentRecognitionEngine();
                    cableTrayEngine.RecognizeMS(acad.Database, new Point3dCollection());
                    EntitiesTransform(transformer, cableTrayEngine.Results);

                    // 提取标注
                    var markExtractor = new ThCircuitMarkRecognitionEngine();
                    markExtractor.RecognizeMS(acad.Database, new Point3dCollection());
                    EntitiesTransform(transformer, markExtractor.Results);

                    // 天正标注
                    var tchWireDimExtractor = new ThTCHWireDim2RecognitionEngine();
                    tchWireDimExtractor.RecognizeMS(acad.Database, new Point3dCollection());
                    EntitiesTransform(transformer, tchWireDimExtractor.Results);

                    // 根据块名提取负载及标注块
                    var loadExtractService = new ThPDSBlockExtractService();
                    loadExtractService.Extract(acad.Database, tableInfo, nameFilter, propertyFilter, distBoxKey);
                    BlockTransform(acad, transformer, loadExtractService.MarkBlocks);
                    BlockTransform(acad, transformer, loadExtractService.DistBoxBlocks);
                    BlockTransform(acad, transformer, loadExtractService.LoadBlocks);

                    // 提取配电箱框线
                    var allDistBoxFrame = ThPDSDistBoxFrameExtraction.GetDistBoxFrame(acad.Database).ToCollection();
                    EntitiesTransform(transformer, allDistBoxFrame);

                    ThPDSGraphService.DistBoxBlocks = loadExtractService.DistBoxBlocks;
                    ThPDSGraphService.LoadBlocks = loadExtractService.LoadBlocks;

                    storeysGeometry.ForEach(x =>
                    {
                        // 回路
                        var cableIndex = new ThCADCoreNTSSpatialIndex(cableEngine.Results);
                        var cables = cableIndex.SelectCrossingPolygon(x).OfType<Curve>().ToList();

                        // 桥架
                        var cableTrayIndex = new ThCADCoreNTSSpatialIndex(cableTrayEngine.Results);
                        var cableTrays = cableTrayIndex.SelectCrossingPolygon(x).OfType<Curve>().ToList();

                        // 标注
                        var markIndex = new ThCADCoreNTSSpatialIndex(markExtractor.Results);
                        var marks = markIndex.SelectCrossingPolygon(x).OfType<Entity>().ToList();

                        // 天正标注
                        var tchWireDimIndex = new ThCADCoreNTSSpatialIndex(tchWireDimExtractor.Results);
                        var tchWireDims = tchWireDimIndex.SelectCrossingPolygon(x).OfType<Entity>().ToList();

                        // 标注块
                        var markBlockIndex = new ThCADCoreNTSSpatialIndex(loadExtractService.MarkBlocks.Keys.ToCollection());
                        var markBlocks = markBlockIndex.SelectCrossingPolygon(x).OfType<Entity>().ToList();
                        var markBlockData = loadExtractService.MarkBlocks
                            .Where(o => markBlocks.Contains(o.Key))
                            .ToDictionary(o => o.Key, o => o.Value);

                        // 配电箱
                        var distBoxIndex = new ThCADCoreNTSSpatialIndex(loadExtractService.DistBoxBlocks.Keys.ToCollection());
                        var distBoxes = distBoxIndex.SelectCrossingPolygon(x).OfType<Entity>().ToList();

                        // 负载
                        var distBoxFrame = new ThCADCoreNTSSpatialIndex(loadExtractService.LoadBlocks.Keys.ToCollection());
                        var loads = distBoxFrame.SelectCrossingPolygon(x).OfType<Entity>().ToList();

                        // 配电箱框线
                        var distBoxFrameIndex = new ThCADCoreNTSSpatialIndex(allDistBoxFrame);
                        var distBoxFrames = distBoxFrameIndex.SelectCrossingPolygon(x).OfType<Polyline>().ToList();

                        //做一个标注的Service
                        var markService = new ThMarkService(marks, markBlockData, tchWireDims);
                        
                        var graphEngine = new ThPDSLoopGraphEngine(acad.Database, distBoxes, loads, cableTrays, cables, markService,
                            distBoxKey, cableTrayNode);

                        graphEngine.MultiDistBoxAnalysis(distBoxFrames);
                        graphEngine.CreatGraph();
                        graphEngine.CopyAttributes();

                        var graph = graphEngine.GetGraph();
                        graphList.Add(graph);
                    });

                    // 移回原位
                    EntitiesReset(transformer, loadExtractService.MarkBlocks.Keys.ToCollection());
                    EntitiesReset(transformer, loadExtractService.DistBoxBlocks.Keys.ToCollection());
                    EntitiesReset(transformer, loadExtractService.LoadBlocks.Keys.ToCollection());
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

        private void EntitiesTransform(ThMEPOriginTransformer transformer, DBObjectCollection collection)
        {
            collection.OfType<Entity>().ForEach(o =>
            {
                transformer.Transform(o);
                ThMEPEntityExtension.ProjectOntoXYPlane(o);
            });
        }

        private void BlockTransform(AcadDatabase acad, ThMEPOriginTransformer transformer,
            Dictionary<Entity, ThPDSBlockReferenceData> blockData)
        {
            blockData.ForEach(o =>
            {
                var block = acad.Element<BlockReference>(o.Value.ObjId, true);
                transformer.Transform(block);
                ThMEPEntityExtension.ProjectOntoXYPlane(block);
            });
        }

        private void EntitiesReset(ThMEPOriginTransformer transformer, DBObjectCollection collection)
        {
            collection.OfType<Entity>().ForEach(o =>
            {
                transformer.Reset(o);
            });
        }
    }
}
