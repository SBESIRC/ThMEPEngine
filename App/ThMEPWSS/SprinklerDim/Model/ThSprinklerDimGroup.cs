using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Diagnostics;
using ThMEPWSS.SprinklerDim.Model;
using ThMEPWSS.SprinklerDim.Service;

namespace ThMEPWSS.SprinklerDim.Model
{
    public class ThSprinklerDimGroup
    {
        public int pt { get; set; } = -1;

        public List<int> PtsDimed { get; set; } = new List<int>();

        private static bool IsDimed(List<Point3d> Pts, int id, List<int> colli, bool IsxAxis, double step,Matrix3d matrix, ThCADCoreNTSSpatialIndex walls)
        {
            bool isConflict1 = ThSprinklerDimensionOperateService.IsConflicted(Pts[id], Pts[colli[colli.Count - 1]], matrix, walls);
            bool isConflict2 = ThSprinklerDimensionOperateService.IsConflicted(Pts[id], Pts[colli[0]], matrix, walls);
            List<double> distance = new List<double>();
            colli.ForEach(p => distance.Add(Pts[p].DistanceTo(Pts[id])));
            double det = Math.Abs(ThCoordinateService.GetOriginalValue(Pts[id], IsxAxis) - ThCoordinateService.GetOriginalValue(Pts[colli[0]], IsxAxis));
            if (colli.Contains(id)) return true;
            else if (det < 45 && distance.Min() < 1.5 * step && !isConflict1 && !isConflict2) return true;
            else return false;
        }

        public void AddPt(int ipt, ThSprinklerNetGroup group, bool IsxAxis, double step, ThCADCoreNTSSpatialIndex walls)
        {
            pt = ipt;
            List<List<int>> anotherCollinearation = new List<List<int>>();
            if (IsxAxis) group.XCollineationGroup.ForEach(p => anotherCollinearation.AddRange(p));
            else group.YCollineationGroup.ForEach(p => anotherCollinearation.AddRange(p));

            foreach (List<int> p in anotherCollinearation)
            {
                if (IsDimed(group.Pts, ipt, p, IsxAxis, step, group.Transformer, walls)) PtsDimed = p;
            }
        }

    }
}
