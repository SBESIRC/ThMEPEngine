using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPElectrical.DCL.Data
{
    internal class LightProtectLeadWireStoreyData
    {        
        public string StoreyId { get; set; } = "";
        public string FloorNumber { get; set; } = ""; //1F,2F
        public string BasePoint { get; set; } = "";
        public string StoreyType { get; set; } = ""; // 非标层，标准层
        public Entity StoreyFrameBoundary { get; set; }
        public List<MPolygon> ArchOutlineAreas { get; set; } = new List<MPolygon>();
        public Dictionary<MPolygon,HashSet<DBObject>> OuterColumns { get; set; } = new Dictionary<MPolygon, HashSet<DBObject>>();
        public DBObjectCollection OtherColumns { get; set; } = new DBObjectCollection();
        public Dictionary<MPolygon, HashSet<DBObject>> OuterShearWalls { get; set; } = new Dictionary<MPolygon, HashSet<DBObject>>();
        public DBObjectCollection OtherShearWalls { get; set; } = new DBObjectCollection();
        public DBObjectCollection Beams { get; set; } = new DBObjectCollection();
        public List<Curve> SpecialBelts { get; set; } = new List<Curve>();
        public List<Curve> DualpurposeBelts { get; set; } = new List<Curve>();
    }
}
