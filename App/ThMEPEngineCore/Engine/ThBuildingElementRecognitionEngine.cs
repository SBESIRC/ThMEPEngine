using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;

namespace ThMEPEngineCore.Engine
{
    public abstract class ThBuildingElementRecognitionEngine
    {
        /// <summary>
        /// 从图纸中提取出来的对象的集合
        /// </summary>
        public List<ThIfcBuildingElement> Elements { get; set; }
        /// <summary>
        /// 去重后唯一的柱子
        /// </summary>
        public List<ThIfcBuildingElement> ValidElements { get; set; }
        protected ThBuildingElementRecognitionEngine()
        {
            Elements = new List<ThIfcBuildingElement>();
        }
        public abstract void Recognize(Database database);
        public DBObjectCollection Collect()
        {
            DBObjectCollection dbObjs = new DBObjectCollection();
            Elements.ForEach(o => dbObjs.Add(o.Outline));
            return dbObjs;
        }
        public IEnumerable<ThIfcBuildingElement> FilterByOutline(DBObjectCollection objs)
        {
            return Elements.Where(o => objs.Contains(o.Outline));
        }
        public ThIfcBuildingElement FilterByOutline(DBObject obj)
        {
            return Elements.Where(o => o.Outline.Equals(obj)).FirstOrDefault();
        }
        public void UpdateValidElements(DBObjectCollection objs)
        {
            ValidElements = new List<ThIfcBuildingElement>();
            ValidElements.AddRange(FilterByOutline(objs));
        }
    }
}
