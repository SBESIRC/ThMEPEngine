
using System.Text;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Collections.Generic;
using Linq2Acad;
using ThMEPWSS.Pipe.Model;
using ThMEPWSS.Pipe.Service;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Pipe.Engine
{
    public class ThCompositeRecognitionEngine : ThContainerRecognitionEngine
    {
        public List<ThCompositeContainer> CompositeContainers { get; set; }
    
        public ThCompositeRecognitionEngine()
        {
            CompositeContainers = new List<ThCompositeContainer>();
        }
    }
}
