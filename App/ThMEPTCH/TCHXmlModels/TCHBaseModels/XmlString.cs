using System.Xml.Serialization;
namespace ThMEPTCH.TCHXmlModels.TCHBaseModels 
{ 
    public class XmlString : TCHXmlBaseModel
    {
        [XmlText()]
        public string value { get; set; }
    }
}