using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Algorithm
{
    public class ThMEPXClipInfo
    {
        public Polyline Polygon { get; set; }
        public bool Inverted { get; set; }

        public bool IsValid
        {
            get
            {
                return Polygon != null;
            }
        }
    }
}
