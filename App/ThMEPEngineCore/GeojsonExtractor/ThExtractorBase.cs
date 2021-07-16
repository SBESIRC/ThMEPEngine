﻿using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Service;
using ThCADCore.NTS;
using NFox.Cad;
using System.Linq;

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
        /// 分组开关，用于控制是否分组
        /// </summary>
        public bool Group2Switch { get; set; }
        /// <summary>
        /// 表示在BuildGeometry时只输出孤立的元素
        /// </summary>
        public bool IsolateSwitch { get; set; }

        public string ElementLayer { get; set; }
        /// <summary>
        /// 是通过Window/Cross筛选
        /// </summary>
        public FilterMode FilterMode { get; set; }
        /// <summary>
        /// 房间框线处理后导致区域变化
        /// </summary>
        protected double LoopBufferLength = 10.0;
        protected Dictionary<Entity, List<string>> GroupOwner { get; set; }
        protected Dictionary<Entity, List<string>> Group2Owner { get; set; }
        public ThExtractorBase()
        {
            Category = "";
            ElementLayer = "";
            GroupSwitch = false;
            Group2Switch = false;
            IsolateSwitch = false;
            UseDb3Engine = true;
            ColorIndex = 256;
            FilterMode = FilterMode.Cross;
            GroupOwner = new Dictionary<Entity, List<string>>();
            Group2Owner = new Dictionary<Entity, List<string>>();
        }
        public abstract void Extract(Database database, Point3dCollection pts);
        public abstract List<ThGeometry> BuildGeometries();
        public virtual void SetRooms(List<ThIfcRoom> rooms)
        {
            //如果要进行孤立判断，需要将Room传入到对应的Extractor中
        }
        protected virtual List<Entity> FilterWindowPolygon(Point3dCollection pts,List<Entity> ents)
        {
            var loop = pts.CreatePolyline();
            var bufferService = new ThNTSBufferService();
            var enlarge = bufferService.Buffer(loop, LoopBufferLength) as Polyline;
            var spatialIndex = new ThCADCoreNTSSpatialIndex(ents.ToCollection());
            return spatialIndex.SelectWindowPolygon(enlarge).Cast<Entity>().ToList();
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
    public enum FilterMode
    {
        Window,
        Cross
    }
}
