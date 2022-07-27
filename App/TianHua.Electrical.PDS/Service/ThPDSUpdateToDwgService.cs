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
            var projectGraph = PDSProjectManagement.ProjectUpdateToDwg(unionGraph);
            var updateService = new ThPDSInfoModifyEngine(nodeMapList, edgeMapList, projectGraph);
            updateService.InfoModify();
            updateService.GenerateRevcloud();
        }

        public void Update(ThPDSProjectGraphNode projectNode)
        {
            var engine = new ThPDSCreateGraphEngine();
            var unionGraph = engine.Execute();
            var nodeMapList = engine.GetNodeMapList();
            var edgeMapList = engine.GetEdgeMapList();
            var projectGraph = PDSProjectManagement.ProjectUpdateToDwg(unionGraph);
            var updateService = new ThPDSInfoModifyEngine(nodeMapList, edgeMapList, projectGraph, projectNode);
            updateService.InfoModify();
            updateService.GenerateRevcloud();
        }

        public void Update(ThPDSProjectGraphEdge projectEdge)
        {
            var engine = new ThPDSCreateGraphEngine();
            var unionGraph = engine.Execute();
            var nodeMapList = engine.GetNodeMapList();
            var edgeMapList = engine.GetEdgeMapList();
            var projectGraph = PDSProjectManagement.ProjectUpdateToDwg(unionGraph);
            var updateService = new ThPDSInfoModifyEngine(nodeMapList, edgeMapList, projectGraph, projectEdge);
            updateService.InfoModify();
            updateService.GenerateRevcloud();
        }

        public void Zoom(ThPDSProjectGraphNode node)
        {
            var engine = new ThPDSCreateGraphEngine();
            var unionGraph = engine.Execute();
            var projectGraph = PDSProjectManagement.ProjectUpdateToDwg(unionGraph);
            var zoomEngine = new ThPDSZoomService();
            zoomEngine.Zoom(node, projectGraph);
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
            var projectGraph = PDSProjectManagement.ProjectUpdateToDwg(unionGraph);
            var addDimensionEngine = new ThPDSAddDimensionEngine();
            addDimensionEngine.AddDimension(node, projectGraph);
        }

        public void AddCircuitDimension(ThPDSProjectGraphEdge edge)
        {
            var engine = new ThPDSCreateGraphEngine();
            var unionGraph = engine.Execute();
            var projectGraph = PDSProjectManagement.ProjectUpdateToDwg(unionGraph);
            var addDimensionEngine = new ThPDSAddDimensionEngine();
            addDimensionEngine.AddDimension(edge, projectGraph);
        }
    }
}
