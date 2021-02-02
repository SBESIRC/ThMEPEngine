using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.Garage.Model
{
    public class ThWireOffsetData
    {
        public Line Center { get; set;}
        public Line First { get; set;}
        public Line Second { get; set; }     
        public bool IsDX { get; set; }
    }
}
