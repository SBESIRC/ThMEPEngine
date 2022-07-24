using TianHua.Electrical.PDS.Project.Module;

namespace TianHua.Electrical.PDS.Extension
{
    public static class ThPDSProjectGraphNodeExtension
    {
        public static bool IsCentralizedPowerCircuit(this ThPDSProjectGraphNode node)
        {
            return node.Details.CircuitFormType is Project.Module.Circuit.IncomingCircuit.CentralizedPowerCircuit;
        }
    }
}
