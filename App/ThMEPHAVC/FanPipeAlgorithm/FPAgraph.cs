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

    public class grid_point : IEquatable<grid_point>
    {
        public int x;
        public int y;
        public grid_point(int a, int b)
        {
            this.x = a;
            this.y = b;
        }

        public override int GetHashCode()
        {
            return (int)x ^ (int)y;
        }
        public bool Equals(grid_point other)
        {
            return x == other.x && y == other.y;
        }
    }


    public class graph
    {
        //网格图
        public List<List<int>> board_0 = new List<List<int>>();
        public List<List<int>> start_distance = new List<List<int>>();
        public int max_x, min_x, max_y, min_y;
        public int width, height;

        //节点属性
        //真实连接的节点
        public List<node> nodes = new List<node>();
        public Dictionary<node, int> indexmap = new Dictionary<node, int>();
        //临时使用的节点
        public List<node> nodes_tmp = new List<node>();
        public Dictionary<node, int> indexmap_tmp = new Dictionary<node, int>();

        //排水管起点，终点，障碍物 —— 图纸真实坐标
        public List<Point3d> real_end_points;
        public Point3d real_start_point;
        public List<edge> boundary;
        public List<edge> hole;

        //排水管起点，终点，障碍物 —— 区域网格坐标

        //List<Polyline> boundary;
        public List<grid_point> end_points = new List<grid_point>();
        public grid_point start_point = new grid_point(0, 0);

        //图的边界
        public double space_min_x, space_min_y, space_max_x, space_max_y;

        //整理线条变量
        public List<edge> long_edges = new List<edge>();
        public Dictionary<grid_point, List<int>> point_edge = new Dictionary<grid_point, List<int>>();
        public List<List<int>> edge_point = new List<List<int>>();
        public List<grid_point> new_node_list = new List<grid_point>(); 
        //工具包
        tool tool0 = new tool();

        public graph(List<Point3d> real_end_points, Point3d real_start_point, List<edge> boundary, List<edge> hole)
        {
            this.real_end_points = real_end_points;
            this.real_start_point = real_start_point;
            this.hole = hole;
            this.boundary = boundary;
            //List<edge> tmp_line = new List<edge>();
            //for (int i = 0; i < boundary.Count; i++)
            //{
            //    List<Line> tmp = boundary[i].ToLines();

            //    for (int j = 0; j < tmp.Count; j++)
            //    {

            //        tmp_line.Add(new edge(tmp[j].StartPoint.X, tmp[j].StartPoint.Y,tmp[j].EndPoint.X, tmp[j].EndPoint.Y));
            //    }
            //}

            discretize();

            //start_point 不放进nodes里面 
            //for (int i = 0; i < end_points.Count; i++) {
            //    int index = nodes.Count;
            //    add_node(index,end_points[i]);
            //}

            //0：能走，1：不能走
            initialize_board(ref board_0, width, height, 0);
            //fillPoly(canvas, width, height, wall, 0);

            //for (int i = 0; i < discrete_column.size(); i++)
            //{
            //    fillPoly(canvas, width, height, discrete_column[i], 1);
            //}
            //添加障碍
            mark_wall(ref board_0, width, height,ref this.boundary, 1);
            mark_wall(ref board_0, width, height, ref this.hole, 100);


            //清楚起点，终点周边障碍
            board_0[start_point.x][start_point.y] = 0;
            if (start_point.x >= 2 && start_point.y >= 2) {
                board_0[start_point.x+1][start_point.y] = 0;
                board_0[start_point.x-1][start_point.y] = 0;
                board_0[start_point.x][start_point.y+1] = 0;
                board_0[start_point.x][start_point.y-1] = 0;
                board_0[start_point.x + 2][start_point.y] = 0;
                board_0[start_point.x - 2][start_point.y] = 0;
                board_0[start_point.x][start_point.y + 2] = 0;
                board_0[start_point.x][start_point.y - 2] = 0;
            }
            for (int i = 0; i < end_points.Count; i++)
            {
                board_0[end_points[i].x][end_points[i].y] = 0;
                if (end_points[i].x > 1 && end_points[i].y > 1) 
                {
                    board_0[end_points[i].x+1][end_points[i].y] = 0;
                    board_0[end_points[i].x-1][end_points[i].y] = 0;
                    board_0[end_points[i].x][end_points[i].y+1] = 0;
                    board_0[end_points[i].x][end_points[i].y-1] = 0;
                }
            }

            

            //将起点加入nodes列表
            node tmpnode = new node(start_point.x, start_point.y);
            nodes.Add(tmpnode);
            indexmap.Add((tmpnode), 0);
            List<List<int>> distance = new List<List<int>>();
            initialize_board(ref start_distance, width, height, 0);
            copy_board(board_0, ref start_distance, width, height, -1);
            if (PublicValue.traversable == 0)
            {
                calculate_distance(board_0, ref start_distance, start_point.x, start_point.y, width, height);
            }
            else
            {
                calculate_distance_traversable(board_0, ref start_distance, start_point.x, start_point.y, width, height);
            }
            nodes[0].distance = start_distance;
        }

        public void discretize()
        {
            space_max_x = -PublicValue.MAX_LENGTH;
            space_min_x = PublicValue.MAX_LENGTH;
            space_max_y = -PublicValue.MAX_LENGTH;
            space_min_y = PublicValue.MAX_LENGTH;

            //获取min_x 和 max_x
            //待修改
            for (int i = 0; i < boundary.Count; i++)
            {
                Point3d pt = new Point3d(boundary[i].rx1, boundary[i].ry1, 0);
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


            for (int i = 0; i < hole.Count; i++)
            {
                Point3d pt = new Point3d(hole[i].rx1, hole[i].ry1, 0);
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

            //流出余量
            space_min_x = space_min_x - 1000;
            space_min_y = space_min_y - 1000;

            min_x = (int)(space_min_x) / PublicValue.CELL;
            max_x = (int)(space_max_x) / PublicValue.CELL;
            min_y = (int)(space_min_y) / PublicValue.CELL;
            max_y = (int)(space_max_y) / PublicValue.CELL;

            int start_x = (int)(real_start_point.X - space_min_x) / PublicValue.CELL;
            int start_y = (int)(real_start_point.Y - space_min_y) / PublicValue.CELL;
            start_point = new grid_point(start_x, start_y);

            for (int i = 0; i < real_end_points.Count; i++)
            {
                int end_point_x = (int)(real_end_points[i].X - space_min_x) / PublicValue.CELL;
                int end_point_y = (int)(real_end_points[i].Y - space_min_y) / PublicValue.CELL;
                end_points.Add(new grid_point(end_point_x, end_point_y));
            }

            height = max_y - min_y + 10;
            width = max_x - min_x + 10;


            //for (int i = 0; i < column.size(); i++)
            //{
            //    vector<pair<int, int>> column_i;
            //    for (int j = 0; j < column[i]->coords.size(); j++)
            //    {
            //        int wall_x = int(column[i]->coords[j].x - space_min_x) / CELL;
            //        int wall_y = int(column[i]->coords[j].y - space_min_y) / CELL;
            //        column_i.push_back(make_pair(wall_x, wall_y));
            //    }
            //    discrete_column.push_back(column_i);
            //}
        }

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

        public void copy_board(List<List<int>> source, ref List<List<int>> target, int width, int height, int scalar)
        {
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    target[i][j] = source[i][j] * scalar;
                }
            }
        }

        public void mark_wall(ref List<List<int>> board_0, int width, int height, ref List<edge> bound, int value) 
        {
            for (int i = 0; i < bound.Count; i++) {

                if (bound[i].rx2 < bound[i].rx1 ) {
                    tool0.Swap(ref bound[i].rx1, ref bound[i].rx2);
                    tool0.Swap(ref bound[i].ry1, ref bound[i].ry2);
                }


                //测试用
                //int test = 0;
                //if (bound[i].rx1 <= 1260078) 
                //{ 
                //    test = 1; 
                //} 

                int start_x = (int)(bound[i].rx1 - space_min_x) / PublicValue.CELL;
                int start_y = (int)(bound[i].ry1 - space_min_y) / PublicValue.CELL;
                int end_x = (int)(bound[i].rx2 - space_min_x) / PublicValue.CELL;
                int end_y = (int)(bound[i].ry2 - space_min_y) / PublicValue.CELL;

                double length = Math.Sqrt(Math.Pow((bound[i].ry2 - bound[i].ry1),2)+ Math.Pow((bound[i].rx2 - bound[i].rx1), 2));
                int step = PublicValue.line_step;
                bool is_vertical = false;
                if ((bound[i].rx2 - bound[i].rx1) < 50) is_vertical = true;

                if (is_vertical) {

                    if(end_y < start_y) tool0.Swap(ref end_y, ref start_y);

                    for (int j = start_y; j <= end_y; j++)
                    {
                        board_0[start_x][j] = value;
                    }
                }
                else 
                {

                    double k = (bound[i].ry2 - bound[i].ry1) / (bound[i].rx2 - bound[i].rx1);
                    double step_x = 1 * step / (Math.Sqrt(1 + Math.Pow(k, 2)));
                    double step_y = k * step / (Math.Sqrt(1 + Math.Pow(k, 2)));

                    for (int j = 0; j * step <= length; j++) {

                        int now_x = (int)(bound[i].rx1 + step_x*j - space_min_x) / PublicValue.CELL;
                        int now_y = (int)(bound[i].ry1 + step_y*j -space_min_y) / PublicValue.CELL;
                        board_0[now_x][now_y] = value;

                        if (PublicValue.extension == 1)
                        {
                            if (now_x > 1)
                            {
                                board_0[now_x - 1][now_y] = value;
                                board_0[now_x + 1][now_y] = value;
                            }
                            if (now_y > 1)
                            {
                                board_0[now_x][now_y - 1] = value;
                                board_0[now_x][now_y + 1] = value;
                            }
                        }
                    }
                }
            }
        }

        //计算distance map
        public void calculate_distance(List<List<int>> board_0, ref List<List<int>> distance, int x, int y, int width, int height)
        {

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    if (distance[i][j] != -1) {
                        distance[i][j] = 999999;
                    }
                }
            }

            List<List<int>> visited = new List<List<int>>();

            //visited = 1;
            initialize_board(ref visited, width, height, 0);
            copy_board(board_0, ref visited, width, height, 1);

            List<grid_point> waiting_list = new List<grid_point>();
            int current_distance = 0;

            waiting_list.Add(new grid_point(x, y));
            visited[x][y] = 1;
            distance[x][y] = current_distance;

            while (waiting_list.Count > 0)
            {
                current_distance += 1;
                List<grid_point> new_waiting_list = new List<grid_point>();
                for (int i = 0; i < waiting_list.Count; i++)
                {
                    int p_x = waiting_list[i].x;
                    int p_y = waiting_list[i].y;
                    if (p_x >= 1 && visited[p_x - 1][p_y] == 0)
                    {
                        distance[p_x - 1][p_y] = current_distance;
                        visited[p_x - 1][p_y] = 1;
                        new_waiting_list.Add(new grid_point(p_x - 1, p_y));
                    }
                    if (p_y >= 1 && visited[p_x][p_y - 1] == 0)
                    {
                        distance[p_x][p_y - 1] = current_distance;
                        visited[p_x][p_y - 1] = 1;
                        new_waiting_list.Add(new grid_point(p_x, p_y - 1));
                    }
                    if (p_x <= width- 2 && visited[p_x + 1][p_y] == 0)
                    {
                        distance[p_x + 1][p_y] = current_distance;
                        visited[p_x + 1][p_y] = 1;
                        new_waiting_list.Add(new grid_point(p_x + 1, p_y));
                    }
                    if (p_y <= height - 2 && visited[p_x][p_y + 1] == 0)
                    {
                        distance[p_x][p_y + 1] = current_distance;
                        visited[p_x][p_y + 1] = 1;
                        new_waiting_list.Add(new grid_point(p_x, p_y + 1));
                    }
                }

                waiting_list.Clear();
                for (int i = 0; i < new_waiting_list.Count; i++)
                {
                    waiting_list.Add(new_waiting_list[i]);
                }
            }

        }

        //真实节点列表
        public void add_node(int index, grid_point point)
        {
            node tmpnode = new node(point.x, point.y);
            nodes.Add(tmpnode);
            // nodes[index] = new Path(point);
            indexmap.Add((tmpnode), index);
            List<List<int>> distance = new List<List<int>>();
            initialize_board(ref distance, width, height, 0);
            copy_board(board_0, ref distance, width, height, -1);

            if (PublicValue.traversable == 0)
            {
                calculate_distance(board_0, ref distance, point.x, point.y, width, height);
            }
            else
            {
                calculate_distance_traversable(board_0, ref distance, point.x, point.y, width, height);
            }
            
            nodes[index].distance = distance;
        }

        //临时节点列表
        public void add_node_tmp(int index, grid_point point)
        {
            node tmpnode = new node(point.x, point.y);
            nodes_tmp.Add(tmpnode);
            // nodes[index] = new Path(point);
            indexmap_tmp.Add((tmpnode), index);
            List<List<int>> distance = new List<List<int>>();
            initialize_board(ref distance, width, height, 0);
            copy_board(board_0, ref distance, width, height, -1);
            if (PublicValue.traversable == 0)
            {
                calculate_distance(board_0, ref distance, point.x, point.y, width, height);
            }
            else
            {
                calculate_distance_traversable(board_0, ref distance, point.x, point.y, width, height);
            }
            nodes_tmp[index].distance = distance;
        }

        //void get_xy(int index, pair<int, int>& pt)
        //{
        //    pt.first = int(index / width);
        //    pt.second = index % width;
        //}

        public void get_total_distance(ref List<List<int>> total, int index1, int index2, int width, int height)
        {
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    total[i][j] = nodes[index1].distance[i][j] + nodes[index2].distance[i][j] + start_distance[i][j] + board_0[i][j] * 1000;
                }
            }
        }

        public void find_shortest_path(int sp_index, int ep_index, ref List<edge> path)
        {
            int x = nodes_tmp[sp_index].x;
            int y = nodes_tmp[sp_index].y;

            if (nodes[ep_index].distance[x][y] == 0)
            {
                return;
            }

            List<edge> min_path = new List<edge>();

            int num_edge = 10000;

            for (int i = 0; i < PublicValue.ITER; i++)
            {
                
                List<edge> current_path = new List<edge>();

                int direction = find_direction_general(ep_index,0, x, y);
                if (direction == 999) {
                    continue;
                }

                int current_x = x;
                int current_y = y;
                int current_distance = nodes[ep_index].distance[current_x][current_y];

                grid_point edge_start = new grid_point(x, y);
                grid_point edge_end = new grid_point(0, 0);

                while (current_distance > 0)
                {
                    int next_x = current_x, next_y = current_y;
                    if (direction == 2)
                    {
                        next_x -= 1;
                    }
                    else if (direction == 3)
                    {
                        next_y -= 1;
                    }
                    else if (direction == 1)
                    {
                        next_y += 1;
                    }
                    else if (direction == 0)
                    {
                        next_x += 1;
                    }

                    if (next_x < 0 || next_x >= width || next_y < 0 || next_y >= height || nodes[ep_index].distance[next_x][next_y] == -1 || nodes[ep_index].distance[next_x][next_y] >= nodes[ep_index].distance[current_x][current_y])
                    {
                        edge_end.x = current_x;
                        edge_end.y = current_y;
                        edge new_edge = new edge(edge_start.x, edge_start.y, edge_end.x, edge_end.y);

                        current_path.Add(new_edge);
                        edge_start.x = current_x;
                        edge_start.y = current_y;

                        direction = find_direction_general(ep_index,0, current_x, current_y);
                        if (direction == 999)
                        {
                            break;
                        }
                    }
                    else if (board_0[next_x][next_y] == 1) 
                    {
                        edge_end.x = current_x;
                        edge_end.y = current_y;
                        edge new_edge = new edge(edge_start.x, edge_start.y, edge_end.x, edge_end.y);

                        current_path.Add(new_edge);
                        edge_start.x = current_x;
                        edge_start.y = current_y;

                        direction = find_direction_general(ep_index, 0,current_x, current_y);

                        if (direction == 999)
                        {
                            break;
                        }

                        current_x = next_x;
                        current_y = next_y;
                        current_distance = nodes[ep_index].distance[current_x][current_y];
                    }
                    else
                    {
                        current_x = next_x;
                        current_y = next_y;
                        current_distance = nodes[ep_index].distance[current_x][current_y];
                    }
                }


                edge new_edge_end = new edge(edge_start.x, edge_start.y,current_x, current_y);
      
                current_path.Add(new_edge_end);

                if (current_distance == 0 && current_path.Count < num_edge )
                {
                    num_edge = current_path.Count;
                    min_path = current_path;
                }
            }

            for (int i = 0; i < min_path.Count; i++)
            {
                path.Add(min_path[i]);
            }

            //先别记录路径
            //nodes[sp_index].child_index.Add(ep_index);
            //nodes[sp_index].paths.Add(min_path);
            //nodes[ep_index].father_index = sp_index;    
        }

        //sp是起点，distance map是以ep为原点建立的
        public void find_shortest_path_clear(grid_point sp_index, int ep_index, ref List<edge> path)
        {
            int x = sp_index.x;
            int y = sp_index.y;

            if (nodes[ep_index].distance[x][y] == 0)
            {
                return;
            }

            List<edge> min_path = new List<edge>();

            int num_edge = 10000;

            for (int i = 0; i < PublicValue.ITER; i++)
            {

                List<edge> current_path = new List<edge>();

                int direction = find_direction_general(ep_index,0, x, y);
                if (direction == 999)
                {
                    continue;
                }

                int current_x = x;
                int current_y = y;
                int current_distance = nodes[ep_index].distance[current_x][current_y];

                grid_point edge_start = new grid_point(x, y);
                grid_point edge_end = new grid_point(0, 0);

                while (current_distance > 0)
                {
                    int next_x = current_x, next_y = current_y;
                    if (direction == 2)
                    {
                        next_x -= 1;
                    }
                    else if (direction == 3)
                    {
                        next_y -= 1;
                    }
                    else if (direction == 1)
                    {
                        next_y += 1;
                    }
                    else if (direction == 0)
                    {
                        next_x += 1;
                    }

                    if (next_x < 0 || next_x >= width || next_y < 0 || next_y >= height || nodes[ep_index].distance[next_x][next_y] == -1 || nodes[ep_index].distance[next_x][next_y] >= nodes[ep_index].distance[current_x][current_y])
                    {
                        edge_end.x = current_x;
                        edge_end.y = current_y;
                        edge new_edge = new edge(edge_start.x, edge_start.y, edge_end.x, edge_end.y);

                        current_path.Add(new_edge);
                        edge_start.x = current_x;
                        edge_start.y = current_y;

                        direction = find_direction_general(ep_index,0, current_x, current_y);
                        if (direction == 999)
                        {
                            break;
                        }
                    }
                    else if (board_0[next_x][next_y] == 1)
                    {
                        edge_end.x = current_x;
                        edge_end.y = current_y;
                        edge new_edge = new edge(edge_start.x, edge_start.y, edge_end.x, edge_end.y);

                        current_path.Add(new_edge);
                        edge_start.x = current_x;
                        edge_start.y = current_y;

                        direction = find_direction_general(ep_index,0, current_x, current_y);

                        if (direction == 999)
                        {
                            break;
                        }

                        current_x = next_x;
                        current_y = next_y;
                        current_distance = nodes[ep_index].distance[current_x][current_y];
                    }
                    else
                    {
                        current_x = next_x;
                        current_y = next_y;
                        current_distance = nodes[ep_index].distance[current_x][current_y];
                    }
                }


                edge new_edge_end = new edge(edge_start.x, edge_start.y, current_x, current_y);

                current_path.Add(new_edge_end);

                if (current_distance == 0 && current_path.Count < num_edge)
                {
                    num_edge = current_path.Count;
                    min_path = current_path;
                }
            }

            for (int i = 0; i < min_path.Count; i++)
            {
                path.Add(min_path[i]);
            }

            //先别记录路径
            //nodes[sp_index].child_index.Add(ep_index);
            //nodes[sp_index].paths.Add(min_path);
            //nodes[ep_index].father_index = sp_index;    
        }

        public void find_shortest_path_start(grid_point sp_index, int ep_index , int style ,ref grid_point room_start,ref grid_point room_out)
        {
            int sx = -1000;
            int sy = -1000;
            List<List<int>> distance_map = new List<List<int>>();
            if (style == 0)
            {
                distance_map = nodes[ep_index].distance;
            }
            else if (style == 1)
            {
                distance_map = nodes_tmp[ep_index].distance;
            }


            bool flag = false;
            int x = sp_index.x;
            int y = sp_index.y;

            grid_point tmp_start = new grid_point(0,0);

            if (distance_map[x][y] == 0)
            {
                return;
            }

            List<edge> min_path = new List<edge>();

            int num_edge = 10000;

            for (int i = 0; i < PublicValue.ITER-45 && flag == false ; i++)
            {

                List<edge> current_path = new List<edge>();

                int direction = find_direction_general(ep_index,style, x, y);
                if (direction == 999)
                {
                    continue;
                }

                int current_x = x;
                int current_y = y;
                int current_distance = distance_map[current_x][current_y];

                grid_point edge_start = new grid_point(x, y);
                grid_point edge_end = new grid_point(0, 0);
                int last =  -1;

                while (current_distance > 0)
                {

                    int next_x = current_x, next_y = current_y;
                    if (direction == 2)
                    {
                        next_x -= 1;
                        last = 2;
                    }
                    else if (direction == 3)
                    {
                        next_y -= 1;
                        last = 3;
                    }
                    else if (direction == 1)
                    {
                        next_y += 1;
                        last = 1;
                    }
                    else if (direction == 0)
                    {
                        next_x += 1;
                        last = 0;
                    }
                    else 
                    {
                        break;
                    }

                    if (next_x < 0 || next_x >= width || next_y < 0 || next_y >= height || distance_map[next_x][next_y] == -1 || distance_map[next_x][next_y] >= distance_map[current_x][current_y])
                    {
                        direction = find_direction_general(ep_index, style , current_x, current_y);
                        if (direction == 999)
                        {
                            break;
                        }
                    }
                    else if (board_0[next_x][next_y] == 1)
                    {

                        //找到出口
                        if (distance_map[current_x][current_y] - distance_map[next_x][next_y] > 45)
                        {
                            tmp_start.x = current_x;
                            tmp_start.y = current_y;
                            room_start = tmp_start;
                            room_out = new grid_point(next_x, next_y);
                            flag = true;
                            break;
                        }
                        else //出口是假的
                        {
                            int direction_old = direction;
                            int iter = 0;
                            while (direction == direction_old && iter < 999) 
                            {
                                direction = find_direction_tabu(ep_index, style, current_x, current_y,direction);
                                if (direction == 999)
                                {
                                    break;
                                }
                                iter++;
                            }
                        }                                                                                 
                    }
                    else
                    {
                        current_x = next_x;
                        current_y = next_y;
                        current_distance = distance_map[current_x][current_y];
                    }
                }
            
            }

            //避免一种非常奇怪的bug，即起点在墙里
            if (tmp_start.x == 0 && tmp_start.y == 0) 
            {
                room_start = sp_index; 
            }
        }

        public int find_direction_general(int ep_index, int style, int x, int y)
        {
            

            List<List<int>> distance_map = new List<List<int>>();
            if (style == 0)
            {
                distance_map = nodes[ep_index].distance;
            }
            else if (style == 1)
            {
                distance_map = nodes_tmp[ep_index].distance;
            }

            int max_shorten = 0;

            List<int> directions = new List<int>();
            if (x >= 1 && distance_map[x - 1][y] != -1)
            {
                int shorten = distance_map[x][y] - distance_map[x - 1][y];
                if (shorten == 1 || shorten == 51)
                {
                    if (shorten > max_shorten)
                    {
                        max_shorten = shorten;
                        directions.Clear();
                        directions.Add(2);
                    }
                    else if (shorten == max_shorten)
                    {
                        directions.Add(2);
                    }
                }
            }
            if (y >= 1 && distance_map[x][y - 1] != -1)
            {
                int shorten = distance_map[x][y] - distance_map[x][y-1];
                if (shorten == 1 || shorten == 51)
                {
                    if (shorten > max_shorten)
                    {
                        max_shorten = shorten;
                        directions.Clear();
                        directions.Add(3);
                    }
                    else if (shorten == max_shorten)
                    {
                        directions.Add(3);
                    }
                }
            }
            if (x <= width - 2 && distance_map[x + 1][y] != -1)
            {
                int shorten = distance_map[x][y] - distance_map[x+1][y];
                if (shorten == 1 || shorten == 51)
                {
                    if (shorten > max_shorten)
                    {
                        max_shorten = shorten;
                        directions.Clear();
                        directions.Add(0);
                    }
                    else if (shorten == max_shorten)
                    {
                        directions.Add(0);
                    }
                }
            }
            if (y <= height  - 2 && distance_map[x][y + 1] != -1)
            {
                int shorten = distance_map[x][y] - distance_map[x][y+1];
                if (shorten == 1 || shorten == 51)
                {
                    if (shorten > max_shorten)
                    {
                        max_shorten = shorten;
                        directions.Clear();
                        directions.Add(1);
                    }
                    else if (shorten == max_shorten)
                    {
                        directions.Add(1);
                    }
                }
            }

            if (directions.Count == 0) {

                return 999;
            }

            int ddd = 0;
            try
            {
                int rand_index = (int)DateTime.Now.Ticks % directions.Count;
                ddd = directions[rand_index];
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine("random");
            }
            return ddd;
        }

        public int find_direction_tabu(int ep_index, int style , int x, int y ,int tabu)
        {

            List<List<int>> distance_map = new List<List<int>>();
            if (style == 0)
            {
                distance_map = nodes[ep_index].distance;
            }
            else if (style == 1)
            {
                distance_map = nodes_tmp[ep_index].distance;
            }

            int max_shorten = 0;

            List<int> directions = new List<int>();
            if (x >= 1 && distance_map[x - 1][y] != -1 && tabu!=2)
            {
                int shorten = distance_map[x][y] - distance_map[x - 1][y];
                if (shorten == 1 || shorten == 51)
                {
                    if (shorten > max_shorten)
                    {
                        max_shorten = shorten;
                        directions.Clear();
                        directions.Add(2);
                    }
                    else if (shorten == max_shorten)
                    {
                        directions.Add(2);
                    }
                }
            }
            if (y >= 1 && distance_map[x][y - 1] != -1 && tabu != 3)
            {
                int shorten = distance_map[x][y] - distance_map[x][y - 1];
                if (shorten == 1 || shorten == 51)
                {
                    if (shorten > max_shorten)
                    {
                        max_shorten = shorten;
                        directions.Clear();
                        directions.Add(3);
                    }
                    else if (shorten == max_shorten)
                    {
                        directions.Add(3);
                    }
                }
            }
            if (x <= width - 2 && distance_map[x + 1][y] != -1 && tabu != 0)
            {
                int shorten = distance_map[x][y] - distance_map[x + 1][y];
                if (shorten == 1 || shorten == 51)
                {
                    if (shorten > max_shorten)
                    {
                        max_shorten = shorten;
                        directions.Clear();
                        directions.Add(0);
                    }
                    else if (shorten == max_shorten)
                    {
                        directions.Add(0);
                    }
                }
            }
            if (y <= height - 2 && distance_map[x][y + 1] != -1 && tabu != 1)
            {
                int shorten = distance_map[x][y] - distance_map[x][y + 1];
                if (shorten == 1 || shorten == 51)
                {
                    if (shorten > max_shorten)
                    {
                        max_shorten = shorten;
                        directions.Clear();
                        directions.Add(1);
                    }
                    else if (shorten == max_shorten)
                    {
                        directions.Add(1);
                    }
                }
            }

            if (directions.Count == 0)
            {

                return 999;
            }

            int ddd = 0;
            try
            {
                int rand_index = (int)DateTime.Now.Ticks % directions.Count;
                ddd = directions[rand_index];
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
                Console.WriteLine("random");
            }
            return ddd;
        }

        //从这里开始。        
        public void find_best_intersection(int index1, int index2, ref grid_point choose_point, ref List<edge> path1, ref List<edge> path2)
        {
            //int index1 = get_index(p1_x, p1_y);
            //int index2 = get_index(p2_x, p2_y);

            List<List<int>> total_distance = new List<List<int>>();
            initialize_board(ref total_distance, width, height, 0);
            get_total_distance(ref total_distance, index1, index2, width, height);

            grid_point shortest_point = new grid_point(0, 0);
            find_shortest_point(total_distance, width, height, ref shortest_point);


            //判断是否是之前已经标记过的交汇点，如果是，则借用之前的计算数据。
            int index3 = -1;
            node test_node = new node(shortest_point.x, shortest_point.y);
            if (indexmap_tmp.ContainsKey(test_node) == false)
            {
                index3 = nodes_tmp.Count;
                add_node_tmp(index3, shortest_point);
            }
            else
            {
                index3 = indexmap_tmp[test_node];
            }

            find_shortest_path(index3, index1, ref path1);
            find_shortest_path(index3, index2, ref path2);

            //记录下选中的点，传递出去
            choose_point.x = shortest_point.x;
            choose_point.y = shortest_point.y;
        }

        public void find_shortest_point(List<List<int>> board, int width, int height, ref grid_point shortest_point)
        {
            int shortest = 10000;
            int mindis = 1000000; 
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    if (board[i][j] < shortest)
                    {
                        shortest = board[i][j];
                        shortest_point.x = i;
                        shortest_point.y = j;
                        mindis = start_distance[i][j];
                    }
                    else if (board[i][j] == shortest) 
                    {
                        if (start_distance[i][j] < mindis) {
                            shortest = board[i][j];
                            shortest_point.x = i;
                            shortest_point.y = j;
                            mindis = start_distance[i][j];
                        }
                       
                    }
                }
            }
        }

        public void calculate_distance_traversable(List<List<int>> board_0, ref List<List<int>> distance, int x, int y, int width, int height)
        {
            int weight = 50;

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    if (distance[i][j] != -1)
                    {
                        distance[i][j] = 999999;
                    }
                }
            }

            List<List<int>> visited = new List<List<int>>();

            //visited = 1;
            initialize_board(ref visited, width, height, 0);
            copy_board(board_0, ref visited, width, height, 10);

            List<grid_point> waiting_list = new List<grid_point>();
            int current_distance = 0;

            waiting_list.Add(new grid_point(x, y));
            visited[x][y] = 1;
            distance[x][y] = current_distance;

            List<List<grid_point>> waiting_wall = new List<List<grid_point>>();
            for (int i = 0; i < weight; i++)
            {
                List<grid_point> tmp = new List<grid_point>();
                waiting_wall.Add(tmp);
            }

            int count_wall = 0;

            while (waiting_list.Count > 0 || count_wall > 0)
            {
                current_distance += 1;
                List<grid_point> new_waiting_list = new List<grid_point>();
                List<grid_point> new_waiting_wall = new List<grid_point>();

                for (int i = 0; i < waiting_list.Count; i++)
                {
                    int p_x = waiting_list[i].x;
                    int p_y = waiting_list[i].y;
                    if (p_x >= 1 && visited[p_x - 1][p_y] == 0)
                    {
                        distance[p_x - 1][p_y] = current_distance;
                        visited[p_x - 1][p_y] = 1;
                        new_waiting_list.Add(new grid_point(p_x - 1, p_y));
                    }
                    else if (p_x >= 1 && visited[p_x - 1][p_y] == 10)
                    {
                        distance[p_x - 1][p_y] = current_distance;
                        visited[p_x - 1][p_y] = 1;
                        new_waiting_wall.Add(new grid_point(p_x - 1, p_y));
                        count_wall++;

                    }
                    if (p_y >= 1 && visited[p_x][p_y - 1] == 0)
                    {
                        distance[p_x][p_y - 1] = current_distance;
                        visited[p_x][p_y - 1] = 1;
                        new_waiting_list.Add(new grid_point(p_x, p_y - 1));
                    }
                    else if (p_y >= 1 && visited[p_x][p_y - 1] == 10)
                    {
                        distance[p_x][p_y - 1] = current_distance;
                        visited[p_x][p_y - 1] = 1;
                        new_waiting_wall.Add(new grid_point(p_x, p_y - 1));
                        count_wall++;
                    }
                    if (p_x <= width - 2 && visited[p_x + 1][p_y] == 0)
                    {
                        distance[p_x + 1][p_y] = current_distance;
                        visited[p_x + 1][p_y] = 1;
                        new_waiting_list.Add(new grid_point(p_x + 1, p_y));
                    }
                    else if (p_x <= width - 2 && visited[p_x + 1][p_y] == 10)
                    {
                        distance[p_x + 1][p_y] = current_distance;
                        visited[p_x + 1][p_y] = 1;
                        new_waiting_wall.Add(new grid_point(p_x + 1, p_y));
                        count_wall++;
                    }
                    if (p_y <= height - 2 && visited[p_x][p_y + 1] == 0)
                    {
                        distance[p_x][p_y + 1] = current_distance;
                        visited[p_x][p_y + 1] = 1;
                        new_waiting_list.Add(new grid_point(p_x, p_y + 1));
                    }
                    else if (p_y <= height - 2 && visited[p_x][p_y + 1] == 10)
                    {
                        distance[p_x][p_y + 1] = current_distance;
                        visited[p_x][p_y + 1] = 1;
                        new_waiting_wall.Add(new grid_point(p_x, p_y + 1));
                        count_wall++;
                    }
                }

                waiting_list.Clear();
                for (int i = 0; i < new_waiting_list.Count; i++)
                {
                    waiting_list.Add(new_waiting_list[i]);
                }

                //延迟输入
                if (waiting_wall.Count != 0 && waiting_wall[0].Count != 0)
                {
                    for (int i = 0; i < waiting_wall[0].Count; i++)
                    {
                        waiting_list.Add(waiting_wall[0][i]);
                    }
                }

                waiting_wall.Add(new_waiting_wall);

                if (waiting_wall.Count != 0)
                {
                    count_wall -= waiting_wall[0].Count;
                    waiting_wall.RemoveAt(0);
                }
            }
        }

        public void calculate_distance_to_main(List<List<int>> board_0, ref List<List<int>> distance, List<List<int>> main, ref grid_point point_in_main, int x, int y)
        {
            int weight = 50;

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    if (distance[i][j] != -1)
                    {
                        distance[i][j] = 999999;
                    }
                }
            }

            List<List<int>> visited = new List<List<int>>();

            //visited = 1;
            initialize_board(ref visited, width, height, 0);
            copy_board(board_0, ref visited, width, height, 10);

            List<grid_point> waiting_list = new List<grid_point>();
            int current_distance = 0;

            waiting_list.Add(new grid_point(x, y));
            visited[x][y] = 1;
            distance[x][y] = current_distance;

            List<List<grid_point>> waiting_wall = new List<List<grid_point>>();
            for (int i = 0; i < weight; i++)
            {
                List<grid_point> tmp = new List<grid_point>();
                waiting_wall.Add(tmp);
            }

            int count_wall = 0;

            bool flag = true;
            while ((waiting_list.Count > 0 || count_wall > 0) && flag == true)
            {
                current_distance += 1;
                List<grid_point> new_waiting_list = new List<grid_point>();
                List<grid_point> new_waiting_wall = new List<grid_point>();

                for (int i = 0; i < waiting_list.Count; i++)
                {

                    int p_x = waiting_list[i].x;
                    int p_y = waiting_list[i].y;

                    //如果找到目标点，则直接退出
                    if (main[p_x][p_y] == 1)
                    {
                        point_in_main = new grid_point(p_x, p_y);
                        flag = false;
                        break;
                    }

                    if (p_x >= 1 && visited[p_x - 1][p_y] == 0)
                    {
                        distance[p_x - 1][p_y] = current_distance;
                        visited[p_x - 1][p_y] = 1;
                        new_waiting_list.Add(new grid_point(p_x - 1, p_y));
                    }
                    else if (p_x >= 1 && visited[p_x - 1][p_y] == 10)
                    {
                        distance[p_x - 1][p_y] = current_distance;
                        visited[p_x - 1][p_y] = 1;
                        new_waiting_wall.Add(new grid_point(p_x - 1, p_y));
                        count_wall++;

                    }
                    if (p_y >= 1 && visited[p_x][p_y - 1] == 0)
                    {
                        distance[p_x][p_y - 1] = current_distance;
                        visited[p_x][p_y - 1] = 1;
                        new_waiting_list.Add(new grid_point(p_x, p_y - 1));
                    }
                    else if (p_y >= 1 && visited[p_x][p_y - 1] == 10)
                    {
                        distance[p_x][p_y - 1] = current_distance;
                        visited[p_x][p_y - 1] = 1;
                        new_waiting_wall.Add(new grid_point(p_x, p_y - 1));
                        count_wall++;
                    }
                    if (p_x <= width - 2 && visited[p_x + 1][p_y] == 0)
                    {

                        distance[p_x + 1][p_y] = current_distance;
                        visited[p_x + 1][p_y] = 1;
                        new_waiting_list.Add(new grid_point(p_x + 1, p_y));
                    }
                    else if (p_x <= width - 2 && visited[p_x + 1][p_y] == 10)
                    {
                        distance[p_x + 1][p_y] = current_distance;
                        visited[p_x + 1][p_y] = 1;
                        new_waiting_wall.Add(new grid_point(p_x + 1, p_y));
                        count_wall++;
                    }
                    if (p_y <= height - 2 && visited[p_x][p_y + 1] == 0)
                    {
                        distance[p_x][p_y + 1] = current_distance;
                        visited[p_x][p_y + 1] = 1;
                        new_waiting_list.Add(new grid_point(p_x, p_y + 1));
                    }
                    else if (p_y <= height - 2 && visited[p_x][p_y + 1] == 10)
                    {
                        distance[p_x][p_y + 1] = current_distance;
                        visited[p_x][p_y + 1] = 1;
                        new_waiting_wall.Add(new grid_point(p_x, p_y + 1));
                        count_wall++;
                    }
                }

                waiting_list.Clear();
                for (int i = 0; i < new_waiting_list.Count; i++)
                {
                    waiting_list.Add(new_waiting_list[i]);
                }

                //延迟输入
                if (waiting_wall.Count != 0 && waiting_wall[0].Count != 0)
                {
                    for (int i = 0; i < waiting_wall[0].Count; i++)
                    {
                        waiting_list.Add(waiting_wall[0][i]);
                    }
                }

                waiting_wall.Add(new_waiting_wall);

                if (waiting_wall.Count != 0)
                {
                    count_wall -= waiting_wall[0].Count;
                    waiting_wall.RemoveAt(0);
                }
            }
        }









        //整线算法
        public void analysis_edge_clear()
        {
            //工具包
            tool tool0 = new tool();

            //
            List<List<int>> tree_x = new List<List<int>>();
            List<List<int>> tree_y = new List<List<int>>();

            initialize_board(ref tree_x, width, height, 0);
            initialize_board(ref tree_y, width, height, 0);

            List<edge> new_long_edges = new List<edge>();

            for (int i = 0; i < long_edges.Count; i++)
            {

                

                edge tmp_e = long_edges[i];

                if (tmp_e.x1 == tmp_e.x2 && tmp_e.y1 == tmp_e.y2) continue;

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
                        new_long_edges.Add(new edge(i, start_y, i, end_y));
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
                        new_long_edges.Add(new edge(start_x, i, end_x, i));
                    }
                }
            }

            long_edges = new_long_edges;
        }

        public void analysis_edge() 
        {
            //工具包
            tool tool0 = new tool();

            //
            List<List<int>> tree_x = new List<List<int>>();
            List<List<int>> tree_y = new List<List<int>>();

            initialize_board(ref tree_x, width, height, 0);
            initialize_board(ref tree_y, width, height, 0);

            for (int i = 0; i < nodes.Count; i++)
            {
                for (int j = 0; j < nodes[i].paths.Count; j++)
                {
                    for (int k = 0; k < nodes[i].paths[j].Count; k++)
                    {

                        edge tmp_e = nodes[i].paths[j][k];

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
                    }
                }
            }

            for (int i = 0; i < nodes.Count; i++)
            {
                for (int j = 0; j < nodes[i].paths.Count; j++)
                {
                    for (int k = 0; k < nodes[i].paths[j].Count; k++)
                    {

                        edge tmp_e = nodes[i].paths[j][k];

                        if (tmp_e.y1 == tmp_e.y2)
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
                        long_edges.Add(new edge(i, start_y, i, end_y));
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
                        long_edges.Add(new edge(start_x, i, end_x, i));
                    }
                }
            }





        }

        public bool on_edge(edge long_edge, grid_point point)
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

        public bool points_on_edge(edge long_edge, List<grid_point> points) 
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

        public void point_to_real() 
        {
            for (int i = 0; i < long_edges.Count; i++) 
            {
                long_edges[i].rx1 = space_min_x + long_edges[i].x1 * PublicValue.CELL;
                long_edges[i].ry1 = space_min_y + long_edges[i].y1 * PublicValue.CELL;
                long_edges[i].rx2 = space_min_x + long_edges[i].x2 * PublicValue.CELL;
                long_edges[i].ry2 = space_min_y + long_edges[i].y2 * PublicValue.CELL;
            }
        }

        public void find_point_edge_relation() 
        {
            for (int i = 0; i < long_edges.Count; i++) 
            {
                grid_point pt1 = new grid_point(long_edges[i].x1, long_edges[i].y1);
                grid_point pt2 = new grid_point(long_edges[i].x2, long_edges[i].y2);
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
            new_node_list = point_edge.Keys.ToList<grid_point>();

            find_edge_point_relation();
        }

        public void find_edge_point_relation()
        {
            for (int i = 0; i < long_edges.Count; i++)
            {
                List<int> tmp = new List<int>();
                for(int j = 0; j < new_node_list.Count; j++) 
                {
                    if (on_edge(long_edges[i], new_node_list[j])) 
                    {
                        tmp.Add(j);
                    }
                }
                edge_point.Add(tmp);
            }
        }

        public void connect_edge() 
        {
            try
            {
                List<int> discarded_edge = new List<int>();
                List<edge> new_edges = new List<edge>();
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

        public bool check_line_blocked(int i ,int j ,int angle)
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
            else if(angle == 90)
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

            //grid_point pt1 = new grid_point(long_edges[i].x1, long_edges[i].y1);
            //grid_point pt2 = new grid_point(long_edges[i].x2, long_edges[i].y2);

            List<grid_point> points = new List<grid_point>();
            points.Add(start_point);
            for (int n = 0; n < end_points.Count; n++) 
            {
                points.Add(end_points[n]);
            }
            if (points_on_edge(long_edges[i], points)) flag = true;

            return flag;
        }

        public bool important_point(int i) {

            bool flag = false;

            List<grid_point> points = new List<grid_point>();
            points.Add(start_point);
            for (int n = 0; n < end_points.Count; n++)
            {
                points.Add(end_points[n]);
            }
            if (points_on_edge(long_edges[i], points)) flag = true;

            return flag;
        }

        public void delete_edge(int i, int j ,int angle) 
        {
            if (angle == 0)
            {
                for (int m = 0; m < edge_point[i].Count; m++) 
                {
                    grid_point pt_tmp = new_node_list[edge_point[i][m]];
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
                    grid_point pt_tmp = new_node_list[edge_point[i][m]];
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

        public void post_processing()
        {
     
            // find all end_points attached to the edges
            List<List<int>> points_on_edge = new List<List<int>>();
            for (int i = 0; i < long_edges.Count; i++)
            {

                List<int> on_edge_point_index = new List<int>();
               
                    for (int j = 0; j < end_points.Count; j++)
                    {
                        if (on_edge(long_edges[i], end_points[j]))
                        {
                            on_edge_point_index.Add(j);
                        }
                    }
                

                if (on_edge(long_edges[i], start_point))
                {
                    on_edge_point_index.Add(-1);
                }
                points_on_edge.Add(on_edge_point_index);
            }

            // 3. first stage
            for (int i = 0; i < points_on_edge.Count; i++)
            {
                int angle = long_edges[i].angle;

                List<double> waiting_list  = new List<double>();
                for (int j = 0; j < points_on_edge[i].Count; j++)
                {
                    if (points_on_edge[i][j] != -1)
                    {
                        if (angle == 0)
                        {
                            waiting_list.Add(real_end_points[points_on_edge[i][j]].Y);
                        }
                        else
                        {
                            waiting_list.Add(real_end_points[points_on_edge[i][j]].X);
                        }
                    }
                    else
                    {
                        if (angle == 0)
                        {
                            waiting_list.Add(real_start_point.Y);
                        }
                        else
                        {
                            waiting_list.Add(real_start_point.X);
                        }
                    }
                }

                if (waiting_list.Count > 0)
                {
                    waiting_list.Sort();
                    long_edges[i].fix_stage_one(waiting_list[0]);
                }
            }

            // 4. second stage
            for (int i = 0; i < long_edges.Count; i++)
            {
                int angle = long_edges[i].angle;
                double coord1 = 0, coord2 = 0;
                // fix first point
                grid_point pt1 = new grid_point(long_edges[i].x1, long_edges[i].y1); 
                bool pt1_fixed = false;
                for (int j = 0; j < long_edges.Count; j++)
                {
                    if (angle == long_edges[j].angle)
                    {
                        continue;
                    }
                    else
                    {
                        if (on_edge(long_edges[j], pt1))
                        {
                            if (angle == 0)
                            {
                                coord1 = long_edges[j].rx1;
                            }
                            else
                            {
                                coord1 = long_edges[j].ry1;
                            }
                            pt1_fixed = true;
                            break;
                        }
                    }
                }

                if (!pt1_fixed)
                {
                    for (int j = 0; j < end_points.Count; j++)
                    {
                        if ((pt1.x == end_points[j].x) && (pt1.y == end_points[j].y))
                        {
                            if (angle == 0)
                            {
                                coord1 = real_end_points[j].X;
                            }
                            else
                            {
                                coord1 = real_end_points[j].Y;
                            }
                            pt1_fixed = true;
                        }
                    }
                }

                if (!pt1_fixed)
                {
                    if ((pt1.x == start_point.x) && (pt1.y == start_point.y))
                    {
                        if (angle == 0)
                        {
                            coord1 = real_start_point.X;
                        }
                        else
                        {
                            coord1 = real_start_point.Y;
                        }
                        pt1_fixed = true;
                    }
                }

                // fix second point
                grid_point pt2 = new grid_point(long_edges[i].x2, long_edges[i].y2);
                bool pt2_fixed = false;
                for (int j = 0; j < long_edges.Count; j++)
                {
                    if (angle  == long_edges[j].angle)
                    {
                        continue;
                    }
                    else
                    {
                        if (on_edge(long_edges[j], pt2))
                        {
                            if (angle == 0)
                            {
                                coord2 = long_edges[j].rx1;
                            }
                            else
                            {
                                coord2 = long_edges[j].ry1;
                            }
                            pt2_fixed = true;
                            break;
                        }
                    }
                }

                if (!pt2_fixed)
                {
                    for (int j = 0; j < end_points.Count; j++)
                    {
                        if ((pt2.x == end_points[j].x) && (pt2.y == end_points[j].y))
                        {
                            if (angle == 0)
                            {
                                coord2 = real_end_points[j].X;
                            }
                            else
                            {
                                coord2 = real_end_points[j].Y;
                            }
                            pt2_fixed = true;
                        }
                    }
                }

                if (!pt2_fixed)
                {
                    if ((pt2.x == start_point.x) && (pt2.y == start_point.y))
                    {
                        if (angle == 0)
                        {
                            coord2 = real_start_point.X;
                        }
                        else
                        {
                            coord2 = real_start_point.Y;
                        }
                        pt2_fixed = true;
                    }
                }

                long_edges[i].fix_stage_two(coord1, coord2);
            }


            // connect end points to edges
            if (PublicValue.arrange_mode != 1)
            {
                List<edge> new_edges = new List<edge>();
                List<Line> new_line = new List<Line>();
                for (int i = 0; i < long_edges.Count; i++)
                {
                    new_line.Add(new Line(new Point3d(long_edges[i].rx1, long_edges[i].ry1, 0), new Point3d(long_edges[i].rx2, long_edges[i].ry2, 0)));
                }

                Point3d new_pt ;
                double min = 100000;
                Point3d now_pt = new Point3d(0, 0, 0);
                Point3d closetPt = new Point3d();

                for (int i = 0; i < real_end_points.Count; i++)
                {
                    new_pt = new Point3d(real_end_points[i].X, real_end_points[i].Y, 0);
                    min = 100000;
                    now_pt = new Point3d(0,0,0);
                    closetPt = new Point3d();

                    for (int j = 0; j < new_line.Count; j++)
                    {

                        closetPt = new_line[j].GetClosestPointTo(new_pt, false);
                        if (closetPt.DistanceTo(new_pt) < min)
                        {
                            min = closetPt.DistanceTo(new_pt);
                            now_pt = closetPt;
                        }
                    }

                    if (min > PublicValue.MIN_DIS && (now_pt.X != 0 || now_pt.Y!= 0))
                    {
                        new_edges.Add(new edge(new_pt.X, new_pt.Y, now_pt.X, now_pt.Y));
                    }
                }

                new_pt = new Point3d(real_start_point.X, real_start_point.Y, 0);
                min = 100000;
                now_pt = new Point3d(0, 0, 0);
                closetPt = new Point3d();

                for (int j = 0; j < new_line.Count; j++)
                {

                    closetPt = new_line[j].GetClosestPointTo(new_pt, false);
                    if (closetPt.DistanceTo(new_pt) < min)
                    {
                        min = closetPt.DistanceTo(new_pt);
                        now_pt = closetPt;
                    }
                }

                if (min > PublicValue.MIN_DIS && (now_pt.X != 0 || now_pt.Y != 0))
                {
                    new_edges.Add(new edge(new_pt.X, new_pt.Y, now_pt.X, now_pt.Y));
                }
            

                long_edges.AddRange(new_edges);
            }
           




            //vector<Edge> new_edges;

            //for (int i = 0; i < output.size(); i++)
            //{
            //    point pt{ output[i]->coords[0].x, output[i]->coords[0].y};
            //    double min_pt_edge_distance = MAX_LENGTH;
            //    int min_pt_edge_index = -1;

            //    for (int j = 0; j < edges.size(); j++)
            //    {
            //        point edge_pt1{ edges[j].ax1, edges[j].ay1};
            //        point edge_pt2{ edges[j].ax2, edges[j].ay2};

            //        double current_pt_edge_distance = point_wall_distance(edge_pt1, edge_pt2, pt);
            //        if (current_pt_edge_distance < min_pt_edge_distance)
            //        {
            //            min_pt_edge_distance = current_pt_edge_distance;
            //            min_pt_edge_index = j;
            //        }
            //    }

            //    if (min_pt_edge_distance > THRES)
            //    {
            //        point edge_pt1{ edges[min_pt_edge_index].ax1, edges[min_pt_edge_index].ay1};
            //        point edge_pt2{ edges[min_pt_edge_index].ax2, edges[min_pt_edge_index].ay2};
            //        point intersection;
            //        get_intersection(edge_pt1, edge_pt2, pt, intersection);
            //        connect_to_space(pt, intersection, new_edges, -1);
            //    }
            //}

            //point pt{ input->coords[0].x, input->coords[0].y};
            //double min_pt_edge_distance = MAX_LENGTH;
            //int min_pt_edge_index = -1;

            //for (int j = 0; j < edges.size(); j++)
            //{
            //    point edge_pt1{ edges[j].ax1, edges[j].ay1};
            //    point edge_pt2{ edges[j].ax2, edges[j].ay2};

            //    double current_pt_edge_distance = point_wall_distance(edge_pt1, edge_pt2, pt);
            //    if (current_pt_edge_distance < min_pt_edge_distance)
            //    {
            //        min_pt_edge_distance = current_pt_edge_distance;
            //        min_pt_edge_index = j;
            //    }
            //}

            //if (min_pt_edge_distance > THRES)
            //{
            //    point edge_pt1{ edges[min_pt_edge_index].ax1, edges[min_pt_edge_index].ay1};
            //    point edge_pt2{ edges[min_pt_edge_index].ax2, edges[min_pt_edge_index].ay2};
            //    point intersection;
            //    get_intersection(edge_pt1, edge_pt2, pt, intersection);
            //    connect_to_space(pt, intersection, new_edges, -1);
            //}

            //for (int i = 0; i < new_edges.size(); i++)
            //{
            //    edges.push_back(new_edges[i]);
            //}

        }
    }
}
