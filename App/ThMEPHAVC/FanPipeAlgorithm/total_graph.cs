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
using ThMEPEngineCore.CAD;
using Linq2Acad;
using NFox.Cad;

namespace ThMEPHVAC.FanPipeAlgorithm
{
    class total_graph
    {

        //工具包
        tool tool_total = new tool();
        data_process data_p = new data_process();

        //最终输出
        public List<edge> processed_edges = new List<edge>();
        public List<edge> main_edges = new List<edge>();

        //一层节点
        public List<Point3d> total_insert = new List<Point3d>();

        //转化后的输入数据
        List<Point3d> real_end_points_0 = new List<Point3d>();
        List<Point3d> real_end_points = new List<Point3d>();
        Point3d real_start_point = new Point3d(0, 0, 0);
        List<Polyline> fan_bounary = new List<Polyline>();
        List<Polyline> boundary = new List<Polyline>();
        List<Polyline> hole = new List<Polyline>();
        List<Polyline> obstacle = new List<Polyline>();
        List<edge> total_wall = new List<edge>();
        List<ThFanCUModel> end_fanmodel = new List<ThFanCUModel>();
        List<List<edge>> boundary_edge = new List<List<edge>>();
        //下面未处理完成
        List<List<edge>> obstacle_edge = new List<List<edge>>();
        List<List<edge>> hole_edge = new List<List<edge>>();
        List<edge> hole_wall = new List<edge>();

        //框线索引
        Dictionary<Polyline, int> hole_index = new Dictionary<Polyline, int>();
        Dictionary<Polyline, int> boundary_index = new Dictionary<Polyline, int>();
        Dictionary<Polyline, int> fan_boundary_index = new Dictionary<Polyline, int>();

        //建立全图
        graph start_graph;

        //有风机的房间编号
        List<int> room_have_fan = new List<int>();

        //有起点的房间编号
        int room_have_start = -1;

        //房间-风机索引
        List<List<int>> room_fan_index = new List<List<int>>();
        //Dictionary<int, int> fan_room_index = new Dictionary<int, int>();
        List<int> fan_room_index = new List<int>();
        List<int> fan_without_room = new List<int>();

        //子房间
        List<List<int>> room_child = new List<List<int>>();

        //父房间
        List<int> room_father = new List<int>();

        //框线和hole的关系
        List<List<int>> room_hole = new List<List<int>>();

        //主干属性
        int main_room = -1;
        Point3d main_start;

        // 有风机的房间入口
        Dictionary<int, Point3d> room_start_points = new Dictionary<int, Point3d>();
        List<Point3d> room_out_points = new List<Point3d>();
        Dictionary<int, int> room_start_points2 = new Dictionary<int, int>();
        Dictionary<int, Point3d> room_start_points3 = new Dictionary<int, Point3d>();
        Dictionary<int, int> room_out_points3 = new Dictionary<int, int>();
        Dictionary<int, Point3d> room_start_points4 = new Dictionary<int, Point3d>();
        Dictionary<int, int> room_out_points4 = new Dictionary<int, int>();

        //风机属性
        List<double> fan_angle = new List<double>();
        List<Vector3d> fan_dir = new List<Vector3d>();

        //其他图
        List<List<int>> main_edge_graph = new List<List<int>>();

