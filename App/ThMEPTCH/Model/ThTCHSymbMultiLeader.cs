using Autodesk.AutoCAD.Geometry;

namespace ThMEPTCH.Model
{
    /// <summary>
    /// 引出标注
    /// </summary>
    public class ThTCHSymbMultiLeader
    {
        public ThTCHSymbMultiLeader(Point3d basePoint, Point3d textLineLocPoint, double lineLength, string upText, string downText, string layer)
        {
            BasePoint = basePoint;
            TextLineLocPoint = textLineLocPoint;
            BaseLen = lineLength;
            upText = upText;
            DownText = downText;
            Layer = layer;
        }
        public ThTCHSymbMultiLeader()
        {

        }
        public Point3d BasePoint;//标注引出点
        public Point3d TextLineLocPoint;
        public double BaseLen;//基线长度
        public string UpText;//上标文字
        public string DownText;//下标文字
        public string Layer;//图层

        public string TextStyle = "TH_STYLE3";//文字样式
        public int AlignType = 3;//对齐方式
        public int ArrowType = 0;//箭头样式
        public double ArrowSize = 3.0;//箭头大小
        public double TextHeight = 3.5;//字高
        public double BaseRatio = 0.3;//离线系数
        public double DocScale = 1;//出图比例
        public int IsParallel = 0;//引线平行
        public int IsMask = 0;//背景屏蔽
    }
}
