using Autodesk.AutoCAD.Geometry;

namespace ThMEPTCH.TCHArchDataConvert.TCHArchTables
{
    public abstract class TArchEntity
    {
        public ulong Id { get; set; }
        public abstract bool IsValid();
        public abstract void TransformBy(Matrix3d transform);
    }
}
