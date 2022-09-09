using Autodesk.AutoCAD.Colors;
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
        public string uid { get; }
        public string floorId { get; }
        public string layerName { get; set; }
        public string blockName { get; }
        public Point3d createPoint { get; set; }
        public double rotateAngle { get; set; }
        public double scaleNum { get; set; }
        public Dictionary<string, string> attNameValues { get; }
        public Dictionary<string, object> dymBlockAttr { get; }
        public EnumEquipmentType equipmentType { get; }
        public string spaceId { get; set; }
        public string tag { get; set; }
        public string belongBlockId { get; }
        public string copyId { get; }
        public CreateBlockInfo(string floorId,string blockName, string layerName, Point3d createPoint, EnumEquipmentType type, string blockId="",string copyId="")
        {
            this.uid = Guid.NewGuid().ToString();
            this.floorId = floorId;
            this.layerName = layerName;
            this.blockName = blockName;
            this.createPoint = createPoint;
            this.copyId = copyId;
            this.equipmentType = type;
            this.attNameValues = new Dictionary<string, string>();
            this.dymBlockAttr = new Dictionary<string, object>();
            this.belongBlockId = blockId;
            this.rotateAngle = 0;
            this.scaleNum = 1;
        }
    }
    public class CreateBasicElement
    {
        public string uid { get; }
        public string floorId { get; }
        public Curve baseCurce { get; set; }
        public Color lineColor { get; }
        public string layerName { get; set; }
        public string belongBlockId { get; }
        public string connectBlockId { get; set; }
        public string curveTag { get; }
        public bool ConvertToTCHElement = false;
        public CreateBasicElement(string floorId,Curve curve,string layerName,string belongId,string tag,Color lineColor =null) 
        {
            this.uid = Guid.NewGuid().ToString();
            this.floorId = floorId;
            this.baseCurce = curve;
            this.layerName = layerName;
            this.lineColor = lineColor;
            this.belongBlockId = belongId;
            this.curveTag = tag;
        }

    }
    public class CreateDBTextElement 
    {
        public string uid { get; }
        public string floorUid { get; }
        public string layerName { get; }
        public string textStyle { get; }
        public Point3d textPoint { get; set; }
        public string belongBlockId { get; }
        public DBText dbText { get; set; }
        public string copyId { get; }
        public bool ConvertToTCHElement = false;
        public string Tag { get; }
        public CreateDBTextElement(string floorId, Point3d textPoint,DBText dBText,string belongId,string layerName,string textStyle,string copyId="", string tag = "")
        {
            this.uid = Guid.NewGuid().ToString();
            this.layerName = layerName;
            this.textStyle = textStyle;
            this.floorUid = floorId;
            this.textPoint = textPoint;
            this.dbText = dBText;
            this.belongBlockId = belongId;
            this.copyId = copyId;
            this.Tag = tag;
        }
    }
    public class CreateResult
    {
        public ObjectId objectId { get; }
        public string floorUid { get; }
        public string tag { get; }
        public string belongBlockId { get; }
        public Point3d createPoint { get; set; }
        public EnumEquipmentType equipmentType { get; }
        public string Layer { get; }
        public CreateResult(ObjectId id, Point3d point, EnumEquipmentType equipment, string floorUid, string tag, string layer,string _belongBlockId)
        {
            this.objectId = id;
            this.equipmentType = equipment;
            this.floorUid = floorUid;
            this.tag = tag;
            this.createPoint = point;
            Layer = layer;
            belongBlockId=_belongBlockId;
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
