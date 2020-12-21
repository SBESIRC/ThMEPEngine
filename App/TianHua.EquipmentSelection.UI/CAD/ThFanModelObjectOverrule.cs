using System;
using Linq2Acad;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Service.Hvac;

namespace TianHua.FanSelection.UI.CAD
{
    public class ThFanModelObjectOverrule : ObjectOverrule
    {
        private Dictionary<string, string> SystemMapping { get; set; }

        public ThFanModelObjectOverrule()
        {
            SystemMapping = new Dictionary<string, string>();
        }

        public void Reset()
        {
            SystemMapping.Clear();
        }

        public override DBObject DeepClone(DBObject dbObject, DBObject ownerObject, IdMapping idMap, bool isPrimary)
        {
            DBObject result = base.DeepClone(dbObject, ownerObject, idMap, isPrimary);

            if (dbObject.IsModel())
            {
                UpdateClonedModelIdentifier(dbObject, idMap);
            }

            return result;
        }

        private void UpdateClonedModelIdentifier(DBObject dbObject, IdMapping idMap)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(dbObject.Database))
            {
                if (idMap[dbObject.ObjectId] != null)
                {
                    ObjectId targetId = idMap[dbObject.ObjectId].Value;
                    if (targetId != ObjectId.Null)
                    {
                        var identifier = dbObject.GetModelIdentifier();
                        if (!SystemMapping.ContainsKey(identifier))
                        {
                            SystemMapping.Add(identifier, Guid.NewGuid().ToString());
                        }
                        targetId.UpdateModelIdentifier(SystemMapping[identifier]);
                    }
                }
            }
        }
    }
}
