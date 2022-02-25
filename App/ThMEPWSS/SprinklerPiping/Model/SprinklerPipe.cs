using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AcHelper;
using NFox.Cad;
using Linq2Acad;
using Dreambuild.AutoCAD;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using ThCADExtension;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.LaneLine;
using NetTopologySuite.Geometries;

using ThMEPWSS.SprinklerConnect.Service;
using ThMEPWSS.SprinklerConnect.Model;


namespace ThMEPWSS.SprinklerPiping.Model
{
    public class SprinklerPipe
    {
        public Line pipe;
        public bool assigned;

        public SprinklerPipe(Line pipe, bool assigned)
        {
            this.pipe = pipe;
            this.assigned = assigned;
        }

        public SprinklerPipe(SprinklerPipe p)
        {
            pipe = p.pipe;
            assigned = p.assigned;
        }
    }
}
