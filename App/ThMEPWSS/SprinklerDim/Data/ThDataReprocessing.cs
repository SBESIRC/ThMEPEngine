using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;

namespace ThMEPWSS.SprinklerDim.Data
{
    public class ThDataReprocessing
    {

        public static List<Polyline> TrimByRoom(List<Polyline> walls, List<Polyline> rooms)
        {

            List<Polyline> newWalls = new List<Polyline>();
            for (int i = 0; i < walls.Count; i++)
            {
                var wall = walls[i];
                for (int j = 0; j < rooms.Count; j++)
                {
                    var room = rooms[j];
                    DBObjectCollection dboc = ThCADCoreNTSOperation.Trim(room, wall);

                    foreach(var dbo in dboc.OfType<Polyline>())
                    {
                        newWalls.Add(dbo);
                    }

                }
                
            }

            return newWalls;
        }

        public static List<Polyline> TrimByRoom(List<Line> axisCurves, List<Polyline> rooms)
        {
            return TrimByRoom(Change(axisCurves), rooms);
        }


        public static List<Polyline> Change(List<Line> axisCurves)
        {
            List<Polyline> newAxisCurves = new List<Polyline>();
            foreach(var l in axisCurves)
            {
                Polyline p = new Polyline();
                p.AddVertexAt(0, l.StartPoint.ToPoint2d(), 0, 0, 0);
                p.AddVertexAt(1, l.EndPoint.ToPoint2d(),0,0,0);
                newAxisCurves.Add(p);
            }

            return newAxisCurves;
        }


    }
}
