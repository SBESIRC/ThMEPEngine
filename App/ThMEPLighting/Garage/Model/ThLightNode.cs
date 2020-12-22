using Autodesk.AutoCAD.Geometry;
using System.Text.RegularExpressions;

namespace ThMEPLighting.Garage.Model
{
    public class ThLightNode
    {
        public Point3d Position { get; set; }

        public string Number { get; set; }
        public ThLightNode()
        {
            Number = "";
        }
        public int GetIndex()
        {
            var match = Regex.Match(Number, @"\d*$");
            return string.IsNullOrEmpty(match.Value) ? -1 : int.Parse(match.Value);
        }
    }
}
