using System.Linq;

namespace TianHua.Electrical.PDS.Service
{
    public static class ThPDSReplaceStringService
    {
        public static string ReplaceLastChar(string str, string source, string target)
        {
            var index = str.LastIndexOf(source);
            if (index == -1)
            {
                return str;
            }
            else
            {
                str = str.Remove(index, source.Count());
                str = str.Insert(index, target);
                return str;
            }
        }
    }
}
