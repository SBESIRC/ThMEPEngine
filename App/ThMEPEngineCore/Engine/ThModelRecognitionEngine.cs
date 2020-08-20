using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;

namespace ThMEPEngineCore.Engine
{
    public abstract class ThModelRecognitionEngine
    {
        /// <summary>
        /// 从图纸中提取出来的对象的集合
        /// </summary>
        public List<ThIfcElement> Elements { get; set; }
        /// <summary>
        /// 去重后唯一的柱子
        /// </summary>
        public List<ThIfcElement> ValidElements { get; set; }
        protected ThModelRecognitionEngine()
        {
            Elements = new List<ThIfcElement>();
        }
        public abstract void Recognize(Database database);
        public DBObjectCollection Collect()
        {
            DBObjectCollection dbObjs = new DBObjectCollection();
            Elements.ForEach(o => dbObjs.Add(o.Outline));
            return dbObjs;
        }
        public IEnumerable<ThIfcElement> FilterByOutline(DBObjectCollection objs)
        {
            return Elements.Where(o => objs.Contains(o.Outline));
        }
        public ThIfcElement FilterByOutline(DBObject obj)
        {
            return Elements.Where(o => o.Outline.Equals(obj)).FirstOrDefault();
        }
        public void UpdateValidElements(DBObjectCollection objs)
        {
            ValidElements = new List<ThIfcElement>();
            ValidElements.AddRange(FilterByOutline(objs));
        }
    }
}
