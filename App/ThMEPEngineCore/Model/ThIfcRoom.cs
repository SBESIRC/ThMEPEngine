using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Model
{
    public class ThIfcRoom : ThIfcSpatialElement
    {
        /// <summary>
        /// 标签
        /// </summary>
        public List<string> Tags { get; set; }

        /// <summary>
        /// 轮廓线
        /// </summary>
        public Entity Boundary { get; set; }

        public static ThIfcRoom Create(Entity entity)
        {
            return new ThIfcRoom()
            {
                Boundary = entity,                
                Tags = new List<string>(),
            };
        }

        public static ThIfcRoom CreateWithTags(Entity entity, List<string> tags)
        {
            return new ThIfcRoom()
            {
                Tags = tags,
                Boundary = entity,
            };
        }
    }
}
