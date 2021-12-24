using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPHVAC.FanConnect.Model;
using ThMEPHVAC.FanConnect.ViewModel;

namespace ThMEPHVAC.FanConnect.Service
{
    public class ThAddValveServiece
    {
        public ThWaterPipeConfigInfo ConfigInfo { set; get; }//界面输入信息
        public void AddValve(ThFanTreeModel tree)
        {
            if (tree.RootNode.Children.Count == 0)
            {
                return;
            }
            //遍历树
            BianLiTree(tree.RootNode);
        }
        public void BianLiTree(ThFanTreeNode<ThFanPipeModel> node)
        {
            foreach (var child in node.Children)
            {
                BianLiTree(child);
            }
            if(node.Item.IsValve)
            {
                return;
            }
            //找到
        }
    }
}
