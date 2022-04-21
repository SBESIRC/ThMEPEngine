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

        public void Update(ThPDSProjectGraphNode node)
        {
            var engine = new ThPDSCreateGraphEngine();
            var unionGraph = engine.Execute();
            var nodeMapList = engine.GetNodeMapList();
            var edgeMapList = engine.GetEdgeMapList();
            var projectGraph = PDSProject.Instance.ProjectUpdateToDwg(unionGraph);
            var updateService = new ThPDSInfoModifyEngine(nodeMapList, edgeMapList, projectGraph, node);
            updateService.InfoModify();
        }

        public void Update(ThPDSProjectGraphEdge edge)
        {
            var engine = new ThPDSCreateGraphEngine();
            var unionGraph = engine.Execute();
            var nodeMapList = engine.GetNodeMapList();
            var edgeMapList = engine.GetEdgeMapList();
            var projectGraph = PDSProject.Instance.ProjectUpdateToDwg(unionGraph);
            var updateService = new ThPDSInfoModifyEngine(nodeMapList, edgeMapList, projectGraph, edge);
            updateService.InfoModify();
        }

        public void Zoom(ThPDSProjectGraphNode node)
        {
            var engine = new ThPDSCreateGraphEngine();
            var unionGraph = engine.Execute();
            var projectGraph = PDSProject.Instance.ProjectUpdateToDwg(unionGraph);
            var zoomEngine = new ThPDSZoomEngine();
            zoomEngine.Zoom(projectGraph.Vertices.First());
        }

        // 测试使用
        public void Zoom()
        {
            var engine = new ThPDSCreateGraphEngine();
            var unionGraph = engine.Execute();
            var projectGraph = PDSProject.Instance.ProjectUpdateToDwg(unionGraph);
            if(projectGraph.Vertices.Count() > 0)
            {
                var zoomEngine = new ThPDSZoomEngine();
                zoomEngine.Zoom(projectGraph.Vertices.First());
            }
        }
    }
}
