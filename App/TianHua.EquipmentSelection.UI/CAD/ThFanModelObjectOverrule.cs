using System;
using Linq2Acad;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;

namespace TianHua.FanSelection.UI.CAD
{
    public class ThFanModelObjectOverrule : ObjectOverrule
    { 
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
                        var identifier = Guid.NewGuid().ToString();
                        targetId.UpdateModelIdentifier(identifier);
                    }
                }
            }
        }
    }
}
