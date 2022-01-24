using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPHVAC.FanPipeAlgorithm;
using ThMEPEngineCore.CAD;


namespace ThMEPHVAC.FanPipeAlgorithm
{
    //用于将网格点上的线条 转化为 真实线条
    class ArrangePipe
    {
        //工具
        Tool tool0 = new Tool();
        
        //输出
        public List<Edge> long_edges;

        //输入
        List<List<Point3d>> real_important_points;
        List<List<GridPoint>> important_points;
        List<List<int>> board_0;
        double space_min_x;
        double space_min_y;

        //其他变量
        int width = 0,height = 0;

        //整理线条变量
        public Dictionary<GridPoint, List<int>> point_edge = new Dictionary<GridPoint, List<int>>();
        public List<List<int>> edge_point = new List<List<int>>();
        public List<GridPoint> new_node_list = new List<GridPoint>();

        public ArrangePipe(List<Edge> long_edges, List<List<Point3d>> real_important_points , List<List<GridPoint>> important_points, List<List<int>> board_0, double space_min_x, double space_min_y)
        {
            this.long_edges = long_edges;
            this.real_important_points = real_important_points;
            this.important_points = important_points;
            this.board_0 = board_0;
            this.space_min_x = space_min_x;
            this.space_min_y = space_min_y;

            width = board_0.Count;
            height = board_0[0].Count;
        }

        
        ////主要整线流程（按顺序）
        
        //将所有短边接在一起形成长边，并规范数据存储方式
        public void analysis_edge_clear()
        {
            List<List<int>> tree_x = new List<List<int>>();
            List<List<int>> tree_y = new List<List<int>>();

            initialize_board(ref tree_x, width, height, 0);
            initialize_board(ref tree_y, width, height, 0);

            List<Edge> new_long_edges = new List<Edge>();
           
            for (int i = 0; i < long_edges.Count; i++)
            {
                Edge tmp_e = long_edges[i];
                if (tmp_e.x1 == tmp_e.x2 && tmp_e.y1 == tmp_e.y2) continue;   //去掉长度为0的线段

                if (tmp_e.x1 == tmp_e.x2)
                {
                    int start = tmp_e.y1;
                    int end = tmp_e.y2;
                    if (start > end)
                    {
                        tool0.Swap(ref start, ref end);
                    }

                    for (int current = start; current <= end; current++)
                    {
                        if (current == start)
                        {
                            if (tree_x[tmp_e.x1][current] == 3)
                            {
                                tree_x[tmp_e.x1][current] = 2;
                            }
                            else if (tree_x[tmp_e.x1][current] == 0)
                            {
                                tree_x[tmp_e.x1][current] = 1;
                            }
                        }
                        else if (current == end)
                        {
                            if (tree_x[tmp_e.x1][current] == 1)
                            {
                                tree_x[tmp_e.x1][current] = 2;
                            }
                            else if (tree_x[tmp_e.x1][current] == 0)
                            {
                                tree_x[tmp_e.x1][current] = 3;
                            }
                        }
                        else
                        {
                            tree_x[tmp_e.x1][current] = 2;
                        }
                    }
                }

                else if (tmp_e.y1 == tmp_e.y2)
                {
                    int start = tmp_e.x1;
                    int end = tmp_e.x2;
                    if (start > end)
                    {
                        tool0.Swap(ref start, ref end);
                    }

                    for (int current = start; current <= end; current++)
                    {
                        if (current == start)
                        {
                            if (tree_y[current][tmp_e.y1] == 3)
                            {
                                tree_y[current][tmp_e.y1] = 2;
                            }
                            else if (tree_y[current][tmp_e.y1] == 0)
                            {
                                tree_y[current][tmp_e.y1] = 1;
                            }
                        }
                        else if (current == end)
                        {
                            if (tree_y[current][tmp_e.y1] == 1)
                            {
                                tree_y[current][tmp_e.y1] = 2;
                            }
                            else if (tree_y[current][tmp_e.y1] == 0)
                            {
                                tree_y[current][tmp_e.y1] = 3;
                            }
                        }
                        else
                        {
                            tree_y[current][tmp_e.y1] = 2;
                        }

                    }
                }
            }

            for (int i = 0; i < width; i++)
            {
                int start_y = -1, end_y = -1;
                for (int j = 0; j < height; j++)
                {
                    if (tree_x[i][j] == 1)
                    {
                        start_y = j;
                    }
                    else if (tree_x[i][j] == 3)
                    {
                        end_y = j;
                        new_long_edges.Add(new Edge(i, start_y, i, end_y));
                    }
                }
            }

            for (int i = 0; i < height; i++)
            {
                int start_x = -1, end_x = -1;
                for (int j = 0; j < width; j++)
                {
                    if (tree_y[j][i] == 1)
                    {
                        start_x = j;
                    }
                    else if (tree_y[j][i] == 3)
                    {
                        end_x = j;
                        new_long_edges.Add(new Edge(start_x, i, end_x, i));
                    }
                }
            }
  
            long_edges = new_long_edges;
        }

