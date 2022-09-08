using Autodesk.AutoCAD.DatabaseServices;
using ThMEPTCH.PropertyServices.PropertyEnums;

namespace ThMEPTCH.PropertyServices.PropertyModels
{
    class SlabProperty : PropertyBase
    {
        public EnumSlabMaterial EnumMaterial { get; set; }

        /// <summary>
        /// 材质
        /// </summary>
        public string Material { get; set; }

        /// <summary>
        /// 建筑顶标高
        /// </summary>
        public double SlabTopElevation { get; set; }

        /// <summary>
        /// 结构板厚
        /// </summary>
        public double SlabThickness { get; set; }

        /// <summary>
        /// 建筑面层厚度
        /// </summary>
        public double SlabBuildingSurfaceThickness { get; set; }

        public SlabProperty(ObjectId objectId) : base(objectId)
        {

        }

        public override object Clone()
        {
            var clone = new SlabProperty(this.ObjId)
            {
                Material = this.Material,
                EnumMaterial = this.EnumMaterial,
                SlabTopElevation = this.SlabTopElevation,
                SlabThickness = this.SlabThickness,
                SlabBuildingSurfaceThickness = this.SlabBuildingSurfaceThickness,
            };
            return clone;
        }
    }
}
