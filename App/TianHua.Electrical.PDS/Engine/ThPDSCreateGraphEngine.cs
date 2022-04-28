using System.Linq;
using System.Collections.Generic;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using QuikGraph;

using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model.Electrical;
using TianHua.Electrical.PDS.Model;
using TianHua.Electrical.PDS.Service;

namespace TianHua.Electrical.PDS.Engine
{
    public class ThPDSCreateGraphEngine
    {
        private List<ThPDSNodeMap> NodeMapList;

        private List<ThPDSEdgeMap> EdgeMapList;

        public BidirectionalGraph<ThPDSCircuitGraphNode, ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>> Execute(List<Database> databases)
        {
            NodeMapList = new List<ThPDSNodeMap>();
            EdgeMapList = new List<ThPDSEdgeMap>();

            // 记录所有图纸中的图
            var graphList = new List<BidirectionalGraph<ThPDSCircuitGraphNode, ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>>>();
            var cableTrayNode = new ThPDSCircuitGraphNode
            {
                NodeType = PDSNodeType.CableCarrier,
            };

            // 读取配置表信息
            var fileService = new ThConfigurationFileService();
            var tableInfo = fileService.Acquire(ThCADCommon.PDSComponentsPath());
            var distBoxKey = new List<string>();
            var nameFilter = new List<string>();
            var propertyFilter = new List<string>();
            var tableAnalysis = new ThPDSTableAnalysisService();
            tableAnalysis.Analysis(tableInfo, ref nameFilter, ref propertyFilter, ref distBoxKey);

            //加载数据
            foreach (Database database in databases)
            {
                if (Application.DocumentManager.GetDocument(database) is Document doc)
                {
                    using (var docLock = doc.LockDocument())
                    using (var acad = AcadDatabase.Use(doc.Database))
                    {
                        var storeysEngine = new ThEStoreysRecognitionEngine();
                        storeysEngine.Recognize(acad.Database, new Point3dCollection());
                        if (storeysEngine.Elements.Count == 0)
                        {
                            continue;
                        }

                        var nodeMap = new ThPDSNodeMap
                        {
                            ReferenceDWG = doc.Database.OriginalFileName.Split("\\".ToCharArray()).Last(),
                        };
                        var edgeMap = new ThPDSEdgeMap
                        {
                            ReferenceDWG = doc.Database.OriginalFileName.Split("\\".ToCharArray()).Last(),
                        };

                        var storeysGeometry = new List<Polyline>();
                        storeysEngine.Elements.ForEach(o =>
                        {
                            var storey = acad.Element<BlockReference>((o as ThEStoreys).ObjectId, true);
                            storeysGeometry.Add(storey.ToOBB(storey.BlockTransform));
                        });

                        // 创建移动到原点的类
                        // 测试使用
                        // var transformerPt = new Point3d();
                        var transformerPt = storeysGeometry[0].StartPoint;
                        var transformer = new ThMEPOriginTransformer(transformerPt);

                        EntitiesTransform(transformer, storeysGeometry.ToCollection());

                        // 提取回路
                        var cableEngine = new ThCableSegmentRecognitionEngine();
                        cableEngine.RecognizeMS(acad.Database, new Point3dCollection());
                        var cableEntities = cableEngine.Results.Select(r => r.Entity).ToCollection();
                        EntitiesTransform(transformer, cableEntities);

                        // 提取桥架
                        var cableTrayEngine = new ThCabletraySegmentRecognitionEngine();
                        cableTrayEngine.RecognizeMS(acad.Database, new Point3dCollection());
                        EntitiesTransform(transformer, cableTrayEngine.Results);

                        // 提取标注
                        var markExtractor = new ThCircuitMarkRecognitionEngine();
                        markExtractor.RecognizeMS(acad.Database, new Point3dCollection());
                        var markEntities = markExtractor.Results.Select(r => r.Entity).ToCollection();
                        EntitiesTransform(transformer, markEntities);

                        // 天正标注
                        var tchWireDimExtractor = new ThTCHWireDim2RecognitionEngine();
                        tchWireDimExtractor.RecognizeMS(acad.Database, new Point3dCollection());
                        var tchWireDimEntities = tchWireDimExtractor.Results.Select(r => r.Entity).ToCollection();
                        EntitiesTransform(transformer, tchWireDimEntities);

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

                        for (var i = 0; i < storeysEngine.Elements.Count; i++)
                        {
                            var x = storeysGeometry[i];
                            var storey = storeysEngine.Elements[i] as ThEStoreys;

                            // 回路
                            var cableIndex = new ThCADCoreNTSSpatialIndex(cableEntities);
                            var cables = cableIndex.SelectCrossingPolygon(x).OfType<Curve>().ToList();

                            // 桥架
                            var cableTrayIndex = new ThCADCoreNTSSpatialIndex(cableTrayEngine.Results);
                            var cableTrays = cableTrayIndex.SelectCrossingPolygon(x).OfType<Curve>().ToList();

                            // 标注
                            var markIndex = new ThCADCoreNTSSpatialIndex(markEntities);
                            var marks = markIndex.SelectCrossingPolygon(x).OfType<Entity>().ToList();
                            var marksInfo = markExtractor.Results.Where(r => marks.Contains(r.Entity)).ToList();

                            // 天正标注
                            var tchWireDimIndex = new ThCADCoreNTSSpatialIndex(tchWireDimEntities);
                            var tchWireDims = tchWireDimIndex.SelectCrossingPolygon(x).OfType<Entity>().ToList();
                            var tchWireDimsInfo = tchWireDimExtractor.Results.Where(r => tchWireDims.Contains(r.Entity)).ToList();

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
                            var loadIndex = new ThCADCoreNTSSpatialIndex(loadExtractService.LoadBlocks.Keys.ToCollection());
                            var loads = loadIndex.SelectCrossingPolygon(x).OfType<Entity>().ToList();

                            // 配电箱框线
                            var distBoxFrameIndex = new ThCADCoreNTSSpatialIndex(allDistBoxFrame);
                            var distBoxFrames = distBoxFrameIndex.SelectCrossingPolygon(x).OfType<Polyline>().ToList();

                            //做一个标注的Service
                            var markService = new ThMarkService(marksInfo, markBlockData, tchWireDimsInfo);

                            var graphEngine = new ThPDSLoopGraphEngine(acad.Database, distBoxes, loads, cableTrays, cables, markService,
                                distBoxKey, cableTrayNode, nodeMap.NodeMap, edgeMap.EdgeMap);

                            graphEngine.MultiDistBoxAnalysis(acad.Database, distBoxFrames);
                            graphEngine.CreatGraph();
                            graphEngine.UnionLightingEdge();
                            graphEngine.CopyAttributes();
                            var storeyBasePoint = new Point3d(storey.Data.Position.X - (double)storey.Data.CustomProperties.GetValue("基点 X"),
                                storey.Data.Position.Y - (double)storey.Data.CustomProperties.GetValue("基点 Y"), 0);
                            graphEngine.AssignStorey(doc.Database, storey.StoreyNumber, storeyBasePoint);

                            var graph = graphEngine.GetGraph();
                            graphList.Add(graph);
                        }

                        NodeMapList.Add(nodeMap);
                        EdgeMapList.Add(edgeMap);

                        // 移回原位
                        EntitiesReset(transformer, loadExtractService.MarkBlocks.Keys.ToCollection());
                        EntitiesReset(transformer, loadExtractService.DistBoxBlocks.Keys.ToCollection());
                        EntitiesReset(transformer, loadExtractService.LoadBlocks.Keys.ToCollection());
                    }
                }
            }

            var unionEngine = new ThPDSGraphUnionEngine(EdgeMapList);
            var circuitGraph = unionEngine.GraphUnion(graphList, cableTrayNode);
            return circuitGraph;
        }

        public BidirectionalGraph<ThPDSCircuitGraphNode, ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>> Execute()
        {
            // 获取所有打开的图纸
            var databases = Application.DocumentManager
                .OfType<Document>()
                .Where(d => d.IsNamedDrawing)
                .Select(d => d.Database)
                .ToList();

            // 获取图纸数据
            return Execute(databases);
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

        public List<ThPDSNodeMap> GetNodeMapList()
        {
            return NodeMapList;
        }

        public List<ThPDSEdgeMap> GetEdgeMapList()
        {
            return EdgeMapList;
        }
    }
}
