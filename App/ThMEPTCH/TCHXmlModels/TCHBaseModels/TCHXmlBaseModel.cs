using System.Xml.Serialization;

namespace ThMEPTCH.TCHXmlModels.TCHBaseModels
{
    public abstract class TCHXmlBaseModel
    {
        [XmlAttribute()]
        public string name { get; set; }
        [XmlAttribute()]
        public string comment { get; set; }
    }
}
