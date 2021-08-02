using Autodesk.AutoCAD.Geometry;

namespace ThMEPWSS.DrainageSystemDiagram
{
    public class ThToiletGJson
    {
        public string Id { get; set; }
        public string AreaId { get; set; }
        public Vector3d Direction { get; set; }
        public string GroupId { get; set; }
        public Point3d Pt { get; set; }

    }
}
