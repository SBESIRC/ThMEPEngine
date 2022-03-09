using System.Collections.Generic;
using System.Linq;

namespace TianHua.Electrical.PDS.UI.Models
{
    public class TreeNode
    {
        public int Id;
        public HashSet<TreeNode> Children = new();
        public override string ToString()
        {
            return $"[{Id},{string.Join("", Children.Select(x => x.ToString()))}]";
        }
    }
}
