using TianHua.Electrical.PDS.Model;

namespace TianHua.Electrical.PDS.Project.Module
{
    public static class ThPDSProjectGraphNodeExtension
    {
        public static bool IsTerminalPanel(this ThPDSProjectGraphNode node)
        {
            return IsTerminalPanel(node.Load.LoadTypeCat_2);
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
