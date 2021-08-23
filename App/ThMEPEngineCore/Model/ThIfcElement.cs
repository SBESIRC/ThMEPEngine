using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPEngineCore.Model
{
    public abstract class ThIfcElement : ThIfcProduct
    {
        public Entity Outline { get; set; }   

        public virtual void TransformBy(Matrix3d transform)
        {
            Outline.TransformBy(transform);
        }
    }
}
