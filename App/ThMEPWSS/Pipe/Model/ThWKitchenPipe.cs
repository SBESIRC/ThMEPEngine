using Autodesk.AutoCAD.Geometry;
namespace ThMEPWSS.Pipe.Model
{
    public class ThWKitchenPipeParameters
    {
        public double Diameter { get; set; }
        public string Identifier { get; set; }
        public ThWKitchenPipeParameters(int number, double diameter)
        {
            Diameter = diameter;
            Identifier = string.Format("废水FLx{0}", number);
        }    
    }

    /// <summary>
    /// 厨房水管
    /// </summary>
    public class ThWKitchenPipe : ThWPipe
    {
        public Point3d Center { get; set; }
    }
}
