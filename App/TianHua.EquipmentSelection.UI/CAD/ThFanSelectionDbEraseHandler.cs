using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Service.Hvac;

namespace TianHua.FanSelection.UI.CAD
{
    public class ThFanSelectionDbEraseHandler : IDisposable
    {
        private Database Database { get; set; }

        public Dictionary<string, List<int>> ErasedModels { get; set; } = new Dictionary<string, List<int>>();

        public Dictionary<string, List<int>> UnerasedModels { get; set; } = new Dictionary<string, List<int>>();

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
            var number = e.DBObject.GetModelNumber();
            var model = e.DBObject.GetModelIdentifier();
            if (!string.IsNullOrEmpty(model))
            {
                if (e.Erased)
                {
                    if (ErasedModels.ContainsKey(model))
                    {
                        ErasedModels[model].Add(number);
                    }
                    else
                    {
                        ErasedModels.Add(model, new List<int>() { number });
                    }
                }
                else
                {
                    if (UnerasedModels.ContainsKey(model))
                    {
                        UnerasedModels[model].Add(number);
                    }
                    else
                    {
                        UnerasedModels.Add(model, new List<int>() { number });
                    }
                }
            }
        }
    }
}
