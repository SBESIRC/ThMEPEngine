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
        public string ChangeToId { get; set; }
        public ThPDSProjectGraphEdge ChangeToEdge { get; set; }

        public ThPDSProjectGraphEdgeIdChangeTag()
        {
            ChangeToId = null;
            ChangeToEdge = null;
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
        public string ToLastCircuitNumber { get; set; }

        public ThPDSProjectGraphEdgeDataTag()
        {
            ToLastCircuitNumber = null;
        }
    }
}
