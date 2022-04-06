using System.Collections.Generic;

namespace TianHua.Electrical.PDS.Project.Module
{
    public abstract class ThPDSProjectGraphNodeTagItem
    {
        //
    }

    public abstract class ThPDSProjectGraphNodeCompareTag
    {
        public readonly Dictionary<string, ThPDSProjectGraphNodeTagItem> Items;
        public ThPDSProjectGraphNodeCompareTag()
        {
            Items = new Dictionary<string, ThPDSProjectGraphNodeTagItem>();
        }
    }
}
