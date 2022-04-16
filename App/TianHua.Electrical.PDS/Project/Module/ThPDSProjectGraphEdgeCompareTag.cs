namespace TianHua.Electrical.PDS.Project.Module
{
    public abstract class ThPDSProjectGraphEdgeCompareTag
    {
        public bool HaveState { get; set; }

        public ThPDSProjectGraphEdgeCompareTag()
        {
            HaveState = false;
        }
    }

    public class ThPDSProjectGraphEdgeNoDifferenceTag : ThPDSProjectGraphEdgeCompareTag
    {
        //
    }

    public class ThPDSProjectGraphEdgeIdChangeTag : ThPDSProjectGraphEdgeCompareTag
    {
        public bool ChangeFrom { get; set; }
        public bool ChangeTo { get; set; }
        public string ChangeFromLastCircuitID { get; set; }
        public string ChangeToLastCircuitID { get; set; }

        public ThPDSProjectGraphEdgeIdChangeTag()
        {
            ChangeFrom = false;
            ChangeTo = false;
            ChangeFromLastCircuitID = null;
            ChangeToLastCircuitID = null;
        }
    }

    public class ThPDSProjectGraphEdgeMoveTag : ThPDSProjectGraphEdgeCompareTag
    {
        public bool MoveFrom { get; set; }
        public bool MoveTo { get; set; }

        public ThPDSProjectGraphEdgeMoveTag()
        {
            MoveFrom = false;
            MoveTo = false;
        }
    }

    public class ThPDSProjectGraphEdgeAddTag : ThPDSProjectGraphEdgeCompareTag
    {
        //
    }

    public class ThPDSProjectGraphEdgeDeleteTag : ThPDSProjectGraphEdgeCompareTag
    {
        //
    }

    public class ThPDSProjectGraphEdgeDataTag : ThPDSProjectGraphEdgeCompareTag
    {
        public string ToLastCircuitID { get; set; }

        public ThPDSProjectGraphEdgeDataTag()
        {
            ToLastCircuitID = null;
        }
    }
}
