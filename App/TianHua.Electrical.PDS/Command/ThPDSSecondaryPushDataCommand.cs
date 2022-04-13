using System;
using System.Collections.Generic;

using ThMEPEngineCore.Command;
using TianHua.Electrical.PDS.Engine;
using TianHua.Electrical.PDS.Model;
using TianHua.Electrical.PDS.Project;

namespace TianHua.Electrical.PDS.Command
{
    public class ThPDSSecondaryPushDataCommand : ThMEPBaseCommand, IDisposable
    {
        private List<ThPDSNodeMap> NodeMapList;

        private List<ThPDSEdgeMap> EdgeMapList;

        public override void SubExecute()
        {
            var engine = new ThPDSCreateGraphEngine();
            var unionGraph = engine.Execute();
            NodeMapList = engine.GetNodeMapList();
            EdgeMapList = engine.GetEdgeMapList();
            PDSProject.Instance.SecondaryPushGraphData(unionGraph);
        }

        public void Dispose()
        {
            throw new NotImplementedException();
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
