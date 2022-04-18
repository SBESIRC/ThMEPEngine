using TianHua.Electrical.PDS.Model;

namespace TianHua.Electrical.PDS.Project.Module
{
    public abstract class ThPDSProjectGraphNodeCompareTag
    {
        public bool HaveState { get; set; }

        public ThPDSProjectGraphNodeCompareTag()
        {
            HaveState = false;
        }
    }

    public class ThPDSProjectGraphNodeNoDifferenceTag : ThPDSProjectGraphNodeCompareTag
    {
        //
    }

    public class ThPDSProjectGraphNodeIdChangeTag : ThPDSProjectGraphNodeCompareTag
    {
        public bool ChangeFrom { get; set; }
        public bool ChangeTo { get; set; }
        public string ChangeFromID { get; set; }
        public string ChangeToID { get; set; }

        public ThPDSProjectGraphNodeIdChangeTag()
        {
            ChangeFrom = false;
            ChangeTo = false;
            ChangeFromID = null;
            ChangeToID = null;
        }
    }

    public class ThPDSProjectGraphNodeExchangeTag : ThPDSProjectGraphNodeCompareTag
    {
        public string ExchangeToID { get; set; }
        public ThPDSProjectGraphNode ExchangeToNode { get; set; }

        public ThPDSProjectGraphNodeExchangeTag()
        {
            ExchangeToID = null;
            ExchangeToNode = null;
        }
    }

    public class ThPDSProjectGraphNodeMoveTag : ThPDSProjectGraphNodeCompareTag
    {
        public bool MoveFrom { get; set; }
        public bool MoveTo { get; set; }

        public ThPDSProjectGraphNodeMoveTag()
        {
            MoveFrom = false;
            MoveTo = false;
        }
    }

    public class ThPDSProjectGraphNodeAddTag : ThPDSProjectGraphNodeCompareTag
    {
        //
    }

    public class ThPDSProjectGraphNodeDeleteTag : ThPDSProjectGraphNodeCompareTag
    {
        //
    }

    public class ThPDSProjectGraphNodeDataTag : ThPDSProjectGraphNodeCompareTag
    {
        //D 描述变化
        public bool TagD { get; set; }
        //public string SouD { get; set; }
        public string TarD { get; set; }

        //F 消防变化
        public bool TagF { get; set; }
        //public bool SouF { get; set; }
        public bool TarF { get; set; }

        //P 功率变化
        public bool TagP { get; set; }
        //public ThInstalledCapacity SouP { get; set; }
        public ThInstalledCapacity TarP { get; set; }

        public ThPDSProjectGraphNodeDataTag()
        {
            TagD = false;
            TagF = false;
            TagP = false;
        }
    }
}
