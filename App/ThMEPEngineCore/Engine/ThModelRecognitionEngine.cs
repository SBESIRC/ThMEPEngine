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
    }
}
