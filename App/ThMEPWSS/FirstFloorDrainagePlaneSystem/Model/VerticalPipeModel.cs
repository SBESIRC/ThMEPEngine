using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Config;

namespace ThMEPWSS.FirstFloorDrainagePlaneSystem.Model
{
    

    public class VerticalPipeModel
    {
        public VerticalPipeModel() { }

        public VerticalPipeModel(Point3d point, Circle pipe, List<Line> markLines, DBText dBText, VerticalPipeType verticalPipe)
        {
            Position = point;
            PipeCircle = pipe;
            LeadLines = markLines;
            BText = dBText;
            PipeType = verticalPipe;
        }

        /// <summary>
        /// 基点
        /// </summary>
        public Point3d Position { get; set; }

        /// <summary>
        /// 立管（天华图块、天正图块中的圆或者画的圆）
        /// </summary>
        public Circle PipeCircle { get; set; }

        /// <summary>
        /// 连接引线
        /// </summary>
        public List<Line> LeadLines { get; set; }

        /// <summary>
        /// 标注
        /// </summary>
        public DBText BText { get; set; }

        /// <summary>
        /// 是否是洁具点位
        /// </summary>
        public bool IsEuiqmentPipe = false;

        /// <summary>
        /// 立管类型
        /// </summary>
        public VerticalPipeType PipeType { get; set; }
    }

    public enum VerticalPipeType
    {
        /// <summary>
        /// 污水立管
        /// </summary>
        [Description("污水立管")]
        SewagePipe,

        /// <summary>
        /// 废水立管
        /// </summary>
        [Description("废水立管")]
        WasteWaterPipe,

        /// <summary>
        /// 冷凝水立管
        /// </summary>
        [Description("冷凝水立管")]
        CondensatePipe,

        /// <summary>
        /// 污废合流立管
        /// </summary>
        [Description("污废合流立管")]
        ConfluencePipe,

        /// <summary>
        /// 雨水立管
        /// </summary>
        [Description("雨水立管")]
        rainPipe,

        /// <summary>
        /// 洞口立管（仅作为洞口躲避）
        /// </summary>
        [Description("洞口立管")]
        holePipe,
    }
}
