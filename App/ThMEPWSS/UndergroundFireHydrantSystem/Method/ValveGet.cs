using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPWSS.Uitl.ExtensionsNs;
using ThMEPWSS.UndergroundFireHydrantSystem.Model;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Method
{
    public class ValveGet
    {
        public static void GetValve(Point3dEx branchPt, Dictionary<Point3dEx, List<ValveCasing>> ValveDic, FireHydrantSystemIn fireHydrantSysIn,
            List<Line> lineList, FireHydrantSystemOut fireHydrantSysOut, Point3d pt1, Point3d pt4, bool flag3 = false)
        {
            var valves = new List<ValveCasing>();
            if (ValveDic.ContainsKey(branchPt))
            {
                valves = ValveDic[branchPt];
            }
            ValveAdd(pt1, pt4, fireHydrantSysOut, fireHydrantSysIn, valves, lineList, flag3);
        }

        public static void ValveAdd(Point3d pt1, Point3d pt4, FireHydrantSystemOut fireHydrantSysOut, FireHydrantSystemIn fireHydrantSysIn,
            List<ValveCasing> valves, List<Line> lineList,bool flag3 = false)
        {
            if (valves.Count > 0)
            {
                double initGap = 350 - valves.Count * 100;
                var pt2 = pt1.OffsetX(initGap);
                lineList.Add(new Line(pt1, pt2));
                double vgap = 300;
                foreach (var v in valves)
                {
                    if(v.Type == 0)
                    {
                        fireHydrantSysOut.Casing.Add(pt2);
                        lineList.Add(new Line(pt2,pt2.OffsetX(200)));
                        vgap = 200;
                    }
                    if(v.Type == 1)
                    {
                        fireHydrantSysOut.DieValve.Add(pt2);
                        vgap = 240;
                    }
                    if(v.Type == 2)
                    {
                        fireHydrantSysOut.GateValve.Add(pt2);
                        vgap = 300;
                    }
                    pt2 = pt2.OffsetX(vgap);
                    lineList.Add(new Line(pt2, pt2.OffsetX(50)));
                    pt2 = pt2.OffsetX(50);
                }
                lineList.Add(new Line(pt2, pt4));
            }
            else
            {
                lineList.Add(new Line(pt1, pt4));
            }
        }
    }
}
