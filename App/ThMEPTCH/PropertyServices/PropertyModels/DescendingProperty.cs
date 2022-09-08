using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPTCH.PropertyServices.PropertyModels
{
    class DescendingProperty : PropertyBase
    {
        /// <summary>
        /// 结构降板厚度
        /// </summary>
        public double DescendingThickness { get; set; }

        /// <summary>
        /// 结构包围厚度
        /// </summary>
        public double DescendingWrapThickness { get; set; }

        /// <summary>
        /// 建筑面层厚度
        /// </summary>
        public double DescendingSurfaceThickness { get; set; }

        public DescendingProperty(ObjectId objectId) : base(objectId)
        {

        }

        public override object Clone()
        {
            var clone = new DescendingProperty(this.ObjId)
            {
                DescendingThickness = this.DescendingThickness,
                DescendingWrapThickness = this.DescendingWrapThickness,
                DescendingSurfaceThickness = this.DescendingSurfaceThickness,
            };
            return clone;
        }
    }
}
