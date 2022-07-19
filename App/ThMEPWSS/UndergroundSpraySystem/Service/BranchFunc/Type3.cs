using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;
using System;
using System.Collections.Generic;
using ThMEPWSS.Uitl.ExtensionsNs;
using ThMEPWSS.UndergroundFireHydrantSystem.Model;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;
using ThMEPWSS.UndergroundSpraySystem.Block;
using ThMEPWSS.UndergroundSpraySystem.Model;

namespace ThMEPWSS.UndergroundSpraySystem.Service.BranchFunc
{
    public class Type3
    {
        public static void Get(Point3d stPt, TermPoint2 termPt, string DN, SprayOut sprayOut)
        {
            var pumpPt = new Point3d(stPt.X, sprayOut.PipeInsertPoint.Y + 1300, 0);
            sprayOut.PipeLine.Add(new Line(stPt, pumpPt));
            sprayOut.WaterPumps.Add(new WaterPump(pumpPt, termPt.PipeNumber, DN));
        }
    }
}
