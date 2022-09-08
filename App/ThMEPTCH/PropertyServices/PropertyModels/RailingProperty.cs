using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPTCH.PropertyServices.PropertyModels
{
    class RailingProperty : PropertyBase
    {
        /// <summary>
        /// 底高
        /// </summary>
        public double RailingBottomHeight { get; set; }

        /// <summary>
        /// 栏杆高
        /// </summary>
        public double RailingHeight { get; set; }

        /// <summary>
        /// 厚度
        /// </summary>
        public double RailingThickness { get; set; }

        public RailingProperty(ObjectId objectId) : base(objectId)
        {

        }

        public override object Clone()
        {
            var clone = new RailingProperty(this.ObjId)
            {
                RailingHeight = this.RailingHeight,
                RailingThickness = this.RailingThickness,
                RailingBottomHeight = this.RailingBottomHeight,
            };
            return clone;
        }
    }
}
