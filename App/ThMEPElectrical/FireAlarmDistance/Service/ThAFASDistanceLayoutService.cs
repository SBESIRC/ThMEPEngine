using System;
using System.Linq;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using NetTopologySuite.Features;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPElectrical.AFAS;

namespace ThMEPElectrical.FireAlarmDistance.Service
{
    public class ThAFASDistanceLayoutService
    {
        public static List<Point3d> ConvertGeom(FeatureCollection features)
        {
            var pts = new List<Point3d>();

            foreach (var f in features)
            {
                var coordinates = f.Geometry.Coordinates;
                var pt = new Point3d(coordinates[0].X, coordinates[0].Y, 0);
                pts.Add(pt);
            }

            return pts;
        }

        public static List<KeyValuePair<Point3d, Vector3d>> FindOutputPtsDir(List<Point3d> pts, List<Polyline> RoomList)
        {
            var ptDir = new List<KeyValuePair<Point3d, Vector3d>>();

            for (int i = 0; i < pts.Count; i++)
            {
                var pt = pts[i];
                var dir = new Vector3d(0, 1, 0);
                var room = RoomList.Where(x => x.ContainsOrOnBoundary(pt)).FirstOrDefault();

                if (room != null)
                {
                    dir = FindPtDir(pt, room);
                }
                ptDir.Add(new KeyValuePair<Point3d, Vector3d>(pt, dir));
            }

            return ptDir;
        }

        public static List<KeyValuePair<Point3d, Vector3d>> FindOutputPtsOnTop(List<KeyValuePair<Point3d, Vector3d>> bottomBlk, string blkName, double scale)
        {
            var ptDir = new List<KeyValuePair<Point3d, Vector3d>>();
            for (int i = 0; i < bottomBlk.Count; i++)
            {
                var pt = bottomBlk[i].Key + bottomBlk[i].Value * ThFaCommon.blk_size[blkName].Item2 * scale;
                ptDir.Add(new KeyValuePair<Point3d, Vector3d>(pt, bottomBlk[i].Value));
            }
            return ptDir;
        }


        private static Vector3d FindPtDir(Point3d pt, Polyline room)
        {
            var dir = new Vector3d(0, 1, 0);
            var closeDist = 100000.0;
            var closeWallIdx = -1;
            var closeLine = new Line();

            for (int i = 0; i < room.NumberOfVertices; i++)
            {
                var l = new Line(room.GetPoint3dAt(i % room.NumberOfVertices), room.GetPoint3dAt((i + 1) % room.NumberOfVertices));
                var dist = l.GetDistToPoint(pt, false);
                if (dist <= closeDist)
                {
                    closeDist = dist;
                    closeWallIdx = i;
                    closeLine = l;
                }
            }
            if (closeWallIdx >= 0)
            {
                var z = Vector3d.ZAxis;
                if (room.IsCCW() == false)
                {
                    z = -z;
                }
                dir = (closeLine.EndPoint - closeLine.StartPoint).RotateBy(90 * Math.PI / 180, z).GetNormal();
            }
            return dir;
        }
    }
}
