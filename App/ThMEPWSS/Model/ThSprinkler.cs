using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.Model.Plumbing;

namespace ThMEPEngineCore.Model
{
    public class ThSprinkler : ThIfcFireSuppressionTerminal
    {
        public string Category {get;set;}

        public Point3d Position { get; set; }

        public Vector3d Direction { get; set; }
    }
}
