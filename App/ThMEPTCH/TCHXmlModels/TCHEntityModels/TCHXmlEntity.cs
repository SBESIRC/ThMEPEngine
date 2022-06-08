using System.Xml.Serialization;
using ThMEPTCH.TCHXmlModels.TCHBaseModels;

namespace ThMEPTCH.TCHXmlModels.TCHEntityModels
{
    public abstract class TCHXmlEntity : TCHXmlBaseModel
    {
        [XmlElement("LAYER")]
        public XmlString Layer { get; set; }
        [XmlElement("Object_ID")]
        public XmlString Object_ID { get; set; }
        [XmlElement("LINETYPE")]
        public XmlString LineType { get; set; }
    }
}
