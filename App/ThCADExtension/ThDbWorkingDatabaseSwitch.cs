using System;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThCADExtension
{
    public class ThDbWorkingDatabaseSwitch : IDisposable
    {

        private Database WorkingDb { get; set; }
        public ThDbWorkingDatabaseSwitch(Database database)
        {
            WorkingDb = HostApplicationServices.WorkingDatabase;
            HostApplicationServices.WorkingDatabase = database;
        }
        public void Dispose()
        {
            HostApplicationServices.WorkingDatabase = WorkingDb;
        }
    }
}
