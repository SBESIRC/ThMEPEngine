using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using TianHua.Electrical.PDS.Engine;
using TianHua.Electrical.PDS.Project;


namespace TianHua.Electrical.PDS.Service
{
    public class ThPDSPushDataService
    {
        public void Push()
        {
            var engine = new ThPDSCreateGraphEngine();
            var unionGraph = engine.Execute();
            PDSProjectManagement.PushGraphData(unionGraph, engine.GetSubstationList());
        }

        public void Push(List<Database> databases)
        {
            var engine = new ThPDSCreateGraphEngine();
            var unionGraph = engine.Execute(databases);
            PDSProjectManagement.PushGraphData(unionGraph, engine.GetSubstationList());
        }
    }
}
