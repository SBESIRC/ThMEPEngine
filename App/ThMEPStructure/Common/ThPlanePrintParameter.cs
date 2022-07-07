namespace ThMEPStructure.Common
{
    /// <summary>
    /// 通用的打印参数
    /// </summary>
    internal class ThPlanePrintParameter
    {
        public double LtScale { get; set; } =500;
        public int Measurement { get; set; } = 0;
        public string DrawingScale { get; set; } = "";
        // 标题文字距离图纸底部的距离
        public double HeadTextDisToPaperBottom { get; set; } = 3500.0;
        /// <summary>
        /// 楼层间距
        /// </summary>
        public double FloorSpacing { get; set; } = 100000;
    }
}
