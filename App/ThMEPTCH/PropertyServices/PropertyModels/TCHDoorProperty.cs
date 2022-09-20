using Autodesk.AutoCAD.DatabaseServices;
using ThMEPTCH.PropertyServices.PropertyEnums;

namespace ThMEPTCH.PropertyServices.PropertyModels
{
    class TCHDoorProperty : PropertyBase
    {
        /// <summary>
        /// 是否门窗统计
        /// </summary>
        public bool Statistics { get; set; }

        /// <summary>
        /// 门高
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

        /// <summary>
        /// 安全出口
        /// </summary>
        public bool Entrance { get; set; }

        public TCHDoorProperty(ObjectId objectId) : base(objectId)
        {

        }

        public override object Clone()
        {
            var clone = new TCHDoorProperty(this.ObjId)
            {
                Statistics = this.Statistics,
                Height = this.Height,
                NumberPrefix = this.NumberPrefix,
                NumberPostfix = this.NumberPostfix,
                Entrance = this.Entrance,
            };
            return clone;
        }
    }
}
