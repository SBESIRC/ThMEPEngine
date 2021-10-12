using System;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Model.Hvac
{
    public class ThIfcDuctSegmentParameters
    {
        /// <summary>
        /// 风管长度
        /// </summary>
        public double Width { get; set; }
        /// <summary>
        /// 风管宽度
        /// </summary>
        public double Length { get; set; }
        /// <summary>
        /// 风管高度
        /// </summary>
        public double Height { get; set; }
        /// <summary>
        /// 几何外包框
        /// </summary>
        public Polyline Outline { get; set; }
    }

    public class ThIfcDuctSegment : ThIfcFlowSegment
    {
        public ThIfcDuctSegmentParameters Parameters { get; set; }

        public ThIfcDuctSegment(ThIfcDuctSegmentParameters parameters)
        {
            Parameters = parameters;
        }

        public static ThIfcDuctSegment Create(ThIfcDuctSegmentParameters parameters)
        {
            throw new NotImplementedException();
        }
    }
}
