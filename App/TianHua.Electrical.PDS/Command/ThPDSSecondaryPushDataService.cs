using TianHua.Electrical.PDS.Engine;
using TianHua.Electrical.PDS.Project;

namespace TianHua.Electrical.PDS.Command
{
    public class ThPDSSecondaryPushDataService
    {
        public void Execute()
        {
            var engine = new ThPDSCreateGraphEngine();
            var unionGraph = engine.Execute();
            PDSProject.Instance.SecondaryPushGraphData(unionGraph);
        }
    }
}
