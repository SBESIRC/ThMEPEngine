namespace ThMEPHVAC.IndoorFanLayout.Models
{
    class FanRectangle
    {
        /// <summary>
        /// 风机名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 风机负荷
        /// </summary>
        public double Load { get; set; }
        /// <summary>
        /// 风机宽度（固定）
        /// </summary>
        public double Width { get; set; }
        /// <summary>
        /// 最小长度
        /// </summary>
        public double MinLength { get; set; }
        /// <summary>
        /// 最大长度
        /// </summary>
        public double MaxLength { get; set; }
        /// <summary>
        /// 最小风口个数
        /// </summary>
        public int MinVentCount { get; set; }
        /// <summary>
        /// 风机距离起点位置
        /// </summary>
        public double FanDistanceToStart { get; set; }
        /// <summary>
        /// 最大风口个数
        /// </summary>
        public int MaxVentCount { get; set; }
        /// <summary>
        /// 风机风口信息
        /// </summary>
        public VentRectangle VentRect { get; set; }
    }

    class VentRectangle
    {
        /// <summary>
        /// 风口宽度
        /// </summary>
        public double VentWidth{ get; set; }
        /// <summary>
        /// 风口长度
        /// </summary>
        public double VentLength { get; set; }
        /// <summary>
        /// 风口距离风机起点最短距离
        /// </summary>
        public double VentMinDistanceToStart { get; set; }
        /// <summary>
        /// 风口距离风机终点最小距离
        /// </summary>
        public double VentMinDistanceToEnd { get; set; }
        /// <summary>
        /// 风口距离上一风口最短距离
        /// </summary>
        public double VentMinDistanceToPrevious { get; set; }
    }
    class FanConstraint 
    {
        /// <summary>
        /// 风机距离轮廓边最短距离
        /// </summary>
        public double MinDistanceToOutline { get; set; }
        /// <summary>
        /// 风机宽度方向最小间距
        /// </summary>
        public double FanWidthMinDistance { get; set; }
        /// <summary>
        /// 风机长度方向最小间距
        /// </summary>
        public double FanLengthMinDistance { get; set; }
    }
}
