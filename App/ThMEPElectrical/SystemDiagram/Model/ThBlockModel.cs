using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.SystemDiagram.Model
{

    public class ThBlockModel
    {
        /// <summary>
        /// 连接块
        /// </summary>
        //public BlockReference Block { get; set; }

        /// <summary>
        /// 块名
        /// </summary>
        public string BlockName { get; set; }

        /// <summary>
        /// 外接属性集合
        /// </summary>
        public Dictionary<string, string> attNameValues { get; set; }

        /// <summary>
        /// 是否显示外接属性
        /// </summary>
        public bool ShowAtt { get; set; } = false;

        /// <summary>
        /// 块别名
        /// </summary>
        public string BlockAliasName { get; set; }

        /// <summary>
        /// 块别名/中文名称
        /// </summary>
        public string BlockNameRemark { get; set; }

        /// <summary>
        /// 块索引
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// 记录楼层信息
        /// </summary>
        //public int FloorIndex { get; set; }

        /// <summary>
        /// 相对于黄色框的相对位置
        /// </summary>
        public Point3d Position { get; set; }

        /// <summary>
        /// 块计数为0时是否可隐藏
        /// </summary>
        public bool CanHidden { get; set; } = false;

        /// <summary>
        /// 是否显示块的计数
        /// </summary>
        public bool ShowQuantity { get; set; } = false;

        /// <summary>
        /// 块计数地址
        /// </summary>
        public Point3d QuantityPosition { get; set; }

        /// <summary>
        /// 是否显示块的计数
        /// </summary>
        public bool ShowText { get; set; } = false;

        /// <summary>
        /// 块计数地址
        /// </summary>
        public Point3d TextPosition { get; set; }

        /// <summary>
        /// 是否包含多个块
        /// </summary>
        public bool HasMultipleBlocks { get; set; } = false;

        /// <summary>
        /// 关联的块集合
        /// </summary>
        public List<ThBlockModel> AssociatedBlocks { get; set; }


    }
}
