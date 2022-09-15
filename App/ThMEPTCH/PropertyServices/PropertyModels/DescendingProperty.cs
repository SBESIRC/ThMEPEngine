using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPTCH.PropertyServices.PropertyModels
{
    class DescendingProperty : PropertyBase
    {
        /// <summary>
        /// 结构降板厚度
        /// </summary>
        public double StructureThickness { get; set; }

        /// <summary>
        /// 降板面层厚度
        /// </summary>
        public double SurfaceThickness { get; set; }

        /// <summary>
        /// 结构包围厚度
        /// </summary>
        public double StructureWrapThickness { get; set; }

        /// <summary>
        /// 包围面层厚度
        /// </summary>
        public double WrapSurfaceThickness { get; set; }

        public DescendingProperty(ObjectId objectId) : base(objectId)
        {

        }

        public override object Clone()
        {
            var clone = new DescendingProperty(this.ObjId)
            {
                StructureThickness = this.StructureThickness,
                SurfaceThickness = this.SurfaceThickness,
                StructureWrapThickness = this.StructureWrapThickness,
                WrapSurfaceThickness = this.WrapSurfaceThickness,
            };
            return clone;
        }
    }
}
