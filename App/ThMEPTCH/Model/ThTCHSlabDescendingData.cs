using Autodesk.AutoCAD.DatabaseServices;
using ProtoBuf;

namespace ThMEPTCH.Model
{
    [ProtoContract]
    public class ThTCHSlabDescendingData
    {
        /// <summary>
        /// 降板高度
        /// </summary>
        [ProtoMember(11)]
        public double DescendingHeight { get; set; }

        /// <summary>
        /// 降板厚度
        /// </summary>
        [ProtoMember(12)]
        public double DescendingThickness { get; set; }

        /// <summary>
        /// 降板包围厚度
        /// </summary>
        [ProtoMember(13)]
        public double DescendingWrapThickness { get; set; }

        /// <summary>
        /// 是否是降板
        /// </summary>
        [ProtoMember(14)]
        public bool IsDescending { get; set; }

        /// <summary>
        /// 降板轮廓线
        /// </summary>
        [ProtoMember(15)]
        public Polyline Outline { get; set; }
    }
}
