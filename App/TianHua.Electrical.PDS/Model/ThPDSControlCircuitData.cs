using Autodesk.AutoCAD.Geometry;

namespace TianHua.Electrical.PDS.Model
{
    public class ThPDSControlCircuitData
    {
        public ThPDSControlCircuitData()
        {
            CircuitNumber = "";
            BelongToCPS = false;
        }

        public string CircuitNumber { get; set; }
        public bool BelongToCPS { get; set; }
        public Point3d StartPoint { get; set; }
        public Point3d EndPoint { get; set; }
    }
}