        //房间层次/距离
        List<int> room_order_far = new List<int>();
        Dictionary<int, int> room_dis = new Dictionary<int, int>();
        Dictionary<int, int> room_out_fan = new Dictionary<int, int>();
        public total_graph(List<ThFanCUModel> end_fanmodel, Point3d real_start_point, List<Polyline> boundary, List<Polyline> hole)
        {
            //记录起点
            this.real_start_point = real_start_point;

            //对风机进行预处理
            this.end_fanmodel = end_fanmodel;
            for (int i = 0; i < end_fanmodel.Count; i++)
            { 
                //框线处理
                fan_bounary.Add(end_fanmodel[i].FanObb);
                fan_boundary_index.Add(end_fanmodel[i].FanObb, i);
                //起点处理
                real_end_points_0.Add(new Point3d(end_fanmodel[i].FanPoint.X, end_fanmodel[i].FanPoint.Y, 0));
            }
            //计算所有风机的方向，并延申出一段距离作为起点
            for (int i = 0; i < real_end_points_0.Count; i++)
            {
                double tmp_angle = 10000;
                Vector3d tmp_vector = new Vector3d(0, 0, 0);
                data_p.find_model_direction(end_fanmodel[i], ref tmp_angle, ref tmp_vector);
                fan_angle.Add(tmp_angle);
                fan_dir.Add(tmp_vector);

                double length = 400;
                Point3d tmp_point = new Point3d();
                tmp_point = real_end_points_0[i] + tmp_vector.GetNormal().MultiplyBy(length);

                real_end_points.Add(tmp_point);
                processed_edges.Add(new edge(real_end_points_0[i].X, real_end_points_0[i].Y, real_end_points[i].X, real_end_points[i].Y));
            }

            //处理boundary
            this.boundary = boundary;
            for (int i = 0; i < boundary.Count; i++)
            {
                //建立索引
                boundary_index.Add(boundary[i], i);

                List<edge> tmp_edge = new List<edge>();
                List<Line> tmp_line = boundary[i].ToLines();
                for (int j = 0; j < tmp_line.Count; j++)
                {
                    tmp_edge.Add(new edge(tmp_line[j].StartPoint.X, tmp_line[j].StartPoint.Y, tmp_line[j].EndPoint.X, tmp_line[j].EndPoint.Y));
                }
                this.boundary_edge.Add(tmp_edge);
            }

            //将所有障碍物放入obstacle
            this.obstacle.AddRange(fan_bounary);
            this.obstacle.AddRange(this.boundary);


            //转换成edge
            for (int i = 0; i < obstacle.Count; i++)
            {
                List<edge> tmp_obstacle = new List<edge>();
                List<Line> tmp = obstacle[i].ToLines();
                for (int j = 0; j < tmp.Count; j++)
                {
                    tmp_obstacle.Add(new edge(tmp[j].StartPoint.X, tmp[j].StartPoint.Y, tmp[j].EndPoint.X, tmp[j].EndPoint.Y));
                }
                this.obstacle_edge.Add(tmp_obstacle);
            }

            //特殊处理wall
            this.total_wall = new List<edge>();
            for (int i = 0; i < obstacle_edge.Count; i++)
            {
                for (int j = 0; j < obstacle_edge[i].Count; j++)
                {
                    this.total_wall.Add(obstacle_edge[i][j]);
                }
            }

            //处理holes
            this.hole = hole;
            for (int i = 0; i < hole.Count; i++)
            {
                //建立索引
                hole_index.Add(hole[i], i);

                List<edge> tmp_edge = new List<edge>();
                List<Line> tmp_line = hole[i].ToLines();
                for (int j = 0; j < tmp_line.Count; j++)
                {
                    tmp_edge.Add(new edge(tmp_line[j].StartPoint.X, tmp_line[j].StartPoint.Y, tmp_line[j].EndPoint.X, tmp_line[j].EndPoint.Y));
                }
                this.hole_edge.Add(tmp_edge);
            }
            for (int i = 0; i < this.fan_bounary.Count; i++)
            {
                List<edge> tmp_edge = new List<edge>();
                List<Line> tmp_line = this.fan_bounary[i].ToLines();
                for (int j = 0; j < tmp_line.Count; j++)
                {
                    tmp_edge.Add(new edge(tmp_line[j].StartPoint.X, tmp_line[j].StartPoint.Y, tmp_line[j].EndPoint.X, tmp_line[j].EndPoint.Y));
                }
                this.hole_edge.Add(tmp_edge);
            }

            //特殊处理holes
            this.hole_wall = new List<edge>();
            for (int i = 0; i < hole_edge.Count; i++)
            {
                for (int j = 0; j < hole_edge[i].Count; j++)
                {
                    this.hole_wall.Add(hole_edge[i][j]);
                }
            }

            find_room_relationship(end_fanmodel, real_start_point, boundary, hole);

            start_connect3();
        }

   
        //mode
        public void find_room_start()
        {

            for (int i = 0; i < room_have_fan.Count; i++)
            {
                int index = room_have_fan[i];
                if (room_have_fan[i] == room_have_start) continue;

                List<grid_point> search_area = new List<grid_point>();

                List<edge> tmp_edge = new List<edge>();
                List<Line> tmp_line = boundary[index].ToLines();
                for (int j = 0; j < tmp_line.Count; j++)
                {
                    tmp_edge.Add(new edge(tmp_line[j].StartPoint.X, tmp_line[j].StartPoint.Y, tmp_line[j].EndPoint.X, tmp_line[j].EndPoint.Y));
                }


                for (int j = 0; j < tmp_edge.Count; j++)
                {

                    if (tmp_edge[j].rx2 < tmp_edge[j].rx1)
                    {
                        tool_total.Swap(ref tmp_edge[j].rx1, ref tmp_edge[j].rx2);
                        tool_total.Swap(ref tmp_edge[j].ry1, ref tmp_edge[j].ry2);
                    }
                    int start_x = (int)(tmp_edge[j].rx1 - start_graph.space_min_x) / PublicValue.bigcell;
                    int start_y = (int)(tmp_edge[j].ry1 - start_graph.space_min_y) / PublicValue.bigcell;
                    int end_x = (int)(tmp_edge[j].rx2 - start_graph.space_min_x) / PublicValue.bigcell;
                    int end_y = (int)(tmp_edge[j].ry2 - start_graph.space_min_y) / PublicValue.bigcell;

                    double length = Math.Sqrt(Math.Pow((tmp_edge[j].ry2 - tmp_edge[j].ry1), 2) + Math.Pow((tmp_edge[j].rx2 - tmp_edge[j].rx1), 2));
                    int step = PublicValue.bigcell - 100;
                    bool is_vertical = false;
                    if ((tmp_edge[j].rx2 - tmp_edge[j].rx1) < 100) is_vertical = true;

                    if (is_vertical)
                    {
                        for (int k = start_y; k <= end_y; k++)
                        {
                            search_area.Add(new grid_point(start_x, k));
                            if (start_x > 1)
                            {
                                search_area.Add(new grid_point(start_x - 1, k));
                                search_area.Add(new grid_point(start_x + 1, k));
                            }
                        }
                    }
                    else
                    {
                        double k = (tmp_edge[j].ry2 - tmp_edge[j].ry1) / (tmp_edge[j].rx2 - tmp_edge[j].rx1);
                        double step_x = 1 * step / (Math.Sqrt(1 + Math.Pow(k, 2)));
                        double step_y = k * step / (Math.Sqrt(1 + Math.Pow(k, 2)));

                        for (int m = 0; m * step <= length; m++)
                        {

                            int now_x = (int)(tmp_edge[j].rx1 + step_x * m - start_graph.space_min_x) / PublicValue.bigcell;
                            int now_y = (int)(tmp_edge[j].ry1 + step_y * m - start_graph.space_min_y) / PublicValue.bigcell;

                            search_area.Add(new grid_point(now_x, now_y));

                            if (now_x > 1)
                            {
                                search_area.Add(new grid_point(now_x + 1, now_y));
                                search_area.Add(new grid_point(now_x - 1, now_y));
                            }
                            if (now_y > 1)
                            {
                                search_area.Add(new grid_point(now_x, now_y + 1));
                                search_area.Add(new grid_point(now_x, now_y - 1));
                            }

                        }
                    }
                }


                int mindis = 999999;
                grid_point minpt = new grid_point(-1, -1);
                if (search_area.Count > 0)
                {
                    for (int j = 0; j < search_area.Count; j++)
                    {
                        if (start_graph.start_distance[search_area[j].x][search_area[j].y] != -1 && start_graph.start_distance[search_area[j].x][search_area[j].y] < 10000)
                        {
                            if (start_graph.start_distance[search_area[j].x][search_area[j].y] < mindis)
                            {
                                mindis = start_graph.start_distance[search_area[j].x][search_area[j].y];
                                minpt = search_area[j];
                            }
                        }
                    }

                    if (mindis < 100000)
                    {
                        double x = minpt.x * PublicValue.bigcell + start_graph.space_min_x;
                        double y = minpt.y * PublicValue.bigcell + start_graph.space_min_y;

                        Point3d real_entrance0 = new Point3d(x, y, 0);
                        Point3d real_entrance = boundary[index].GetClosePoint(real_entrance0);
                        processed_edges.Add(new edge(real_entrance0.X, real_entrance0.Y, real_entrance.X, real_entrance.Y));
                        room_start_points.Add(index, real_entrance);
                        room_out_points.Add(real_entrance0);
                    }
                }
            }


        }

        public void find_room_start2()
        {
            List<Line> main_line = new List<Line>();
            for (int i = 0; i < main_edges.Count; i++)
            {
                Line tmp = new Line(new Point3d(main_edges[i].rx1, main_edges[i].ry1, 0), new Point3d(main_edges[i].rx1, main_edges[i].ry2, 0));
                main_line.Add(tmp);
            }

            for (int i = 0; i < room_have_fan.Count; i++)
            {
                int room_index = room_have_fan[i];
                if (room_have_fan[i] == room_have_start) continue;

                double mindis = 1000000;
                int minindex = -1;
                for (int j = 0; j < room_fan_index[room_index].Count; j++)
                {
                    int fan_index = room_fan_index[room_index][j];
                    for (int k = 0; k < main_line.Count; k++)
                    {
                        double dis = main_line[k].GetClosestPointTo(real_end_points[fan_index], false).DistanceTo(real_end_points[fan_index]);
                        if (dis < mindis)
                        {
                            mindis = dis;
                            minindex = fan_index;
                        }
                    }
                }

                if (minindex != -1)
                {
                    room_start_points2.Add(room_index, minindex);
                }
            }
        }

