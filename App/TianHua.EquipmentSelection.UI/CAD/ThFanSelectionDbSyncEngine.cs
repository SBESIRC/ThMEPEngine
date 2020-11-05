using AcHelper;
using Linq2Acad;
using System.Linq;
using TianHua.FanSelection.Messaging;

namespace TianHua.FanSelection.UI.CAD
{
    public class ThFanSelectionDbSyncEngine
    {
        public void Sync()
        {
            using (Active.Document.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (ThFanSelectionDbManager dbManager = new ThFanSelectionDbManager(Active.Database))
            {
                ThModelSyncMessage.SendWith(new ThModelSyncMessageArgs()
                {
                    Models = dbManager.Models.Select(o => o.Key).ToList(),
                });
            }
        }
    }
}
