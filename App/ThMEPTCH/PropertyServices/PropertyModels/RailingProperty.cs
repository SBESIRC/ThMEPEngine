using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPTCH.PropertyServices.PropertyModels
{
    class RailingProperty : PropertyBase
    {
        /// <summary>
        /// 底高
        /// </summary>
        public double BottomElevation { get; set; }

        /// <summary>
        /// 栏杆高
        /// </summary>
        public double Height { get; set; }

        /// <summary>
        /// 厚度
        /// </summary>
        public double Thickness { get; set; }

        public RailingProperty(ObjectId objectId) : base(objectId)
        {

        }

        public override object Clone()
        {
            var clone = new RailingProperty(this.ObjId)
            {
                Height = this.Height,
                Thickness = this.Thickness,
                BottomElevation = this.BottomElevation,
            };
            return clone;
        }
    }
}
