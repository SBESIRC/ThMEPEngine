﻿using System.Linq;
using Autodesk.AutoCAD.Runtime;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Service.Hvac;

namespace TianHua.FanSelection.UI.CAD
{
    public class ThFanModelObjectOverrule : ObjectOverrule
    {
        private List<FanDataModel> Models { get; set; }

        public ThFanModelObjectOverrule()
        {
            Models = new List<FanDataModel>();
        }

        public void Reset()
        {
            Models.Clear();
        }

        public override void Erase(DBObject dbObject, bool erasing)
        {
            // 删除风机图块
            base.Erase(dbObject, erasing);

            // 删除风机模型
            EraseModelData(dbObject, erasing);
        }

        public override DBObject DeepClone(DBObject dbObject, DBObject ownerObject, IdMapping idMap, bool isPrimary)
        {
            DBObject result = base.DeepClone(dbObject, ownerObject, idMap, isPrimary);

            // 处理COPY命令
            if (idMap.DeepCloneContext == DeepCloneType.Copy)
            {
                CacheModelData(dbObject, idMap);
                CloneModelData(result, idMap);
            }

            // 处理PASTECLIP命令
            if (idMap.DeepCloneContext == DeepCloneType.Explode)
            {
                CloneModelData(result, idMap);
            }

            return result;
        }

        public override DBObject WblockClone(DBObject dbObject, RXObject ownerObject, IdMapping idMap, bool isPrimary)
        {
            DBObject result = base.WblockClone(dbObject, ownerObject, idMap, isPrimary);

            // 处理WBLOCK命令
            if (idMap.DeepCloneContext == DeepCloneType.Wblock)
            {
                CacheModelData(dbObject, idMap);
            }

            return result;
        }

        private void CloneModelData(DBObject dbObject, IdMapping idMap)
        {
            //
            var dest = new ThFanModelDataDbSource();
            dest.Load(idMap.DestinationDatabase);

            // 
            var identifier = dbObject.GetModelIdentifier();
            if (Models.Where(o => o.ID == identifier).Any())
            {
                if (dest.Models.Where(o => o.ID == identifier).Any())
                {
                    // 复制风机模型
                    var modelId = dest.Models.CloneModel(identifier);
                    dest.Save(idMap.DestinationDatabase);

                    // 关联风机到新的风机模型
                    dbObject.UpdateModelIdentifier(modelId);
                }
                else
                {
                    // 复制风机模型
                    var modelId = "";
                    dest.Models.AddRange(Models.CloneModel(identifier, ref modelId));
                    dest.Save(idMap.DestinationDatabase);

                    // 关联风机到新的风机模型
                    dbObject.UpdateModelIdentifier(modelId);
                }
            }
        }

        private void CacheModelData(DBObject dbObject, IdMapping idMap)
        {
            //
            var orig = new ThFanModelDataDbSource();
            orig.Load(idMap.OriginalDatabase);

            // 
            var identifier = dbObject.GetModelIdentifier();
            if (orig.Models.Any(o => o.ID == identifier) && 
                !Models.Any(o => o.ID == identifier))
            {
                Models.AddRange(orig.Models.Where(o => o.ID == identifier));
                Models.AddRange(orig.Models.Where(o => o.PID == identifier));
            }
        }

        private void EraseModelData(DBObject dbObject, bool erasing)
        {
            var identifier = dbObject.GetModelIdentifier();
            if (!string.IsNullOrEmpty(identifier) && erasing)
            {
                var ds = new ThFanModelDataDbSource();
                ds.Erase(dbObject.Database, identifier);
            }
        }
    }
}