        //寻找关键点与边的关系
        public void find_point_edge_relation()
        {        
            for (int i = 0; i < long_edges.Count; i++)
            {
                GridPoint pt1 = new GridPoint(long_edges[i].x1, long_edges[i].y1);
                GridPoint pt2 = new GridPoint(long_edges[i].x2, long_edges[i].y2);
                if (point_edge.ContainsKey(pt1) == false)
                {
                    List<int> tmp = new List<int>();
                    tmp.Add(i);
                    point_edge.Add(pt1, tmp);
                }
                else
                {
                    point_edge[pt1].Add(i);
                }
                if (point_edge.ContainsKey(pt2) == false)
                {
                    List<int> tmp = new List<int>();
                    tmp.Add(i);
                    point_edge.Add(pt2, tmp);
                }
                else
                {
                    point_edge[pt2].Add(i);
                }
            }
            new_node_list = point_edge.Keys.ToList<GridPoint>();

            find_edge_point_relation();
        }

        public void find_edge_point_relation()
        {
            for (int i = 0; i < long_edges.Count; i++)
            {
                List<int> tmp = new List<int>();
                for (int j = 0; j < new_node_list.Count; j++)
                {
                    if (on_edge(long_edges[i], new_node_list[j]))
                    {
                        tmp.Add(j);
                    }
                }
                edge_point.Add(tmp);
            }
        }

