using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Service.Hvac;

namespace TianHua.FanSelection.UI.CAD
{
    public class ThFanSelectionDbUndoHandler : IDisposable
    {
        private Database Database { get; set; }
        public Dictionary<string, List<int>> UnappendedModels { get; set; } = new Dictionary<string, List<int>>();
        public Dictionary<string, List<int>> ReappendedModels { get; set; } = new Dictionary<string, List<int>>();

        public ThFanSelectionDbUndoHandler(Database database)
        {
            Database = database;
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
            var number = e.DBObject.GetModelNumber();
            var model = e.DBObject.GetModelIdentifier();
            if (!string.IsNullOrEmpty(model) && e.DBObject.IsUndoing)
            {
                if (ReappendedModels.ContainsKey(model))
                {
                    ReappendedModels[model].Add(number);
                }
                else
                {
                    ReappendedModels.Add(model, new List<int>() { number });
                }
            }
        }
        private void DbEvent_ObjectUnappended_Handler(object sender, ObjectEventArgs e)
        {
            var number = e.DBObject.GetModelNumber();
            var model = e.DBObject.GetModelIdentifier();
            if (!string.IsNullOrEmpty(model) && e.DBObject.IsUndoing)
            {
                if (UnappendedModels.ContainsKey(model))
                {
                    UnappendedModels[model].Add(number);
                }
                else
                {
                    UnappendedModels.Add(model, new List<int>() { number });
                }
            }
        }
    }
}
