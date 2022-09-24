using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPTCH.PropertyServices.PropertyModels
{
    class RoomProperty : PropertyBase
    {
        /// <summary>
        /// 底高
        /// </summary>
        public double BottomElevation { get; set; }

        public double Height { get; set; }

        public string Name { get; set; }

        public RoomProperty(ObjectId objectId) : base(objectId)
        {
        }

        public override object Clone()
        {
            var clone = new RoomProperty(this.ObjId)
            {
                Name = this.Name,
                Height = this.Height,
                BottomElevation = this.BottomElevation,
            };
            return clone;
        }
    }
}
