using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.SprinklerDim.Model;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADCore.NTS;

namespace ThMEPWSS.SprinklerDim.Service
{
    public class ThRoomWallConflictService
    {
        public static List<ThSprinklerNetGroup> ReGroupByRoom(List<ThSprinklerNetGroup> netList, List<Polyline> rooms)
        {
            List<ThSprinklerNetGroup> newNetList = new List<ThSprinklerNetGroup>();
            if (rooms.Count > 0)
            {
                foreach (ThSprinklerNetGroup net in netList)
                {
                    DBObjectCollection lines = new DBObjectCollection();
                    foreach (ThSprinklerGraph graph in net.PtsGraph)
                    {
                        foreach(Line l in graph.Print(net.Pts))
                            lines.Add(l);
                    }
                    ThCADCoreNTSSpatialIndex linesSI = new ThCADCoreNTSSpatialIndex(lines);

                    foreach (Polyline room in rooms)
                    {
                        DBObjectCollection dbSelect = linesSI.SelectWindowPolygon(room);
                        List<Line> selectLines = new List<Line>();
                        foreach(DBObject dbo in dbSelect)
                        {
                            selectLines.Add((Line)dbo);
                        }

                        if (selectLines.Count > 0)
                        {
                            newNetList.Add(ThSprinklerNetGraphService.CreateNetwork(net.Angle, selectLines));
                        }

                    }

                }

            }
            else
                newNetList = netList;

            return newNetList;
        }



    }
}
