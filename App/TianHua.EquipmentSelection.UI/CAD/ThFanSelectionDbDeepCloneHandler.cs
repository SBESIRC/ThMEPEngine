using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace TianHua.FanSelection.UI.CAD
{
    public class ThFanSelectionDbDeepCloneHandler : IDisposable
    {
        private Database Database { get; set; }
        private IdMapping Mapping { get; set; }
        public Dictionary<string, string> ModelMapping { get; set; }

        public ThFanSelectionDbDeepCloneHandler(Database database)
        {
            Database = database;
            ModelMapping = new Dictionary<string, string>();
            Database.DeepCloneEnded += DbEvent_DeepCloneEnded_Handler;
            Database.BeginDeepCloneTranslation += DbEvent_BeginDeepCloneTranslation_Handler;
        }

        public void Dispose()
        {
            Database.DeepCloneEnded -= DbEvent_DeepCloneEnded_Handler;
            Database.BeginDeepCloneTranslation -= DbEvent_BeginDeepCloneTranslation_Handler;
        }

        public void DbEvent_BeginDeepCloneTranslation_Handler(object sender, IdMappingEventArgs e)
        {
            Mapping = e.IdMapping;
        }

        public void DbEvent_DeepCloneEnded_Handler(object sender, EventArgs e)
        {
            using (var tx = Database.TransactionManager.StartOpenCloseTransaction())
            {
                foreach (IdPair pair in Mapping)
                {
                    var sourceModel = pair.Key.GetDBObject(tx).GetModelIdentifier();
                    var targetModel = pair.Value.GetDBObject(tx).GetModelIdentifier();
                    if (!string.IsNullOrEmpty(sourceModel) &&
                        !string.IsNullOrEmpty(targetModel) &&
                        (sourceModel != targetModel))
                    {
                        // 考虑到一个对象可以被复制多次，
                        // 这里将新复制对象作为键值，而将被复制对象作为值
                        ModelMapping[targetModel] = sourceModel;
                    }
                }
            }
        }
    }
}
