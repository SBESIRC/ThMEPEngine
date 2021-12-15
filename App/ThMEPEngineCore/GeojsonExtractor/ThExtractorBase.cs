using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Algorithm;
using ThCADExtension;

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
        /// <summary>
        /// 是通过Window/Cross筛选
        /// </summary>
        public FilterMode FilterMode { get; set; }
        /// <summary>
        /// 房间框线处理后导致区域变化
        /// </summary>
        protected double LoopBufferLength = 10.0;

        protected double TesslateLength = 10.0;
        protected double SmallAreaTolerance = 1.0;
        protected Dictionary<Entity, List<string>> GroupOwner { get; set; }

        protected ThMEPOriginTransformer transformer;
        public ThExtractorBase()
        {
            Category = "";
            ElementLayer = "";
            GroupSwitch = false;
            IsolateSwitch = false;
            UseDb3Engine = true;
            ColorIndex = 256;
            FilterMode = FilterMode.Cross;
            GroupOwner = new Dictionary<Entity, List<string>>();
            transformer = new ThMEPOriginTransformer(Point3d.Origin);
        }
        public virtual void Extract(Database database, Point3dCollection pts)
        {
            //TODO
        }
        public abstract List<ThGeometry> BuildGeometries();
        public virtual void SetRooms(List<ThIfcRoom> rooms)
        {
            //如果要进行孤立判断，需要将Room传入到对应的Extractor中
        }
        protected virtual List<Entity> FilterWindowPolygon(Point3dCollection pts,List<Entity> ents)
        {
            var center = pts.Envelope().CenterPoint();
            var transformer = new ThMEPOriginTransformer(center);
            ents.ForEach(e=> transformer.Transform(e));
            var newPts = transformer.Transform(pts);
            var loop = newPts.CreatePolyline();
            var bufferService = new ThNTSBufferService();
            var enlarge = bufferService.Buffer(loop, LoopBufferLength) as Polyline;
            var spatialIndex = new ThCADCoreNTSSpatialIndex(ents.ToCollection());
            var querys = spatialIndex.SelectWindowPolygon(enlarge).Cast<Entity>().ToList();
            querys.ForEach(e=> transformer.Reset(e));
            return querys;
        }
        protected string BuildString(Dictionary<Entity, List<string>> owners, Entity curve, string linkChar = ";")
        {
            if (owners.ContainsKey(curve))
            {
                return string.Join(linkChar, owners[curve]);
            }
            return "";
        }
        protected List<string> FindCurveGroupIds(Dictionary<Entity, string> groupId, Entity curve)
        {
            var ids = new List<string>();
            var groups = groupId.Select(g => g.Key).ToList().Where(g => g.IsContains(curve)).ToList();
            groups.ForEach(g => ids.Add(groupId[g]));
            return ids;
        }
        public virtual List<Entity> GetEntities()
        {
            return new List<Entity>();
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
