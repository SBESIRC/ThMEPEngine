using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using TianHua.Electrical.PDS.Engine;
using TianHua.Electrical.PDS.Project;

namespace TianHua.Electrical.PDS.Service
{
    public class ThPDSSecondaryPushDataService
    {
        public void Push()
        {
            var engine = new ThPDSCreateGraphEngine();
            var unionGraph = engine.Execute();
            PDSProject.Instance.SecondaryPushGraphData(unionGraph);
        }

        public void Push(List<Database> databases)
        {
            var engine = new ThPDSCreateGraphEngine();
            var unionGraph = engine.Execute(databases);
            PDSProject.Instance.SecondaryPushGraphData(unionGraph);
        }
    }
}
