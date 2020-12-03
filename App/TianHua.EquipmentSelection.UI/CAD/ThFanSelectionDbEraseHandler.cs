using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Service.Hvac;

namespace TianHua.FanSelection.UI.CAD
{
    public class ThFanSelectionDbEraseHandler : IDisposable
    {
        private Database Database { get; set; }

        public List<string> ErasedModels { get; set; }

        public List<string> UnerasedModels { get; set; }

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
            var model = e.DBObject.GetModelIdentifier();
            if (!string.IsNullOrEmpty(model))
            {
                if (e.Erased)
                {
                    ErasedModels.Add(model);
                }
                else
                {
                    UnerasedModels.Add(model);
                }
            }
        }
    }
}