        public void find_room_start3()
        {
            List<Line> main_line = new List<Line>();
            for (int i = 0; i < main_edges.Count; i++)
            {
                Line tmp = new Line(new Point3d(main_edges[i].rx1, main_edges[i].ry1, 0), new Point3d(main_edges[i].rx1, main_edges[i].ry2, 0));
                main_line.Add(tmp);
            }

            for (int i = 0; i < room_have_fan.Count; i++)
            {
                int room_index = room_have_fan[i];
                if (room_have_fan[i] == room_have_start || room_have_fan[i] == main_room) continue;

                double mindis = 1000000;
                int minindex = -1;
                for (int j = 0; j < room_fan_index[room_index].Count; j++)
                {
                    int fan_index = room_fan_index[room_index][j];

                    double dis = real_start_point.DistanceTo(real_end_points[fan_index]);
                    if (dis < mindis)
                    {
                        mindis = dis;
                        minindex = fan_index;
                    }
                }

                if (minindex != -1)
                {

                    List<List<int>> tmp_dis = new List<List<int>>();
                    grid_point point_in_main = new grid_point(-1, -1);
                    start_graph.initialize_board(ref tmp_dis, start_graph.width, start_graph.height, 0);
                    start_graph.copy_board(start_graph.board_0, ref tmp_dis, start_graph.width, start_graph.height, -1);
                    start_graph.calculate_distance_to_main(start_graph.board_0, ref tmp_dis, main_edge_graph, ref point_in_main, start_graph.end_points[minindex].x, start_graph.end_points[minindex].y);

                    //分两种情况
                    double main_x = -1,main_y = -1;
                    single_point_to_real(ref main_x, ref main_y, point_in_main.x, point_in_main.y, PublicValue.bigcell, 1);

                    Point3d internal_point = new Point3d(main_x, main_y, 0);
                    find_real_point(point_in_main ,ref internal_point);
                    //Point3d real_entrance = .GetClosePoint();

                    if (this.boundary[room_index].Contains(internal_point))
                    {
                        //处理主干经过有风机的房间的情况
                        int tmp_index = start_graph.real_end_points.Count;
                        start_graph.end_points.Add(point_in_main);
                        start_graph.real_end_points.Add(internal_point);
                        //room_out_points3.Add(room_index,tmp_index);
                        room_start_points3.Add(room_index, internal_point);
                        continue;
                    }
                    else
                    {
                        //处理主干不经过有风机的房间的正常情况
                        List<edge> path = new List<edge>();

                        int index = start_graph.nodes_tmp.Count;

                        //以与主干的交点为原点，建立distance_map
                        grid_point tmp = point_in_main;
                        node test_node = new node(tmp.x, tmp.y);
                        if (start_graph.indexmap_tmp.ContainsKey(test_node) == false)
                        {
                            start_graph.add_node_tmp(index, tmp);
                        }
                        else
                        {
                            index = start_graph.indexmap_tmp[test_node];
                        }

                        //寻找最近的风机
                        int mindis2 = 100000;
                        int minindex2 = -1;
                        for (int j = 0; j < room_fan_index[room_index].Count; j++)
                        {
                            int fan_index = room_fan_index[room_index][j];
                            int fan_x = start_graph.end_points[fan_index].x; 
                            int fan_y = start_graph.end_points[fan_index].y;
                            int dis = start_graph.nodes_tmp[index].distance[fan_x][fan_y];
                            if (dis < mindis2)
                            {
                                mindis2 = dis;
                                minindex2 = fan_index;
                            }
                        }


                        //寻找内部的起点
                        grid_point tmp_start = new grid_point(0, 0);
                        grid_point tmp_out = new grid_point(0, 0);
                        start_graph.find_shortest_path_start(start_graph.end_points[minindex2], index, 1, ref tmp_start, ref tmp_out);

                        if (tmp_out.x == 0 && tmp_out.y == 0) 
                        {
                            tmp_out.x = tmp_start.x;
                            tmp_out.y = tmp_start.y;
                        }

                        double tmp_x = -1, tmp_y = -1;
                        single_point_to_real(ref tmp_x, ref tmp_y, tmp_start.x, tmp_start.y, PublicValue.bigcell, 1);
                        
                        //调整起点真实位置;
                        if (tmp_start.y == start_graph.end_points[minindex].y) tmp_y = start_graph.real_end_points[minindex].Y;
                        if (tmp_start.x == start_graph.end_points[minindex].x) tmp_x = start_graph.real_end_points[minindex].X;

                        Point3d real_entrance = new Point3d(tmp_x, tmp_y, 0);
                        room_start_points3.Add(room_index, real_entrance);

                        //寻找外部的起点
                        double end_x = -1, end_y = -1;
                        single_point_to_real(ref end_x, ref end_y, tmp_out.x, tmp_out.y, PublicValue.bigcell, 1);

                        if (tmp_start.y == tmp_out.y) end_y = tmp_y;
                        if (tmp_start.x == tmp_out.x) end_x = tmp_x;

                        processed_edges.Add(new edge(end_x, end_y, real_entrance.X, real_entrance.Y));
                        int out_index = start_graph.real_end_points.Count;
                        start_graph.end_points.Add(tmp_out);
                        start_graph.real_end_points.Add(new Point3d(end_x, end_y, 0));

                        room_out_points3.Add(room_index, out_index);
                    }
                }
            }
        }

