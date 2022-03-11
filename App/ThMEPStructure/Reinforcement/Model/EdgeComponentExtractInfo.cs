using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPStructure.Reinforcement.Model
{
    public class EdgeComponentExtractInfo
    {
        public Dictionary<string, int> SpecDict { get; set; }=new Dictionary<string, int>();
        /// <summary>
        /// 边构
        /// </summary>
        public Polyline EdgeComponent { get; set; }
        /// <summary>
        /// 编号
        /// eg. GBZ24,GBZ1
        /// </summary>
        public string Number { get; set; } = "";
        /// <summary>
        /// 规格
        /// eg. 一字型: 1650x200,L型：200x800,200,300
        /// </summary>
        public string Spec { get; set; } = "";
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
        /// 是否是标准构件
        /// </summary>
        public bool IsStandard { get; set; }
        /// <summary>
        /// 类型代号，用于标识标准-A,标准-B,标准Cal-A,标准Cal-B
        /// 取值为: A 或 B
        /// </summary>
        public string TypeCode { get; set; } = "";
        public string LinkWallPos { get; set; } = "";
        /// <summary>
        /// 是计算书图层
        /// </summary>

        public bool IsCalculation { get; set; }

        /// <summary>
        /// 配筋率
        /// </summary>
        public double ReinforceRatio { get; set; }
        /// <summary>
        /// 配箍率
        /// </summary>
        public double StirrupRatio { get; set; }

        /// <summary>
        /// 全部纵筋面积
        /// </summary>
        public double AllReinforceArea { get; set; }

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
        public string GetCode()
        {
            //GBZ11->GBZ, 
            if (string.IsNullOrEmpty(Number) || Number.Length<3)
            {
                return "";
            }
            return Number.Substring(0, 3).ToUpper();
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
