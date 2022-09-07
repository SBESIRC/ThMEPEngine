using Autodesk.AutoCAD.DatabaseServices;
using ProtoBuf;
using System;

namespace ThMEPTCH.Model
{
    [ProtoContract]
    public class ThTCHSlabDescendingData : ICloneable
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
        /// 降板内轮廓线
        /// </summary>
        [ProtoMember(15)]
        public Polyline Outline { get; set; }

        /// <summary>
        /// 降板外轮廓线
        /// </summary>
        [ProtoMember(16)]
        public Polyline OutlineBuffer { get; set; }

        public object Clone()
        {
            var clone = new ThTCHSlabDescendingData();
            if (this.Outline != null)
                clone.Outline = this.Outline.Clone() as Polyline;
            if (this.OutlineBuffer != null)
                clone.OutlineBuffer = this.OutlineBuffer.Clone() as Polyline;
            clone.IsDescending = this.IsDescending;
            clone.DescendingHeight = this.DescendingHeight;
            clone.DescendingThickness = this.DescendingThickness;
            clone.DescendingWrapThickness = this.DescendingWrapThickness;
            return clone;
        }
    }
}