        public void find_room_start4(int room_index)
        {
            List<Line> main_line = new List<Line>();
            for (int i = 0; i < main_edges.Count; i++)
            {
                Line tmp = new Line(new Point3d(main_edges[i].rx1, main_edges[i].ry1, 0), new Point3d(main_edges[i].rx1, main_edges[i].ry2, 0));
                main_line.Add(tmp);
            }

            //寻找最佳出口
            double mindis = 1000000;
            int minindex = -1;
            for (int j = 0; j < room_fan_index[room_index].Count; j++)
            {
                int fan_index = room_fan_index[room_index][j];

                double dis = real_start_point.DistanceTo(real_end_points[fan_index]);
                if (dis < mindis)
                {
                    mindis = dis;
                    minindex = fan_index;
                }
            }

            if (minindex != -1)
            {

                List<List<int>> tmp_dis = new List<List<int>>();
                grid_point point_in_main = new grid_point(-1, -1);
                start_graph.initialize_board(ref tmp_dis, start_graph.width, start_graph.height, 0);
                start_graph.copy_board(start_graph.board_0, ref tmp_dis, start_graph.width, start_graph.height, -1);
                start_graph.calculate_distance_to_main(start_graph.board_0, ref tmp_dis, main_edge_graph, ref point_in_main, start_graph.end_points[minindex].x, start_graph.end_points[minindex].y);

                //分两种情况
                double main_x = start_graph.space_min_x + point_in_main.x * PublicValue.bigcell;
                double main_y = start_graph.space_min_y + point_in_main.y * PublicValue.bigcell;

                Point3d internal_point = new Point3d(main_x, main_y, 0);
                find_real_point(point_in_main, ref internal_point);
                //Point3d real_entrance = .GetClosePoint();

                if (this.boundary[room_index].Contains(internal_point))
                {
                    //处理主干经过有风机的房间的情况
                    int tmp_index = start_graph.real_end_points.Count;
                    start_graph.end_points.Add(point_in_main);
                    start_graph.real_end_points.Add(internal_point);
                    //room_out_points3.Add(room_index,tmp_index);
                    room_start_points4.Add(room_index, internal_point);
                }
                else
                {
                    //处理主干不经过有风机的房间的正常情况
                    List<edge> path = new List<edge>();

                    int index = start_graph.nodes_tmp.Count;

                    //以与主干的交点为原点，建立distance_map
                    grid_point tmp = point_in_main;
                    node test_node = new node(tmp.x, tmp.y);
                    if (start_graph.indexmap_tmp.ContainsKey(test_node) == false)
                    {
                        start_graph.add_node_tmp(index, tmp);
                    }
                    else
                    {
                        index = start_graph.indexmap_tmp[test_node];
                    }

                    //寻找内部的起点
                    grid_point tmp_start = new grid_point(0, 0);
                    grid_point tmp_out = new grid_point(0, 0);
                    start_graph.find_shortest_path_start(start_graph.end_points[minindex], index, 1, ref tmp_start, ref tmp_out);

                    double tmp_x = start_graph.space_min_x + tmp_start.x * PublicValue.bigcell;
                    double tmp_y = start_graph.space_min_y + tmp_start.y * PublicValue.bigcell;

                    //调整起点真实位置;
                    if (tmp_start.y == start_graph.end_points[minindex].y) tmp_y = start_graph.real_end_points[minindex].Y;
                    if (tmp_start.x == start_graph.end_points[minindex].x) tmp_x = start_graph.real_end_points[minindex].X;

                    Point3d real_entrance = new Point3d(tmp_x, tmp_y, 0);
                    room_start_points4.Add(room_index, real_entrance);

                    double end_x = start_graph.space_min_x + tmp_out.x * PublicValue.bigcell;
                    double end_y = start_graph.space_min_y + tmp_out.y * PublicValue.bigcell;

                    if (tmp_start.y == tmp_out.y) end_y = tmp_y;
                    if (tmp_start.x == tmp_out.x) end_x = tmp_x;

                    processed_edges.Add(new edge(end_x, end_y, real_entrance.X, real_entrance.Y));
                    int out_index = start_graph.real_end_points.Count;
                    start_graph.end_points.Add(tmp_out);
                    start_graph.real_end_points.Add(new Point3d(end_x, end_y, 0));

                    room_out_points4.Add(room_index, out_index);

                }            
            }
        }

        public void start_connect()
        {
            //寻找主干
            PublicValue.CELL = PublicValue.bigcell;
            PublicValue.extension = 0;
            PublicValue.traversable = 1;
            start_graph = new graph(real_end_points, real_start_point, total_wall, this.hole_wall);
            find_farthest_room();
            PublicValue.extension = 1;
            PublicValue.traversable = 1;
            PublicValue.CELL = PublicValue.smallcell;

            //find_room_start();

            ////记载node
            total_insert.Add(real_start_point);
            total_insert.AddRange(room_out_points);


            //正式开始连线
            List<int> room_waiting = new List<int>();

            for (int i = 0; i < room_have_fan.Count; i++)
            {

                int index = room_have_fan[i];
                if (room_start_points.ContainsKey(index) && room_fan_index[index].Count != 0)
                {
                    List<Point3d> ex_end_points = new List<Point3d>();
                    single_room(index, room_start_points[index], ex_end_points);
                }
                else if (index != room_have_start)
                {
                    room_waiting.Add(index);
                }

            }

            //中心区域连接
            List<Point3d> total_end_point = new List<Point3d>();
            //for (int i = 0; i < room_have_fan.Count; i++)
            //{
            //    int index = room_have_fan[i];
            //    if (room_start_points.ContainsKey(index))
            //    {
            //        total_end_point.Add(room_start_points[index]);
            //    }
            //}

            total_end_point.AddRange(room_out_points);

            if (room_fan_index[room_have_start].Count != 0)
            {
                for (int i = 0; i < room_fan_index[room_have_start].Count; i++)
                {
                    int index = room_fan_index[room_have_start][i];
                    total_end_point.Add(real_end_points[index]);
                }
            }

            single_room(room_have_start, real_start_point, total_end_point);

            //其余区域连接
            for (int i = 0; i < room_waiting.Count; i++)
            {
                far_room_connect(room_waiting[i]);
            }

        }

        public void start_connect2()
        {
            //寻找主干
            PublicValue.CELL = PublicValue.bigcell;
            PublicValue.extension = 0;
            PublicValue.traversable = 1;
            PublicValue.arrange_mode = 1;
            start_graph = new graph(real_end_points, real_start_point, total_wall, this.hole_wall);
            find_farthest_room();

            find_room_start2();
            //find_room_start3();

            //每个房间连接到主干
            List<int> keylist = new List<int>(room_start_points2.Keys);
            for (int i = 0; i < keylist.Count; i++)
            {
                if (keylist[i] == main_room) continue;
                int fan_index = room_start_points2[keylist[i]];

                List<List<int>> tmp_dis = new List<List<int>>();
                grid_point point_in_main = new grid_point(-1, -1);
                start_graph.initialize_board(ref tmp_dis, start_graph.width, start_graph.height, 0);
                start_graph.copy_board(start_graph.board_0, ref tmp_dis, start_graph.width, start_graph.height, -1);
                start_graph.calculate_distance_to_main(start_graph.board_0, ref tmp_dis, main_edge_graph, ref point_in_main, start_graph.end_points[fan_index].x, start_graph.end_points[fan_index].y);

                List<edge> path = new List<edge>();

                int index = start_graph.nodes.Count;

                grid_point tmp = start_graph.end_points[fan_index];
                node test_node = new node(tmp.x, tmp.y);
                if (start_graph.indexmap.ContainsKey(test_node) == false)
                {
                    index = start_graph.nodes.Count;
                    start_graph.add_node(index, start_graph.end_points[fan_index]);
                }
                else
                {
                    index = start_graph.indexmap[test_node];
                }

                start_graph.find_shortest_path_clear(point_in_main, index, ref path);
                start_graph.long_edges.AddRange(path);
            }

            start_graph.analysis_edge();
            start_graph.point_to_real(-1);
            start_graph.find_point_edge_relation(-1);
            start_graph.connect_edge(-1);
            start_graph.post_processing(-1);
            processed_edges.AddRange(start_graph.long_edges);

            PublicValue.arrange_mode = 0;
            PublicValue.extension = 1;
            PublicValue.traversable = 0;
            PublicValue.CELL = PublicValue.smallcell;

            //中心区域连接
            List<Point3d> total_end_point = new List<Point3d>();
            single_room(room_have_start, real_start_point, total_end_point);

            //连接每个房间内的风机
            for (int i = 0; i < room_have_fan.Count; i++)
            {
                int index = room_have_fan[i];
                if (room_start_points2.ContainsKey(index) && room_fan_index[index].Count != 0)
                {
                    List<Point3d> ex_end_points = new List<Point3d>();
                    Point3d tmp_start = real_end_points[room_start_points2[index]];
                    single_room(index, tmp_start, ex_end_points);
                }
            }
        }

