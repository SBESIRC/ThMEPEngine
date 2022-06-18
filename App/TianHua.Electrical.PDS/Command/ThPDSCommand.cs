using System;

using ThMEPEngineCore.Command;
using TianHua.Electrical.PDS.Engine;
using TianHua.Electrical.PDS.Project;

namespace TianHua.Electrical.PDS.Command
{
    public class ThPDSCommand : ThMEPBaseCommand, IDisposable
    {
        public override void SubExecute()
        {
            var engine = new ThPDSCreateGraphEngine();
            var unionGraph = engine.Execute();
            PDSProjectManagement.PushGraphData(unionGraph);
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
