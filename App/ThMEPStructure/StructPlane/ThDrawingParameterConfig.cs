using System.Collections.Generic;

namespace ThMEPStructure.StructPlane
{
    public class ThDrawingParameterConfig
    {
        private static readonly ThDrawingParameterConfig instance = new ThDrawingParameterConfig() { };
        public static ThDrawingParameterConfig Instance { get { return instance; } }
        internal ThDrawingParameterConfig()
        {
            DrawingScale = "1:100";
            FileFormatOption = "IFC";
            Storeies = new List<string>();            
            DrawingScales = new List<string> { "1:100", "1:150" };
        }
        static ThDrawingParameterConfig()
        {
        }
        public List<string> DrawingScales { get; private set; }
        public List<string> Storeies { get; set; }

        /// <summary>
        /// 图纸比例
        /// </summary>
        public string DrawingScale { get; set; } = "";
        /// <summary>
        /// 默认板厚
        /// </summary>
        public double DefaultSlabThick { get; set; }
        /// <summary>
        /// 楼层
        /// </summary>
        public string Storey { get; set; } = "";
        /// <summary>
        /// 全部楼层
        /// </summary>
        public bool IsAllStorey { get; set; }
        /// <summary>
        /// 文件格式
        /// </summary>
        public string FileFormatOption { get; set; } = "";
    }
}
