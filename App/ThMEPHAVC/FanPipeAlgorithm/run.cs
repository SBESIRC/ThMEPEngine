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
        tool tool_run = new tool();
        data_process data_p = new data_process();

        //最终输出
        public List<edge> processed_edges = new List<edge>();

 
        public run() {


        }

        public List<Polyline> return_polyline(List<ThFanCUModel> end_fanmodel, Point3d real_start_point, List<Polyline> boundary, List<Polyline> hole)
        {
            total_graph total_graph0 = new total_graph(end_fanmodel, real_start_point, boundary, hole);

            processed_edges.AddRange(total_graph0.processed_edges);

            //传回已经整理好的线条
           
            //single_area(real_end_points, real_start_point, boundary, hole);
            List<Polyline> edges_to_draw = new List<Polyline>();
            for (int i = 0; i < processed_edges.Count; i++)
            {
                Polyline tmp = new Polyline();
                tmp.AddVertexAt(0, new Point2d(processed_edges[i].rx1, processed_edges[i].ry1), 0, 0, 0);
                tmp.AddVertexAt(1, new Point2d(processed_edges[i].rx2, processed_edges[i].ry2), 0, 0, 0);
                edges_to_draw.Add(tmp);
            }

            return edges_to_draw;
        }

       

    }

}