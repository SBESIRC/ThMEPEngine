using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using ThMEPEngineCore.Diagnostics;
using ThMEPWSS.SprinklerDim.Model;
using ThMEPWSS.SprinklerDim.Service;

namespace ThMEPWSS.SprinklerDim.Model
{
    public class ThSprinklerDimGroup
    {
        public int pt { get; set; } = -1;

        public List<int> PtsDimed { get; set; } = new List<int>();

        private static bool IsDimed(List<Point3d> Pts, int id, List<int> colli, bool IsxAxis, double step)
        {
            List<double> distance = new List<double>();
            colli.ForEach(p => distance.Add(Pts[p].DistanceTo(Pts[id])));
            double det = Math.Abs(ThCoordinateService.GetOriginalValue(Pts[id], IsxAxis) - ThCoordinateService.GetOriginalValue(Pts[colli[0]], IsxAxis));
            if (colli.Contains(id)) return true;
            else if (det < 45 && distance.Min() < 1.5 * step) return true;
            else return false;
        }

        public void AddPt(List<Point3d> Pts, int ipt, List<List<int>> anotherCollinearation, bool IsxAxis, double step)
        {
            pt = ipt;
            foreach(List<int> p in anotherCollinearation)
            {
                if (IsDimed(Pts, ipt, p, IsxAxis, step)) PtsDimed = p;
            }
        }

    }
}
