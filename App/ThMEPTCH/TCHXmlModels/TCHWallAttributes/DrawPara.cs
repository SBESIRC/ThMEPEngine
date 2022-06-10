using ThMEPTCH.TCHXmlModels.TCHBaseModels;

namespace ThMEPTCH.TCHXmlModels.TCHWallAttributes
{
    public class DrawPara : TCHXmlBaseModel
    {
        public XmlPoint Left_start_point { get; set; }
        public XmlPoint Left_end_point { get; set; }
        public XmlAngle Left_Central_ang { get; set; }
        public XmlPoint Right_start_point { get; set; }
        public XmlPoint Right_end_point { get; set; }
        public XmlAngle Right_Central_ang { get; set; }
    }
}
