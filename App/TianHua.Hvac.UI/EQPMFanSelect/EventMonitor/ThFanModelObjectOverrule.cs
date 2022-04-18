using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using System;
using ThMEPEngineCore.Service.Hvac;
using ThMEPHVAC.EQPMFanSelect;

namespace TianHua.Hvac.UI.EQPMFanSelect.EventMonitor
{
    public class ThFanModelObjectOverrule : ObjectOverrule
    {
        public ThFanModelObjectOverrule()
        { }
        public override DBObject DeepClone(DBObject dbObject, DBObject ownerObject, IdMapping idMap, bool isPrimary)
        {
            DBObject result = base.DeepClone(dbObject, ownerObject, idMap, isPrimary);

            // 处理COPY命令
            if (idMap.DeepCloneContext == DeepCloneType.Copy)
            {
            }

            // 处理PASTECLIP命令
            if (idMap.DeepCloneContext == DeepCloneType.Explode)
            {
                CloneModelData(result, idMap);
            }

            return result;
        }
        private void CloneModelData(DBObject dbObject, IdMapping idMap)
        {
            if (dbObject == null)
                return;
            if (dbObject is BlockReference block)
            {
                using (AcadDatabase acadDatabase = AcadDatabase.Use(dbObject.Database))
                {
                    var pFanModel = FanDataModelExtension.ReadBlockAllFanData(block, out FanDataModel cFanModel, out bool isCopy);
                    if (pFanModel == null)
                        return;
                    if (!isCopy)
                        return;
                    pFanModel.ID = Guid.NewGuid().ToString();
                    if (null != cFanModel)
                    {
                        cFanModel.ID = Guid.NewGuid().ToString();
                        cFanModel.PID = pFanModel.ID;
                    }
                    dbObject.Id.SetModelIdentifier(pFanModel.XDataValueList(1, cFanModel, dbObject.Id.Handle.ToString()), ThHvacCommon.RegAppName_FanSelectionEx);
                }
            }
        }
    }
}
