using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Service.Hvac;

namespace TianHua.Hvac.UI.EQPMFanSelect.EventMonitor
{
    class ThFanSelectionDbUndoHandler : IDisposable
    {
        private Database Database;
        public List<string> UnappendedModels { get; }
        public List<string> ReappendedModels { get;}

        public ThFanSelectionDbUndoHandler(Database database)
        {
            Database = database;
            UnappendedModels = new List<string>();
            ReappendedModels = new List<string>();
            Database.ObjectReappended += DbEvent_ObjectReappended_Handler;
            Database.ObjectUnappended += DbEvent_ObjectUnappended_Handler;
        }

        public void Dispose()
        {
            Database.ObjectReappended -= DbEvent_ObjectReappended_Handler;
            Database.ObjectUnappended -= DbEvent_ObjectUnappended_Handler;
            Database = null;
        }

        private void DbEvent_ObjectReappended_Handler(object sender, ObjectEventArgs e)
        {
            var model = e.DBObject.GetModelIdentifier(ThHvacCommon.RegAppName_FanSelectionEx);
            if (!string.IsNullOrEmpty(model) && e.DBObject.IsUndoing)
            {
                if (!ReappendedModels.Any(c => c == model))
                    ReappendedModels.Add(model);
            }
        }
        private void DbEvent_ObjectUnappended_Handler(object sender, ObjectEventArgs e)
        {
            var model = e.DBObject.GetModelIdentifier(ThHvacCommon.RegAppName_FanSelectionEx);
            if (!string.IsNullOrEmpty(model) && e.DBObject.IsUndoing)
            {
                if (!UnappendedModels.Any(c => c == model))
                    UnappendedModels.Add(model);
            }
        }
    }
    class ThFanSelectionDbEraseHandler : IDisposable
    {
        private Database Database;

        public List<string> ErasedModels { get; }

        public List<string> UnerasedModels { get;}

        public ThFanSelectionDbEraseHandler(Database database)
        {
            Database = database;
            ErasedModels = new List<string>();
            UnerasedModels = new List<string>();
            Database.ObjectErased += DbEvent_ObjectErased_Handler;
        }

        public void Dispose()
        {
            Database.ObjectErased -= DbEvent_ObjectErased_Handler;
        }

        public void DbEvent_ObjectErased_Handler(object sender, ObjectErasedEventArgs e)
        {
            var model = e.DBObject.GetModelIdentifier(ThHvacCommon.RegAppName_FanSelectionEx);
            if (!string.IsNullOrEmpty(model))
            {
                if (e.Erased)
                {
                    if (!ErasedModels.Any(c => c == model))
                        ErasedModels.Add(model);
                }
                else
                {
                    if (!UnerasedModels.Any(c => c == model))
                        UnerasedModels.Add(model);
                }
            }
        }
    }
}
