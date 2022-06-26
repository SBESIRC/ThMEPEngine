using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPStructure.Reinforcement.Model
{
    public class ColumnReinforceExtractInfo
    {
        public Dictionary<string, int> SpecDict { get; set; }=new Dictionary<string, int>();
        /// <summary>
        /// 柱子轮廓
        /// </summary>
        public Polyline Outline { get; set; }
        /// <summary>
        /// 编号
        /// </summary>
        public string Number { get; set; } = "";
        /// <summary>
        /// 规格
        /// eg. 矩形型: 1650x200
        /// </summary>
        public string Spec { get; set; } = "";
        /// <summary>
        /// 柱的轴压比
        /// </summary>
        public double Uc { get; set; }
        /// <summary>
        /// 单根角筋的面积
        /// </summary>
        public double Asc { get; set; }
        /// <summary>
        /// 柱节点域抗剪箍筋面积(暂不考虑)
        /// </summary>
        public double Asvj { get; set; }
        /// <summary>
        /// B边配筋面积
        /// </summary>
        public double Asx { get; set; }
        /// <summary>
        /// H边配筋面积
        /// </summary>
        public double Asy { get; set; }
        /// <summary>
        /// 加密区斜截面抗剪箍筋面积(cm2)
        /// </summary>
        public double Asv { get; set; }
        /// <summary>
        /// 非加密区斜截面抗剪箍筋面积(cm2)
        /// </summary>
        public double Asv0 { get; set; }
        /// <summary>
        /// 箍筋标志
        /// </summary>
        public string G { get; set; } = "";
        /// <summary>
        /// 柱全截面的配筋面积
        /// As=2*(Asx+Asy)-4*Asc
        /// </summary>
        public double As
        {
            get
            {
                return 2 * (Asx + Asy) - 4 * Asc;
            }
        }
        public string ColumnType { get; set; } = "";
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
        /// 是计算书图层
        /// </summary>
        public bool IsCalculation { get; set; }
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
        public string StandardType
        {
            get
            {
                return GetStandardType();
            }            
        }
        private string GetStandardType()
        {
            if(IsStandard)
            {
                if(IsCalculation)
                {
                    return "标准C";
                }
                else
                {
                    return "标准";
                }
            }
            else
            {
                return "非标";
            }
        }
        /// <summary>
        /// 查询对应的规格
        /// </summary>
        /// <param name="key">关键字</param>
        /// <returns>规格值</returns>
        public int? QuerySpec(string key)
        {
            foreach (var item in SpecDict)
            {
                if (item.Key.ToLower() == key.ToLower())
                {
                    return item.Value;
                }
            }
            return null;
        }
        /// <summary>
        /// 是否为角柱
        /// </summary>
        public bool IsCornerColumn
        {
            get
            {
                return ColumnType.Contains("角柱");
            }
        }
    }
}
