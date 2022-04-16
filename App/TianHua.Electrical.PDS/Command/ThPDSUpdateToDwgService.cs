using TianHua.Electrical.PDS.Engine;
using TianHua.Electrical.PDS.Project;
using TianHua.Electrical.PDS.Service;

namespace TianHua.Electrical.PDS.Command
{
    public class ThPDSUpdateToDwgService
    {
        public void Execute()
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
