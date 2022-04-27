using System;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPStructure.StructPlane.Print
{
    internal class PrintConfig
    {
        public string LayerName { get; set; }
        public string LineType { get; set; }
        public LineWeight LineWeight { get; set; }
        public PrintConfig()
        {
            LayerName = "0";
            LineType = "ByLayer";
            LineWeight = LineWeight.ByLayer;
        }
    }
    internal class HatchPrintConfig : PrintConfig,ICloneable
    {
        public string PatternName { get; set; }
        public double PatternScale { get; set; }
        public double PatternAngle { get; set; }
        public double PatternSpace { get; set; }
        public double Elevation { get; set; }
        public bool Associative { get; set; }
        public Vector3d Normal { get; set; }
        public HatchPatternType PatternType { get; set; }
        public HatchPrintConfig()
        {
            PatternName = "SOLID";
            PatternScale = 1.0;
            LayerName = "0";
            PatternSpace = 1.0;
            PatternAngle = 0.0;
            Associative = true;
            Elevation = 0.0;
            Normal = new Vector3d(0.0, 0.0, 1.0);
            PatternType = HatchPatternType.PreDefined;
        }

        public object Clone()
        {
            var hatchConfig = new HatchPrintConfig();
            hatchConfig.PatternName = this.PatternName;
            hatchConfig.PatternType = this.PatternType;
            hatchConfig.PatternScale = this.PatternScale;
            hatchConfig.PatternSpace = this.PatternSpace;
            hatchConfig.Elevation = this.Elevation;
            hatchConfig.Associative = this.Associative;
            hatchConfig.Normal = this.Normal;
            hatchConfig.PatternType = this.PatternType;
            hatchConfig.LayerName = base.LayerName;
            hatchConfig.LineType = base.LineType;
            hatchConfig.LineWeight  = base.LineWeight;
            return hatchConfig;
        }
    }
    internal class AnnotationPrintConfig : PrintConfig
    {
        /// <summary>
        /// 文字高度
        /// </summary>
        public double Height { get; set; }
        /// <summary>
        /// 样式名
        /// </summary>
        public string TextStyleName { get; set; } = "";
        /// <summary>
        /// 文字内容
        /// </summary>
        public string TextString { get; set; } = "";
        /// <summary>
        /// 旋转角度
        /// </summary>
        public double Rotation { get; set; }
        /// <summary>
        /// 宽度因子
        /// </summary>
        public double WidthFactor { get; set; }
        /// <summary>
        /// 倾斜角度
        /// </summary>
        public double Oblique { get; set; }
    }
}
