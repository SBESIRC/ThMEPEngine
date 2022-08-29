namespace ThMEPTCH.TCHArchDataConvert.TCHArchTables
{
    public enum DoorTypeOperationEnum
    {
        /// <summary>
        /// 平开门
        /// </summary>
        SWING,
        /// <summary>
        /// 推拉门
        /// </summary>
        SLIDING,
    }

    public enum SwingEnum
    {
        SWING_RIGHT_IN  = 0,
        SWING_LEFT_IN   = 1,
        SWING_LEFT_OUT  = 2,
        SWING_RIGHT_OUT = 3,
    }

    public class TArchDoor : TArchEntity
    {
        public double TextPointZ { get; set; }
        public double TextPointX { get; set; }
        public double TextPointY { get; set; }
        public double BasePointX { get; set; }
        public double BasePointY { get; set; }
        public double BasePointZ { get; set; }
        public ulong Quadrant { get; set; }
        public string Name { get; set; }
        public double Height { get; set; }
        public double Width { get; set; }
        public double Thickness { get; set; }
        public int Kind { get; set; }
        public string SubKind { get; set; }
        public double Rotation { get; set; }
        public SwingEnum Swing { get; set; }
        public DoorTypeOperationEnum OperationType { get; set; }
        public override bool IsValid()
        {
            return Width > 1.0 && Thickness > 1.0;
        }
    }
}
