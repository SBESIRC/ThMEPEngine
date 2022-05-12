using System;

namespace TianHua.Electrical.PDS.Project.Module
{
    [Serializable]
    public abstract class ThPDSProjectGraphEdgeValidateTag : ThPDSProjectGraphEdgeTag
    {
        //
    }

    public class ThPDSProjectGraphEdgeDuplicateTag : ThPDSProjectGraphEdgeValidateTag
    {

    }

    public class ThPDSProjectGraphEdgeSingleTag : ThPDSProjectGraphEdgeValidateTag
    {

    }

    public class ThPDSProjectGraphEdgeCascadingErrorTag : ThPDSProjectGraphEdgeValidateTag
    {

    }
}
