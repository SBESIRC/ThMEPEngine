using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using ThMEPTCH.Data.IO;
using ThMEPTCH.TCHXmlModels.TCHBaseModels;

namespace ThMEPTCH.TCHXmlModels.TCHEntityModels
{
    public class TCH_SLab : TCHXmlEntity
    {
        public XmlDouble Flr_thick { get; set; }
        public XmlPoint VtNormal { get; set; }
        [XmlAnyElement()]
        public List<XmlElement> Baseline { get; set; }
        public List<XmlString> GetAllPoints() 
        {
            var allNodes = new List<XmlString>();
            var factory = new XmlSerializerFactory<XmlString>();
            foreach (var item in Baseline) 
            {
                foreach (XmlNode node in item.ChildNodes) 
                {
                    var addXml = new XmlString();
                    foreach (var attr in node.Attributes) 
                    {
                        var attribute = attr as XmlAttribute;
                        var name = attribute.Name.ToString().ToLower();
                        if (name == "name")
                        {
                            addXml.name = attribute.Value;
                        }
                        else if (name == "comment") 
                        {
                            addXml.comment = attribute.Value;
                        }
                    }
                    addXml.value = node.InnerText;
                    allNodes.Add(addXml);
                }
            }
            return allNodes;
        
        }
    }
}
