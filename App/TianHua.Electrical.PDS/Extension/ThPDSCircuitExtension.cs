using System.Linq;
using TianHua.Electrical.PDS.Project.Module;

namespace TianHua.Electrical.PDS.Extension
{
    public static class ThPDSCircuitExtension
    {
        public static string GetCircuitID(this ThPDSProjectGraphEdge edge)
        {
            return edge.Circuit.ID.CircuitID.Last();
        }

        public static void SetCircuitID(this ThPDSProjectGraphEdge edge, string circuitID)
        {
            edge.Circuit.ID.CircuitID[edge.Circuit.ID.CircuitID.Count - 1] = circuitID;
        }
    }
}
