using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPWSS.DrainageSystemAG.Models;
using ThMEPWSS.Model;

namespace ThMEPWSS.DrainageSystemAG
{
    class DrainSysAGCommon
    {
        public static Point3dCollection GetPolyLinePointColllection(Polyline polyline) 
        {
            var firstPt = polyline.GetPoint3dAt(0);
            var pts = new List<Point3d>();
            pts.Add(firstPt);
            for (int i = 1; i < polyline.NumberOfVertices; i++)
            {
                var pt = polyline.GetPoint3dAt(i);
                if (firstPt.DistanceTo(pt) < 1)
                    continue;
                pts.Add(pt);
            }
            pts.Add(firstPt);
            return new Point3dCollection(pts.ToArray());
        }
        public static Point3d PolyLineCenterPoint(Polyline polyline) 
        {
            Point3d sumPoints = new Point3d();
            int count = 0;
            var xVect = new Vector3d();
            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                var sp = polyline.GetPoint3dAt(i);
                var ep = polyline.GetPoint3dAt((i + 1) % polyline.NumberOfVertices);
                if (sp.DistanceTo(ep) < 0.0001)
                    continue;
                var dir = (ep - sp).GetNormal();
                count += 1;
                var lineMidPoint = sp + dir.MultiplyBy(sp.DistanceTo(ep) / 2);
                if (xVect.Length < 0.4)
                    xVect = dir;
                sumPoints = new Point3d(sumPoints.X + lineMidPoint.X, sumPoints.Y + lineMidPoint.Y, sumPoints.Z + lineMidPoint.Z);
                
            }
            Point3d point = sumPoints / count;
            return point;
        }
        public static List<Line> PolyLineToLines(Polyline polyline)
        {
            List<Line> lines = new List<Line>();
            var newPline = polyline.DPSimplify(10);
            for (int i = 0; i < newPline.NumberOfVertices; i++)
            {
                var sp = newPline.GetPoint3dAt(i);
                var ep = newPline.GetPoint3dAt((i + 1) % newPline.NumberOfVertices);
                if (sp.DistanceTo(ep) < 0.0001)
                    continue;
                lines.Add(new Line(sp, ep));
            }
            return lines;
        }
        public static List<DynBlockWidthLength> GetDynBlockMaxWidth(AcadDatabase acdb, List<DynBlockWidthLength> dynBlockWidthLengths)
        {
            //这里的块都是正方形，不用考虑朝向问题
            foreach (var item in dynBlockWidthLengths)
            {
                var objIds = ThDynamicBlockUtils.VisibleEntities(acdb.Database, item.blockName, item.dynName).Cast<ObjectId>().ToList();
                if (null == objIds || objIds.Count < 1)
                    continue;
                var entitys = new List<Entity>();
                foreach (ObjectId id in objIds)
                {
                    var ent = acdb.Element<Entity>(id);
                    entitys.Add(ent);
                }
                if (entitys.Count < 1)
                    continue;
                var extents = entitys[0].GeometricExtents;
                for (int i = 1; i < entitys.Count; i++)
                    extents.AddExtents(entitys[i].GeometricExtents);
                item.width = extents.ToEnvelope().Width;
                item.length = extents.ToEnvelope().Height;
            }
            return dynBlockWidthLengths;
        }
        public static List<DynBlockWidthLength> GetDynBlockMaxWidth(List<DynBlockWidthLength> dynBlockWidthLengths)
        {
            //这里的块都是正方形，不用考虑朝向问题
            using (AcadDatabase acdb = AcadDatabase.Active()) 
            {
                return GetDynBlockMaxWidth(acdb, dynBlockWidthLengths);
            }
        }
        public static List<TubeWellsRoomModel> GetTubeWellRoomRelation(List<RoomModel> allToilteKitchenRooms, List<RoomModel> tubeWellRooms)
        {
            List<TubeWellsRoomModel> retRooms = new List<TubeWellsRoomModel>();
            if (null == tubeWellRooms || tubeWellRooms.Count < 1)
                return retRooms;
            foreach (var room in tubeWellRooms)
            {
                var gmtry = room.outLine.ToNTSGeometry().EnvelopeInternal;
                var centerPoint = new Point3d((gmtry.MinX + gmtry.MaxX) / 2, (gmtry.MinY + gmtry.MaxY) / 2, 0);
                var bufferGmtry = room.outLine.ToNTSGeometry().Buffer(500);
                TubeWellsRoomModel roomModel = new TubeWellsRoomModel(room, centerPoint);
                //管道井 和 卫生间厨房关系判断
                foreach (var item in allToilteKitchenRooms)
                {
                    var pline = item.outLine.ToNTSPolygon().ToDbPolylines().FirstOrDefault();
                    if (pline.Contains(centerPoint))
                    {
                        //在内部
                        roomModel.innerRoomIds.Add(item.thIFCRoom.Uuid);
                    }
                    else if (bufferGmtry.Intersects(item.outLine.ToNTSGeometry()))
                    {
                        //相交
                        roomModel.intersectRoomIds.Add(item.thIFCRoom.Uuid);
                    }
                }
                retRooms.Add(roomModel);
            }
            return retRooms;
        }
    }
}
