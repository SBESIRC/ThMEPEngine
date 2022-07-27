using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;
using Dreambuild.AutoCAD;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Diagnostics;


namespace ThMEPWSS.SprinklerDim.Data
{
    public class ThSprinklerDimDataProcessService
    {
        private double LineTol = 1;
        //----input
        public List<ThIfcFlowSegment> TchPipeData { private get; set; } = new List<ThIfcFlowSegment>();
        public List<Point3d> SprinklerPt { get; set; } = new List<Point3d>();

        //----output
        public List<Line> TchPipe { get; private set; } = new List<Line>();


        public void CreateTchPipe()
        {
            var line = TchPipeData.Select(o => o.Outline).OfType<Line>().Where(x => x.Length >= LineTol);
            TchPipe.AddRange(line);
        }
        public void RemoveDuplicateSprinklerPt()
        {
            SprinklerPt = SprinklerPt.Distinct().ToList();
        }

        public void ProjectOntoXYPlane()
        {
            TchPipe.ForEach(x => x.ProjectOntoXYPlane());
            SprinklerPt = SprinklerPt.Select(x => new Point3d(x.X, x.Y, 0)).ToList();


        }
        public void Print()
        {
            DrawUtils.ShowGeometry(TchPipe, "l0Pipe", 140);
            SprinklerPt.ForEach(x => DrawUtils.ShowGeometry(x, "l0sprinkler", 3));
        }
    }
}
