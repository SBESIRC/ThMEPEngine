using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.CAD;
using NFox.Cad;
using Linq2Acad;
using ThMEPEngineCore.Diagnostics;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Model.Hvac;
using ThMEPHVAC.FloorHeatingCoil.Heating;

namespace ThMEPHVAC.FloorHeatingCoil.Heating
{

    class EstimateService
    {
        static double ComputeTotalDistance(List<TopoTreeNode> idTree, int doorIdEnd, int doorIdStart)
        {
            double distante = 0;
               

            return distante;
        }

        public static double ComputeUsedPipeLength(Polyline room ,double bufferDisWall,double bufferDisPipe)
        {
            double usedPipeLength = 0;
            List<List<Polyline>> polyList = new List<List<Polyline>>();

            double bufferDis = -bufferDisWall; 
            while (true)
            {
                var roomBuffer = Buffer(room, bufferDis);
                if (roomBuffer.Count == 0)
                    break;
                polyList.Add(new List<Polyline>());
                foreach (Polyline poly in roomBuffer)
                {
                    usedPipeLength += poly.Length;
                    polyList.Last().Add(poly);
                }
                bufferDis = bufferDis - bufferDisPipe; 
            }
            return usedPipeLength;
        }

        public static List<Polyline> Buffer(Polyline frame, double distance)
        {

            //Stopwatch sw = new Stopwatch();
            //sw.Start();
            // Test code
            //sw.Stop();
            //var t1 = sw.ElapsedTicks;
            var results = frame.Buffer(distance);
            return results.Cast<Polyline>().ToList();
        }
    }

}
