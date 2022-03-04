using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPStructure.Reinforcement.Model
{
    public class EdgeComponentExtractInfo
    {
        /// <summary>
        /// 边构
        /// </summary>
        public Polyline EdgeComponent { get; set; }
        /// <summary>
        /// 编号
        /// eg. GBZ24,GBZ1
        /// </summary>
        public string Number { get; set; }
        /// <summary>
        /// 规格
        /// eg. 一字型: 1650x200,L型：200x800,200,300
        /// </summary>
        public string Spec { get; set; }
        /// <summary>
        /// 形状
        /// eg. 一形，L形，T形
        /// </summary>
        public string Shape 
        { 
            get
            {
                return ToString(ShapeCode);
            }
        }
        /// <summary>
        /// 外形代号
        /// </summary>
        internal ShapeCode ShapeCode { get; set; }

        /// <summary>
        /// 类型
        /// eg 标准，标准C,非标
        /// </summary>
        public string Type { get; set; }
        /// <summary>
        /// 类型代号，用于标识标准-A,标准-B,标准Cal-A,标准Cal-B
        /// 取值为: A 或 B
        /// </summary>
        public string TypeCode { get; set; }
        /// <summary>
        /// 配筋率
        /// </summary>
        public double ReinforceRatio { get; set; }
        /// <summary>
        /// 配箍率
        /// </summary>
        public double StirrupRatio { get; set; }

        private string ToString(ShapeCode shapeCode)
        {
            var result = "";
            switch (shapeCode)
            {
                case ShapeCode.L:
                    result = "L形";
                    break;
                case ShapeCode.T:
                    result = "T形";
                    break;
                case ShapeCode.Rect:
                    result = "一形";
                    break;
                default:
                    result = "";
                    break;
            }
            return result;
        }
    }
    internal enum ShapeCode
    {
        L,
        T,
        Rect,
        Unknown
    }
}
