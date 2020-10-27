using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Pipe
{
    public class ThWPipeZone
    {
        public Polyline Boundary { get; set; }
        public Polyline Outline { get; set; }
        public Point3dCollection Pipes { get; set; }
    }
}
