using TianHua.Electrical.PDS.Model;
using TianHua.Electrical.PDS.Project.Module.Circuit;

namespace TianHua.Electrical.PDS.Project.Module
{
    public struct ThPDSProjectGraphNodeData
    {
        public double Power;
        public bool FireLoad;
        public string Number;
        public string Storey;
        public string Description;
        public ThPDSPhase Phase;
        public ImageLoadType Type;
        public PhaseSequence PhaseSequence;
    }
}
