using System.Linq;
using TianHua.Electrical.PDS.Engine;
using TianHua.Electrical.PDS.Project;
using TianHua.Electrical.PDS.Project.Module;

namespace TianHua.Electrical.PDS.Service
{
    public class ThPDSUpdateToDwgService
    {
        public void Update()
        {
            var engine = new ThPDSCreateGraphEngine();
            var unionGraph = engine.Execute();
            var nodeMapList = engine.GetNodeMapList();
            var edgeMapList = engine.GetEdgeMapList();
            var projectGraph = PDSProject.Instance.ProjectUpdateToDwg(unionGraph);
            var updateService = new ThPDSInfoModifyEngine(nodeMapList, edgeMapList, projectGraph);
            updateService.InfoModify();
        }

        public void Update(ThPDSProjectGraphNode projectNode)
        {
            var engine = new ThPDSCreateGraphEngine();
            var unionGraph = engine.Execute();
            var nodeMapList = engine.GetNodeMapList();
            var edgeMapList = engine.GetEdgeMapList();
            var projectGraph = PDSProject.Instance.ProjectUpdateToDwg(unionGraph);
            var updateService = new ThPDSInfoModifyEngine(nodeMapList, edgeMapList, projectGraph, projectNode);
            updateService.InfoModify();
        }

        public void Update(ThPDSProjectGraphEdge projectEdge)
        {
            var engine = new ThPDSCreateGraphEngine();
            var unionGraph = engine.Execute();
            var nodeMapList = engine.GetNodeMapList();
            var edgeMapList = engine.GetEdgeMapList();
            var projectGraph = PDSProject.Instance.ProjectUpdateToDwg(unionGraph);
            var updateService = new ThPDSInfoModifyEngine(nodeMapList, edgeMapList, projectGraph, projectEdge);
            updateService.InfoModify();
        }

        public void Zoom(ThPDSProjectGraphNode node)
        {
            var engine = new ThPDSCreateGraphEngine();
            var unionGraph = engine.Execute();
            var projectGraph = PDSProject.Instance.ProjectUpdateToDwg(unionGraph);
            var zoomEngine = new ThPDSZoomService();
            zoomEngine.Zoom(node, projectGraph);
        }

        // 测试使用
        public void Zoom()
        {
            var engine = new ThPDSCreateGraphEngine();
            var unionGraph = engine.Execute();
            var projectGraph = PDSProject.Instance.ProjectUpdateToDwg(unionGraph);
            if (projectGraph.Vertices.Count() > 0)
            {
                var zoomEngine = new ThPDSZoomService();
                zoomEngine.Zoom(projectGraph.Vertices.First(), projectGraph);
            }
        }

        public void ImmediatelyZoom(ThPDSProjectGraphNode node)
        {
            var zoomEngine = new ThPDSZoomService();
            zoomEngine.ImmediatelyZoom(node);
        }

        public void AddLoadDimension(ThPDSProjectGraphNode node)
        {
            var engine = new ThPDSCreateGraphEngine();
            var unionGraph = engine.Execute();
            var projectGraph = PDSProject.Instance.ProjectUpdateToDwg(unionGraph);
            var addDimensionEngine = new ThPDSAddDimensionEngine();
            addDimensionEngine.AddDimension(node, projectGraph);
        }

        // 测试使用
        public void AddLoadDimension()
        {
            var engine = new ThPDSCreateGraphEngine();
            var unionGraph = engine.Execute();
            var projectGraph = PDSProject.Instance.ProjectUpdateToDwg(unionGraph);
            if (projectGraph.Vertices.Count() > 0)
            {
                var addDimensionEngine = new ThPDSAddDimensionEngine();
                addDimensionEngine.AddDimension(projectGraph.Vertices.First(), projectGraph);
            }
        }

        public void AddCircuitDimension(ThPDSProjectGraphEdge edge)
        {
            var engine = new ThPDSCreateGraphEngine();
            var unionGraph = engine.Execute();
            var projectGraph = PDSProject.Instance.ProjectUpdateToDwg(unionGraph);
            var addDimensionEngine = new ThPDSAddDimensionEngine();
            addDimensionEngine.AddDimension(edge, projectGraph);
        }

        // 测试使用
        public void AddCircuitDimension()
        {
            var engine = new ThPDSCreateGraphEngine();
            var unionGraph = engine.Execute();
            var projectGraph = PDSProject.Instance.ProjectUpdateToDwg(unionGraph);
            if (projectGraph.Edges.Count() > 0)
            {
                var addDimensionEngine = new ThPDSAddDimensionEngine();
                addDimensionEngine.AddDimension(projectGraph.Edges.First(), projectGraph);
            }
        }
    }
}
