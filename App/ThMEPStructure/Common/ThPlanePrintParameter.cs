namespace ThMEPStructure.Common
{
    /// <summary>
    /// 通用的打印参数
    /// </summary>
    public class ThPlanePrintParameter
    {
        public double LtScale { get; set; } =500;
        public int Measurement { get; set; } = 0;
        /// <summary>
        /// 图纸比例
        /// </summary>
        public string DrawingScale { get; set; } = "";
        /// <summary>
        /// 标题文字距离图纸底部的距离
        /// </summary>        
        public double HeadTextDisToPaperBottom { get; set; } = 3500.0;
        /// <summary>
        /// 楼层间距
        /// </summary>
        public double FloorSpacing { get; set; } = 100000;
        /// <summary>
        /// 是否过滤空调板
        /// </summary>
        public bool IsFilterCantiSlab { get; set; } = false;
        /// <summary>
        /// 默认板厚
        /// </summary>
        public double DefaultSlabThick { get; set; }
    }
}
