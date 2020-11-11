using System;
using AcHelper;
using Linq2Acad;
using System.Collections.Generic;
using TianHua.FanSelection.Messaging;
using Autodesk.AutoCAD.DatabaseServices;

namespace TianHua.FanSelection.UI.CAD
{
    public class ThFanSelectionDbEventHandler : IDisposable
    {
        private Database Database { get; set; }

        private IdMapping Mapping { get; set; }

        public ThFanSelectionDbEventHandler(Database database)
        {
            Database = database;
            Database.ObjectErased += DbEvent_ObjectErased_Handler;
            Database.DeepCloneEnded += DbEvent_DeepCloneEnded_Handler;
            Database.BeginDeepCloneTranslation += DbEvent_BeginDeepCloneTranslation_Handler;
        }

        public void Dispose()
        {
            Database.ObjectErased -= DbEvent_ObjectErased_Handler;
            Database.DeepCloneEnded -= DbEvent_DeepCloneEnded_Handler;
            Database.BeginDeepCloneTranslation -= DbEvent_BeginDeepCloneTranslation_Handler;
        }

        public void DbEvent_BeginDeepCloneTranslation_Handler(object sender, IdMappingEventArgs e)
        {
            Mapping = e.IdMapping;
        }

        public void DbEvent_DeepCloneEnded_Handler(object sender, EventArgs e)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var mapping = new Dictionary<string, string>();
                foreach (IdPair pair in Mapping)
                {
                    var sourceModel = pair.Key.GetModelIdentifier();
                    var targetModel = pair.Value.GetModelIdentifier();
                    if (!string.IsNullOrEmpty(sourceModel) &&
                        !string.IsNullOrEmpty(targetModel) &&
                        (sourceModel != targetModel))
                    {
                        mapping[sourceModel] = targetModel;
                    }
                }
                if (mapping.Count > 0)
                {
                    ThModelCopyMessage.SendWith(new ThModelCopyMessageArgs()
                    {
                        ModelMapping = mapping,
                    });
                }
            }
        }

        public void DbEvent_ObjectErased_Handler(object sender, ObjectErasedEventArgs e)
        {
            if (e.DBObject.IsUndoing)
            {
                return;
            }

            var model = e.DBObject.GetModelIdentifier();
            if (!string.IsNullOrEmpty(model) && !e.DBObject.IsUndoing)
            {
                ThModelDeleteMessage.SendWith(new ThModelDeleteMessageArgs()
                {
                    Model = model,
                    Erased = e.Erased,
                });
            }
        }
    }
}
