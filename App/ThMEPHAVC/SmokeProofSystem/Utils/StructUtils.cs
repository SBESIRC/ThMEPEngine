using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPHVAC.SmokeProofSystem.Service;
using Autodesk.AutoCAD.Geometry;
using ThCADCore.NTS;

namespace ThMEPHVAC.SmokeProofSystem.Utils
{
    public static class StructUtils
    {
        /// <summary>
        /// 获取在容差范围内认为相切的线
        /// </summary>
        /// <param name="airShaftRoom"></param>
        /// <param name="layoutRoom"></param>
        /// <param name="tol"></param>
        /// <returns></returns>
        public static Dictionary<Line, Line> GetTangentEdge(this Polyline airShaftRoom, Polyline layoutRoom, double tol)
        {
            var airShaftLines = airShaftRoom.GetRoomEdges();
            var layoutRoomLines = layoutRoom.GetRoomEdges();
            Dictionary<Line, Line> resDic = new Dictionary<Line, Line>();
            foreach (var line in airShaftLines)
            {
                var lines = line.GetParallelLines(layoutRoomLines, new Tolerance(0.1, 0.1));
                var parallelLine = lines.Where(x => x.Distance(line) < tol).OrderByDescending(x => x.Length).FirstOrDefault();
                if (parallelLine != null)
                {
                    resDic.Add(line, parallelLine);
                }
            }

            return resDic;
        }
    }
}
