using System;

namespace TianHua.Electrical.PDS.Project.Module
{
    [Serializable]
    public abstract class ThPDSProjectGraphNodeValidateTag : ThPDSProjectGraphNodeTag
    {
        //
    }

    public class ThPDSProjectGraphNodeDuplicateTag : ThPDSProjectGraphNodeValidateTag
    {

    }

    public class ThPDSProjectGraphNodeSingleTag : ThPDSProjectGraphNodeValidateTag
    {

    }

    public class ThPDSProjectGraphNodeFireTag : ThPDSProjectGraphNodeValidateTag
    {

    }
}
