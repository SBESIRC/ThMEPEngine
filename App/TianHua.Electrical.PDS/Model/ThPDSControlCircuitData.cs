using Autodesk.AutoCAD.Geometry;

namespace TianHua.Electrical.PDS.Model
{
    public class ThPDSControlCircuitData
    {
        public ThPDSControlCircuitData()
        {
            CircuitUID = "";
            BelongToCPS = false;
        }

        public string CircuitUID { get; set; }
        public bool BelongToCPS { get; set; }
        public Point3d StartPoint { get; set; }
        public Point3d EndPoint { get; set; }
    }
}
