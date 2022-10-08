using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPTCH.PropertyServices.PropertyModels
{
    internal class TCHAxisProperty : PropertyBase
    {
        /// <summary>
        /// 底高
        /// </summary>
        public double BottomElevation { get; set; }
        public string Category { get; set; }
        public TCHAxisProperty(ObjectId objectId) : base(objectId)
        {
            Category = "";
        }
        public override object Clone()
        {
            var clone = new TCHAxisProperty(this.ObjId)
            {
                Category = this.Category,
                BottomElevation = this.BottomElevation,
            };
            return clone;
        }
    }
}