        public void start_connect3()
        {
            //寻找主干
            PublicValue.CELL = PublicValue.bigcell;
            PublicValue.extension = 0;
            PublicValue.traversable = 1;
            PublicValue.arrange_mode = 1; //以后要把整线的单独拎出来
            PublicValue.center = 1; 
            start_graph = new graph(real_end_points, real_start_point, total_wall, this.hole_wall);


            //print_graph();
            //print_distance();
            //print_point();
            find_farthest_room();

            //每个房间连接到主干
            if (main_edges.Count != 0)
            {
                //find_room_start2();
                find_room_start3();
                List<int> keylist = new List<int>(room_out_points3.Keys);
                for (int i = 0; i < keylist.Count; i++)
                {
                    if (keylist[i] == main_room) continue;
                    int fan_index = room_out_points3[keylist[i]];

                    List<List<int>> tmp_dis = new List<List<int>>();
                    grid_point point_in_main = new grid_point(-1, -1);
                    start_graph.initialize_board(ref tmp_dis, start_graph.width, start_graph.height, 0);
                    start_graph.copy_board(start_graph.board_0, ref tmp_dis, start_graph.width, start_graph.height, -1);
                    start_graph.calculate_distance_to_main(start_graph.board_0, ref tmp_dis, main_edge_graph, ref point_in_main, start_graph.end_points[fan_index].x, start_graph.end_points[fan_index].y);

                    List<edge> path = new List<edge>();

                    int index = start_graph.nodes.Count;

                    grid_point tmp = start_graph.end_points[fan_index];
                    node test_node = new node(tmp.x, tmp.y);
                    if (start_graph.indexmap.ContainsKey(test_node) == false)
                    {
                        index = start_graph.nodes.Count;
                        start_graph.add_node(index, tmp);
                    }
                    else
                    {
                        index = start_graph.indexmap[test_node];
                    }

                    start_graph.find_shortest_path_clear(point_in_main, index, ref path);

                    start_graph.long_edges.AddRange(path);

                    for (int n = 0; n < path.Count; n++)
                    {
                        int sx = path[n].x1;
                        int sy = path[n].y1;
                        int ex = path[n].x2;
                        int ey = path[n].y2;
                        if (sy == ey)
                        {
                            for (int a = sx; a <= ex; a++)
                            {
                                main_edge_graph[a][sy] = 1;
                            }
                        }
                        else
                        {
                            for (int a = sy; a <= ey; a++)
                            {
                                main_edge_graph[sx][a] = 1;
                            }
                        }
                    }
                }

                start_graph.analysis_edge_clear(-1);     //这里有一个严重的问题
                start_graph.point_to_real(-1);
                start_graph.find_point_edge_relation(-1);
                start_graph.connect_edge(-1);
                start_graph.post_processing(-1);
                processed_edges.AddRange(start_graph.long_edges);
            }


            PublicValue.arrange_mode = 0;
            PublicValue.extension = 1;
            PublicValue.traversable = 0;
            PublicValue.CELL = PublicValue.smallcell;
            PublicValue.center = 0;

            //中心区域连接
            List<Point3d> total_end_point = new List<Point3d>();
            //single_room(room_have_start, real_start_point, total_end_point);

            //连接每个房间内的风机
            for (int i = 0; i < room_have_fan.Count; i++)
            {
                int index = room_have_fan[i];
                if (room_start_points3.ContainsKey(index) && room_fan_index[index].Count != 0)
                {
                    List<Point3d> ex_end_points = new List<Point3d>();
                    Point3d tmp_start = room_start_points3[index];
                    single_room(index, tmp_start, ex_end_points);
                }
            }
        }

        public void start_connect4()
        {
            //寻找主干
            PublicValue.CELL = PublicValue.bigcell;
            PublicValue.extension = 0;
            PublicValue.traversable = 1;
            PublicValue.arrange_mode = 1; //以后要把整线的单独拎出来
            start_graph = new graph(real_end_points, real_start_point, total_wall, this.hole_wall);

            //print_graph();
            //print_distance();
            //print_point();
            find_farthest_room();

            //每个房间连接到主干
            if (main_edges.Count != 0)
            {
                //将房间按距离起点的距离排序
                room_dis_list();

                for (int i = 0; i < room_order_far.Count; i++)
                {

                    PublicValue.CELL = PublicValue.bigcell;
                    PublicValue.extension = 0;
                    PublicValue.traversable = 1;
                    PublicValue.arrange_mode = 1; //以后要把整线的单独拎出来
                    start_graph = new graph(real_end_points, real_start_point, total_wall, this.hole_wall);

                    int room_index = room_order_far[i];
                    if (room_index == main_room) continue;
                    find_room_start4(room_index);

                    //如果在外部
                    if (room_out_points4.ContainsKey(room_index)) 
                    {
                        int out_index = room_out_points4[room_index];
                        Point3d room_start = room_start_points4[room_index];

                        List<List<int>> tmp_dis = new List<List<int>>();
                        grid_point point_in_main = new grid_point(-1, -1);
                        start_graph.initialize_board(ref tmp_dis, start_graph.width, start_graph.height, 0);
                        start_graph.copy_board(start_graph.board_0, ref tmp_dis, start_graph.width, start_graph.height, -1);
                        start_graph.calculate_distance_to_main(start_graph.board_0, ref tmp_dis, main_edge_graph, ref point_in_main, start_graph.end_points[out_index].x, start_graph.end_points[out_index].y);

                        List<edge> path = new List<edge>();

                        int index = start_graph.nodes.Count;
                    
                        grid_point tmp = start_graph.end_points[out_index];
                        node test_node = new node(tmp.x, tmp.y);
                        if (start_graph.indexmap.ContainsKey(test_node) == false)
                        {
                            index = start_graph.nodes.Count;
                            start_graph.add_node(index, tmp);
                        }
                        else
                        {
                            index = start_graph.indexmap[test_node];
                        }

                        start_graph.find_shortest_path_clear(point_in_main, index, ref path);

                        start_graph.long_edges_list.Add(path);

   

                        for (int n = 0; n < path.Count; n++)
                        {
                            int sx = path[n].x1;
                            int sy = path[n].y1;
                            int ex = path[n].x2;
                            int ey = path[n].y2;
                            if (sy == ey)
                            {
                                for (int a = sx; a <= ex; a++)
                                {
                                    main_edge_graph[a][sy] = 1;
                                }
                            }
                            else
                            {
                                for (int a = sy; a <= ey; a++)
                                {
                                    main_edge_graph[sx][a] = 1;
                                }
                            }
                        }
                    }

                    int index4 = start_graph.long_edges_list.Count-1;

                    start_graph.analysis_edge_clear(index4);     //这里有一个严重的问题
                    start_graph.point_to_real(index4);
                    start_graph.find_point_edge_relation(index4);
                    start_graph.connect_edge(index4);
                    start_graph.post_processing(index4);
                    processed_edges.AddRange(start_graph.long_edges_list[index4]);

                    PublicValue.arrange_mode = 0;
                    PublicValue.extension = 1;
                    PublicValue.traversable = 0;
                    PublicValue.CELL = PublicValue.smallcell;

                    if (room_start_points4.ContainsKey(room_index) && room_fan_index[room_index].Count != 0)
                    {
                        List<Point3d> ex_end_points = new List<Point3d>();
                        Point3d tmp_start = room_start_points4[room_index];
                        single_room(room_index, tmp_start, ex_end_points);
                    }
                } 
            }


          

            //中心区域连接
            List<Point3d> total_end_point = new List<Point3d>();
            //single_room(room_have_start, real_start_point, total_end_point);

        }

