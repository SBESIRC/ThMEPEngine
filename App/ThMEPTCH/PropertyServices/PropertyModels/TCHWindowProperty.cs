using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPTCH.PropertyServices.PropertyModels
{
    class TCHWindowProperty : PropertyBase
    {
        /// <summary>
        /// 是否门窗统计
        /// </summary>
        public bool Statistics { get; set; }

        /// <summary>
        /// 底高
        /// </summary>
        public double BottomElevation { get; set; }

        /// <summary>
        /// 窗高
        /// </summary>
        public double Height { get; set; }

        /// <summary>
        /// 编号前缀
        /// </summary>
        public string NumberPrefix { get; set; }

        /// <summary>
        /// 编号后缀
        /// </summary>
        public string NumberPostfix { get; set; }

        public TCHWindowProperty(ObjectId objectId) : base(objectId)
        {

        }

        public override object Clone()
        {
            var clone = new TCHWindowProperty(this.ObjId)
            {
                Statistics = this.Statistics,
                BottomElevation = this.BottomElevation,
                Height = this.Height,
                NumberPrefix = this.NumberPrefix,
                NumberPostfix = this.NumberPostfix,
            };
            return clone;
        }
    }
}