        //合并一些比较接近的平行线（舍弃长度换取更少的弯折）
        public void merge_edge()
        {
            try
            {
                List<int> discarded_edge = new List<int>();
                List<Edge> new_edges = new List<Edge>();
                for (int i = 0; i < long_edges.Count; i++)
                {
                    if (long_edges[i].angle == 90 || discarded_edge.Contains(i)) continue;
                    for (int j = i + 1; j < long_edges.Count; j++)
                    {
                        if (long_edges[j].angle == 90 || discarded_edge.Contains(i) || discarded_edge.Contains(j)) continue;
                        if (Math.Abs(long_edges[i].rx2 - long_edges[j].rx1) < 5)
                        {
                            if (Math.Abs(long_edges[i].ry1 - long_edges[j].ry2) < 500)
                            {
                                double length1 = long_edges[i].rx2 - long_edges[i].rx1;
                                double length2 = long_edges[j].rx2 - long_edges[j].rx1;
                                if (length2 > length1 && !check_line_blocked(i, j, long_edges[i].angle))
                                {
                                    //edge[i] 要被删除了，需要将其他连接点都调整到新的边上
                                    long_edges[j].rx1 = long_edges[i].rx1;
                                    long_edges[j].x1 = long_edges[i].x1;

                                    //i是要被删除的
                                    delete_edge(i, j, long_edges[i].angle);
                                    discarded_edge.Add(i);
                                }
                                else
                                {
                                    if (check_line_blocked(j, i, long_edges[i].angle)) continue;
                                    long_edges[i].rx2 = long_edges[j].rx2;
                                    long_edges[i].x2 = long_edges[j].x2;
                                    delete_edge(j, i, long_edges[i].angle);
                                    discarded_edge.Add(j);
                                }
                            }
                        }
                        else if (Math.Abs(long_edges[i].rx1 - long_edges[j].rx2) < 5)
                        {
                            if (Math.Abs(long_edges[i].ry1 - long_edges[j].ry2) < 500)
                            {
                                double length1 = long_edges[i].rx2 - long_edges[i].rx1;
                                double length2 = long_edges[j].rx2 - long_edges[j].rx1;
                                if (length2 > length1 && !check_line_blocked(i, j, long_edges[i].angle))
                                {
                                    long_edges[j].rx2 = long_edges[i].rx2;
                                    long_edges[j].x2 = long_edges[i].x2;
                                    delete_edge(i, j, long_edges[i].angle);
                                    discarded_edge.Add(i);
                                }
                                else
                                {
                                    if (check_line_blocked(j, i, long_edges[i].angle)) continue;
                                    long_edges[i].rx1 = long_edges[j].rx1;
                                    long_edges[i].x1 = long_edges[j].x1;
                                    delete_edge(j, i, long_edges[i].angle);
                                    discarded_edge.Add(j);
                                }
                            }
                        }
                    }
                }

                for (int i = 0; i < long_edges.Count; i++)
                {
                    if (long_edges[i].angle == 0 || discarded_edge.Contains(i)) continue;
                    for (int j = i + 1; j < long_edges.Count; j++)
                    {
                        if (long_edges[j].angle == 0 || discarded_edge.Contains(i) || discarded_edge.Contains(j)) continue;
                        if (Math.Abs(long_edges[i].ry2 - long_edges[j].ry1) < 5)
                        {
                            if (Math.Abs(long_edges[i].rx1 - long_edges[j].rx2) < 500)
                            {
                                double length1 = long_edges[i].ry2 - long_edges[i].ry1;
                                double length2 = long_edges[j].ry2 - long_edges[j].ry1;
                                if (length2 > length1 && !check_line_blocked(i, j, long_edges[i].angle))
                                {

                                    long_edges[j].ry1 = long_edges[i].ry1;
                                    long_edges[j].y1 = long_edges[i].y1;
                                    delete_edge(i, j, long_edges[i].angle);
                                    discarded_edge.Add(i);
                                }
                                else
                                {
                                    if (check_line_blocked(j, i, long_edges[i].angle)) continue;
                                    long_edges[i].ry2 = long_edges[j].ry2;
                                    long_edges[i].y2 = long_edges[j].y2;
                                    delete_edge(j, i, long_edges[i].angle);
                                    discarded_edge.Add(j);
                                }
                            }
                        }
                        else if (Math.Abs(long_edges[i].ry1 - long_edges[j].ry2) < 5)
                        {
                            if (Math.Abs(long_edges[i].rx1 - long_edges[j].rx2) < 500)
                            {
                                double length1 = long_edges[i].ry2 - long_edges[i].ry1;
                                double length2 = long_edges[j].ry2 - long_edges[j].ry1;
                                if (length2 > length1 && !check_line_blocked(i, j, long_edges[i].angle))
                                {
                                    long_edges[j].ry1 = long_edges[i].ry1;
                                    long_edges[j].y1 = long_edges[i].y1;
                                    delete_edge(i, j, long_edges[i].angle);
                                    discarded_edge.Add(i);
                                }
                                else
                                {
                                    if (check_line_blocked(j, i, long_edges[i].angle)) continue;
                                    long_edges[i].ry1 = long_edges[j].ry1;
                                    long_edges[i].y1 = long_edges[j].y1;
                                    delete_edge(j, i, long_edges[i].angle);
                                    discarded_edge.Add(j);
                                }
                            }
                        }
                    }
                }

                for (int i = 0; i < long_edges.Count; i++)
                {
                    if (discarded_edge.Contains(i) == false && (long_edges[i].x1 != long_edges[i].x2 || long_edges[i].y1 != long_edges[i].y2))
                    {
                        new_edges.Add(long_edges[i]);
                    }
                }
            
                    long_edges = new_edges;
            }

            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
                Console.WriteLine("connect");
            }
        }
        
