namespace ThMEPHVAC.IndoorFanModels
{
    public class IndoorFanCheckModel
    {
        public EnumHotColdType HotColdType { get; set; }
        public bool MarkNotEnoughRoom { get; set; }
        public bool MarkOverRoom { get; set; }
        public double MarkOverPercentage { get; set; }
    }
}
