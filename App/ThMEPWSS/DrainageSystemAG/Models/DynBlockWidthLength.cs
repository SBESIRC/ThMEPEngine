using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPWSS.DrainageSystemAG.Models
{
    public class CreateBlockInfo
    {
        public double rotateAngle { get; set; }
        public double scaleNum { get; set; }
        public string dyBlockTypeName { get; set; }
        public Dictionary<string, string> attNameValues { get; }
        public Dictionary<string, object> dymBlockAttr { get; }
        public string layoutName { get; }
        public string blockName { get; }
        public Point3d createPoint { get; set; }
        public string spaceId { get; set; }
        public string tag { get; set; }
        public string belongBlockId { get; }
        public CreateBlockInfo(string blockName, string layoutName, Point3d createPoint,string blockId="")
        {
            this.layoutName = layoutName;
            this.blockName = blockName;
            this.createPoint = createPoint;
            this.attNameValues = new Dictionary<string, string>();
            this.dymBlockAttr = new Dictionary<string, object>();
            this.belongBlockId = blockId;
            this.rotateAngle = 0;
            this.scaleNum = 1;
        }
    }
    public class CreateResult
    {
        public ObjectId objectId { get; }
        public string tag { get; }
        public string belongBlockId { get; }
        public Point3d createPoint { get; }
        public EnumCreateEquipment createEquipment { get; }
        public CreateResult(ObjectId id, Point3d point, EnumCreateEquipment equipment, string tag)
        {
            this.objectId = id;
            this.createEquipment = equipment;
            this.tag = tag;
            this.createPoint = point;
        }
    }

    class DynBlockWidthLength
    {
        public string blockName { get; }
        public string dynName { get; }
        public double width { get; set; }
        public double length { get; set; }
        public string tag { get; set; }
        public DynBlockWidthLength(string blockName, string dynName, string tag)
        {
            this.blockName = blockName;
            this.dynName = dynName;
            this.tag = tag;
        }
    }
    
}
