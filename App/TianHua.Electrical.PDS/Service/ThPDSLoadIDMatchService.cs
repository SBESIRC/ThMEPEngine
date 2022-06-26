using System.Linq;
using System.Collections.Generic;

using TianHua.Electrical.PDS.Model;

namespace TianHua.Electrical.PDS.Service
{
    public static class ThPDSLoadIDMatchService
    {
        /// <summary>
        /// sourceNodes的个数须大于0
        /// </summary>
        /// <param name="node"></param>
        /// <param name="sourceNodes"></param>
        /// <returns></returns>
        public static ThPDSCircuitGraphNode Match(ThPDSCircuitGraphNode node, List<ThPDSCircuitGraphNode> sourceNodes)
        {
            if (string.IsNullOrEmpty(node.Loads[0].ID.LoadID))
            {
                return sourceNodes[0];
            }

            var results = sourceNodes[0];
            var maxCharNum = CharMatch(node.Loads[0].ID.LoadID, sourceNodes[0].Loads[0].ID.LoadID);
            for (var i = 1; i < sourceNodes.Count; i++)
            {
                var charNum = CharMatch(node.Loads[0].ID.LoadID, sourceNodes[i].Loads[0].ID.LoadID);
                if (charNum > maxCharNum)
                {
                    results = sourceNodes[i];
                    maxCharNum = charNum;
                }
            }
            return results;
        }

        private static int CharMatch(string source, string target)
        {
            if (string.IsNullOrEmpty(target))
            {
                return 0;
            }
            for (var i = 0; i < source.Count(); i++)
            {
                if (!target[i].Equals(source[i]))
                {
                    return i;
                }
            }
            return source.Count();
        }
    }
}
