using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace TianHua.FanSelection.UI.CAD
{
    public class ThFanSelectionDbEraseHandler : IDisposable
    {
        private Database Database { get; set; }

        public bool Erased { get; set; }
        public string Model { get; set; }

        public ThFanSelectionDbEraseHandler(Database database)
        {
            Database = database;
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
                Model = model;
                Erased = e.Erased;
            }
        }
    }
}
