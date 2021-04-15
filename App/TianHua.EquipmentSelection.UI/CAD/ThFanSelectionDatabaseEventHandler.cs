using System;
using TianHua.FanSelection.Messaging;
using Autodesk.AutoCAD.DatabaseServices;

namespace TianHua.FanSelection.UI.CAD
{
    public class ThFanSelectionDatabaseEventHandler : IDisposable
    {
        public Database Database { get; set; }

        public ThFanSelectionDatabaseEventHandler(Database database)
        {
            Database = database;
            Database.BeginSave += DbEvent_BeginSave_handler;
        }

        public void Dispose()
        {
            Database.BeginSave -= DbEvent_BeginSave_handler;
        }

        public void DbEvent_BeginSave_handler(object sender, DatabaseIOEventArgs e)
        {
            SendBegineSaveMessage(e.FileName);
        }

        private void SendBegineSaveMessage(string fileName)
        {
            ThModelBeginSaveMessage.SendWith(new ThModelBeginSaveMessageArgs()
            {
                FileName = fileName,
            });
        }
    }
}
