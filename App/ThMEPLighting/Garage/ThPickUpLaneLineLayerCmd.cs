using System;
using AcHelper;
using Linq2Acad;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Command;
using ThMEPLighting.ViewModel;

namespace ThMEPLighting.Garage
{
    public class ThPickUpLaneLineLayerCmd : ThMEPBaseCommand, IDisposable
    {
        LightingViewModel _UiConfigs;
        public ThPickUpLaneLineLayerCmd(LightingViewModel uiConfigs)
        {
            _UiConfigs = uiConfigs;
        }
        public void Dispose()
        {
        }
        public override void SubExecute()
        {
            using (var docLock = Active.Document.LockDocument())
            using (var acdb = AcadDatabase.Active())
            {
                while(true)
                {
                    var pneo = new PromptNestedEntityOptions("\n请指一根定车道线:");
                    var pner = Active.Editor.GetNestedEntity(pneo);
                    if (pner.Status == PromptStatus.OK)
                    {
                        if (pner.ObjectId != ObjectId.Null)
                        {
                            var entity = acdb.Element<Entity>(pner.ObjectId);
                            if (entity is Curve)
                            {
                                _UiConfigs.Add(entity.Layer);
                            }
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }
    }
}
