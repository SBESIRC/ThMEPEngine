namespace ThMEPTCH.TCHArchDataConvert.TCHArchTables
{
    public enum WindowTypeEnum
    {
        /// <summary>
        /// 普通窗
        /// </summary>
        Window = 0,
        /// <summary>
        /// 百叶窗
        /// </summary>
        Shutter = 1,
        /// <summary>
        /// 偏心窗
        /// </summary>
        Eccentric = 2,
    };

    public class TArchWindow:TArchEntity
    {
        public string Number { get; set; }
        public double TextPointZ { get; set; }
        public double TextPointX { get; set; }
        public double TextPointY { get; set; }
        public double BasePointX { get; set; }
        public double BasePointY { get; set; }
        public double BasePointZ { get; set; }
        public ulong Quadrant { get; set; }
        public double Height { get; set; }
        public double SillHeight { get; set; }
        public double Width { get; set; }
        public double Thickness { get; set; }
        public int Kind { get; set; }
        public string SubKind { get; set; }
        public double Rotation { get; set; }
        public WindowTypeEnum WindowType { get; set; }
        public override bool IsValid()
        {
            return Width > 1.0 && Thickness > 1.0;
        }
    }
}