        //网格点转化为真实点
        public void long_edges_to_real()
        {
            for (int i = 0; i < long_edges.Count; i++)
            {
                if (PublicValue.Center == 0)
                {
                    long_edges[i].rx1 = space_min_x + long_edges[i].x1 * PublicValue.CELL;
                    long_edges[i].ry1 = space_min_y + long_edges[i].y1 * PublicValue.CELL;
                    long_edges[i].rx2 = space_min_x + long_edges[i].x2 * PublicValue.CELL;
                    long_edges[i].ry2 = space_min_y + long_edges[i].y2 * PublicValue.CELL;
                }
                else if (PublicValue.Center == 1)
                {
                    long_edges[i].rx1 = space_min_x + long_edges[i].x1 * PublicValue.CELL + 150;
                    long_edges[i].ry1 = space_min_y + long_edges[i].y1 * PublicValue.CELL + 150;
                    long_edges[i].rx2 = space_min_x + long_edges[i].x2 * PublicValue.CELL + 150;
                    long_edges[i].ry2 = space_min_y + long_edges[i].y2 * PublicValue.CELL + 150;
                }
            }
        }

        //调整edges端点的径向，法向坐标，使得与关键点连接，且互相之间不断开
        public void post_processing()
        {
            // find all important_point attached to the edges
            List<List<int>> points_on_edge0 = new List<List<int>>();
            List<List<int>> points_on_edge1 = new List<List<int>>();
            for (int i = 0; i < long_edges.Count; i++)
            {
                List<int> on_edge_point_index0 = new List<int>();
                List<int> on_edge_point_index1 = new List<int>();

                for (int j = 0; j < important_points[0].Count; j++)
                {
                    if (on_edge(long_edges[i], important_points[0][j]))
                    {
                        on_edge_point_index0.Add(j);
                    }
                }
                points_on_edge0.Add(on_edge_point_index0);

                for (int j = 0; j < important_points[1].Count; j++)
                {
                    if (on_edge(long_edges[i], important_points[1][j]))
                    {
                        on_edge_point_index1.Add(j);
                    }
                }
                points_on_edge1.Add(on_edge_point_index1);
            }

            //first stage，调整线段端点法向坐标
            for (int i = 0; i < long_edges.Count; i++)
            {
                if (points_on_edge0[i].Count == 0 && points_on_edge1[i].Count == 0) continue;

                int angle = long_edges[i].angle;

                List<double> waiting_list = new List<double>();
                for (int j = 0; j < points_on_edge0[i].Count; j++)
                {

                    if (angle == 0)
                    {
                        waiting_list.Add(real_important_points[0][points_on_edge0[i][j]].Y);
                    }
                    else
                    {
                        waiting_list.Add(real_important_points[0][points_on_edge0[i][j]].X);
                    }
                }

                for (int j = 0; j < points_on_edge1[i].Count; j++)
                {

                    if (angle == 0)
                    {
                        waiting_list.Add(real_important_points[1][points_on_edge1[i][j]].Y);
                    }
                    else
                    {
                        waiting_list.Add(real_important_points[1][points_on_edge1[i][j]].X);
                    }
                }

                if (waiting_list.Count > 0)
                {
                    waiting_list.Sort();
                    long_edges[i].fix_stage_one(waiting_list[0]);
                }
            }

            //second stage，调整线段端点径向坐标
            for (int i = 0; i < long_edges.Count; i++)
            {
                int angle = long_edges[i].angle;

                double coord1 = 0, coord2 = 0;
                // fix first point
                GridPoint pt1 = new GridPoint(long_edges[i].x1, long_edges[i].y1);
                bool pt1_fixed = false;
                fix_point(pt1, angle,ref coord1,ref pt1_fixed);

                // fix second point
                GridPoint pt2 = new GridPoint(long_edges[i].x2, long_edges[i].y2);
                bool pt2_fixed = false;
                fix_point(pt2, angle, ref coord2, ref pt2_fixed);

                //实施固定
                long_edges[i].fix_stage_two(coord1, coord2);
            }

            // 将部分还没连接上的关键点连接上
            if (PublicValue.Arrange_mode != 1)
            {
                List<Edge> new_edges = new List<Edge>();
                List<Line> new_line = new List<Line>();
                for (int i = 0; i < long_edges.Count; i++)
                {
                    new_line.Add(new Line(new Point3d(long_edges[i].rx1, long_edges[i].ry1, 0), new Point3d(long_edges[i].rx2, long_edges[i].ry2, 0)));
                }
                for (int i = 0; i < important_points[0].Count; i++)
                {
                    Point3d start_point = new Point3d(real_important_points[0][i].X, real_important_points[0][i].Y, 0);
                    connect_leftover_point(new_line, start_point, ref new_edges);
                }
                for (int i = 0; i < important_points[1].Count; i++)
                {
                    Point3d start_point = new Point3d(real_important_points[1][i].X, real_important_points[1][i].Y, 0);
                    connect_leftover_point(new_line, start_point, ref new_edges);
                }
            }

            //清空
            point_edge.Clear();
            edge_point.Clear();
            new_node_list.Clear();
        }



