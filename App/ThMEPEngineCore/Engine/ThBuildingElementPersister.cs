using Autodesk.AutoCAD.DatabaseServices;
using System.Collections.Generic;

namespace ThMEPEngineCore.Engine
{
    public abstract class ThBuildingElementPersister
    {
        public abstract void Persist(Database database);
        public List<ThBuildingElementRecognitionEngine> Engines { get; set; }

        public ThBuildingElementPersister()
        {
            Engines = new List<ThBuildingElementRecognitionEngine>();
        }
    }
}
