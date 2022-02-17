using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPWSS.FirstFloorDrainagePlaneSystem.Service
{
    public static class StructGeoService
    {
        /// <summary>
        /// 计算墙方向
        /// </summary>
        /// <param name="wall"></param>
        /// <returns></returns>
        public static Vector3d GetWallDir(Polyline wall)
        {
            var allLines = GetAllLineByPolyline(wall);
            var firLine = allLines.OrderByDescending(x => x.Length).First();
            var dir = (firLine.EndPoint - firLine.StartPoint).GetNormal();
            return dir;
        }

        /// <summary>
        /// 获得polyline上所有线
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public static List<Line> GetAllLineByPolyline(Polyline polyline)
        {
            var allLines = new List<Line>();
            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                allLines.Add(new Line(polyline.GetPoint3dAt(i), polyline.GetPoint3dAt((i + 1) % polyline.NumberOfVertices)));
            }
            return allLines;
        }
    }
}