        ////流程中用到的其他函数
        public void initialize_board(ref List<List<int>> board, int width, int height, int value)
        {
            board = new List<List<int>>(width);
            for (int i = 0; i < width; i++)
            {
                List<int> tmp = new List<int>(height);
                for (int j = 0; j < height; j++)
                {
                    tmp.Add(value);
                }
                board.Add(tmp);
            }
        }

        public bool on_edge(Edge long_edge, GridPoint point)
        {
            int x1 = long_edge.x1, y1 = long_edge.y1, x2 = long_edge.x2, y2 = long_edge.y2;

            if (x1 > x2)
            {
                tool0.Swap(ref x1, ref x2);
            }

            if (y1 > y2)
            {
                tool0.Swap(ref y1, ref y2);
            }

            return point.x >= x1 && point.x <= x2 && point.y >= y1 && point.y <= y2;
        }

        public bool points_on_edge(Edge long_edge, List<GridPoint> points)
        {
            bool flag = false;
            for (int i = 0; i < points.Count; i++)
            {
                if (on_edge(long_edge, points[i]) == true)
                {
                    flag = true;
                    break;
                }
            }
            return flag;
        }
        
        //查看新的边是否经过墙体，如果经过就不做这次合并
        public bool check_line_blocked(int i, int j, int angle)
        {
            bool flag = false;
            if (angle == 0)
            {
                int fixed_axis = long_edges[j].y1;
                int start = Math.Min(long_edges[i].x1, long_edges[j].x1);
                int end = Math.Max(long_edges[i].x2, long_edges[j].x2);
                for (int n = start; n <= end; n++)
                {
                    if (board_0[n][fixed_axis] != 0)
                    {
                        flag = true;
                        break;
                    }
                }

            }
            else if (angle == 90)
            {
                int fixed_axis = long_edges[j].x1;
                int start = Math.Min(long_edges[i].y1, long_edges[j].y1);
                int end = Math.Max(long_edges[i].y2, long_edges[j].y2);
                for (int n = start; n <= end; n++)
                {
                    if (board_0[fixed_axis][n] != 0)
                    {
                        flag = true;
                        break;
                    }
                }
            }

            List<GridPoint> points = new List<GridPoint>();
            for (int n = 0; n < important_points[0].Count; n++)
            {
                points.Add(important_points[0][n]);
            }
            for (int n = 0; n < important_points[1].Count; n++)
            {
                points.Add(important_points[1][n]);
            }
            if (points_on_edge(long_edges[i], points)) flag = true;

            return flag;
        }
        
        //查看被删除的边是否连有关键点，如果连有就不做这次合并
        public bool have_important_point(int i)
        {
            bool flag = false;

            List<GridPoint> points = new List<GridPoint>();
            for (int n = 0; n < important_points[0].Count; n++)
            {
                points.Add(important_points[0][n]);
            }
            for (int n = 0; n < important_points[1].Count; n++)
            {
                points.Add(important_points[1][n]);
            }
            if (points_on_edge(long_edges[i], points)) flag = true;

            return flag;
        }

        //两条edge合并，并删除原边
        public void delete_edge(int i, int j, int angle)
        {
            if (angle == 0)
            {
                for (int m = 0; m < edge_point[i].Count; m++)
                {
                    GridPoint pt_tmp = new_node_list[edge_point[i][m]];
                    for (int n = 0; n < point_edge[pt_tmp].Count; n++)
                    {
                        int edge_index = point_edge[pt_tmp][n];
                        if (long_edges[edge_index].angle == long_edges[j].angle) continue;
                        if (pt_tmp.x == long_edges[edge_index].x1 && pt_tmp.y == long_edges[edge_index].y1)
                        {
                            long_edges[edge_index].y1 = long_edges[j].y1;
                        }
                        else
                        {
                            long_edges[edge_index].y2 = long_edges[j].y1;
                        }
                    }

                }

            }
            else
            {
                for (int m = 0; m < edge_point[i].Count; m++)
                {
                    GridPoint pt_tmp = new_node_list[edge_point[i][m]];
                    for (int n = 0; n < point_edge[pt_tmp].Count; n++)
                    {
                        int edge_index = point_edge[pt_tmp][n];
                        if (long_edges[edge_index].angle == long_edges[j].angle) continue;
                        if (pt_tmp.x == long_edges[edge_index].x1 && pt_tmp.y == long_edges[edge_index].y1)
                        {
                            long_edges[edge_index].x1 = long_edges[j].x1;
                        }
                        else
                        {
                            long_edges[edge_index].x2 = long_edges[j].x1;
                        }
                    }
                }
            }
        }