        public void far_room_connect(int room_index)
        {

            //寻找最佳点位
            double mindis = 999999;
            Point3d minpt = new Point3d(0, 0, 0);
            for (int i = 0; i < total_insert.Count; i++)
            {
                if (boundary[room_index].Distance(total_insert[i]) < mindis)
                {
                    mindis = boundary[room_index].Distance(total_insert[i]);
                    minpt = total_insert[i];
                }
            }

            //以此点位做特殊处理
            PublicValue.CELL = PublicValue.bigcell;
            //PublicValue.extension = 0;
            PublicValue.traversable = 1;
            PublicValue.arrange_mode = 1;

            graph connect_far_room_graph = new graph(real_end_points, minpt, this.total_wall, this.hole_wall);

            int mindis2 = 999999;
            grid_point min_model = new grid_point(0, 0);
            int min_model_index = 999999;
            for (int i = 0; i < room_fan_index[room_index].Count; i++)
            {
                int model_index = room_fan_index[room_index][i];
                int x = connect_far_room_graph.end_points[model_index].x;
                int y = connect_far_room_graph.end_points[model_index].y;
                if (connect_far_room_graph.start_distance[x][y] < mindis2)
                {
                    mindis2 = connect_far_room_graph.start_distance[x][y];
                    min_model = connect_far_room_graph.end_points[model_index];
                    min_model_index = model_index;
                }
            }

            if (mindis2 != 999999)
            {


                List<edge> path = new List<edge>();
                connect_far_room_graph.add_node_tmp(0, min_model);
                connect_far_room_graph.find_shortest_path(0, 0, ref path);

                connect_far_room_graph.long_edges.AddRange(path);
                connect_far_room_graph.analysis_edge();
                connect_far_room_graph.point_to_real(-1);
                connect_far_room_graph.post_processing(-1);

                processed_edges.AddRange(connect_far_room_graph.long_edges);

                Point3d far_room_start = new Point3d(real_end_points[min_model_index].X, real_end_points[min_model_index].Y, 0);


                PublicValue.extension = 1;
                PublicValue.CELL = PublicValue.smallcell;
                PublicValue.traversable = 0;
                PublicValue.arrange_mode = 0;

                List<Point3d> ex_end_points = new List<Point3d>();
                single_room(room_index, far_room_start, ex_end_points);
            }
            //

            //返回边
        }

        public void find_farthest_room()
        {         
            int maxdis = 0;
            int max_index = -1;

            for (int i = 0; i < room_have_fan.Count; i++)
            {
                int tmp_room = room_have_fan[i];
                int tmpmindis = 10000000;
                int tmpminindex = -1;

                for (int j = 0; j < room_fan_index[tmp_room].Count; j++)
                {
                    int tmpfanindex = room_fan_index[tmp_room][j];
                    int x = start_graph.end_points[tmpfanindex].x;
                    int y = start_graph.end_points[tmpfanindex].y;
                    if (start_graph.start_distance[x][y] < tmpmindis)
                    {
                        tmpmindis = start_graph.start_distance[x][y];
                        tmpminindex = tmpfanindex;
                    }
                }

                if (tmpmindis > maxdis)
                {
                    maxdis = tmpmindis;
                    max_index = tmpminindex;
                }
            }

            int room_index = fan_room_index[max_index];
            main_room = room_index;
            int min_index = max_index;

            if (min_index != -1 && main_room != room_have_start)
            {
                //寻找第一个房间的入口/寻找起点
                grid_point tmp_start = new grid_point(0, 0);
                grid_point tmp_out = new grid_point(0, 0);
                start_graph.find_shortest_path_start(start_graph.end_points[min_index], 0, 0, ref tmp_start, ref tmp_out);

                double tmp_x = -1, tmp_y = -1;
                single_point_to_real(ref tmp_x, ref tmp_y, tmp_start.x, tmp_start.y, PublicValue.bigcell, 1);

                //调整起点真实位置;
                if (tmp_start.y == start_graph.end_points[min_index].y) tmp_y = start_graph.real_end_points[min_index].Y;
                if (tmp_start.x == start_graph.end_points[min_index].x) tmp_x = start_graph.real_end_points[min_index].X;

                Point3d real_entrance = new Point3d(tmp_x, tmp_y, 0);
                room_start_points3.Add(room_index, real_entrance);

                //寻找外部的起点

                //Point3d intersection = boundary[room_index].GetClosePoint(real_entrance);
                //processed_edges.Add(new edge(intersection.X, intersection.Y, real_entrance.X, real_entrance.Y));

                //int end_point_x = (int)(intersection.X - start_graph.space_min_x) / PublicValue.bigcell;
                //int end_point_y = (int)(intersection.Y - start_graph.space_min_y) / PublicValue.bigcell;

                //int out_index = start_graph.real_end_points.Count;
                //start_graph.end_points.Add(new grid_point(end_point_x, end_point_y));
                //start_graph.real_end_points.Add(intersection);

                double end_x = -1, end_y = -1;
                single_point_to_real(ref end_x, ref end_y, tmp_out.x, tmp_out.y, PublicValue.bigcell, 1);


                if (tmp_start.y == tmp_out.y) end_y = tmp_y;
                if (tmp_start.x == tmp_out.x) end_x = tmp_x;


                processed_edges.Add(new edge(end_x, end_y, real_entrance.X, real_entrance.Y));
                int out_index = start_graph.real_end_points.Count;
                start_graph.end_points.Add(tmp_out);
                start_graph.real_end_points.Add(new Point3d(end_x, end_y, 0));

                room_out_points3.Add(room_index, out_index);

                //连线
                List<edge> path = new List<edge>();

                start_graph.find_shortest_path_clear(start_graph.end_points[room_out_points3[room_index]], 0, ref path);
                start_graph.long_edges.AddRange(path);

                start_graph.analysis_edge_clear(-1);
                start_graph.point_to_real(-1);
                start_graph.find_point_edge_relation(-1);
                start_graph.connect_edge(-1);
                start_graph.post_processing(-1);

                //processed_edges.AddRange(start_graph.long_edges);
                main_edges.AddRange(start_graph.long_edges);

                //记录主干              
                start_graph.initialize_board(ref main_edge_graph, start_graph.width, start_graph.height, 0);
                for (int i = 0; i < start_graph.long_edges.Count; i++)
                {
                    int sx = start_graph.long_edges[i].x1;
                    int sy = start_graph.long_edges[i].y1;
                    int ex = start_graph.long_edges[i].x2;
                    int ey = start_graph.long_edges[i].y2;
                    if (sy == ey)
                    {
                        for (int n = sx; n <= ex; n++)
                        {
                            main_edge_graph[n][sy] = 1;
                        }
                    }
                    else
                    {
                        for (int n = sy; n <= ey; n++)
                        {
                            main_edge_graph[sx][n] = 1;
                        }
                    }
                }


            }

        }

