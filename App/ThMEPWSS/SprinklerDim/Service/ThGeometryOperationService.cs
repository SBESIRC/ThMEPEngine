using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;

namespace ThMEPWSS.SprinklerDim.Service
{
    public class ThGeometryOperationService
    {
      
        public static List<Polyline> Trim(List<Polyline> reference, MPolygon room)
        {
            List<Polyline> referenceInShell = new List<Polyline>();
            referenceInShell.AddRange(ThGeometryOperationService.Trim(reference, room.Shell(), false));

            List<Polyline> referenceInRoom = referenceInShell;
            foreach (Polyline hole in room.Holes())
            {
                referenceInRoom = ThGeometryOperationService.Trim(referenceInRoom, hole, true);
            }

            return referenceInRoom;
        }

        public static List<Polyline> Trim(List<Polyline> reference, Polyline room, bool inverted=false)
        {
            List<Polyline> polylines = new List<Polyline>();

            foreach (Polyline r in reference)
            {
                DBObjectCollection dboc = room.Trim(r, inverted);
                polylines.AddRange(ThDataTransformService.GetBothPolylinesAndLines(dboc));
            }

            return polylines;
        }

        public static bool Contains(MPolygon room, List<Polyline> dimTextBoxes)
        {
            foreach(Polyline dimTextBoxe in dimTextBoxes)
            {
                if(!room.Contains(dimTextBoxe))
                    return false;
            }

            return true;
        }

        public static List<Polyline> Intersection(List<Polyline> reference, MPolygon room)
        {
            if (reference.Count == 0)
                return new List<Polyline>();

            List<Polyline> referenceInShell = Intersection(reference, room.Shell(), false);

            List<Polyline> referenceInRoom = referenceInShell;
            foreach (Polyline hole in room.Holes())
            {
                referenceInRoom = ThGeometryOperationService.Intersection(referenceInRoom, hole, true);
            }

            return referenceInRoom;
        }

        public static List<Polyline> Intersection(List<Polyline> reference, Polyline room, bool inverted = false)
        {
            if(reference.Count == 0)
                return new List<Polyline>();
           
            List<Polyline> intersectPart = new List<Polyline>();
            if (!inverted)
            {
                DBObjectCollection dboc = ThDataTransformService.ChangeToDboc(reference);
                intersectPart.AddRange(ThDataTransformService.GetBothPolylinesAndLines(room.Intersection(dboc)));
            }
            else
            {
                foreach (Polyline r in reference)
                {
                    intersectPart.AddRange(ThDataTransformService.GetBothPolylinesAndLines(r.Difference(room)));
                }
            }
                
            return intersectPart;
        }



    }
}
