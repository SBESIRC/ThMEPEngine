using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
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
        public static List<Polyline> Trim(List<Line> reference, MPolygon room)
        {
            return Trim(ThDataTransformService.Change(reference), room);
        }

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



        public static bool IsContained(MPolygon room, Point3d pt)
        {
            if (!ThCADCoreNTSPolygonExtension.Contains(room.Shell(), pt))
            {
                return false;
            }

            List<Polyline> holes = room.Holes();
            foreach (Polyline hole in holes)
            {
                if (ThCADCoreNTSPolygonExtension.Contains(hole, pt))
                    return false;
            }

            return true;
        }




        public static List<Polyline> SelectWindowPolygon(ThCADCoreNTSSpatialIndex spatialIndex, MPolygon room)
        {
            return ThDataTransformService.GetBothPolylinesAndLines(spatialIndex.SelectWindowPolygon(room));
        }

        public static List<Polyline> SelectFence(ThCADCoreNTSSpatialIndex spatialIndex, MPolygon room)
        {
            return SelectFence(spatialIndex, ThDataTransformService.Change(room));
        }

        public static List<Polyline> SelectFence(ThCADCoreNTSSpatialIndex linesSI, List<Polyline> boxes)
        {
            List<Polyline> fences = new List<Polyline>();   
            foreach(Polyline box in boxes)
            {
                fences.AddRange(ThDataTransformService.GetBothPolylinesAndLines(linesSI.SelectFence(box))); // 仅相交
            }

            return fences;
        }

        public static List<Polyline> SelectFence(ThCADCoreNTSSpatialIndex spatialIndex, Line line)
        {
            return ThDataTransformService.GetBothPolylinesAndLines(spatialIndex.SelectFence(line));
        }

        public static List<Polyline> SelectCrossingPolygon(ThCADCoreNTSSpatialIndex reference, Polyline box)
        {
            return ThDataTransformService.GetBothPolylinesAndLines(reference.SelectCrossingPolygon(box)); // 相交或内部
        }


    }
}
