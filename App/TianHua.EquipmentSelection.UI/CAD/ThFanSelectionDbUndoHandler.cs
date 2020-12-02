using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPHAVC.CAD;

namespace TianHua.FanSelection.UI.CAD
{
    public class ThFanSelectionDbUndoHandler : IDisposable
    {
        private Database Database { get; set; }
        public List<string> UnappendedModels { get; set; } = new List<string>();
        public List<string> ReappendedModels { get; set; } = new List<string>();

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
            var model = e.DBObject.GetModelIdentifier();
            if (!string.IsNullOrEmpty(model) && e.DBObject.IsUndoing)
            {
                ReappendedModels.Add(model);
            }
        }
        private void DbEvent_ObjectUnappended_Handler(object sender, ObjectEventArgs e)
        {
            var model = e.DBObject.GetModelIdentifier();
            if (!string.IsNullOrEmpty(model) && e.DBObject.IsUndoing)
            {
                UnappendedModels.Add(model);
            }
        }
    }
}
