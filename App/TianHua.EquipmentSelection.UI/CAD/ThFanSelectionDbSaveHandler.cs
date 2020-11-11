using System;
using Autodesk.AutoCAD.DatabaseServices;

namespace TianHua.FanSelection.UI.CAD
{
    public class ThFanSelectionDbSaveHandler : IDisposable
    {
        public string FileName { get; set; }

        private Database Database { get; set; }

        public ThFanSelectionDbSaveHandler(Database database)
        {
            Database = database;
            Database.SaveComplete += DbEvent_SaveComplete_handler;
        }

        public void Dispose()
        {
            Database.SaveComplete -= DbEvent_SaveComplete_handler;
        }

        public void DbEvent_SaveComplete_handler(object sender, DatabaseIOEventArgs e)
        {
            FileName = e.FileName;
        }
    }
}
