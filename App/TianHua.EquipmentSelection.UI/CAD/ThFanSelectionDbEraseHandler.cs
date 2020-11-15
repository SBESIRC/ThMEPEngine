using System;
using System.Collections.Generic;
using TianHua.FanSelection.Messaging;
using Autodesk.AutoCAD.DatabaseServices;

namespace TianHua.FanSelection.UI.CAD
{
    public class ThFanSelectionDbEraseHandler : IDisposable
    {
        private Database Database { get; set; }
        public Dictionary<string, bool> Models { get; set; }

        public ThFanSelectionDbEraseHandler(Database database)
        {
            Database = database;
            Models = new Dictionary<string, bool>();
            Database.ObjectErased += DbEvent_ObjectErased_Handler;
        }

        public void Dispose()
        {
            Database.ObjectErased -= DbEvent_ObjectErased_Handler;
        }

        public void DbEvent_ObjectErased_Handler(object sender, ObjectErasedEventArgs e)
        {
            var model = e.DBObject.GetModelIdentifier();
            if (!string.IsNullOrEmpty(model))
            {
                Models[model] = e.Erased;
            }
        }
    }
}
