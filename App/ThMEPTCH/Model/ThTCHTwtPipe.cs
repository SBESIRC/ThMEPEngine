namespace ThMEPTCH.Model
{
    public class ThTCHTwtPipe
    {
        /// <summary>
        /// 起点
        /// </summary>
        public ThTCHTwtPoint StartPtID { get; set; }

        /// <summary>
        /// 终点
        /// </summary>
        public ThTCHTwtPoint EndPtID { get; set; }

        /// <summary>
        /// 系统
        /// </summary>
        public string System { get; set; }

        /// <summary>
        /// 材料
        /// </summary>
        public string Material { get; set; }

        /// <summary>
        /// 前缀
        /// </summary>
        public string DnType { get; set; }

        /// <summary>
        /// 管径
        /// </summary>
        public double Dn { get; set; }

        /// <summary>
        /// 坡度
        /// </summary>
        public double Gradient { get; set; }

        /// <summary>
        /// 线宽
        /// </summary>
        public double Weight { get; set; }

        /// <summary>
        /// 遮挡优先级
        /// </summary>
        public int HideLevel { get; set; }

        /// <summary>
        /// 出图比例
        /// </summary>
        public double DocScale { get; set; }

        /// <summary>
        /// 标注
        /// </summary>
        public ThTCHTwtPipeDimStyle DimID { get; set; }
    }
}
