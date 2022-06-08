using ThMEPTCH.TCHXmlModels.TCHBaseModels;

namespace ThMEPTCH.TCHXmlModels.TCHWallAttributes
{
    public class Baseline: TCHXmlBaseModel
    {
        public XmlPoint Start_point { get; set; }
        public XmlPoint End_point { get; set; }
        public XmlAngle Central_ang { get; set; }
    }
}
