using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPTCH.TCHArchDataConvert.TCHArchTables;

namespace ThMEPTCH.TCHArchDataConvert.THArchEntity
{
    public abstract class THArchEntityBase
    {
        public ulong DBId { get; }
        public MPolygon Outline { get; set; }
        public TArchEntity DBArchEntity { get; }
        public THArchEntityBase(TArchEntity dbEntity)
        {
            DBId = dbEntity.Id;
            DBArchEntity = dbEntity;
        }
        public virtual void TransformBy(Matrix3d transform)
        {
            Outline.TransformBy(transform);
            DBArchEntity.TransformBy(transform);
        }
    }
}
