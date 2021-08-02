using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;

namespace ThMEPWSS.FireProtectionSystemDiagram.Models
{
    public class CreateBlockInfo
    {
        public string layerName { get; }
        public string blockName { get; }
        public Point3d createPoint { get; set; }
        public double rotateAngle { get; set; }
        public double scaleNum { get; set; }
        public Dictionary<string, string> attNameValues { get; }
        public Dictionary<string, object> dymBlockAttr { get; }
        public string tag { get; set; }
        public CreateBlockInfo(string blockName, string layerName, Point3d createPoint)
        {
            this.layerName = layerName;
            this.blockName = blockName;
            this.createPoint = createPoint;
            this.attNameValues = new Dictionary<string, string>();
            this.dymBlockAttr = new Dictionary<string, object>();
            this.rotateAngle = 0;
            this.scaleNum = 1;
        }
    }
    public class CreateBasicElement
    {
        public Curve baseCurce { get; }
        public Color lineColor { get; }
        public string layerName { get; set; }
        public CreateBasicElement(Curve curve,string layerName,Color lineColor =null) 
        {
            this.baseCurce = curve;
            this.layerName = layerName;
            this.lineColor = lineColor;
        }

    }
    public class CreateDBTextElement 
    {
        public string layerName { get; }
        public string textStyle { get; }
        public Point3d textPoint { get; }
        public DBText dbText { get; }
        public CreateDBTextElement(Point3d textPoint,DBText dBText,string layerName,string textStyle)
        {
            this.layerName = layerName;
            this.textStyle = textStyle;
            this.textPoint = textPoint;
            this.dbText = dBText;
        }
    }
}
