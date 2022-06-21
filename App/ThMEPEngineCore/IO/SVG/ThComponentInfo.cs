using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.IO.SVG
{
    public class ThComponentInfo
    {
        public Entity Element { get; set; }
        /// <summary>
        /// 类型(IfcWindow,IfcDoor)
        /// </summary>
        public string Type { get; set; } = "";
        /// <summary>
        /// 起点
        /// </summary>
        public string Start { get; set; } = "";
        /// <summary>
        /// 终点
        /// </summary>
        public string End { get; set; } = "";
        /// <summary>
        /// 门、窗所在的洞宽度
        /// </summary>
        public string HoleWidth { get; set; }="";
        /// <summary>
        /// 门、窗所在的洞高度
        /// </summary>
        public string HoleHeight { get; set; } = "";
        /// <summary>
        /// 矩阵
        /// </summary>
        public string Matrix { get; set; } = "";
        /// <summary>
        /// 旋转角度
        /// </summary>
        public string Rotation { get; set; } = "";
        /// <summary>
        /// 圆心角
        /// </summary>
        public string CenterAngle { get; set; } = "";
        /// <summary>
        /// 开启方向<0,1,2,3>
        /// </summary>
        public string OpenDirection { get; set; } = "";
        /// <summary>
        /// 实体对应的块名
        /// </summary>
        public string BlockName { get; set; } = "";
        /// <summary>
        /// 构件自身的厚度
        /// </summary>
        public string Thickness { get; set; } = "";
        public string BasePoint { get; set; } = "";
        public string CenterPoint { get; set; } = "";
        public ThComponentInfo()
        {            
        }
    }
}
