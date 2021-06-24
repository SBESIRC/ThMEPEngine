using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using ThCADExtension;
using ThCADCore.NTS;


namespace ThMEPWSS.DrainageSystemDiagram
{
    public class ThDrainageSDConnectSubService
    {
        public static List<Line> linkGroupSub(Dictionary<string, List<ThIfcSanitaryTerminalToilate>> groupList, Dictionary<ThIfcSanitaryTerminalToilate, Point3d> ptForVirtualDict)
        {
            var subBranch = new List<Line>();

            foreach (var group in groupList)
            {
                var toilateInVirtualList = group.Value.Where(x => ptForVirtualDict.ContainsKey(x)).ToList();
                if (toilateInVirtualList.Count > 0)
                {
                    var leadToi = toilateInVirtualList.First();

                    //if this group has virtual pt
                    //link each pt in group
                    var orderPts = ThDrainageSDColdPtProcessService.orderSupplyPtInGroup(group.Value);
                    var lines = NoDraw.Line(orderPts.ToArray()).ToList();
                    subBranch.AddRange(lines);


                    //link virtual pt to supplyPt
                    var vPt = ptForVirtualDict[leadToi];

                    //if virtual pt is on line of sub branch=>not link virtual pt to supplyPt
                    var tempLine = new Line(orderPts.First(), orderPts.Last());
                    var vPtOnTempLine = tempLine.GetClosestPointTo(vPt, true);
                    if (tempLine.IsOnLine(vPtOnTempLine) == false || vPt.DistanceTo(vPtOnTempLine) > DrainageSDCommon.MovedLength)
                    {
                        var sPt = orderPts.OrderBy(x => x.DistanceTo(vPt)).First();
                        var line = new Line(vPt, sPt);
                        subBranch.Add(line);
                    }

                }







            }
            return subBranch;
        }

    }
}
