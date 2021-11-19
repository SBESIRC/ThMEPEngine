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



namespace ThMEPWSS.SprinklerConnect.Model
{
    public class ThSprinklerParameter
    {
        public List<Point3d> SprinklerPt { get; set; } = new List<Point3d>();

        public List<Line> AllPipe { get; set; } = new List<Line>();
        public List<Line> MainPipe { get; set; } = new List<Line>();
        public List<Line> SubMainPipe { get; set; } = new List<Line>();
    }
}
