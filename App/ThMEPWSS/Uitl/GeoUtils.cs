using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPWSS.Model;

namespace ThMEPWSS.Utils
{
    public static class GeoUtils
    {
        /// <summary>
        /// 计算出房间内的喷淋的布置点
        /// </summary>
        /// <param name="room"></param>
        /// <param name="layoutPts"></param>
        /// <returns></returns>
        public static List<SprayLayoutData> CalRoomSpray(Polyline room, List<SprayLayoutData> sprays, out List<SprayLayoutData> outsideSpray)
        {
            outsideSpray = new List<SprayLayoutData>();
            var roomSprays = new List<SprayLayoutData>();
            foreach (var spray in sprays)
            {
                if (room.Contains(spray.Position))
                {
                    roomSprays.Add(spray);
                }
                else
                {
                    outsideSpray.Add(spray);
                }
            }
            return roomSprays;
        }

        /// <summary>
        /// 判断喷淋是否在polyline内
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="spray"></param>
        /// <returns></returns>
        public static bool IsInPolyline(this Polyline polyline, SprayLayoutData spray)
        {
            if (polyline.Contains(spray.Position))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 移动polyline
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="length"></param>
        /// <param name="moveDir"></param>
        /// <returns></returns>
        public static Polyline MovePolyline(this Polyline polyline, double length, Vector3d moveDir)
        {
            bool isClosed = polyline.Closed;
            Polyline newPolyline = new Polyline() { Closed = isClosed };
            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                var pt = (polyline.GetPoint3dAt(i) + moveDir * length).ToPoint2D();
                newPolyline.AddVertexAt(0, pt, 0, 0, 0);
            }

            return newPolyline;
        }
    }
}
