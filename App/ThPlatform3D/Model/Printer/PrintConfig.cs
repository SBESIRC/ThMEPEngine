using System;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using ThPlatform3D.Common;

namespace ThPlatform3D.Model.Printer
{
    internal class PrintConfig
    {
        public string LayerName { get; set; }
        public string LineType { get; set; }
        public int Color { get; set; }
        public string TextureMaterial { get; set; }
        public LineWeight LineWeight { get; set; }
        public double? LineTypeScale { get; set; }
        public PrintConfig()
        {
            LayerName = "0";
            LineType = "ByLayer";
            TextureMaterial = "";
            LineTypeScale = null;
            LineWeight = LineWeight.ByLayer;
            Color = (int)ColorIndex.BYLAYER;
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
        private string _textStyleName = "";
        /// <summary>
        /// 样式名
        /// </summary>
        public string TextStyleName 
        { 
            get
            {
                return _textStyleName;
            }
            set
            {
                _textStyleName = value;
                SetTextStyleId();
            }
        }
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

        public ObjectId TextStyleId { get; private set; } = ObjectId.Null;

        /// <summary>
        /// 按比例缩放文字高度
        /// </summary>
        /// <param name="drawingScale"></param>
        public void ScaleHeight(string drawingScale)
        {
            var pair = drawingScale.GetDrawScaleValue();
            Height = Height.GetScaleTextHeight(pair.Item1, pair.Item2);
        }
        private void SetTextStyleId()
        {
            if (string.IsNullOrEmpty(_textStyleName))
            {
                TextStyleId = ObjectId.Null;
            }
            else
            {
                TextStyleId = DbHelper.GetTextStyleId(_textStyleName);
            }
        }
    }
    internal class DimensionPrintConfig : PrintConfig
    {
        private string _dimStyleName = "";
        /// <summary>
        /// 标注样式名
        /// </summary>
        public string DimStyleName
        {
            get
            {
                return _dimStyleName;
            }
            set
            {
                _dimStyleName = value;
            }
        }
    }
}
