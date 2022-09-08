using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPTCH.PropertyServices.PropertyModels
{
    class DescendingProperty : PropertyBase
    {
        /// <summary>
        /// 结构降板厚度
        /// </summary>
        public double Thickness { get; set; }

        /// <summary>
        /// 结构包围厚度
        /// </summary>
        public double WrapThickness { get; set; }

        /// <summary>
        /// 建筑面层厚度
        /// </summary>
        public double SurfaceThickness { get; set; }

        public DescendingProperty(ObjectId objectId) : base(objectId)
        {

        }

        public override object Clone()
        {
            var clone = new DescendingProperty(this.ObjId)
            {
                Thickness = this.Thickness,
                WrapThickness = this.WrapThickness,
                SurfaceThickness = this.SurfaceThickness,
            };
            return clone;
        }
    }
}
