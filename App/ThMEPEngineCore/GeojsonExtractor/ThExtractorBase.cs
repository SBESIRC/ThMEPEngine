using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.GeojsonExtractor
{
    public abstract class ThExtractorBase
    {
        /// <summary>
        /// 类别
        /// </summary>
        public string Category { get; set; }   
        /// <summary>
        /// 用于将提取的元素打印出要显示的颜色
        /// </summary>

        public short ColorIndex { get; set; }
        /// <summary>
        /// 是否是从DB3图纸数据中提取元素
        /// 默认为True
        /// </summary>

        public bool UseDb3Engine { get; set; }
        /// <summary>
        /// 分组开关，用于控制是否分组
        /// </summary>
        public bool GroupSwitch { get; set; }
        /// <summary>
        /// 表示在BuildGeometry时只输出孤立的元素
        /// </summary>
        public bool IsolateSwitch { get; set; }

        public string ElementLayer { get; set; }

        public ThExtractorBase()
        {
            Category = "";
            ElementLayer = "";
            GroupSwitch = false;
            IsolateSwitch = false;
            UseDb3Engine = true;
            ColorIndex = 256;
        }
        public abstract void Extract(Database database, Point3dCollection pts);
        public abstract List<ThGeometry> BuildGeometries();
        public virtual void SetRooms(List<ThIfcRoom> rooms)
        {
            //如果要进行孤立判断，需要将Room传入到对应的Extractor中
        }
    }
    public enum SwitchStatus
    {
        Open,
        Close
    }
    public enum Privacy
    {
        Unknown,
        Private,
        Public
    }
}
