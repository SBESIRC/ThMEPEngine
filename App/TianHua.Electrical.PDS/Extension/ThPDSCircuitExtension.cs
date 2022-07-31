using TianHua.Electrical.PDS.Project.Module;

namespace TianHua.Electrical.PDS.Extension
{
    public static class ThPDSCircuitExtension
    {
        public static string GetCircuitID(this ThPDSProjectGraphEdge edge)
        {
            return edge.Circuit.ID.CircuitID;
        }

        public static void SetCircuitID(this ThPDSProjectGraphEdge edge, string circuitID)
        {
            edge.Circuit.ID.CircuitID = circuitID;
        }
    }
}
