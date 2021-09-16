using System.Linq;
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

        public override DBObject DeepClone(DBObject dbObject, DBObject ownerObject, IdMapping idMap, bool isPrimary)
        {
            DBObject result = base.DeepClone(dbObject, ownerObject, idMap, isPrimary);

            // 处理PASTECLIP命令
            if (idMap.DeepCloneContext == DeepCloneType.Explode)
            {
                CloneModels(result, idMap);
            }

            // 处理COPY命令
            if (idMap.DeepCloneContext == DeepCloneType.Copy)
            {
                CacheModels(dbObject, idMap);
                CloneModels(result, idMap);
            }

            return result;
        }

        public override DBObject WblockClone(DBObject dbObject, RXObject ownerObject, IdMapping idMap, bool isPrimary)
        {
            DBObject result = base.WblockClone(dbObject, ownerObject, idMap, isPrimary);

            if (idMap.DeepCloneContext == DeepCloneType.Wblock)
            {
                CacheModels(dbObject, idMap);
            }

            return result;
        }

        private void CloneModels(DBObject dbObject, IdMapping idMap)
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

        private void CacheModels(DBObject dbObject, IdMapping idMap)
        {
            //
            var orig = new ThFanModelDataDbSource();
            orig.Load(idMap.OriginalDatabase);

            // 
            var identifier = dbObject.GetModelIdentifier();
            if (orig.Models.Where(o => o.ID == identifier).Any() &&
                !Models.Where(o => o.ID == identifier).Any())
            {
                Models.AddRange(orig.Models.Where(o => o.ID == identifier));
                Models.AddRange(orig.Models.Where(o => o.PID == identifier));
            }
        }
    }
}
