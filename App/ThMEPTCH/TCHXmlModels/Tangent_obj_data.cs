using System.Collections.Generic;
using System.Xml.Serialization;
using ThMEPTCH.TCHXmlModels.TCHBaseModels;
using ThMEPTCH.TCHXmlModels.TCHEntityModels;

namespace ThMEPTCH.TCHXmlModels
{
    public class Tangent_obj_data : TCHXmlBaseModel
    {
        [XmlElement("Project_name")]
        public XmlString project_Name { get; set; }
        [XmlArray("Floor_Table"),XmlArrayItem("Floor")]
        public List<TCHFloor> Floors { get; set; }
        [XmlArray("Entity_Table"),XmlArrayItem("Entity_Collection")]
        public List<Entity_Collection> FloorEntitys { get; set; }
    }
}
