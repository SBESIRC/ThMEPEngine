using TianHua.Electrical.PDS.Model;

namespace TianHua.Electrical.PDS.Service
{
    public class ThPDSTerminalPanelService
    {
        public static bool IsTerminalPanel(ThPDSCircuitGraphNode node)
        {
            return IsTerminalPanel(node.Loads[0].LoadTypeCat_2);
        }

        public static bool IsTwowayTerminalPanel(ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode> edge)
        {
            return IsSamePanel(edge) 
                && IsTerminalPanel(edge.Source) 
                && IsTerminalPanel(edge.Target);
        }

        private static bool IsSamePanel(ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode> edge)
        {
            return edge.Source.Loads[0].LoadTypeCat_2.Equals(edge.Target.Loads[0].LoadTypeCat_2);
        }

        private static bool IsTerminalPanel(ThPDSLoadTypeCat_2 load)
        {
            return load.Equals(ThPDSLoadTypeCat_2.ResidentialDistributionPanel)
                || load.Equals(ThPDSLoadTypeCat_2.ElectricalControlPanel)
                || load.Equals(ThPDSLoadTypeCat_2.IsolationSwitchPanel)
                || load.Equals(ThPDSLoadTypeCat_2.FireEmergencyLightingDistributionPanel);
        }
    }
}
