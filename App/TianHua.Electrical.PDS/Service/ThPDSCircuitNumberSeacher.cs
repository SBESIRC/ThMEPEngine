using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using QuikGraph;
using Dreambuild.AutoCAD;

using TianHua.Electrical.PDS.Project.Module;

namespace TianHua.Electrical.PDS.Service
{
    public static class ThPDSCircuitNumberSeacher
    {
        public static List<string> Seach(ThPDSProjectGraphNode node,
             BidirectionalGraph<ThPDSProjectGraphNode, ThPDSProjectGraphEdge> graph)
        {
            return graph.InEdges(node)
                .Select(e => e.Circuit.ID.CircuitNumber)
                .OrderBy(str => str)
                .ToList();
        }

        public static List<string> Seach(ThPDSProjectGraphNode node)
        {
            return Merge(node.Load.ID.CircuitNumberList);
        }

        private static List<string> Merge(List<string> circuitNumberList)
        {
            var results = new List<string>();
            var sortList = new Dictionary<string, List<string>>();
            circuitNumberList.ForEach(number =>
            {
                if (string.IsNullOrEmpty(number))
                {
                    return;
                }
                var index = number.LastIndexOf('-');
                var source = number.Substring(0, index + 1);
                var id = number.Substring(index + 1);
                if (sortList.ContainsKey(source))
                {
                    sortList[source].Add(id);
                }
                else
                {
                    sortList.Add(source, new List<string> { id });
                }
            });
            sortList.ForEach(pair =>
            {
                var numberRegex = new Regex(@"[0-9]+");
                var numberList = new List<int>();
                var head = "";
                pair.Value.ForEach(id =>
                {
                    var match = numberRegex.Match(id);
                    if (match.Success)
                    {
                        head = id.Replace(match.Value, "");
                        var number = Convert.ToInt32(match.Value);
                        numberList.Add(number);
                    }
                });
                numberList = numberList.OrderBy(num => num).ToList();
                var first = numberList.First();
                var last = numberList.Last();
                results.Add(pair.Key + head + (first < 10 ? "0" + first.ToString() : first.ToString())
                    + "~" + head + (last < 10 ? "0" + last.ToString() : last.ToString()));
            });
            return results;
        }
    }
}
