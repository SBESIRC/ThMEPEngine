using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Model
{
    public class ThGeometry
    {
        /// <summary>
        /// 几何信息
        /// </summary>
        public Entity Boundary { get; set; }
        /// <summary>
        /// 属性信息
        /// </summary>
        public Dictionary<string, object> Properties { get; private set; }
        /// <summary>
        /// 构造函数
        /// </summary>
        public ThGeometry()
        {
            Properties = new Dictionary<string, object>();
        }

        public static ThGeometry Create(Entity boundary, Dictionary<string, object> properties)
        {
            return new ThGeometry()
            {
                Boundary = boundary,
                Properties = properties,
            };
        }
    }
}
