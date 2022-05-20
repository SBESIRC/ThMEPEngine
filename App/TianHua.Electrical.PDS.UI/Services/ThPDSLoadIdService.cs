using TianHua.Electrical.PDS.Project.Module;

namespace TianHua.Electrical.PDS.UI.Services
{
    public static class ThPDSLoadIdService
    {
        public static string LoadIdString(this ThPDSProjectGraphNode node)
        {
            var str = node.Load.ID.LoadID;
            if (string.IsNullOrWhiteSpace(str))
            {
                str = node.Load.ID.Description;
            }
            if (string.IsNullOrWhiteSpace(str))
            {
                str = "未知负载";
            }
            return str;
        }
    }
}
