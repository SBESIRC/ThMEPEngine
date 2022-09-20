using Autodesk.AutoCAD.DatabaseServices;
using ProtoBuf;
using System;

namespace ThMEPTCH.Model
{
    [ProtoContract]
    public class ThTCHDescending : ICloneable
    {
        /// <summary>
        /// 降板高度
        /// </summary>
        [ProtoMember(21)]
        public double DescendingHeight { get; set; }

        /// <summary>
        /// 降板厚度
        /// </summary>
        [ProtoMember(22)]
        public double DescendingThickness { get; set; }

        /// <summary>
        /// 降板包围厚度
        /// </summary>
        [ProtoMember(23)]
        public double DescendingWrapThickness { get; set; }

        /// <summary>
        /// 是否是降板
        /// </summary>
        [ProtoMember(24)]
        public bool IsDescending { get; set; }

        /// <summary>
        /// 降板内轮廓线
        /// </summary>
        [ProtoMember(25)]
        public Polyline Outline { get; set; }

        /// <summary>
        /// 降板外轮廓线
        /// </summary>
        [ProtoMember(26)]
        public Polyline OutlineBuffer { get; set; }

        public object Clone()
        {
            var clone = new ThTCHDescending();
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
