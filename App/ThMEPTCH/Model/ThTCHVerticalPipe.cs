using Autodesk.AutoCAD.Geometry;

namespace ThMEPTCH.Model
{
    public class ThTCHVerticalPipe
    {
        public Point3d PipeBottomPoint;
        public Point3d PipeTopPoint;
        public string PipeSystem;
        public string PipeMaterial;
        public string DnType;
        public string ShortCode;
        public double PipeDN;
        public string FloorNum;
        public string PipeNum;
        public Point3d TurnPoint;
        public Vector3d TextDirection;
        public string PipeDimText;
        public string DimTypeText;
        public double TextHeight;
        public string TextStyle;
        public int DimType;
        public double DocScale;
        public double DimRadius;
        public double Spacing;
        public int FloorType;
    }
}
