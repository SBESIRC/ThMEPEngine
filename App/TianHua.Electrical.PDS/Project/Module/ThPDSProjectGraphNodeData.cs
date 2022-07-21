using TianHua.Electrical.PDS.Model;
using TianHua.Electrical.PDS.Project.Module.Circuit;

namespace TianHua.Electrical.PDS.Project.Module
{
    public struct ThPDSProjectGraphNodeData
    {
        public double KV;
        public double Power;
        public bool FireLoad;
        public string Number;
        public string Storey;
        public string Description;
        public ThPDSPhase Phase;
        public ImageLoadType Type;
        public PhaseSequence PhaseSequence;

        public void Sync()
        {
            if (KV == 0.38)
            {
                Phase = ThPDSPhase.三相;
                PhaseSequence = PhaseSequence.L123;
            }
            else if (KV == 0.22)
            {
                Phase = ThPDSPhase.一相;
                PhaseSequence = PhaseSequence.L1;
            }
        }

        public static ThPDSProjectGraphNodeData Create()
        {
            return new ThPDSProjectGraphNodeData()
            {
                KV = 0.38,
                Number = "",
                Power = 0.0,
                Storey = "1F",
                FireLoad = false,
                Description = "备用",
                Type = ImageLoadType.None,
                Phase = ThPDSPhase.三相,
                PhaseSequence = PhaseSequence.L123,
            };
        }
    }
}
