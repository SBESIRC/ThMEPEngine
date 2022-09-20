using Autodesk.AutoCAD.DatabaseServices;
using ThMEPTCH.PropertyServices.PropertyEnums;

namespace ThMEPTCH.PropertyServices.PropertyModels
{
    class SlabProperty : PropertyBase
    {
        /// <summary>
        /// 材质
        /// </summary>
        public EnumSlabMaterial EnumMaterial { get; set; }

        /// <summary>
        /// 建筑顶标高
        /// </summary>
        public double TopElevation { get; set; }

        /// <summary>
        /// 结构板厚
        /// </summary>
        public double Thickness { get; set; }

        /// <summary>
        /// 建筑面层厚度
        /// </summary>
        public double SurfaceThickness { get; set; }

        public SlabProperty(ObjectId objectId) : base(objectId)
        {

        }

        public override object Clone()
        {
            var clone = new SlabProperty(this.ObjId)
            {
                EnumMaterial = this.EnumMaterial,
                TopElevation = this.TopElevation,
                Thickness = this.Thickness,
                SurfaceThickness = this.SurfaceThickness,
            };
            return clone;
        }
    }
}