        //general
        public void find_room_relationship(List<ThFanCUModel> end_fanmodel, Point3d real_start_point, List<Polyline> boundary, List<Polyline> hole)
        {
            //初始化 占位
            for (int i = 0; i < boundary.Count; i++)
            {
                room_father.Add(-1);
            }

            for (int i = 0; i < real_end_points.Count; i++)
            {
                fan_room_index.Add(-1);
            }

            for (int i = 0; i < boundary.Count; i++)
            {
                room_fan_index.Add(new List<int>());
            }

            //读取点即可
            var hole_spindex = new ThCADCoreNTSSpatialIndex(hole.ToCollection());
            var boundary_spindex = new ThCADCoreNTSSpatialIndex(boundary.ToCollection());
            var fan_spindex = new ThCADCoreNTSSpatialIndex(fan_bounary.ToCollection());  
                        
            //

            for (int i = 0; i < boundary.Count; i++)
            {
                //起点
                if (boundary[i].Contains(real_start_point)) room_have_start = i;

                //风机
                var fan_list = fan_spindex.SelectCrossingPolygon(boundary[i]);
                foreach (var db in fan_list) 
                {
                    var pline = db as Polyline;
                    int a = fan_boundary_index[pline];
                 
                    if (fan_room_index[a] == -1)
                    {
                        fan_room_index[a] = i;
                    }
                    else
                    {
                        int room_index = fan_room_index[a];
                        if (boundary[i].Contains(boundary[room_index]))
                        {
                            fan_room_index[a] = room_index;
                        }
                        else
                        {
                            fan_room_index[a] = i;
                        }
                    }
                }

                //if (tmp.Count != 0) room_have_fan.Add(i);
                //room_fan_index.Add(tmp);

                //框线
                var boundary_list = boundary_spindex.SelectCrossingPolygon(boundary[i]);
                List<int> tmp2 = new List<int>();
                foreach (var db in boundary_list)
                {
                    var pline = db as Polyline;
                    int a = boundary_index[pline];
                    if (a == i) continue;

                    tmp2.Add(a);
                    room_father[a] = i;
                }
                room_child.Add(tmp2);

                //hole
                var hole_list = hole_spindex.SelectCrossingPolygon(boundary[i]);
                List<int> tmp3 = new List<int>();
                foreach (var db in hole_list)
                {
                    var pline = db as Polyline;
                    int a = hole_index[pline];
                    tmp3.Add(a);
                }
                room_hole.Add(tmp3);

            }

            //
            for (int i = 0; i < fan_room_index.Count; i++)
            {
                int room_index = fan_room_index[i];
                if (room_index == -1)
                {
                    fan_without_room.Add(i);
                    continue;
                }
                if (room_have_fan.Contains(room_index) == false) room_have_fan.Add(room_index);
                room_fan_index[room_index].Add(i);
            }
        }

        public void find_minxy(ref Point3d pointxy, int index)
        {

            double space_max_x = -PublicValue.MAX_LENGTH;
            double space_min_x = PublicValue.MAX_LENGTH;
            double space_max_y = -PublicValue.MAX_LENGTH;
            double space_min_y = PublicValue.MAX_LENGTH;

            //获取min_x 和 max_x
            //待修改
            for (int i = 0; i < boundary_edge[index].Count; i++)
            {
                Point3d pt = new Point3d(boundary_edge[index][i].rx1, boundary_edge[index][i].ry1, 0);
                if (pt.X > space_max_x)
                {
                    space_max_x = pt.X;
                }
                if (pt.X < space_min_x)
                {
                    space_min_x = pt.X;
                }
                if (pt.Y > space_max_y)
                {
                    space_max_y = pt.Y;
                }
                if (pt.Y < space_min_y)
                {
                    space_min_y = pt.Y;
                }
            }

            pointxy = new Point3d(space_min_x, space_min_y, 0);

        }