        //post_process中拆分出的函数
        public void fix_point(GridPoint pt, int angle, ref double coord, ref bool is_fixed) 
        {
            for (int j = 0; j < long_edges.Count; j++)
            {
                if (angle == long_edges[j].angle)  //如果是平行的线，则跳过
                {
                    continue;
                }
                else
                {
                    if (on_edge(long_edges[j], pt))
                    {
                        if (angle == 0)
                        {
                            coord = long_edges[j].rx1;
                        }
                        else
                        {
                            coord = long_edges[j].ry1;
                        }
                        is_fixed = true;
                        break;
                    }
                }
            }

            if (!is_fixed)  //如果该点还没有被固定，说明该点并没有和其他边相连接，说明该点不是起点就是终点;
            {
                for (int j = 0; j < important_points[0].Count; j++)
                {
                    if ((pt.x == important_points[0][j].x) && (pt.y == important_points[0][j].y))
                    {
                        if (angle == 0)
                        {
                            coord = real_important_points[0][j].X;
                        }
                        else
                        {
                            coord = real_important_points[0][j].Y;
                        }
                        is_fixed = true;
                    }
                }
            }

            if (!is_fixed)  //同理，如果该点还没有被固定，说明该点并没有和其他边相连接，说明该点不是起点就是终点;
            {
                for (int j = 0; j < important_points[1].Count; j++)
                {
                    if ((pt.x == important_points[1][j].x) && (pt.y == important_points[1][j].y))
                    {
                        if (angle == 0)
                        {
                            coord = real_important_points[1][j].X;
                        }
                        else
                        {
                            coord = real_important_points[1][j].Y;
                        }
                        is_fixed = true;
                    }
                }
            }

        }

        public void connect_leftover_point(List<Line> new_line, Point3d start_point, ref List<Edge> new_edges) 
        {
            double min = 1000000;
            Point3d end_point = new Point3d(0, 0, 0);
            Point3d closetPt = new Point3d();

            for (int j = 0; j < new_line.Count; j++)
            {

                closetPt = new_line[j].GetClosestPointTo(start_point, false);
                if (closetPt.DistanceTo(start_point) < min)
                {
                    min = closetPt.DistanceTo(start_point);
                    end_point = closetPt;
                }
            }

            if (min > PublicValue.MIN_DIS && (end_point.X != 0 || end_point.Y != 0))
            {
                new_edges.Add(new Edge(start_point.X, start_point.Y, end_point.X, end_point.Y));
            }
        }


        //标准流程
        public void standard_process() 
        {
            analysis_edge_clear();
            find_point_edge_relation();
            long_edges_to_real();
            merge_edge();            
            post_processing();
        }

        //输出结果
        public void output_edges(ref List<Edge> edges_out) 
        {
            edges_out = long_edges;
        }

        //完全标准整线流程
        static public void graph_arrange(ref Graph graph0) 
        {
            //开始整线
            List<Edge> long_edges_to_arrange = new List<Edge>();
            List<List<GridPoint>> important_points = new List<List<GridPoint>>();
            List<List<Point3d>> real_important_points = new List<List<Point3d>>();

            long_edges_to_arrange = graph0.long_edges;
            graph0.extract_important_points(ref important_points, ref real_important_points);

            ArrangePipe single_room_arrange = new ArrangePipe(long_edges_to_arrange, real_important_points, important_points, graph0.board_0, graph0.space_min_x, graph0.space_min_y);

            //走一遍标准整线流程
            single_room_arrange.standard_process();

            graph0.long_edges = single_room_arrange.long_edges;
        }

    }
}
