using TianHua.Electrical.PDS.Engine;
using TianHua.Electrical.PDS.Project;

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
            updateService.Execute();
        }
    }
}
