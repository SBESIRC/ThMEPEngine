using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPTCH.TCHTables
{
    struct TCHTgSymbMultiLeader
    {
        public int ID;//索引
        public int LeaderPtID;//基线起点
        public string Layer;//图层
        public string TextStyle;//文字样式
        public string UpText;//上标文字
        public string DownText;//下边文字
        public int AlignType;//对齐方式
        public int ArrowType;//箭头样式
        public double ArrowSize;//箭头大小
        public double TextHeight;//字高
        public double BaseRatio;//离线系数
        public double BaseLen;//基线长度
        public double DocScale;//出图比例
        public int IsParallel;//引线平行
        public int IsMask;//背景屏蔽
        public int VertexesPointStartID;//引线点链表起始ID
    }
}
