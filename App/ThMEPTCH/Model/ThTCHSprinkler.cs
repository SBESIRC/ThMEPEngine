using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.Model.Plumbing;

namespace ThMEPTCH.Model
{
    public class ThTCHSprinkler : ThIfcSprinkler
    {
        public Point3d Location { get; set; }
        public int Type { get; set; }
        public int LinkMode { get; set; }
        public string System { get; set; }
        public double Radius { get; set; }
        public double PipeLength { get; set; }
        public double PipeDn { get; set; }
        public int K { get; set; }
        public double Angle { get; set; }
        public double SizeX { get; set; }
        public double SizeY { get; set; }
        public int HidePipe { get; set; }
        public int MirrorByX { get; set; }
        public int MirrorByY { get; set; }
        public double DocScale { get; set; }
    }
}
