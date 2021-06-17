using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.SystemDiagram.Engine
{
    public abstract class ThEntityCommonExtractionEngine
    {
        public List<ThEntityData> Results { get; protected set; }

        public ThEntityCommonExtractionEngine()
        {
            Results = new List<ThEntityData>();
        }

        public abstract void Extract(Database database);

        public abstract void ExtractFromMS(Database database);
    }
}
