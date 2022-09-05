namespace ThMEPTCH.Model
{
    public class ThTCHTwtPipeValve
    {
        /// <summary>
        /// 插入点
        /// </summary>
        public ThTCHTwtPoint LocationID { get; set; }

        /// <summary>
        /// 方向
        /// </summary>
        public ThTCHTwtVector DirectionID { get; set; }

        /// <summary>
        /// 图块
        /// </summary>
        public ThTCHTwtBlock BlockID { get; set; }

        /// <summary>
        /// 所在管线
        /// </summary>
        public ThTCHTwtPipe PipeID { get; set; }

        /// <summary>
        /// 系统
        /// </summary>
        public string System { get; set; }

        /// <summary>
        /// 长度
        /// </summary>
        public double Length { get; set; }

        /// <summary>
        /// 宽度
        /// </summary>
        public double Width { get; set; }

        /// <summary>
        /// 打断宽度
        /// </summary>
        public double InterruptWidth { get; set; }

        /// <summary>
        /// 出图比例
        /// </summary>
        public double DocScale { get; set; }
    }
}
