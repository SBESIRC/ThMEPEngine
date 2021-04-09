using Autodesk.AutoCAD.Geometry;

namespace ThMEPEngineCore.SplitMPolygon.Model
{
    public class ThRectangle
    {
        public double MinX { get; set; }
        public double MinY { get; set; }
        public double MaxX { get; set; }
        public double MaxY { get; set; }
        public bool InHole { get; set; }

        public Point3d Center
        {
            get
            {
                return new Point3d((MinX + MaxX) / 2.0, (MinY + MaxY) / 2.0, 0);
            }
        }

        public double Width
        {
            get
            {
                return MaxX - MinX;
            }
        }
        public double Height
        {
            get
            {
                return MaxY - MinY;
            }
        }
    }
}
