using Autodesk.AutoCAD.DatabaseServices;
using ThMEPTCH.PropertyServices.PropertyEnums;

namespace ThMEPTCH.PropertyServices.PropertyModels
{
    class TCHWallProperty : PropertyBase
    {
        public EnumTCHWallMaterial EnumMaterial { get; set; }

        /// <summary>
        /// 材质
        /// </summary>
        public string Material { get; set; }

        /// <summary>
        /// 墙高
        /// </summary>
        public double Height { get; set; }

        /// <summary>
        /// 底高
        /// </summary>
        public double BottomElevation { get; set; }

        public TCHWallProperty(ObjectId objectId) : base(objectId)
        {

        }

        public override object Clone()
        {
            var clone = new TCHWallProperty(this.ObjId)
            {
                Material = this.Material,
                EnumMaterial = this.EnumMaterial,
                Height = this.Height,
                BottomElevation = this.BottomElevation,
            };
            return clone;
        }
    }
}
