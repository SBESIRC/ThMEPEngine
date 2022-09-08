using Autodesk.AutoCAD.Geometry;
using ProtoBuf;

namespace ThMEPTCH.Model
{
    /// <summary>
    /// 引出标注
    /// </summary>
    [ProtoContract]
    public class ThTCHSymbMultiLeader
    {
        public ThTCHSymbMultiLeader(Point3d basePoint, Point3d textLineLocPoint, double lineLength, string upText, string downText, string layer)
        {
            BasePoint = basePoint;
            TextLineLocPoint = textLineLocPoint;
            BaseLen = lineLength;
            UpText = upText;
            DownText = downText;
            Layer = layer;
        }
        public ThTCHSymbMultiLeader()
        {

        }
        [ProtoMember(1)]
        public Point3d BasePoint;//标注引出点
        [ProtoMember(2)]
        public Point3d TextLineLocPoint;
        [ProtoMember(3)]
        public double BaseLen;//基线长度
        [ProtoMember(4)]
        public string UpText;//上标文字
        [ProtoMember(5)]
        public string DownText;//下标文字
        [ProtoMember(6)]
        public string Layer;//图层
        [ProtoMember(7)]
        public string TextStyle = "TH_STYLE3";//文字样式
        [ProtoMember(8)]
        public int AlignType = 3;//对齐方式
        [ProtoMember(9)]
        public int ArrowType = 0;//箭头样式
        [ProtoMember(10)]
        public double ArrowSize = 3.0;//箭头大小
        [ProtoMember(11)]
        public double TextHeight = 3.5;//字高
        [ProtoMember(12)]
        public double BaseRatio = 0.3;//离线系数
        [ProtoMember(13)]
        public double DocScale = 100;//出图比例
        [ProtoMember(14)]
        public int IsParallel = 0;//引线平行
        [ProtoMember(15)]
        public int IsMask = 0;//背景屏蔽
        [ProtoMember(16)]
        public double LayoutRotation = 0;//背景屏蔽
    }
}
