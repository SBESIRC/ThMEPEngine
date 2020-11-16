using System;
using Linq2Acad;
using System.Linq;
using Catel.Collections;
using Autodesk.AutoCAD.DatabaseServices;

namespace TianHua.FanSelection.UI.CAD
{
    public class ThFanSelectionDbModelFoundationManager : IDisposable
    {
        private Database HostDb { get; set; }

        public ObjectIdCollection Geometries { get; set; }

        public ThFanSelectionDbModelFoundationManager(Database database)
        {
            HostDb = database;
            LoadFromDb(HostDb);
        }

        public void Dispose()
        {
        }

        private void LoadFromDb(Database database)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                Geometries = new ObjectIdCollection();
                acadDatabase.ModelSpace
                    .Where(o => ThFanSelectionDbUtils.IsModelFoundation(o))
                    .ForEach(o => Geometries.Add(o.ObjectId));
            }
        }
    }
}
