using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPHVAC.FanConnect.Model;
using ThMEPHVAC.FanConnect.ViewModel;

namespace ThMEPHVAC.FanConnect.Service
{
    class ThWaterPipeExtendServiece : ThPipeExtendBaseServiece
    {
        public ThWaterPipeConfigInfo ConfigInfo { set; get; }//界面输入信息
        public override void PipeExtend(ThFanTreeModel<ThFanPipeModel> tree)
        {
            //遍历树
            BianLiTree(tree.RootNode);
        }

        public void BianLiTree(ThFanTreeNode<ThFanPipeModel>  node)
        {
            //获取当前结点T1
            ThFanPipeModel t1 = node.Item;
            //对当前结点进行扩展
            WaterPipeExtend(t1);
            
            //获取父节点T2
            if (node.Parent != null)
            {
                //判断两个结点，是否同端点
                //如果同端点，那么不绘制小圆点
                //如果不同端点，那么在远离父结点的端点，绘制小圆点

            }
            
            //
            foreach (var n in node.Children)
            {
                BianLiTree(n);
            }
        }
        public void WaterPipeExtend(ThFanPipeModel pipe)
        {
            var line = pipe.LineSegment;

            
        }
    }
}
