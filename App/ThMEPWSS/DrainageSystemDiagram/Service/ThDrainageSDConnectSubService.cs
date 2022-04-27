using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using NFox.Cad;

using ThMEPWSS.DrainageSystemDiagram.Service;

namespace ThMEPWSS.DrainageSystemDiagram
{
    public class ThDrainageSDConnectSubService
    {
        public static List<Line> linkGroupSub(Dictionary<string, List<ThTerminalToilet>> groupList, Dictionary<ThTerminalToilet, Point3d> ptForVirtualDict, Dictionary<string, (string, string)> islandPare, List<Line> branchList)
        {
            var subBranch = new List<Line>();
            if (branchList.Count > 0)
            {
                foreach (var group in groupList)
                {
                    var toiletInVirtualList = group.Value.Where(x => ptForVirtualDict.ContainsKey(x)).ToList();

                    if (toiletInVirtualList.Count > 0 && islandPare.ContainsKey(group.Key) == false)
                    {
                        var leadToi = toiletInVirtualList.First();
                        var vpt = ptForVirtualDict[leadToi];

                        //普通组
                        subBranch.AddRange(linkSubInGroupNormal(group, out var orderPts));
                        subBranch.AddRange(linkSubToVirtualForNormal(vpt, orderPts, branchList));

                    }

                    if (toiletInVirtualList.Count > 0 && islandPare.ContainsKey(group.Key) == true)
                    {
                        //岛 vpt组
                        var leadToi = toiletInVirtualList.First();
                        var vpt = ptForVirtualDict[leadToi];
                        subBranch.AddRange(linkSubInGroupNormal(group, out var orderPts));
                        subBranch.AddRange(linkSubToVirtualForNormal(vpt, orderPts, branchList));

                        //岛 另外一组
                        var otherPair = groupList.Where(x => x.Key == islandPare[group.Key].Item2).First();
                        subBranch.AddRange(linkSubInGroupNormal(otherPair, out var orderOtherPts));
                        subBranch.AddRange(linkSubToVirtualForIsland(vpt, orderOtherPts, otherPair.Value, branchList));
                    }
                }
            }
            return subBranch;
        }

        private static List<Line> linkSubInGroupNormal(KeyValuePair<string, List<ThTerminalToilet>> group, out List<Point3d> orderPts)
        {
            var subBranch = new List<Line>();

            //link each pt in group
            var pts = group.Value.SelectMany(x => x.SupplyCoolOnBranch).ToList();
            orderPts = ThDrainageSDCommonService.orderPtInStrightLine(pts);
            var lines = NoDraw.Line(orderPts.ToArray()).ToList();
            subBranch.AddRange(lines);

            return subBranch;
        }

        private static List<Line> linkSubToVirtualForNormal(Point3d vPt, List<Point3d> orderPts, List<Line> branchList)
        {

            var subBranch = new List<Line>();
            var tol = new Tolerance(10, 10);

            //if virtual pt is on line of sub branch=>not link virtual pt to supplyPt
            var tempLine = new Line(orderPts.First(), orderPts.Last());
            var vPtOnTempLine = tempLine.GetClosestPointTo(vPt, true);

            if (tempLine.ToCurve3d().IsOn(vPtOnTempLine, tol) == false || vPt.DistanceTo(vPtOnTempLine) > ThDrainageSDCommon.MoveDistVirtualPt)
            {
                var sPt = orderPts.OrderBy(x => x.DistanceTo(vPt)).First();
                var line = new Line(vPt, sPt);

                //if (branchList.Where(x => x.Overlaps(line)).Count() == 0)
                //{
                //    subBranch.Add(line);
                //}

                subBranch.Add(line);
            }

            return subBranch;
        }

        private static List<Line> linkSubToVirtualForIsland(Point3d vPt, List<Point3d> orderOtherPts, List<ThTerminalToilet> group, List<Line> branchList)
        {

            var subBranch = new List<Line>();
            var tol = new Tolerance(10, 10);

            var pt = orderOtherPts.First().DistanceTo(vPt) <= orderOtherPts.Last().DistanceTo(vPt) ? orderOtherPts.First() : orderOtherPts.Last();

            var ptOnWall = nearbyBranch(pt, group.First().Dir, branchList);

            if (ptOnWall != null && ptOnWall != Point3d.Origin)
            {
                //connect to main branch directly
                var subLine = new Line(pt, ptOnWall);
                subBranch.Add(subLine);
            }
            else
            {
                //connect to virtual Pt
                var tempLine = new Line(orderOtherPts.First(), orderOtherPts.Last());
                var connPt = tempLine.GetClosestPointTo(vPt, true);

                var subLine = new Line(vPt, connPt);
                var subLineSec = new Line(connPt, pt);
                subBranch.Add(subLine);
                subBranch.Add(subLineSec);
            }

            return subBranch;
        }

        private static Point3d nearbyBranch(Point3d pt, Vector3d ptDir, List<Line> branchList)
        {
            var tol = new Tolerance(10, 10);
            int nearBranchTol = 1000;
            Point3d ptOnWall = new Point3d();
            var ptOnBranch = branchList.ToDictionary(x => x, x => x.GetClosestPointTo(pt, true));
            var nearBranch = ptOnBranch.Where(branch =>
            {
                var bReturn = true;
                if (branch.Value.DistanceTo(pt) >= nearBranchTol)
                {
                    bReturn = false;
                }
                if (branch.Key.ToCurve3d().IsOn(branch.Value, tol) == false)
                {
                    bReturn = false;
                }
                return bReturn;
            });

            if (nearBranch.Count() > 0)
            {
                var parallelBranch = nearBranch.Where(branch =>
                  {
                      var branchDir = (branch.Key.EndPoint - branch.Key.StartPoint).GetNormal();
                      var angle = ptDir.GetAngleTo(branchDir, Vector3d.ZAxis);
                      var bReturn = false;
                      if (Math.Abs(Math.Cos(angle)) >= Math.Cos(10 * Math.PI / 180))
                      {
                          bReturn = true;
                      }

                      return bReturn;
                  });

                if (parallelBranch.Count() > 0)
                {
                    ptOnWall = parallelBranch.OrderBy(x => x.Value.DistanceTo(pt)).First().Value;

                }
            }

            return ptOnWall;
        }

    }
}
