using System;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPTCH.Model
{
    public class ThTCHDescending : ICloneable
    {
        /// <summary>
        /// 降板高度
        /// </summary>
        public double DescendingHeight { get; set; }

        /// <summary>
        /// 降板厚度
        /// </summary>
        public double DescendingThickness { get; set; }

        /// <summary>
        /// 降板包围厚度
        /// </summary>
        public double DescendingWrapThickness { get; set; }

        /// <summary>
        /// 是否是降板
        /// </summary>
        public bool IsDescending { get; set; }

        /// <summary>
        /// 降板内轮廓线
        /// </summary>
        public Polyline Outline { get; set; }

        /// <summary>
        /// 降板外轮廓线
        /// </summary>
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
