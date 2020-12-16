using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Service.Hvac;

namespace TianHua.FanSelection.UI.CAD
{
    public class ThFanSelectionDbDeepCloneHandler : IDisposable
    {
        private Database Database { get; set; }
        private IdMapping Mapping { get; set; }
        public Dictionary<string, string> ModelSystemMapping { get; set; }

        public ThFanSelectionDbDeepCloneHandler(Database database)
        {
            Database = database;
            ThFanModelOverruleManager.Instance.Register();
            ModelSystemMapping = new Dictionary<string, string>();
            Database.DeepCloneEnded += DbEvent_DeepCloneEnded_Handler;
            Database.BeginDeepCloneTranslation += DbEvent_BeginDeepCloneTranslation_Handler;
        }

        public void Dispose()
        {
            ThFanModelOverruleManager.Instance.UnRegister();
            Database.DeepCloneEnded -= DbEvent_DeepCloneEnded_Handler;
            Database.BeginDeepCloneTranslation -= DbEvent_BeginDeepCloneTranslation_Handler;
        }

        public void DbEvent_BeginDeepCloneTranslation_Handler(object sender, IdMappingEventArgs e)
        {
            Mapping = e.IdMapping;
            ThFanModelOverruleManager.Instance.Reset();
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
                        // 考虑到一个风机系统可以被复制多次，
                        // 这里将新复制风机系统作为键值，而将被复制风机系统作为值
                        ModelSystemMapping[targetModel] = sourceModel;
                    }
                }
            }
        }
    }
}
