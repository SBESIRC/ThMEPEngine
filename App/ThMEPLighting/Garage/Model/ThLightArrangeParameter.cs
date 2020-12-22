namespace ThMEPLighting.Garage.Model
{
    public class ThLightArrangeParameter
    {
        /// <summary>
        /// 灯间距
        /// </summary>
        public double Interval { get; set; }
        /// <summary>
        /// 灯距离线边的最小距离
        /// </summary>
        public double Margin { get; set; }
        /// <summary>
        /// 线槽间距
        /// (双排布置，线槽往两端偏移的间距)
        /// </summary>
        public double RacywaySpace { get; set; }
        /// <summary>
        /// 线槽宽度
        /// </summary>
        public double Width { get; set; }
        /// <summary>
        /// 单排布置
        /// </summary>
        public bool IsSingleRow { get; set; }
        /// <summary>
        /// 回路数量
        /// </summary>
        public int LoopNumber { get; set; }
        /// <summary>
        /// 自动生成灯
        /// </summary>
        public bool AutoGenerate { get; set; } = true;
        /// <summary>
        /// 图纸比例
        /// </summary>
        public int PaperRatio { get; set; }
    }
}