        public void single_room(int room_index, Point3d start_point, List<Point3d> extra_end_points)
        {

            //标记终点
            List<Point3d> end_points = new List<Point3d>();

            //标记起点

            //整合boundary + 标记终点
            List<Polyline> tmp_boundary = new List<Polyline>();
            tmp_boundary.Add(boundary[room_index]);
            for (int i = 0; i < room_fan_index[room_index].Count; i++)
            {
                int fan_index = room_fan_index[room_index][i];
                tmp_boundary.Add(end_fanmodel[fan_index].FanObb);
                end_points.Add(real_end_points[fan_index]);
            }

            //将boundary统一转换成edge
            List<edge> tmp_line = new List<edge>();

            for (int i = 0; i < tmp_boundary.Count; i++)
            {
                List<Line> tmp = tmp_boundary[i].ToLines();

                for (int j = 0; j < tmp.Count; j++)
                {
                    tmp_line.Add(new edge(tmp[j].StartPoint.X, tmp[j].StartPoint.Y, tmp[j].EndPoint.X, tmp[j].EndPoint.Y));
                }
            }
            for (int i = 0; i < room_fan_index[room_index].Count; i++)
            {
                int fan_index = room_fan_index[room_index][i];
                tmp_line.Add(new edge(real_end_points[fan_index].X, real_end_points[fan_index].Y, real_end_points_0[fan_index].X, real_end_points_0[fan_index].Y));
            }



            //整合hole+转换
            List<edge> tmp_hole = new List<edge>();
            for (int i = 0; i < room_hole[room_index].Count; i++)
            {
                int index = room_hole[room_index][i];
                tmp_hole.AddRange(hole_edge[index]);
            }

            //判断终点是否被调整过（多出了额外终点）
            if (extra_end_points.Count != 0)
            {
                end_points = extra_end_points;
            }

            //判断区域方向
            int trans = 1;
            double angle = 99999;
            Vector3d vector = new Vector3d();
            if (room_fan_index[room_index].Count != 0)
            {
                angle = fan_angle[room_fan_index[room_index][0]];
                vector = fan_dir[room_fan_index[room_index][0]];
                if (Math.Abs(vector.X) < 50 || Math.Abs(vector.Y) < 50) trans = 0;
            }
            else
            {
                trans = 0;
            }

            //寻找新坐标原点
            Point3d minxy = new Point3d();
            if (trans == 1)
            {

                //if (angle > Math.PI) angle = angle - 2 * Math.PI;   
                find_minxy(ref minxy, room_index);

                //坐标变换
                //for(int i = 0;i < tmp_line.Count)
                data_p.rotate_area(-angle, minxy, ref end_points, ref start_point, ref tmp_line, ref tmp_hole);
            }


            //连线
            single_graph room_0 = new single_graph(end_points, start_point, tmp_line, tmp_hole);
            List<edge> edges_out = room_0.processed_edges;


            //输出线条坐标逆变换
            if (trans == 1)
            {
                data_p.rotate_edgelist(angle, minxy, ref edges_out);
            }


            //返回边
            processed_edges.AddRange(edges_out);
            //记录node
            for (int i = 0; i < edges_out.Count; i++)
            {
                total_insert.Add(new Point3d(edges_out[i].rx1, edges_out[i].ry1, 0));
            }

            //total_node.AddRange(room_0.mark_node);
        }

        public void room_dis_list()
        {
            for (int i = 0; i < room_have_fan.Count; i++)
            {
                //包含起点的房间不考虑;
                int tmp_room = room_have_fan[i];
                if (tmp_room == room_have_start) continue;

                int tmp_mindis = 10000000;
                int tmp_min_fanindex = -1;

                for (int j = 0; j < room_fan_index[tmp_room].Count; j++)
                {
                    int tmp_fan_index = room_fan_index[tmp_room][j];
                    int x = start_graph.end_points[tmp_fan_index].x;
                    int y = start_graph.end_points[tmp_fan_index].y;
                    if (start_graph.start_distance[x][y] < tmp_mindis)
                    {
                        tmp_mindis = start_graph.start_distance[x][y];
                        tmp_min_fanindex = tmp_fan_index;
                    }
                }

                room_dis.Add(tmp_room, tmp_mindis);
                room_out_fan.Add(tmp_room, tmp_min_fanindex);
                room_order_far.Add(tmp_room);
            }

            room_order_far.Sort(Compare);
        }

        public void single_point_to_real(ref double rx, ref double ry, int x, int y, int cell , int mode) 
        {
            if (mode == 0)
            {
                rx = start_graph.space_min_x + x * cell;
                ry = start_graph.space_min_y + y * cell;
            } 
            else if(mode == 1)
            {
                rx = start_graph.space_min_x + x * cell+  PublicValue.deviation;
                ry = start_graph.space_min_y + y * cell+  PublicValue.deviation;
            }
        }  
        ////general_draw


        public void find_real_point(grid_point pt, ref Point3d real_pt) 
        {
            double tmp_x = real_pt.X;
            double tmp_y = real_pt.Y;
            for (int i = 0; i < start_graph.long_edges.Count; i++)
            {
                if (start_graph.on_edge(start_graph.long_edges[i],pt))
                {
                    if (pt.x == start_graph.long_edges[i].x1) tmp_x = start_graph.long_edges[i].rx1;
                    if (pt.y == start_graph.long_edges[i].y1) tmp_y = start_graph.long_edges[i].ry1;
                    break;
                } 
            }
            real_pt = new Point3d(tmp_x, tmp_y, 0);
        }

        public void point_to_point_real() 
        {
        }

        public void point_to_point_general()
        {
        
        }


        //print 
        public void print_graph()
        {
            using (AcadDatabase acad = AcadDatabase.Active())
            {
                for (int i = 0; i < start_graph.width; i++)
                {
                    for (int j = 0; j < start_graph.height; j++)
                    {
                        if (start_graph.board_0[i][j] == 1)
                        {
                            double tmp_x = start_graph.space_min_x + i * PublicValue.bigcell;
                            double tmp_y = start_graph.space_min_y + j * PublicValue.bigcell;

                            var sp = new Point3d(tmp_x, tmp_y, 0);
                            var ep = new Point3d(tmp_x + 250, tmp_y, 0);
                            var line = new Line(sp, ep);
                            acad.CurrentSpace.Add(line);
                        }
                    }
                }
            }
        }

        public void print_distance()
        {
            using (AcadDatabase acad = AcadDatabase.Active())
            {
                for (int i = Math.Max(0, start_graph.start_point.x - 50); i < Math.Min(start_graph.width, start_graph.start_point.x + 50); i++)
                {
                    for (int j = Math.Max(0, start_graph.start_point.y - 50); j < Math.Min(start_graph.height, start_graph.start_point.y + 50); j++)
                    {

                        double tmp_x = start_graph.space_min_x + i * PublicValue.bigcell;
                        double tmp_y = start_graph.space_min_y + j * PublicValue.bigcell;
                        var text = new DBText
                        {
                            TextString = start_graph.start_distance[i][j].ToString(),

                            Position = new Point3d(tmp_x + 50, tmp_y + 50, 0),
                            WidthFactor = 0.7,
                            Height = 150,
                        };
                        acad.CurrentSpace.Add(text);
                    }
                }
            }


        }

        public void print_point()
        {
            using (AcadDatabase acad = AcadDatabase.Active())
            {
                double tmp_x = start_graph.space_min_x + 402 * PublicValue.bigcell;
                double tmp_y = start_graph.space_min_y + 172 * PublicValue.bigcell;
                var circle = new Circle(new Point3d(tmp_x, tmp_y, 0), new Vector3d(0, 0, 1), 1000);

                acad.CurrentSpace.Add(circle);
            }



        }

        //tool
        public int Compare(int r1, int r2)
        {
            return room_dis[r1].CompareTo(room_dis[r2]);
        }

    }
}
