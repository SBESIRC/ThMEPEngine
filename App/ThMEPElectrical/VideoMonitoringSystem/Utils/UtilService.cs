using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.VideoMonitoringSystem.Utls
{
    public static class UtilService
    {
        /// <summary>
        /// 获取polyline所有line
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public static List<Line> GetAllLinesInPolyline(this Polyline polyline)
        {
            List<Line> lines = new List<Line>();
            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                lines.Add(new Line(polyline.GetPoint3dAt(i), polyline.GetPoint3dAt((i + 1) % polyline.NumberOfVertices)));
            }

            return lines;
        }
    }
}
