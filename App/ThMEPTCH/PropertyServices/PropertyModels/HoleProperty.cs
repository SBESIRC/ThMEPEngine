using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPTCH.PropertyServices.PropertyModels
{
    class HoleProperty : PropertyBase
    {
        /// <summary>
        /// 是否忽略尺寸标注
        /// </summary>
        public bool ShowDimension { get; set; }

        /// <summary>
        /// 是否遮挡元素
        /// </summary>
        public bool Hidden { get; set; }

        /// <summary>
        /// 底高
        /// </summary>
        public double BottomHeight { get; set; }

        /// <summary>
        /// 洞高
        /// </summary>
        public double HoleHeight { get; set; }

        /// <summary>
        /// 编号前缀
        /// </summary>
        public string NumberPrefix { get; set; }

        /// <summary>
        /// 编号后缀
        /// </summary>
        public string NumberPostfix { get; set; }

        /// <summary>
        /// 立面显示
        /// </summary>
        public bool ElevationDisplay { get; set; }

        public HoleProperty(ObjectId objectId) : base(objectId)
        {

        }

        public override object Clone()
        {
            var clone = new HoleProperty(this.ObjId)
            {
                ShowDimension = this.ShowDimension,
                Hidden = this.Hidden,
                BottomHeight = this.BottomHeight,
                HoleHeight = this.HoleHeight,
                NumberPrefix = this.NumberPrefix,
                NumberPostfix = this.NumberPostfix,
                ElevationDisplay = this.ElevationDisplay,
            };
            return clone;
        }
    }
}
