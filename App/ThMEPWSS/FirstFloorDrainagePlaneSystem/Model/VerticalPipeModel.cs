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
        /// 立管类型
        /// </summary>
        public VerticalPipeType PipeType { get; set; }
    }

    public enum VerticalPipeType
    {
        [Description("污水立管")]
        SewagePipe,

        [Description("废水立管")]
        WasteWaterPipe,

        [Description("冷凝水立管")]
        CondensatePipe,

        [Description("污废合流立管")]
        ConfluencePipe,

        [Description("雨水立管")]
        rainPipe,
    }
}
