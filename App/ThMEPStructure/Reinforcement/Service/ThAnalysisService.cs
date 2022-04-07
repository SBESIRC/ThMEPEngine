using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.CAD;

namespace ThMEPStructure.Reinforcement.Service
{
    internal abstract class ThAnalysisService
    {
        public abstract void Analysis(Polyline polyline);
    }
}
