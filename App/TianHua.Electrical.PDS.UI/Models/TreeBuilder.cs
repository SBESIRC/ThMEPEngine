using System;
using System.Collections.Generic;
namespace TianHua.Electrical.PDS.UI.Models
{
    public class TreeBuilder
    {
        readonly HashSet<KeyValuePair<int, int>> Pairs = new();
        readonly Dictionary<int, TreeNode> Cache = new();
        public void Add(int st, int ed)
        {
            Pairs.Add(new KeyValuePair<int, int>(st, ed));
        }
        public int SecureCount = 0;
        public int MaxSecureCount = 10000;
        public TreeNode Visit(int i)
        {
            ++SecureCount;
            if (SecureCount > MaxSecureCount) throw new Exception();
            if (!Cache.TryGetValue(i, out TreeNode node))
            {
                node = new TreeNode() { Id = i };
                Cache.Add(i, node);
                foreach (var kv in Pairs)
                {
                    if (kv.Key == i)
                    {
                        node.Children.Add(Visit(kv.Value));
                    }
                }
            }
            return node;
        }
    }
}
