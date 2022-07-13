using Autodesk.AutoCAD.Geometry;

namespace ThMEPTCH.Model
{
    /// <summary>
    /// 引出标注
    /// </summary>
    public class ThTCHSymbMultiLeader
    {
        public Point3d Point;
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
