using System;

using Autodesk.AutoCAD.DatabaseServices;

namespace TianHua.Electrical.PDS.Model
{
    public class ThPDSEntityInfo
    {
        /// <summary>
        /// 实体（可能不存在于图纸中）
        /// </summary>
        public Entity Entity { get; set; }

        /// <summary>
        /// 源实体类型
        /// </summary>
        public Type SourceEntityType { get; set; }

        /// <summary>
        /// 源实体Id
        /// </summary>
        public ObjectId SourceObjectId { get; set; }

        public ThPDSEntityInfo(Entity entity, bool clone)
        {
            if(clone)
            {
                Entity = entity.Clone() as Entity;
            }
            else
            {
                Entity = entity;
            }
            SourceEntityType = entity.GetType();
            SourceObjectId = entity.ObjectId;
        }

        public ThPDSEntityInfo(Entity entity, ThPDSEntityInfo info)
        {
            Entity = entity;
            SourceEntityType = info.SourceEntityType;
            SourceObjectId = info.SourceObjectId;
        }
    }
}
