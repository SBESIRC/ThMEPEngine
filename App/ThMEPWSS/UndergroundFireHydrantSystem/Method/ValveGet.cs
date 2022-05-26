using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPWSS.Command;
using ThMEPWSS.UndergroundFireHydrantSystem.Model;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Method
{
    public class ValveGet
    {
        public static void GetValve(Point3dEx branchPt, Dictionary<Point3dEx, List<Point3dEx>> ValveDic, FireHydrantSystemIn fireHydrantSysIn,
            ref List<Line> lineList, ref FireHydrantSystemOut fireHydrantSysOut, Point3d pt1, Point3d pt4, bool flag3 = false)
        {
            var valve = false;
            var valveSite = new Point3d();
            int isCasing = 0;
            if (ValveDic.ContainsKey(branchPt))
            {
                if (ValveDic[branchPt].Count > 0)
                {
                    valve = true;
                    valveSite = ValveDic[branchPt][0]._pt;
                    isCasing = Casing.HasCasing(ValveDic[branchPt], fireHydrantSysIn);
                }
            }
            ValveAdd(pt1, pt4, ref fireHydrantSysOut, fireHydrantSysIn, valveSite, valve, ref lineList, isCasing, flag3);
        }

        public static void ValveAdd(Point3d pt1, Point3d pt4, ref FireHydrantSystemOut fireHydrantSysOut, FireHydrantSystemIn fireHydrantSysIn,
            Point3d valveSite, bool valve, ref List<Line> lineList, int isCasing,bool flag3 = false)
        {
            double valveSize = 240;
            if (valve)
            {
                double pt2X = 280;
                if (isCasing == 2 || flag3)
                {
                    pt2X = 180;
                }
                var pt2 = new Point3d(pt1.X + pt2X, pt1.Y, 0);
                var flag = false;
                ValveCheck(ref fireHydrantSysOut, valveSite, pt2, fireHydrantSysIn, ref flag);
                if (flag)
                {
                    valveSize = 300;
                }
                var pt3 = new Point3d(pt2.X + valveSize, pt2.Y, 0);
                lineList.Add(new Line(pt1, pt2));
                lineList.Add(new Line(pt3, pt4));
                fireHydrantSysOut.Valve.Add(pt2);
                if (isCasing == 1)
                {
                    fireHydrantSysOut.IsCasing.Add(new Point3d(pt2.X - 250, pt2.Y, 0));
                }
                if (isCasing == 2)
                {
                    fireHydrantSysOut.IsCasing.Add(new Point3d(pt3.X + 50, pt3.Y, 0));
                }
            }
            else
            {
                lineList.Add(new Line(pt1, pt4));
            }
        }

        private static void ValveCheck(ref FireHydrantSystemOut fireHydrantSysOut, Point3d valve, Point3d stPt,
            FireHydrantSystemIn fireHydrantSysIn, ref bool flag)
        {
            foreach (var pt in fireHydrantSysIn.GateValves)
            {
                if (valve.DistanceTo(pt) < 250)
                {
                    fireHydrantSysOut.IsGateValve.Add(stPt);
                    flag = true;
                    break;
                }
            }
        }
    }
}
