using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using ThMEPHVAC.FanConnect.Model;
using ThMEPHVAC.FanPipeAlgorithm;
using ThCADCore.NTS;


namespace ThMEPHVAC.FanPipeAlgorithm
{
    class run
    {

        //工具包
        Tool Tool0 = new Tool();
        DataProcess DataP = new DataProcess();

        //最终输出
        public List<Edge> ProcessedEdges = new List<Edge>();

        public run() {

        }

        public List<Polyline> return_polyline(List<ThFanCUModel> end_fanmodel, Point3d real_start_point, List<Polyline> boundary, List<Polyline> hole)
        {
            TotalGraph total_graph0 = new TotalGraph(end_fanmodel, real_start_point, boundary, hole);

            ProcessedEdges.AddRange(total_graph0.processed_edges);

            //传回已经整理好的线条
           
            //single_area(real_end_points, real_start_point, boundary, hole);
            List<Polyline> edges_to_draw = new List<Polyline>();
            for (int i = 0; i < ProcessedEdges.Count; i++)
            {
                Polyline tmp = new Polyline();
                tmp.AddVertexAt(0, new Point2d(ProcessedEdges[i].rx1, ProcessedEdges[i].ry1), 0, 0, 0);
                tmp.AddVertexAt(1, new Point2d(ProcessedEdges[i].rx2, ProcessedEdges[i].ry2), 0, 0, 0);
                edges_to_draw.Add(tmp);
            }

            return edges_to_draw;
        }
    }

}