using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Model;

namespace ThMEPEngineCore.Engine
{
    public abstract class ThModelRecognitionEngine
    {
        /// <summary>
        /// 从图纸中提取出来的对象的集合
        /// </summary>
        public abstract List<ThIfcElement> Elements { get; set; }
        public abstract void Recognize();
    }
}
