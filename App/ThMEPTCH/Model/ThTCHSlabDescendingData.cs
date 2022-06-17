using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPTCH.Model
{
    public class ThTCHSlabDescendingData
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
        /// 降板轮廓线
        /// </summary>
        public Polyline Outline { get; set; }
    }
}
