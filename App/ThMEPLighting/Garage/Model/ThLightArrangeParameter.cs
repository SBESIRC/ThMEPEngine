using ThMEPLighting.Garage.Service;

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

        public ThQueryLightBlockService LightBlockQueryService { get; set; }
        /// <summary>
        /// 最小边的长度
        /// </summary>
        public double MinimumEdgeLength { get; set; }
        /// <summary>
        /// 灯编号文字高度
        /// </summary>
        public double LightNumberTextHeight { get; set; } = 350;
        /// <summary>
        /// 灯编号文字宽度因子
        /// </summary>
        public double LightNumberTextWidthFactor { get; set; } = 0.65;
        /// <summary>
        /// 灯编号文字基点距离灯线高度
        /// </summary>
        public double LightNumberTextOffsetHeight { get; set; } = 450;
        /// <summary>
        /// 灯编号文字样式
        /// </summary>
        public string LightNumberTextStyle { get; set; } = "TH-STYLE3";
    }
}
